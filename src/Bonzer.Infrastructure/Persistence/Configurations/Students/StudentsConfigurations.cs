using Bonzer.Infrastructure.Persistence.Configurations.Organization;

namespace Bonzer.Infrastructure.Persistence.Configurations.Students;

internal sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> b)
    {
        b.ToTable("students");
        b.Property(x => x.AdmissionNo).HasMaxLength(50);
        b.Property(x => x.FirstName).HasMaxLength(100);
        b.Property(x => x.MiddleName).HasMaxLength(100);
        b.Property(x => x.LastName).HasMaxLength(100);
        b.Property(x => x.BirthPlace).HasMaxLength(150);
        b.Property(x => x.BirthCertificateNo).HasMaxLength(50);
        b.Property(x => x.BloodGroup).HasMaxLength(10);
        b.Property(x => x.Religion).HasMaxLength(50);
        b.Property(x => x.Caste).HasMaxLength(50);
        b.Property(x => x.Category).HasMaxLength(50);
        b.Property(x => x.Nationality).HasMaxLength(50);
        b.Property(x => x.MotherTongue).HasMaxLength(50);
        b.Property(x => x.Phone).HasMaxLength(20);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.LeavingReason).HasMaxLength(250);

        b.OwnsOne(x => x.Medical, m =>
        {
            m.Property(p => p.HeightCm).HasPrecision(5, 2);
            m.Property(p => p.WeightKg).HasPrecision(5, 2);
            m.Property(p => p.EmergencyContactName).HasMaxLength(150);
            m.Property(p => p.EmergencyContactPhone).HasMaxLength(20);
        });
        b.OwnsOne(x => x.Scheme, s =>
        {
            s.Property(p => p.ApaarId).HasMaxLength(20);
            s.Property(p => p.PenNumber).HasMaxLength(20);
            s.Property(p => p.UdiseStudentCode).HasMaxLength(30);
            s.Property(p => p.RteApplicationNo).HasMaxLength(50);
            s.Property(p => p.EwsDgCategory).HasMaxLength(20);
            s.Property(p => p.BplCardNo).HasMaxLength(50);
            s.Property(p => p.ScholarshipType).HasMaxLength(100);
            s.Property(p => p.ScholarshipStatus).HasMaxLength(50);
            s.Property(p => p.ScholarshipAmount).HasPrecision(12, 2);
        });

        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => x.UserId).IsUnique().HasFilter("user_id IS NOT NULL");
        b.HasIndex(x => new { x.BranchId, x.AdmissionNo }).IsUnique().HasFilter(Sql.NotDeleted);
        b.HasIndex(x => new { x.BranchId, x.Status });
        b.HasIndex(x => new { x.BranchId, x.FirstName, x.LastName });
    }
}

internal sealed class GuardianConfiguration : IEntityTypeConfiguration<Guardian>
{
    public void Configure(EntityTypeBuilder<Guardian> b)
    {
        b.ToTable("guardians");
        b.Property(x => x.FirstName).HasMaxLength(100);
        b.Property(x => x.LastName).HasMaxLength(100);
        b.Property(x => x.Phone).HasMaxLength(20);
        b.Property(x => x.AlternatePhone).HasMaxLength(20);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.Occupation).HasMaxLength(100);
        b.Property(x => x.Qualification).HasMaxLength(100);
        b.Property(x => x.AnnualIncome).HasPrecision(14, 2);

        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => x.UserId).IsUnique().HasFilter("user_id IS NOT NULL");
        b.HasIndex(x => new { x.TenantId, x.Phone });
    }
}

internal sealed class StudentGuardianConfiguration : IEntityTypeConfiguration<StudentGuardian>
{
    public void Configure(EntityTypeBuilder<StudentGuardian> b)
    {
        b.ToTable("student_guardians");
        b.HasOne(x => x.Student).WithMany(s => s.Guardians).HasForeignKey(x => x.StudentId);
        b.HasOne(x => x.Guardian).WithMany(g => g.Students).HasForeignKey(x => x.GuardianId);
        b.HasIndex(x => new { x.StudentId, x.GuardianId }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class StudentEnrollmentConfiguration : IEntityTypeConfiguration<StudentEnrollment>
{
    public void Configure(EntityTypeBuilder<StudentEnrollment> b)
    {
        b.ToTable("student_enrollments");
        b.Property(x => x.RollNumber).HasMaxLength(20);
        b.Property(x => x.EnrollmentNo).HasMaxLength(50);
        b.Property(x => x.Remarks).HasMaxLength(500);
        b.HasOne(x => x.Student).WithMany(s => s.Enrollments).HasForeignKey(x => x.StudentId);

        // One placement per student, year and class level (tuition students may join several programs).
        b.HasIndex(x => new { x.StudentId, x.AcademicYearId, x.ClassLevelId }).IsUnique().HasFilter(Sql.NotDeleted);
        b.HasIndex(x => new { x.SectionId, x.RollNumber }).IsUnique().HasFilter("roll_number IS NOT NULL AND is_deleted = false");
        b.HasIndex(x => new { x.AcademicYearId, x.ClassLevelId, x.Status });
    }
}

internal sealed class StudentSubjectConfiguration : IEntityTypeConfiguration<StudentSubject>
{
    public void Configure(EntityTypeBuilder<StudentSubject> b)
    {
        b.ToTable("student_subjects");
        b.HasOne(x => x.Enrollment).WithMany(e => e.Subjects).HasForeignKey(x => x.EnrollmentId);
        b.HasIndex(x => new { x.EnrollmentId, x.SubjectId }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class StudentPreviousEducationConfiguration : IEntityTypeConfiguration<StudentPreviousEducation>
{
    public void Configure(EntityTypeBuilder<StudentPreviousEducation> b)
    {
        b.ToTable("student_previous_educations");
        b.Property(x => x.InstitutionName).HasMaxLength(200);
        b.Property(x => x.Board).HasMaxLength(100);
        b.Property(x => x.ClassOrProgram).HasMaxLength(100);
        b.Property(x => x.Result).HasMaxLength(50);
        b.Property(x => x.Percentage).HasPrecision(5, 2);
        b.Property(x => x.TransferCertificateNo).HasMaxLength(50);
        b.Property(x => x.ContactNumber).HasMaxLength(20);
        b.HasOne(x => x.Student).WithMany(s => s.PreviousEducations).HasForeignKey(x => x.StudentId);
    }
}

internal sealed class StudentDocumentConfiguration : IEntityTypeConfiguration<StudentDocument>
{
    public void Configure(EntityTypeBuilder<StudentDocument> b)
    {
        b.ToTable("student_documents");
        b.Property(x => x.DocumentType).HasMaxLength(50);
        b.Property(x => x.FileName).HasMaxLength(255);
        b.Property(x => x.FileUrl).HasMaxLength(1000);
        b.Property(x => x.ContentType).HasMaxLength(100);
        b.HasOne(x => x.Student).WithMany(s => s.Documents).HasForeignKey(x => x.StudentId);
    }
}
