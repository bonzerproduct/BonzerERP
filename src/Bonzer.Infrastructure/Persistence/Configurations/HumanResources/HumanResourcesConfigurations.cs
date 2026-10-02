using Bonzer.Infrastructure.Persistence.Configurations.Organization;

namespace Bonzer.Infrastructure.Persistence.Configurations.HumanResources;

internal sealed class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> b)
    {
        b.ToTable("staff");
        b.Property(x => x.EmployeeCode).HasMaxLength(30);
        b.Property(x => x.FirstName).HasMaxLength(100);
        b.Property(x => x.MiddleName).HasMaxLength(100);
        b.Property(x => x.LastName).HasMaxLength(100);
        b.Property(x => x.BloodGroup).HasMaxLength(10);
        b.Property(x => x.Nationality).HasMaxLength(50);
        b.Property(x => x.Religion).HasMaxLength(50);
        b.Property(x => x.Category).HasMaxLength(50);
        b.Property(x => x.Phone).HasMaxLength(20);
        b.Property(x => x.AlternatePhone).HasMaxLength(20);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.PersonalEmail).HasMaxLength(150);
        b.Property(x => x.PhotoUrl).HasMaxLength(1000);
        b.Property(x => x.EmergencyContactName).HasMaxLength(150);
        b.Property(x => x.EmergencyContactRelation).HasMaxLength(50);
        b.Property(x => x.EmergencyContactPhone).HasMaxLength(20);
        b.Property(x => x.Designation).HasMaxLength(100);
        b.Property(x => x.ExitReason).HasMaxLength(500);
        b.Property(x => x.HighestQualification).HasMaxLength(100);
        b.Property(x => x.Specialization).HasMaxLength(150);
        b.Property(x => x.PriorExperienceYears).HasPrecision(4, 1);
        b.Property(x => x.ProfessionalRegistrationNo).HasMaxLength(50);

        b.OwnsOne(x => x.Bank, bank =>
        {
            bank.Property(p => p.AccountHolderName).HasMaxLength(150);
            bank.Property(p => p.IfscCode).HasMaxLength(11);
            bank.Property(p => p.BankName).HasColumnName("bank_name").HasMaxLength(100);
            bank.Property(p => p.BranchName).HasMaxLength(100);
        });
        b.OwnsOne(x => x.Statutory, s =>
        {
            s.Property(p => p.Uan).HasMaxLength(12);
            s.Property(p => p.PfNumber).HasMaxLength(30);
            s.Property(p => p.EsiNumber).HasMaxLength(20);
        });

        b.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.ReportingTo).WithMany().HasForeignKey(x => x.ReportingToStaffId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => x.UserId).IsUnique().HasFilter("user_id IS NOT NULL");
        b.HasIndex(x => new { x.BranchId, x.EmployeeCode }).IsUnique().HasFilter(Sql.NotDeleted);
        b.HasIndex(x => new { x.BranchId, x.Status, x.StaffType });
        b.HasIndex(x => new { x.TenantId, x.Phone });
    }
}

internal sealed class StaffQualificationConfiguration : IEntityTypeConfiguration<StaffQualification>
{
    public void Configure(EntityTypeBuilder<StaffQualification> b)
    {
        b.ToTable("staff_qualifications");
        b.Property(x => x.Degree).HasMaxLength(100);
        b.Property(x => x.Specialization).HasMaxLength(150);
        b.Property(x => x.Institution).HasMaxLength(200);
        b.Property(x => x.BoardOrUniversity).HasMaxLength(200);
        b.Property(x => x.Score).HasMaxLength(20);
        b.HasOne(x => x.Staff).WithMany(s => s.Qualifications).HasForeignKey(x => x.StaffId);
    }
}

internal sealed class StaffExperienceConfiguration : IEntityTypeConfiguration<StaffExperience>
{
    public void Configure(EntityTypeBuilder<StaffExperience> b)
    {
        b.ToTable("staff_experiences", t => t.HasCheckConstraint("ck_staff_experiences_dates", "to_date IS NULL OR to_date >= from_date"));
        b.Property(x => x.Organization).HasMaxLength(200);
        b.Property(x => x.Designation).HasMaxLength(100);
        b.Property(x => x.Responsibilities).HasMaxLength(1000);
        b.Property(x => x.LeavingReason).HasMaxLength(250);
        b.HasOne(x => x.Staff).WithMany(s => s.Experiences).HasForeignKey(x => x.StaffId);
    }
}

internal sealed class StaffDocumentConfiguration : IEntityTypeConfiguration<StaffDocument>
{
    public void Configure(EntityTypeBuilder<StaffDocument> b)
    {
        b.ToTable("staff_documents");
        b.Property(x => x.DocumentType).HasMaxLength(50);
        b.Property(x => x.FileName).HasMaxLength(255);
        b.Property(x => x.FileUrl).HasMaxLength(1000);
        b.Property(x => x.ContentType).HasMaxLength(100);
        b.HasOne(x => x.Staff).WithMany(s => s.Documents).HasForeignKey(x => x.StaffId);
        // Expiry alerts (driving licence, police verification).
        b.HasIndex(x => x.ExpiresOn).HasFilter("expires_on IS NOT NULL AND is_deleted = false");
    }
}

internal sealed class StaffAttendanceConfiguration : IEntityTypeConfiguration<StaffAttendance>
{
    public void Configure(EntityTypeBuilder<StaffAttendance> b)
    {
        b.ToTable("staff_attendance");
        b.Property(x => x.Remark).HasMaxLength(250);
        b.HasIndex(x => new { x.StaffId, x.AttendanceDate }).IsUnique().HasFilter(Sql.NotDeleted);
        b.HasIndex(x => new { x.BranchId, x.AttendanceDate });
    }
}
