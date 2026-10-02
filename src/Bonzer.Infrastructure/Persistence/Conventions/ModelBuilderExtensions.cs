using System.Linq.Expressions;
using System.Reflection;
using Bonzer.Infrastructure.Persistence.Encryption;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Bonzer.Infrastructure.Persistence.Conventions;

/// <summary>
/// Cross-cutting model rules applied to every entity, so individual configurations only describe
/// what is specific to them (lengths, indexes). This is what replaces hundreds of repeated
/// HasColumnName / HasDefaultValueSql lines in the old context.
/// </summary>
internal static class ModelBuilderExtensions
{
    private static readonly Assembly DomainAssembly = typeof(BaseEntity).Assembly;

    public static void ApplyErpConventions(this ModelBuilder builder, BonzerDbContext context, IFieldEncryptor encryptor)
    {
        foreach (var entityType in builder.Model.GetEntityTypes().Where(e => !e.IsOwned()).ToList())
        {
            var clr = entityType.ClrType;
            var entity = builder.Entity(clr);

            // 1. Client-generated UUID v7 primary keys (no database-specific default SQL).
            if (entityType.FindPrimaryKey()?.Properties is [{ ClrType: var keyType } key] && keyType == typeof(Guid))
                entity.Property(key.Name).HasValueGenerator<UuidV7ValueGenerator>().ValueGeneratedOnAdd();

            if (clr.Assembly != DomainAssembly) continue;

            // 2. Tenant / branch foreign keys for every tenant- or branch-owned table.
            if (typeof(ITenantEntity).IsAssignableFrom(clr) && !HasForeignKey(entityType, nameof(ITenantEntity.TenantId)))
                entity.HasOne(typeof(Tenant)).WithMany().HasForeignKey(nameof(ITenantEntity.TenantId));

            if (typeof(IBranchEntity).IsAssignableFrom(clr) && !HasForeignKey(entityType, nameof(IBranchEntity.BranchId)))
                entity.HasOne(typeof(Branch)).WithMany().HasForeignKey(nameof(IBranchEntity.BranchId));

            // 3. Optimistic concurrency via PostgreSQL xmin.
            if (typeof(IHasRowVersion).IsAssignableFrom(clr))
                entity.Property(nameof(IHasRowVersion.RowVersion)).IsRowVersion();

            // 4. Tenant-defined custom fields as jsonb.
            if (typeof(IHasCustomFields).IsAssignableFrom(clr))
                entity.Property(nameof(IHasCustomFields.CustomFields)).HasColumnType("jsonb");

            // 5. Global query filter: soft delete + tenant isolation.
            var filter = BuildQueryFilter(clr, context);
            if (filter is not null)
                entity.HasQueryFilter(filter);
        }

        // 6. Encrypt [Encrypted] columns at rest, including properties of owned types (e.g. bank account number).
        var converter = new EncryptedStringConverter(encryptor);
        foreach (var property in builder.Model.GetEntityTypes()
                     .Where(e => e.ClrType.Assembly == DomainAssembly)
                     .SelectMany(e => e.GetDeclaredProperties())
                     .Where(p => p.PropertyInfo?.IsDefined(typeof(EncryptedAttribute)) == true))
        {
            property.SetValueConverter(converter);
        }

        // 7. Never cascade-delete business data (rows are soft deleted anyway). Owned types are unaffected.
        foreach (var foreignKey in builder.Model.GetEntityTypes()
                     .Where(e => e.ClrType.Assembly == DomainAssembly)
                     .SelectMany(e => e.GetForeignKeys())
                     .Where(fk => !fk.IsOwnership && fk.DeleteBehavior == DeleteBehavior.Cascade))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }

        // 8. Owned value objects (address, medical...) are always materialised, even when all columns are null.
        foreach (var navigation in builder.Model.GetEntityTypes()
                     .SelectMany(e => e.GetNavigations())
                     .Where(n => n.ForeignKey.IsOwnership && !n.IsCollection))
        {
            navigation.ForeignKey.IsRequiredDependent = true;
        }
    }

    private static bool HasForeignKey(IMutableEntityType entityType, string propertyName) =>
        entityType.FindProperty(propertyName)?.GetContainingForeignKeys().Any() == true;

    /// <summary>
    /// e => !e.IsDeleted &amp;&amp; (context.BypassTenantFilter || e.TenantId == context.CurrentTenantId)
    /// Referencing the context instance makes EF evaluate the tenant per query, not once at model build.
    /// </summary>
    private static LambdaExpression? BuildQueryFilter(Type clr, BonzerDbContext context)
    {
        var parameter = Expression.Parameter(clr, "e");
        Expression? body = null;

        if (typeof(ISoftDeletable).IsAssignableFrom(clr))
            body = Expression.Not(Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted)));

        if (typeof(ITenantEntity).IsAssignableFrom(clr))
        {
            var ctx = Expression.Constant(context);
            var bypass = Expression.Property(ctx, nameof(BonzerDbContext.BypassTenantFilter));
            var sameTenant = Expression.Equal(
                Expression.Convert(Expression.Property(parameter, nameof(ITenantEntity.TenantId)), typeof(Guid?)),
                Expression.Property(ctx, nameof(BonzerDbContext.CurrentTenantId)));
            Expression tenant = Expression.OrElse(bypass, sameTenant);
            body = body is null ? tenant : Expression.AndAlso(body, tenant);
        }

        return body is null ? null : Expression.Lambda(body, parameter);
    }
}
