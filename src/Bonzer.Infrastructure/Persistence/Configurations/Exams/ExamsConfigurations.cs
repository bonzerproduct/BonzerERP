using Bonzer.Infrastructure.Persistence.Configurations.Organization;

namespace Bonzer.Infrastructure.Persistence.Configurations.Exams;

internal sealed class GradingSchemeConfiguration : IEntityTypeConfiguration<GradingScheme>
{
    public void Configure(EntityTypeBuilder<GradingScheme> b)
    {
        b.ToTable("grading_schemes");
        b.Property(x => x.Name).HasMaxLength(100);
        b.Property(x => x.MaxGradePoint).HasPrecision(4, 2);
        b.HasIndex(x => new { x.BranchId, x.Name }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class GradeBandConfiguration : IEntityTypeConfiguration<GradeBand>
{
    public void Configure(EntityTypeBuilder<GradeBand> b)
    {
        b.ToTable("grade_bands", t => t.HasCheckConstraint("ck_grade_bands_range", "max_percent >= min_percent"));
        b.Property(x => x.Grade).HasMaxLength(10);
        b.Property(x => x.Description).HasMaxLength(100);
        b.Property(x => x.MinPercent).HasPrecision(5, 2);
        b.Property(x => x.MaxPercent).HasPrecision(5, 2);
        b.Property(x => x.GradePoint).HasPrecision(4, 2);
        b.HasOne(x => x.GradingScheme).WithMany(s => s.Bands).HasForeignKey(x => x.GradingSchemeId);
        b.HasIndex(x => new { x.GradingSchemeId, x.Grade }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class ExamTypeConfiguration : IEntityTypeConfiguration<ExamType>
{
    public void Configure(EntityTypeBuilder<ExamType> b)
    {
        b.ToTable("exam_types");
        b.Property(x => x.Name).HasMaxLength(100);
        b.Property(x => x.Code).HasMaxLength(30);
        b.Property(x => x.WeightagePercent).HasPrecision(5, 2);
        b.HasIndex(x => new { x.BranchId, x.Name }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class ExamComponentTypeConfiguration : IEntityTypeConfiguration<ExamComponentType>
{
    public void Configure(EntityTypeBuilder<ExamComponentType> b)
    {
        b.ToTable("exam_component_types");
        b.Property(x => x.Name).HasMaxLength(50);
        b.Property(x => x.Code).HasMaxLength(20);
        b.HasIndex(x => new { x.BranchId, x.Name }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> b)
    {
        b.ToTable("exams");
        b.Property(x => x.Name).HasMaxLength(150);
        b.HasIndex(x => new { x.BranchId, x.AcademicYearId });
    }
}

internal sealed class ExamSubjectConfiguration : IEntityTypeConfiguration<ExamSubject>
{
    public void Configure(EntityTypeBuilder<ExamSubject> b)
    {
        b.ToTable("exam_subjects", t => t.HasCheckConstraint("ck_exam_subjects_marks", "pass_marks <= max_marks"));
        b.Property(x => x.Room).HasMaxLength(50);
        b.Property(x => x.MaxMarks).HasPrecision(6, 2);
        b.Property(x => x.PassMarks).HasPrecision(6, 2);
        b.HasOne(x => x.Exam).WithMany(e => e.ExamSubjects).HasForeignKey(x => x.ExamId);
        b.HasIndex(x => new { x.ExamId, x.ClassLevelId, x.SectionId, x.SubjectId })
            .IsUnique().AreNullsDistinct(false).HasFilter(Sql.NotDeleted);
    }
}

internal sealed class ExamSubjectComponentConfiguration : IEntityTypeConfiguration<ExamSubjectComponent>
{
    public void Configure(EntityTypeBuilder<ExamSubjectComponent> b)
    {
        b.ToTable("exam_subject_components");
        b.Property(x => x.MaxMarks).HasPrecision(6, 2);
        b.Property(x => x.PassMarks).HasPrecision(6, 2);
        b.Property(x => x.WeightagePercent).HasPrecision(5, 2);
        b.HasOne(x => x.ExamSubject).WithMany(s => s.Components).HasForeignKey(x => x.ExamSubjectId);
        b.HasIndex(x => new { x.ExamSubjectId, x.ComponentTypeId }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class MarkConfiguration : IEntityTypeConfiguration<Mark>
{
    public void Configure(EntityTypeBuilder<Mark> b)
    {
        b.ToTable("marks");
        b.Property(x => x.MarksObtained).HasPrecision(6, 2);
        b.Property(x => x.Grade).HasMaxLength(10);
        b.Property(x => x.Remark).HasMaxLength(250);
        b.HasIndex(x => new { x.ExamSubjectComponentId, x.StudentId }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class StudentResultConfiguration : IEntityTypeConfiguration<StudentResult>
{
    public void Configure(EntityTypeBuilder<StudentResult> b)
    {
        b.ToTable("student_results");
        b.Property(x => x.TotalMarks).HasPrecision(8, 2);
        b.Property(x => x.MaxMarks).HasPrecision(8, 2);
        b.Property(x => x.Percentage).HasPrecision(5, 2);
        b.Property(x => x.GradePoint).HasPrecision(4, 2);
        b.Property(x => x.Grade).HasMaxLength(10);
        b.Property(x => x.Remarks).HasMaxLength(500);
        b.HasIndex(x => new { x.ExamId, x.StudentId }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class HallTicketConfiguration : IEntityTypeConfiguration<HallTicket>
{
    public void Configure(EntityTypeBuilder<HallTicket> b)
    {
        b.ToTable("hall_tickets");
        b.Property(x => x.TicketNumber).HasMaxLength(50);
        b.Property(x => x.SeatNumber).HasMaxLength(30);
        b.Property(x => x.ExamCentre).HasMaxLength(200);
        b.HasIndex(x => new { x.ExamId, x.StudentId }).IsUnique().HasFilter(Sql.NotDeleted);
        b.HasIndex(x => new { x.TenantId, x.TicketNumber }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}
