namespace Bonzer.Domain.Entities.Exams;

/// <summary>Computed result of a student for an exam (report card header).</summary>
public class StudentResult : TenantEntity
{
    public Guid ExamId { get; set; }
    public Guid StudentId { get; set; }
    public Guid EnrollmentId { get; set; }

    public decimal TotalMarks { get; set; }
    public decimal MaxMarks { get; set; }
    public decimal Percentage { get; set; }
    public string? Grade { get; set; }
    public decimal? GradePoint { get; set; }
    public ResultStatus Result { get; set; }
    public int? Rank { get; set; }
    public string? Remarks { get; set; }

    public DateTime GeneratedAt { get; set; }
    public bool IsPublished { get; set; }

    public Exam Exam { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public StudentEnrollment Enrollment { get; set; } = null!;
}
