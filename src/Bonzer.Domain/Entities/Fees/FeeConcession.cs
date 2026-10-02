namespace Bonzer.Domain.Entities.Fees;

/// <summary>Discount / scholarship / sibling / staff-ward / RTE concession for a student.</summary>
public class FeeConcession : BranchEntity
{
    public Guid StudentId { get; set; }
    public Guid AcademicYearId { get; set; }

    /// <summary>Null = applies to every fee head.</summary>
    public Guid? FeeHeadId { get; set; }

    public DiscountType DiscountType { get; set; }
    public decimal Value { get; set; }
    public string? ConcessionCategory { get; set; }
    public string? Reason { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public Student Student { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
    public FeeHead? FeeHead { get; set; }
}
