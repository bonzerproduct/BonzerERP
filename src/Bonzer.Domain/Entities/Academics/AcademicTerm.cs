namespace Bonzer.Domain.Entities.Academics;

/// <summary>Term / semester / trimester inside an academic year.</summary>
public class AcademicTerm : BranchEntity
{
    public Guid AcademicYearId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public AcademicYear AcademicYear { get; set; } = null!;
}
