namespace Bonzer.Domain.Entities.Students;

/// <summary>
/// The person record of a learner. Class / section / roll number live in <see cref="StudentEnrollment"/>
/// (one row per academic year) so promotion history is never overwritten. Parents live in
/// <see cref="Guardian"/> so siblings share one parent record and one parent login.
/// </summary>
public class Student : BranchEntity, IHasRowVersion, IHasCustomFields
{
    public string AdmissionNo { get; set; } = string.Empty;
    public DateOnly AdmissionDate { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public Gender Gender { get; set; } = Gender.NotSpecified;
    public DateOnly DateOfBirth { get; set; }
    public string? BirthPlace { get; set; }
    public string? BirthCertificateNo { get; set; }

    // Values come from LookupValue so each school controls the lists.
    public string? BloodGroup { get; set; }
    public string? Religion { get; set; }
    public string? Caste { get; set; }
    public string? Category { get; set; }
    public string? Nationality { get; set; }
    public string? MotherTongue { get; set; }

    /// <summary>Aadhaar (India) or other national ID. Encrypted at rest.</summary>
    [Encrypted]
    public string? NationalIdNumber { get; set; }
    public bool IsNationalIdVerified { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoUrl { get; set; }
    public string? SignatureUrl { get; set; }
    public Address Address { get; set; } = new();

    public MedicalInfo Medical { get; set; } = new();
    public GovernmentSchemeInfo Scheme { get; set; } = new();

    public StudentStatus Status { get; set; } = StudentStatus.Active;
    public DateOnly? LeavingDate { get; set; }
    public string? LeavingReason { get; set; }

    /// <summary>Optional student portal login (higher classes, college, tuition).</summary>
    public Guid? UserId { get; set; }

    public string? CustomFields { get; set; }
    public uint RowVersion { get; set; }

    public ICollection<StudentGuardian> Guardians { get; set; } = new List<StudentGuardian>();
    public ICollection<StudentEnrollment> Enrollments { get; set; } = new List<StudentEnrollment>();
    public ICollection<StudentPreviousEducation> PreviousEducations { get; set; } = new List<StudentPreviousEducation>();
    public ICollection<StudentDocument> Documents { get; set; } = new List<StudentDocument>();

    public string FullName => string.Join(' ', new[] { FirstName, MiddleName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
