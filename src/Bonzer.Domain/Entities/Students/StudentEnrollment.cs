namespace Bonzer.Domain.Entities.Students;

/// <summary>
/// A student's placement for one academic year: class level, section, roll number and outcome.
/// One student can have several enrollments in the same year for different programs (tuition).
/// </summary>
public class StudentEnrollment : BranchEntity
{
    public Guid StudentId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid ClassLevelId { get; set; }
    public Guid SectionId { get; set; }

    public string? RollNumber { get; set; }
    public string? EnrollmentNo { get; set; }

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public DateOnly EnrolledOn { get; set; }
    public DateOnly? ExitedOn { get; set; }
    public string? Remarks { get; set; }

    public Student Student { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
    public ClassLevel ClassLevel { get; set; } = null!;
    public Section Section { get; set; } = null!;
    public ICollection<StudentSubject> Subjects { get; set; } = new List<StudentSubject>();
}
