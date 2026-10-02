namespace Bonzer.Domain.Entities.Academics;

/// <summary>
/// A step inside a program: "Nursery", "Std 5", "Semester 3", "Year 2", "Foundation Batch".
/// Replaces the old "classes" table. <see cref="NextClassLevelId"/> drives year-end promotion.
/// </summary>
public class ClassLevel : BranchEntity
{
    public Guid ProgramId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    /// <summary>Ordering within the program (Nursery = 1, LKG = 2...).</summary>
    public int SortOrder { get; set; }

    public Guid? NextClassLevelId { get; set; }

    public AcademicProgram Program { get; set; } = null!;
    public ClassLevel? NextClassLevel { get; set; }
    public ICollection<Section> Sections { get; set; } = new List<Section>();
}
