namespace Bonzer.Domain.Entities.Academics;

/// <summary>Division / section / batch of a class level for one academic year (replaces "divisions").</summary>
public class Section : BranchEntity
{
    public Guid ClassLevelId { get; set; }
    public Guid AcademicYearId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    /// <summary>Maximum students; null = unlimited.</summary>
    public int? Capacity { get; set; }
    public string? Room { get; set; }
    public string? Shift { get; set; }

    public Guid? ClassTeacherId { get; set; }

    public ClassLevel ClassLevel { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
    public Staff? ClassTeacher { get; set; }
}
