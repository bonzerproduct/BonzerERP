namespace Bonzer.Domain.Entities.Exams;

/// <summary>Paper of an exam for a class level (optionally a single section), with its schedule.</summary>
public class ExamSubject : BranchEntity
{
    public Guid ExamId { get; set; }
    public Guid ClassLevelId { get; set; }

    /// <summary>Null = all sections of the class level.</summary>
    public Guid? SectionId { get; set; }
    public Guid SubjectId { get; set; }

    public DateOnly? ExamDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Room { get; set; }

    public decimal MaxMarks { get; set; }
    public decimal PassMarks { get; set; }

    public Exam Exam { get; set; } = null!;
    public ClassLevel ClassLevel { get; set; } = null!;
    public Section? Section { get; set; }
    public Subject Subject { get; set; } = null!;
    public ICollection<ExamSubjectComponent> Components { get; set; } = new List<ExamSubjectComponent>();
}

/// <summary>Split of a paper into components (Theory 80 + Practical 20).</summary>
public class ExamSubjectComponent : TenantEntity
{
    public Guid ExamSubjectId { get; set; }
    public Guid ComponentTypeId { get; set; }
    public decimal MaxMarks { get; set; }
    public decimal PassMarks { get; set; }
    public decimal? WeightagePercent { get; set; }

    public ExamSubject ExamSubject { get; set; } = null!;
    public ExamComponentType ComponentType { get; set; } = null!;
}
