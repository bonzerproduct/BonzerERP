namespace Bonzer.Domain.Entities.Academics;

/// <summary>Holiday or vacation range for a branch (replaces "school_holidays").</summary>
public class Holiday : BranchEntity
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public HolidayType Type { get; set; } = HolidayType.School;

    /// <summary>Null = whole branch; otherwise only that class level (e.g. exam leave for Std 10).</summary>
    public Guid? ClassLevelId { get; set; }

    public ClassLevel? ClassLevel { get; set; }
}
