using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Bonzer.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Writes an <see cref="AuditLog"/> row for every created / updated / deleted business entity in the
/// same transaction as the change. Encrypted columns are masked; [AuditIgnore] types are skipped.
/// </summary>
public sealed class AuditTrailInterceptor(ICurrentUser currentUser, TimeProvider clock) : SaveChangesInterceptor
{
    private static readonly HashSet<string> SkippedProperties =
    [
        nameof(IAuditableEntity.CreatedAt), nameof(IAuditableEntity.CreatedBy),
        nameof(IAuditableEntity.UpdatedAt), nameof(IAuditableEntity.UpdatedBy),
        nameof(ISoftDeletable.DeletedAt), nameof(ISoftDeletable.DeletedBy),
        nameof(IHasRowVersion.RowVersion)
    ];

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AddAuditEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditEntries(DbContext? context)
    {
        if (context is null) return;
        context.ChangeTracker.DetectChanges();

        var now = clock.GetUtcNow().UtcDateTime;
        var logs = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>().ToList())
        {
            if (entry.Entity is AuditLog || entry.State is EntityState.Detached) continue;
            if (entry.Metadata.ClrType.IsDefined(typeof(AuditIgnoreAttribute))) continue;

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();
            var changed = new List<string>();

            Collect(entry, entry.State, prefix: null, oldValues, newValues, changed);

            // Owned value objects (address, medical...) are tracked as separate entries.
            foreach (var reference in entry.References.Where(r => r.TargetEntry?.Metadata.IsOwned() == true))
                Collect(reference.TargetEntry!, entry.State == EntityState.Added ? EntityState.Added : reference.TargetEntry!.State,
                    reference.Metadata.Name + ".", oldValues, newValues, changed);

            if (entry.State == EntityState.Unchanged && changed.Count == 0) continue;
            if (entry.State == EntityState.Modified && changed.Count == 0) continue;

            var action = entry.State switch
            {
                EntityState.Added => "Create",
                EntityState.Deleted => "Delete",
                _ when entry.Entity is ISoftDeletable { IsDeleted: true } && changed.Contains(nameof(ISoftDeletable.IsDeleted)) => "Delete",
                _ => "Update"
            };

            logs.Add(new AuditLog
            {
                TenantId = (entry.Entity as ITenantEntity)?.TenantId ?? currentUser.TenantId,
                UserId = currentUser.UserId,
                UserName = currentUser.UserName,
                Action = action,
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = entry.Entity.Id.ToString(),
                OldValues = oldValues.Count == 0 ? null : JsonSerializer.Serialize(oldValues),
                NewValues = newValues.Count == 0 ? null : JsonSerializer.Serialize(newValues),
                ChangedColumns = changed.Count == 0 ? null : string.Join(',', changed),
                IpAddress = currentUser.IpAddress,
                CorrelationId = currentUser.CorrelationId,
                Timestamp = now
            });
        }

        if (logs.Count > 0)
            context.Set<AuditLog>().AddRange(logs);
    }

    private static void Collect(EntityEntry entry, EntityState state, string? prefix,
        Dictionary<string, object?> oldValues, Dictionary<string, object?> newValues, List<string> changed)
    {
        foreach (var property in entry.Properties)
        {
            var metadata = property.Metadata;
            if (metadata.IsShadowProperty() || metadata.IsPrimaryKey() || SkippedProperties.Contains(metadata.Name)) continue;

            var info = metadata.PropertyInfo;
            if (info?.IsDefined(typeof(AuditIgnoreAttribute)) == true) continue;
            var sensitive = info?.IsDefined(typeof(EncryptedAttribute)) == true;

            var name = prefix + metadata.Name;
            object? Mask(object? value) => sensitive && value is not null ? "***" : value;

            switch (state)
            {
                case EntityState.Added:
                    if (property.CurrentValue is not null) newValues[name] = Mask(property.CurrentValue);
                    break;
                case EntityState.Deleted:
                    oldValues[name] = Mask(property.OriginalValue);
                    break;
                case EntityState.Modified when property.IsModified && !Equals(property.OriginalValue, property.CurrentValue):
                    oldValues[name] = Mask(property.OriginalValue);
                    newValues[name] = Mask(property.CurrentValue);
                    changed.Add(name);
                    break;
            }
        }
    }
}
