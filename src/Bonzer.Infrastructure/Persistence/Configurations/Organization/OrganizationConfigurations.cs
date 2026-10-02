namespace Bonzer.Infrastructure.Persistence.Configurations.Organization;

// Partial unique indexes ("... WHERE is_deleted = false") let a soft-deleted code be reused.
internal static class Sql
{
    public const string NotDeleted = "is_deleted = false";
}

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("tenants");
        b.Property(x => x.Name).HasMaxLength(200);
        b.Property(x => x.Code).HasMaxLength(20);
        b.Property(x => x.LegalName).HasMaxLength(250);
        b.Property(x => x.Subdomain).HasMaxLength(63);
        b.Property(x => x.ContactEmail).HasMaxLength(150);
        b.Property(x => x.ContactPhone).HasMaxLength(20);
        b.Property(x => x.TimeZone).HasMaxLength(64);
        b.Property(x => x.Currency).HasMaxLength(3);
        b.Property(x => x.Locale).HasMaxLength(10);

        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.Subdomain).IsUnique().HasFilter("subdomain IS NOT NULL");
    }
}

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> b)
    {
        b.ToTable("branches");
        b.Property(x => x.Name).HasMaxLength(200);
        b.Property(x => x.Code).HasMaxLength(20);
        b.Property(x => x.Board).HasMaxLength(100);
        b.Property(x => x.AffiliationNumber).HasMaxLength(50);
        b.Property(x => x.UdiseCode).HasMaxLength(20);
        b.Property(x => x.MediumOfInstruction).HasMaxLength(50);
        b.Property(x => x.Phone).HasMaxLength(20);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.Website).HasMaxLength(200);
        b.Property(x => x.WeeklyOffDays).HasMaxLength(100);

        b.HasOne(x => x.Tenant).WithMany(t => t.Branches).HasForeignKey(x => x.TenantId);
        b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.ToTable("departments");
        b.Property(x => x.Name).HasMaxLength(150);
        b.Property(x => x.Code).HasMaxLength(30);
        b.HasOne<Staff>().WithMany().HasForeignKey(x => x.HeadStaffId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.BranchId, x.Name }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class LookupValueConfiguration : IEntityTypeConfiguration<LookupValue>
{
    public void Configure(EntityTypeBuilder<LookupValue> b)
    {
        b.ToTable("lookup_values");
        b.Property(x => x.Category).HasMaxLength(50);
        b.Property(x => x.Code).HasMaxLength(50);
        b.Property(x => x.Name).HasMaxLength(150);
        b.HasIndex(x => new { x.TenantId, x.Category, x.Code }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> b)
    {
        b.ToTable("app_settings");
        b.Property(x => x.Key).HasMaxLength(150);
        b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId);
        b.HasIndex(x => new { x.TenantId, x.BranchId, x.Key }).IsUnique().AreNullsDistinct(false).HasFilter(Sql.NotDeleted);
    }
}

internal sealed class DocumentSequenceConfiguration : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> b)
    {
        b.ToTable("document_sequences");
        b.Property(x => x.Prefix).HasMaxLength(30);
        b.Property(x => x.Format).HasMaxLength(100);
        b.HasOne<AcademicYear>().WithMany().HasForeignKey(x => x.AcademicYearId);
        b.HasIndex(x => new { x.TenantId, x.BranchId, x.DocumentType, x.AcademicYearId })
            .IsUnique().AreNullsDistinct(false).HasFilter(Sql.NotDeleted);
    }
}
