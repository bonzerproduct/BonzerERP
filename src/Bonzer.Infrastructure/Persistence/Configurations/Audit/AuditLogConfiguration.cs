namespace Bonzer.Infrastructure.Persistence.Configurations.Audit;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        // Append-only. No FK to tenants/users on purpose: history must outlive the rows it describes.
        b.ToTable("audit_logs");
        b.Property(x => x.UserName).HasMaxLength(256);
        b.Property(x => x.Action).HasMaxLength(30);
        b.Property(x => x.EntityName).HasMaxLength(100);
        b.Property(x => x.EntityId).HasMaxLength(64);
        b.Property(x => x.OldValues).HasColumnType("jsonb");
        b.Property(x => x.NewValues).HasColumnType("jsonb");
        b.Property(x => x.IpAddress).HasMaxLength(45);
        b.Property(x => x.CorrelationId).HasMaxLength(100);
        b.HasIndex(x => new { x.TenantId, x.EntityName, x.EntityId });
        b.HasIndex(x => x.Timestamp);
    }
}
