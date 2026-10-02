namespace Bonzer.Domain.Entities.HumanResources;

/// <summary>Degree, diploma or certification (B.Ed, M.Sc Physics, CTET, NET...).</summary>
public class StaffQualification : TenantEntity
{
    public Guid StaffId { get; set; }
    public string Degree { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public string? Institution { get; set; }
    public string? BoardOrUniversity { get; set; }
    public int? YearOfPassing { get; set; }

    /// <summary>Percentage, CGPA or grade as printed on the certificate.</summary>
    public string? Score { get; set; }
    public bool IsHighest { get; set; }

    public Staff Staff { get; set; } = null!;
}
