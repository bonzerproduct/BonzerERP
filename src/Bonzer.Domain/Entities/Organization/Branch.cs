namespace Bonzer.Domain.Entities.Organization;

/// <summary>A campus / school / college / coaching centre belonging to a tenant.</summary>
public class Branch : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public InstitutionType InstitutionType { get; set; } = InstitutionType.School;

    /// <summary>Board or affiliating body: CBSE, ICSE, IB, State Board, University name, etc.</summary>
    public string? Board { get; set; }
    public string? AffiliationNumber { get; set; }
    public string? UdiseCode { get; set; }
    public string? MediumOfInstruction { get; set; }

    public Address Address { get; set; } = new();
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }

    // Calendar rules used by attendance and timetable.
    public WeekDays WeeklyOffDays { get; set; } = WeekDays.Sunday;
    public SaturdayOffPattern SaturdayOffPattern { get; set; } = SaturdayOffPattern.None;
    public AttendanceMode AttendanceMode { get; set; } = AttendanceMode.Daily;

    public bool IsActive { get; set; } = true;

    public Tenant Tenant { get; set; } = null!;
}
