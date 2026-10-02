namespace Bonzer.Domain.Entities.HumanResources;

/// <summary>Previous employment before joining this institution.</summary>
public class StaffExperience : TenantEntity
{
    public Guid StaffId { get; set; }
    public string Organization { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public string? Responsibilities { get; set; }
    public string? LeavingReason { get; set; }

    public Staff Staff { get; set; } = null!;
}
