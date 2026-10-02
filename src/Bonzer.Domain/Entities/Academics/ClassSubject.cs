namespace Bonzer.Domain.Entities.Academics;

/// <summary>Curriculum: which subjects a class level studies in a given year (replaces "class_subject_map").</summary>
public class ClassSubject : BranchEntity
{
    public Guid ClassLevelId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid SubjectId { get; set; }

    public bool IsOptional { get; set; }

    /// <summary>Students choose one subject from each elective group, e.g. "Second Language".</summary>
    public string? ElectiveGroup { get; set; }
    public decimal? Credits { get; set; }
    public int SortOrder { get; set; }

    public ClassLevel ClassLevel { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}
