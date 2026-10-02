namespace Bonzer.Domain.Entities.Exams;

public class Mark : TenantEntity, IHasRowVersion
{
    public Guid ExamSubjectComponentId { get; set; }
    public Guid StudentId { get; set; }
    public Guid EnrollmentId { get; set; }

    /// <summary>Null when absent / exempt or when a descriptive grade is used instead.</summary>
    public decimal? MarksObtained { get; set; }
    public string? Grade { get; set; }
    public MarkStatus Status { get; set; } = MarkStatus.Present;
    public string? Remark { get; set; }

    public uint RowVersion { get; set; }

    public ExamSubjectComponent ExamSubjectComponent { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public StudentEnrollment Enrollment { get; set; } = null!;
}
