namespace Bonzer.Domain.Entities.Communication;

/// <summary>Homework / assignment for a section and subject.</summary>
public class Homework : BranchEntity
{
    public Guid AcademicYearId { get; set; }
    public Guid SectionId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid AssignedByStaffId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateOnly AssignedOn { get; set; }
    public DateOnly? DueOn { get; set; }
    public decimal? MaxMarks { get; set; }
    public bool RequiresSubmission { get; set; }

    public AcademicYear AcademicYear { get; set; } = null!;
    public Section Section { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public Staff AssignedByStaff { get; set; } = null!;
    public ICollection<HomeworkSubmission> Submissions { get; set; } = new List<HomeworkSubmission>();
}

public class HomeworkSubmission : TenantEntity
{
    public Guid HomeworkId { get; set; }
    public Guid StudentId { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? Content { get; set; }
    public string? AttachmentUrl { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
    public decimal? Marks { get; set; }
    public string? Feedback { get; set; }
    public Guid? GradedBy { get; set; }

    public Homework Homework { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
