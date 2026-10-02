namespace Bonzer.Domain.Entities.Attendance;

/// <summary>Request to change a locked attendance mark; applied only after approval.</summary>
public class AttendanceCorrectionRequest : BranchEntity
{
    public Guid AttendanceId { get; set; }
    public AttendanceStatus RequestedStatus { get; set; }
    public string Reason { get; set; } = string.Empty;

    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public Guid? RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewRemark { get; set; }

    public StudentAttendance Attendance { get; set; } = null!;
}
