namespace Bonzer.Domain.Entities.Attendance;

/// <summary>Leave application by (or for) a student or a staff member, with an approval workflow.</summary>
public class LeaveRequest : BranchEntity
{
    public ApplicantType ApplicantType { get; set; }
    public Guid? StudentId { get; set; }
    public Guid? StaffId { get; set; }

    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public bool IsHalfDay { get; set; }
    public string? LeaveType { get; set; }
    public string? Reason { get; set; }
    public string? AttachmentUrl { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public Guid? RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewRemark { get; set; }

    public Student? Student { get; set; }
    public Staff? Staff { get; set; }
}
