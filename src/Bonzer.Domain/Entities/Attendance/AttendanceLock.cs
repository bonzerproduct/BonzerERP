namespace Bonzer.Domain.Entities.Attendance;

/// <summary>
/// Freezes attendance for a date (whole branch) or a section + date.
/// Replaces "attendance_date_locks"; year-level locks are AcademicYear.IsAttendanceLocked.
/// </summary>
public class AttendanceLock : BranchEntity
{
    public AttendanceLockScope Scope { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid? SectionId { get; set; }
    public DateOnly LockDate { get; set; }

    public bool IsLocked { get; set; } = true;
    public DateTime? LockedAt { get; set; }
    public Guid? LockedBy { get; set; }
    public DateTime? UnlockedAt { get; set; }
    public Guid? UnlockedBy { get; set; }
    public string? Reason { get; set; }

    public AcademicYear AcademicYear { get; set; } = null!;
    public Section? Section { get; set; }
}
