namespace Bonzer.Domain.Entities.Exams;

/// <summary>An exam event. One exam can cover many class levels (each via its ExamSubjects).</summary>
public class Exam : BranchEntity
{
    public Guid AcademicYearId { get; set; }
    public Guid? TermId { get; set; }
    public Guid ExamTypeId { get; set; }
    public Guid? GradingSchemeId { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public ExamStatus Status { get; set; } = ExamStatus.Draft;

    public bool IsMarksLocked { get; set; }
    public DateTime? PublishedAt { get; set; }

    public AcademicYear AcademicYear { get; set; } = null!;
    public AcademicTerm? Term { get; set; }
    public ExamType ExamType { get; set; } = null!;
    public GradingScheme? GradingScheme { get; set; }
    public ICollection<ExamSubject> ExamSubjects { get; set; } = new List<ExamSubject>();
}
