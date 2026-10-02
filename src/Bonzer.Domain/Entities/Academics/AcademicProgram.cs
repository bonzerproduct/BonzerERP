namespace Bonzer.Domain.Entities.Academics;

/// <summary>
/// A course of study offered by a branch. This is what makes the ERP generic:
///   School      : "Pre-Primary" (Nursery, LKG, UKG), "Primary" (Std 1-5) ...
///   College/Univ: "B.Sc Computer Science" (Semester 1..6), "MBA" ...
///   Tuition     : "JEE 2-Year Program", "Spoken English" ...
/// </summary>
public class AcademicProgram : BranchEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public EducationLevel Level { get; set; }
    public TermStructure TermStructure { get; set; } = TermStructure.Annual;

    /// <summary>Duration in years (or months for tuition, see TermStructure.Monthly).</summary>
    public int Duration { get; set; } = 1;

    public Guid? DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;

    public Department? Department { get; set; }
    public ICollection<ClassLevel> ClassLevels { get; set; } = new List<ClassLevel>();
}
