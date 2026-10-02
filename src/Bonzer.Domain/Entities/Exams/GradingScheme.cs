namespace Bonzer.Domain.Entities.Exams;

/// <summary>
/// Configurable grading (replaces the single global "grade_master"): CBSE 9-point, university
/// 10-point CGPA, state-board percentage, or descriptive grades for pre-primary.
/// </summary>
public class GradingScheme : BranchEntity
{
    public string Name { get; set; } = string.Empty;
    public GradingType Type { get; set; } = GradingType.Percentage;
    public decimal? MaxGradePoint { get; set; }
    public bool IsDefault { get; set; }

    public ICollection<GradeBand> Bands { get; set; } = new List<GradeBand>();
}

public class GradeBand : TenantEntity
{
    public Guid GradingSchemeId { get; set; }
    public string Grade { get; set; } = string.Empty;
    public decimal MinPercent { get; set; }
    public decimal MaxPercent { get; set; }
    public decimal? GradePoint { get; set; }
    public string? Description { get; set; }
    public bool IsPass { get; set; } = true;
    public int SortOrder { get; set; }

    public GradingScheme GradingScheme { get; set; } = null!;
}
