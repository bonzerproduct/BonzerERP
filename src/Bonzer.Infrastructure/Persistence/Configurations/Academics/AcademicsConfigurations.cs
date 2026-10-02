using Bonzer.Infrastructure.Persistence.Configurations.Organization;

namespace Bonzer.Infrastructure.Persistence.Configurations.Academics;

internal sealed class AcademicYearConfiguration : IEntityTypeConfiguration<AcademicYear>
{
    public void Configure(EntityTypeBuilder<AcademicYear> b)
    {
        b.ToTable("academic_years");
        b.Property(x => x.Name).HasMaxLength(20);
        b.HasIndex(x => new { x.BranchId, x.Name }).IsUnique().HasFilter(Sql.NotDeleted);
        // Exactly one current academic year per branch.
        b.HasIndex(x => x.BranchId, "ux_academic_years_one_current").HasDatabaseName("ux_academic_years_one_current").IsUnique().HasFilter("is_current = true AND is_deleted = false");
        b.ToTable(t => t.HasCheckConstraint("ck_academic_years_dates", "end_date > start_date"));
    }
}

internal sealed class AcademicTermConfiguration : IEntityTypeConfiguration<AcademicTerm>
{
    public void Configure(EntityTypeBuilder<AcademicTerm> b)
    {
        b.ToTable("academic_terms");
        b.Property(x => x.Name).HasMaxLength(50);
        b.HasIndex(x => new { x.AcademicYearId, x.Name }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class AcademicProgramConfiguration : IEntityTypeConfiguration<AcademicProgram>
{
    public void Configure(EntityTypeBuilder<AcademicProgram> b)
    {
        b.ToTable("programs");
        b.Property(x => x.Name).HasMaxLength(150);
        b.Property(x => x.Code).HasMaxLength(30);
        b.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.BranchId, x.Code }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class ClassLevelConfiguration : IEntityTypeConfiguration<ClassLevel>
{
    public void Configure(EntityTypeBuilder<ClassLevel> b)
    {
        b.ToTable("class_levels");
        b.Property(x => x.Name).HasMaxLength(100);
        b.Property(x => x.Code).HasMaxLength(30);
        b.HasOne(x => x.Program).WithMany(p => p.ClassLevels).HasForeignKey(x => x.ProgramId);
        b.HasOne(x => x.NextClassLevel).WithMany().HasForeignKey(x => x.NextClassLevelId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.ProgramId, x.Name }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> b)
    {
        b.ToTable("sections");
        b.Property(x => x.Name).HasMaxLength(50);
        b.Property(x => x.Code).HasMaxLength(30);
        b.Property(x => x.Room).HasMaxLength(50);
        b.Property(x => x.Shift).HasMaxLength(30);
        b.HasOne(x => x.ClassLevel).WithMany(c => c.Sections).HasForeignKey(x => x.ClassLevelId);
        b.HasOne(x => x.ClassTeacher).WithMany().HasForeignKey(x => x.ClassTeacherId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.ClassLevelId, x.AcademicYearId, x.Name }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> b)
    {
        b.ToTable("subjects");
        b.Property(x => x.Name).HasMaxLength(150);
        b.Property(x => x.Code).HasMaxLength(30);
        b.Property(x => x.Credits).HasPrecision(5, 2);
        b.HasIndex(x => new { x.BranchId, x.Code }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class ClassSubjectConfiguration : IEntityTypeConfiguration<ClassSubject>
{
    public void Configure(EntityTypeBuilder<ClassSubject> b)
    {
        b.ToTable("class_subjects");
        b.Property(x => x.ElectiveGroup).HasMaxLength(50);
        b.Property(x => x.Credits).HasPrecision(5, 2);
        b.HasIndex(x => new { x.ClassLevelId, x.AcademicYearId, x.SubjectId }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class TeacherAssignmentConfiguration : IEntityTypeConfiguration<TeacherAssignment>
{
    public void Configure(EntityTypeBuilder<TeacherAssignment> b)
    {
        b.ToTable("teacher_assignments");
        b.HasIndex(x => new { x.StaffId, x.SectionId, x.SubjectId, x.AcademicYearId })
            .IsUnique().AreNullsDistinct(false).HasFilter(Sql.NotDeleted);
        b.HasIndex(x => new { x.SectionId, x.AcademicYearId });
    }
}

internal sealed class TimetableSlotConfiguration : IEntityTypeConfiguration<TimetableSlot>
{
    public void Configure(EntityTypeBuilder<TimetableSlot> b)
    {
        b.ToTable("timetable_slots");
        b.Property(x => x.Name).HasMaxLength(50);
        b.HasIndex(x => new { x.BranchId, x.Name }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class TimetableEntryConfiguration : IEntityTypeConfiguration<TimetableEntry>
{
    public void Configure(EntityTypeBuilder<TimetableEntry> b)
    {
        b.ToTable("timetable_entries");
        b.Property(x => x.Room).HasMaxLength(50);
        // A section cannot have two subjects in the same slot, and a teacher cannot be in two places at once.
        b.HasIndex(x => new { x.SectionId, x.AcademicYearId, x.DayOfWeek, x.SlotId }, "ux_timetable_section_slot").HasDatabaseName("ux_timetable_section_slot")
            .IsUnique().HasFilter(Sql.NotDeleted);
        b.HasIndex(x => new { x.StaffId, x.AcademicYearId, x.DayOfWeek, x.SlotId }, "ux_timetable_teacher_slot").HasDatabaseName("ux_timetable_teacher_slot")
            .IsUnique().HasFilter("staff_id IS NOT NULL AND is_deleted = false");
    }
}

internal sealed class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> b)
    {
        b.ToTable("holidays");
        b.Property(x => x.Name).HasMaxLength(200);
        b.HasIndex(x => new { x.BranchId, x.StartDate, x.EndDate });
        b.ToTable(t => t.HasCheckConstraint("ck_holidays_dates", "end_date >= start_date"));
    }
}
