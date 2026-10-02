namespace Bonzer.Domain.Entities.Attendance;

/// <summary>
/// One attendance mark. <see cref="PeriodNo"/> = 0 means daily attendance (schools);
/// 1..n means period / lecture-wise attendance (colleges, universities, tuition).
/// Unique per (student, date, period).
/// </summary>
public class StudentAttendance : BranchEntity
{
    public Guid StudentId { get; set; }
    public Guid EnrollmentId { get; set; }
    public Guid SectionId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public short PeriodNo { get; set; }
    public Guid? SubjectId { get; set; }

    public AttendanceStatus Status { get; set; }
    public string? LeaveType { get; set; }
    public string? Remark { get; set; }
    public AttendanceSource Source { get; set; } = AttendanceSource.Manual;

    public Student Student { get; set; } = null!;
    public StudentEnrollment Enrollment { get; set; } = null!;
    public Section Section { get; set; } = null!;
    public Subject? Subject { get; set; }
}
