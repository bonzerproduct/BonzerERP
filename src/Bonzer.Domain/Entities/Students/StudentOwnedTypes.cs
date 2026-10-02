namespace Bonzer.Domain.Entities.Students;

/// <summary>Health details, stored in the students table (columns prefixed <c>medical_</c>).</summary>
public class MedicalInfo
{
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Conditions { get; set; }
    public string? Allergies { get; set; }
    public string? Disability { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
}

/// <summary>
/// Government / compliance data (UDISE+, RTE, scholarships), stored in the students table
/// (columns prefixed <c>scheme_</c>). Kept separate so non-Indian tenants can ignore it.
/// </summary>
public class GovernmentSchemeInfo
{
    public string? ApaarId { get; set; }
    public string? PenNumber { get; set; }
    public string? UdiseStudentCode { get; set; }

    public bool IsRteSeat { get; set; }
    public string? RteApplicationNo { get; set; }
    public string? EwsDgCategory { get; set; }

    public bool IsBpl { get; set; }
    public string? BplCardNo { get; set; }
    public bool IsMinority { get; set; }
    public bool IsSingleGirlChild { get; set; }
    public bool IsFirstGenerationLearner { get; set; }

    public bool HasScholarship { get; set; }
    public string? ScholarshipType { get; set; }
    public string? ScholarshipStatus { get; set; }
    public decimal? ScholarshipAmount { get; set; }
}
