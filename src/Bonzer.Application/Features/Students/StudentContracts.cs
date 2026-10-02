namespace Bonzer.Application.Features.Students;

public sealed record StudentListItemDto(
    Guid Id,
    string AdmissionNo,
    string FullName,
    Gender Gender,
    DateOnly DateOfBirth,
    StudentStatus Status,
    string? ClassName,
    string? SectionName,
    string? RollNumber,
    string? PrimaryGuardianPhone,
    string? PhotoUrl);

public sealed record GuardianDto(Guid Id, string FullName, GuardianRelation Relation, string Phone, string? Email, bool IsPrimaryContact);

public sealed record EnrollmentDto(
    Guid Id, Guid AcademicYearId, string AcademicYear, Guid ClassLevelId, string ClassName,
    Guid SectionId, string SectionName, string? RollNumber, EnrollmentStatus Status);

public sealed record StudentDetailDto(
    Guid Id,
    Guid BranchId,
    string AdmissionNo,
    DateOnly AdmissionDate,
    string FirstName,
    string? MiddleName,
    string? LastName,
    Gender Gender,
    DateOnly DateOfBirth,
    string? BloodGroup,
    string? Religion,
    string? Category,
    string? Phone,
    string? Email,
    Address Address,
    StudentStatus Status,
    string? NationalIdMasked,
    IReadOnlyList<GuardianDto> Guardians,
    IReadOnlyList<EnrollmentDto> Enrollments);

public sealed record GuardianInput(
    GuardianRelation Relation, string FirstName, string? LastName, string Phone, string? Email,
    string? Occupation, bool IsPrimaryContact);

public sealed record CreateStudentRequest(
    Guid BranchId,
    Guid AcademicYearId,
    Guid ClassLevelId,
    Guid SectionId,
    string FirstName,
    string? MiddleName,
    string? LastName,
    Gender Gender,
    DateOnly DateOfBirth,
    DateOnly? AdmissionDate,
    string? AdmissionNo, // empty = auto-generated from the branch admission-number sequence
    string? RollNumber,
    string? BloodGroup,
    string? Religion,
    string? Category,
    string? NationalIdNumber,
    string? Phone,
    string? Email,
    Address? Address,
    IReadOnlyList<GuardianInput> Guardians);

public sealed class StudentQuery : PagedRequest
{
    public Guid BranchId { get; set; }
    public Guid? AcademicYearId { get; set; }
    public Guid? ClassLevelId { get; set; }
    public Guid? SectionId { get; set; }
    public StudentStatus? Status { get; set; }
}

public interface IStudentService
{
    Task<PagedResult<StudentListItemDto>> GetPagedAsync(StudentQuery query, CancellationToken cancellationToken = default);
    Task<StudentDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(CreateStudentRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Parent portal: children linked to the signed-in guardian.</summary>
    Task<IReadOnlyList<StudentListItemDto>> GetMyChildrenAsync(CancellationToken cancellationToken = default);
}

public sealed class CreateStudentRequestValidator : AbstractValidator<CreateStudentRequest>
{
    public CreateStudentRequestValidator(TimeProvider clock)
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.AcademicYearId).NotEmpty();
        RuleFor(x => x.ClassLevelId).NotEmpty();
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MiddleName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.AdmissionNo).MaximumLength(50);
        RuleFor(x => x.DateOfBirth)
            .LessThan(_ => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime))
            .WithMessage("Date of birth must be in the past.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.NationalIdNumber).Matches(@"^\d{12}$").When(x => !string.IsNullOrWhiteSpace(x.NationalIdNumber))
            .WithMessage("Aadhaar number must be 12 digits.");

        RuleFor(x => x.Guardians).NotEmpty().WithMessage("At least one parent / guardian is required.");
        RuleFor(x => x.Guardians).Must(g => g.Count(x => x.IsPrimaryContact) <= 1)
            .WithMessage("Only one guardian can be the primary contact.");
        RuleForEach(x => x.Guardians).ChildRules(g =>
        {
            g.RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            g.RuleFor(x => x.Phone).NotEmpty().Matches(@"^\+?[0-9]{10,15}$").WithMessage("Enter a valid phone number.");
            g.RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        });
    }
}
