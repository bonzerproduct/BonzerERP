using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bonzer.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Runs before every save:
///  - stamps CreatedAt/By and UpdatedAt/By,
///  - assigns TenantId to new rows and blocks writes into another tenant,
///  - turns deletes of ISoftDeletable rows into soft deletes.
/// Must be registered before <see cref="AuditTrailInterceptor"/>.
/// </summary>
public sealed class AuditableEntityInterceptor(ICurrentUser currentUser, TimeProvider clock) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null) return;
        context.ChangeTracker.DetectChanges();

        var now = clock.GetUtcNow().UtcDateTime;
        var userId = currentUser.UserId;
        var trusted = currentUser.IsSystem || currentUser.IsPlatformAdmin;

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is ITenantEntity tenantEntity)
                GuardTenant(entry, tenantEntity, trusted);

            if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable })
                SoftDelete(entry, now, userId);

            if (entry.Entity is not IAuditableEntity auditable) continue;

            if (entry.State == EntityState.Added)
            {
                auditable.CreatedAt = now;
                auditable.CreatedBy ??= userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                auditable.UpdatedAt = now;
                auditable.UpdatedBy = userId;
                entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
                entry.Property(nameof(IAuditableEntity.CreatedBy)).IsModified = false;
            }
        }
    }

    private void GuardTenant(EntityEntry entry, ITenantEntity entity, bool trusted)
    {
        if (entry.State == EntityState.Added && entity.TenantId == Guid.Empty)
        {
            entity.TenantId = currentUser.TenantId
                ?? throw new InvalidOperationException($"TenantId must be set explicitly for {entry.Metadata.ClrType.Name}.");
            return;
        }

        if ((entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            && !trusted && entity.TenantId != currentUser.TenantId)
        {
            throw new ForbiddenAccessException("Cross-tenant write blocked.");
        }

        if (entry.State == EntityState.Modified && entry.Property(nameof(ITenantEntity.TenantId)).IsModified)
            throw new InvalidOperationException("TenantId cannot be changed.");
    }

    private static void SoftDelete(EntityEntry entry, DateTime now, Guid? userId)
    {
        // Reset to Unchanged, then modify only the soft-delete columns.
        entry.State = EntityState.Unchanged;
        entry.Property(nameof(ISoftDeletable.IsDeleted)).CurrentValue = true;
        entry.Property(nameof(ISoftDeletable.DeletedAt)).CurrentValue = now;
        entry.Property(nameof(ISoftDeletable.DeletedBy)).CurrentValue = userId;

        // Owned value objects share the row; EF marked them Deleted together with the owner.
        foreach (var reference in entry.References)
        {
            if (reference.TargetEntry is { State: EntityState.Deleted } owned && owned.Metadata.IsOwned())
                owned.State = EntityState.Unchanged;
        }
    }
}
