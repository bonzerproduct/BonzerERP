using Bonzer.Infrastructure.Persistence.Configurations.Organization;

namespace Bonzer.Infrastructure.Persistence.Configurations.Attendance;

internal sealed class StudentAttendanceConfiguration : IEntityTypeConfiguration<StudentAttendance>
{
    public void Configure(EntityTypeBuilder<StudentAttendance> b)
    {
        // Highest-volume table (students x days x periods). Partition by month once it grows large.
        b.ToTable("student_attendance");
        b.Property(x => x.LeaveType).HasMaxLength(50);
        b.Property(x => x.Remark).HasMaxLength(250);
        b.HasIndex(x => new { x.StudentId, x.AttendanceDate, x.PeriodNo }).IsUnique().HasFilter(Sql.NotDeleted);
        b.HasIndex(x => new { x.SectionId, x.AttendanceDate });
        b.HasIndex(x => new { x.BranchId, x.AttendanceDate });
    }
}

internal sealed class AttendanceLockConfiguration : IEntityTypeConfiguration<AttendanceLock>
{
    public void Configure(EntityTypeBuilder<AttendanceLock> b)
    {
        b.ToTable("attendance_locks");
        b.Property(x => x.Reason).HasMaxLength(250);
        b.HasIndex(x => new { x.BranchId, x.Scope, x.LockDate, x.SectionId })
            .IsUnique().AreNullsDistinct(false).HasFilter(Sql.NotDeleted);
    }
}

internal sealed class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> b)
    {
        b.ToTable("leave_requests", t =>
        {
            t.HasCheckConstraint("ck_leave_requests_applicant",
                "(applicant_type = 'Student' AND student_id IS NOT NULL) OR (applicant_type = 'Staff' AND staff_id IS NOT NULL)");
            t.HasCheckConstraint("ck_leave_requests_dates", "to_date >= from_date");
        });
        b.Property(x => x.LeaveType).HasMaxLength(50);
        b.Property(x => x.ReviewRemark).HasMaxLength(500);
        b.HasIndex(x => new { x.BranchId, x.Status });
    }
}

internal sealed class AttendanceCorrectionRequestConfiguration : IEntityTypeConfiguration<AttendanceCorrectionRequest>
{
    public void Configure(EntityTypeBuilder<AttendanceCorrectionRequest> b)
    {
        b.ToTable("attendance_correction_requests");
        b.Property(x => x.Reason).HasMaxLength(2000);
        b.Property(x => x.ReviewRemark).HasMaxLength(500);
        b.HasIndex(x => new { x.BranchId, x.Status });
    }
}
