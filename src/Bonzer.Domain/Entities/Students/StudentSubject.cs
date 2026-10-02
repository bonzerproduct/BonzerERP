namespace Bonzer.Domain.Entities.Students;

/// <summary>Elective / optional subjects chosen by a student for an enrollment.</summary>
public class StudentSubject : TenantEntity
{
    public Guid EnrollmentId { get; set; }
    public Guid SubjectId { get; set; }

    public StudentEnrollment Enrollment { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}
