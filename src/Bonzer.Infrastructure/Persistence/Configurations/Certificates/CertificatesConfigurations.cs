using Bonzer.Infrastructure.Persistence.Configurations.Organization;

namespace Bonzer.Infrastructure.Persistence.Configurations.Certificates;

internal sealed class CertificateTemplateConfiguration : IEntityTypeConfiguration<CertificateTemplate>
{
    public void Configure(EntityTypeBuilder<CertificateTemplate> b)
    {
        b.ToTable("certificate_templates");
        b.Property(x => x.Name).HasMaxLength(150);
        b.HasIndex(x => new { x.BranchId, x.Type, x.Name }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class IssuedCertificateConfiguration : IEntityTypeConfiguration<IssuedCertificate>
{
    public void Configure(EntityTypeBuilder<IssuedCertificate> b)
    {
        b.ToTable("issued_certificates");
        b.Property(x => x.CertificateNo).HasMaxLength(50);
        b.Property(x => x.Purpose).HasMaxLength(250);
        b.Property(x => x.Remarks).HasMaxLength(500);
        b.Property(x => x.Data).HasColumnType("jsonb");
        b.HasOne(x => x.OriginalCertificate).WithMany().HasForeignKey(x => x.OriginalCertificateId);
        b.HasIndex(x => new { x.TenantId, x.Type, x.CertificateNo }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}
