namespace Bonzer.Domain.Entities.HumanResources;

/// <summary>
/// Employee master for every person who works for the institution: teachers, lecturers, tutors,
/// principal, office staff, accountant, librarian, drivers, conductors, guards, cleaners, nurses...
/// (<see cref="StaffType"/> separates teaching from non-teaching).
///
/// Related tables:
///   staff_qualifications - degrees / certifications (B.Ed, M.Sc, NET...)
///   staff_experiences    - previous employment history
///   staff_documents      - resume, ID proofs, police verification, driving licence...
///   staff_attendance     - daily attendance
///   teacher_assignments  - subjects / sections taught
///   users + user_roles   - optional login and roles, linked by <see cref="UserId"/>
/// Salary structures and payslips belong to the Payroll module; bank and PF/ESI details are kept here
/// because HR collects them at joining.
/// </summary>
public class Staff : BranchEntity, IHasRowVersion, IHasCustomFields
{
    public string EmployeeCode { get; set; } = string.Empty;

    // Personal
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public Gender Gender { get; set; } = Gender.NotSpecified;
    public DateOnly? DateOfBirth { get; set; }
    public string? BloodGroup { get; set; }
    public MaritalStatus? MaritalStatus { get; set; }
    public string? Nationality { get; set; }
    public string? Religion { get; set; }
    public string? Category { get; set; }

    /// <summary>Aadhaar / national ID. Encrypted at rest.</summary>
    [Encrypted]
    public string? NationalIdNumber { get; set; }

    /// <summary>PAN (income tax). Encrypted at rest.</summary>
    [Encrypted]
    public string? PanNumber { get; set; }

    // Contact
    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }

    /// <summary>Official (institution) email.</summary>
    public string? Email { get; set; }
    public string? PersonalEmail { get; set; }
    public Address Address { get; set; } = new();
    public Address PermanentAddress { get; set; } = new();
    public string? PhotoUrl { get; set; }

    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public string? EmergencyContactPhone { get; set; }

    // Employment
    public StaffType StaffType { get; set; } = StaffType.Teaching;
    public Guid? DepartmentId { get; set; }

    /// <summary>Free text backed by the "Designation" lookup (PGT, TGT, PRT, Lecturer, Driver, Clerk...).</summary>
    public string? Designation { get; set; }
    public EmploymentType EmploymentType { get; set; } = EmploymentType.Permanent;
    public DateOnly JoiningDate { get; set; }
    public DateOnly? ProbationEndDate { get; set; }
    public DateOnly? ConfirmationDate { get; set; }
    public Guid? ReportingToStaffId { get; set; }

    public StaffStatus Status { get; set; } = StaffStatus.Active;
    public DateOnly? LeavingDate { get; set; }
    public string? ExitReason { get; set; }

    // Professional summary (details in Qualifications / Experiences)
    public string? HighestQualification { get; set; }
    public string? Specialization { get; set; }

    /// <summary>Experience before joining this institution, in years.</summary>
    public decimal? PriorExperienceYears { get; set; }

    /// <summary>Teacher eligibility / council registration (CTET, TET, NET, nursing council, driving licence no...).</summary>
    public string? ProfessionalRegistrationNo { get; set; }

    // Payroll-ready details
    public BankDetails Bank { get; set; } = new();
    public StatutoryDetails Statutory { get; set; } = new();

    public Guid? UserId { get; set; }

    public string? CustomFields { get; set; }
    public uint RowVersion { get; set; }

    public Department? Department { get; set; }
    public Staff? ReportingTo { get; set; }
    public ICollection<StaffQualification> Qualifications { get; set; } = new List<StaffQualification>();
    public ICollection<StaffExperience> Experiences { get; set; } = new List<StaffExperience>();
    public ICollection<StaffDocument> Documents { get; set; } = new List<StaffDocument>();

    public bool IsCurrentEmployee => Status is not (StaffStatus.Resigned or StaffStatus.Terminated or StaffStatus.Retired);

    public string FullName => string.Join(' ', new[] { FirstName, MiddleName, LastName }.Where(s => !string.IsNullOrWhiteSpace(s)));
}
