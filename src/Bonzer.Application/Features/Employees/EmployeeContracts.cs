using System.Text.Json;
using Bonzer.Domain.Entities.HumanResources;

namespace Bonzer.Application.Features.Employees;

// ---------- Inputs ----------

/// <summary>All editable employee fields. Used as the body of create (inside CreateStaffRequest) and update.</summary>
public sealed record StaffProfile(
    string FirstName,
    string? MiddleName,
    string? LastName,
    Gender Gender,
    DateOnly? DateOfBirth,
    string? BloodGroup,
    MaritalStatus? MaritalStatus,
    string? Nationality,
    string? Religion,
    string? Category,
    string? NationalIdNumber,
    string? PanNumber,
    string? Phone,
    string? AlternatePhone,
    string? Email,
    string? PersonalEmail,
    Address? Address,
    Address? PermanentAddress,
    string? PhotoUrl,
    string? EmergencyContactName,
    string? EmergencyContactRelation,
    string? EmergencyContactPhone,
    StaffType StaffType,
    Guid? DepartmentId,
    string? Designation,
    EmploymentType EmploymentType,
    DateOnly JoiningDate,
    DateOnly? ProbationEndDate,
    DateOnly? ConfirmationDate,
    Guid? ReportingToStaffId,
    string? HighestQualification,
    string? Specialization,
    decimal? PriorExperienceYears,
    string? ProfessionalRegistrationNo,
    BankDetails? Bank,
    StatutoryDetails? Statutory,
    string? CustomFields);

public sealed record QualificationInput(
    string Degree, string? Specialization, string? Institution, string? BoardOrUniversity,
    int? YearOfPassing, string? Score, bool IsHighest);

public sealed record ExperienceInput(
    string Organization, string? Designation, DateOnly FromDate, DateOnly? ToDate,
    string? Responsibilities, string? LeavingReason);

public sealed record StaffDocumentInput(
    string DocumentType, string FileName, string FileUrl, string? ContentType, long SizeBytes, DateOnly? ExpiresOn);

/// <param name="UserName">Empty = derived from the employee code (e.g. "dps-emp-00012").</param>
/// <param name="Password">Empty = a temporary password is generated and returned once.</param>
/// <param name="AdditionalBranchIds">Extra branches this person works in (the home branch is always included).</param>
public sealed record StaffLoginRequest(
    string? UserName, string? Password, IReadOnlyList<string> Roles, IReadOnlyList<Guid>? AdditionalBranchIds);

public sealed record CreateStaffRequest(
    Guid BranchId,
    string? EmployeeCode, // empty = auto-generated from the branch "StaffCode" sequence
    StaffProfile Profile,
    IReadOnlyList<QualificationInput>? Qualifications,
    IReadOnlyList<ExperienceInput>? Experiences,
    StaffLoginRequest? Login);

public sealed record ChangeStaffStatusRequest(StaffStatus Status, DateOnly? EffectiveDate, string? Reason);

public sealed record SetStaffRolesRequest(IReadOnlyList<string> Roles);

public sealed class StaffQuery : PagedRequest
{
    public Guid BranchId { get; set; }
    public Guid? DepartmentId { get; set; }
    public StaffType? StaffType { get; set; }
    public StaffStatus? Status { get; set; }
    public EmploymentType? EmploymentType { get; set; }

    /// <summary>true = only current employees (default), false = only ex-employees, null = all.</summary>
    public bool? Current { get; set; } = true;
}

// ---------- Outputs ----------

public sealed record StaffListItemDto(
    Guid Id,
    string EmployeeCode,
    string FullName,
    string? Designation,
    string? DepartmentName,
    StaffType StaffType,
    EmploymentType EmploymentType,
    StaffStatus Status,
    string? Phone,
    string? Email,
    DateOnly JoiningDate,
    string? PhotoUrl,
    bool HasLogin);

public sealed record QualificationDto(
    Guid Id, string Degree, string? Specialization, string? Institution, string? BoardOrUniversity,
    int? YearOfPassing, string? Score, bool IsHighest);

public sealed record ExperienceDto(
    Guid Id, string Organization, string? Designation, DateOnly FromDate, DateOnly? ToDate,
    string? Responsibilities, string? LeavingReason);

public sealed record StaffDocumentDto(
    Guid Id, string DocumentType, string FileName, string FileUrl, string? ContentType, long SizeBytes,
    DateOnly? ExpiresOn, bool IsVerified, DateTime? VerifiedAt);

public sealed record BankDetailsDto(string? AccountHolderName, string? AccountNumber, string? IfscCode, string? BankName, string? BranchName);

public sealed record StaffDetailDto(
    Guid Id,
    Guid BranchId,
    string EmployeeCode,
    string FullName,
    StaffProfile Profile,
    BankDetailsDto Bank,
    StaffStatus Status,
    DateOnly? LeavingDate,
    string? ExitReason,
    string? DepartmentName,
    string? ReportingToName,
    IReadOnlyList<QualificationDto> Qualifications,
    IReadOnlyList<ExperienceDto> Experiences,
    IReadOnlyList<StaffDocumentDto> Documents,
    UserAccountInfo? Login,
    uint RowVersion);

public sealed record StaffCreatedDto(Guid Id, string EmployeeCode, CreatedUserAccount? Login);

public sealed record StaffLoginDto(CreatedUserAccount Account);

// ---------- Service ----------

public interface IEmployeeService
{
    Task<PagedResult<StaffListItemDto>> GetPagedAsync(StaffQuery query, CancellationToken cancellationToken = default);
    Task<StaffDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<StaffCreatedDto> CreateAsync(CreateStaffRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, StaffProfile profile, CancellationToken cancellationToken = default);
    Task ChangeStatusAsync(Guid id, ChangeStaffStatusRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<QualificationDto> AddQualificationAsync(Guid staffId, QualificationInput input, CancellationToken cancellationToken = default);
    Task RemoveQualificationAsync(Guid staffId, Guid qualificationId, CancellationToken cancellationToken = default);

    Task<ExperienceDto> AddExperienceAsync(Guid staffId, ExperienceInput input, CancellationToken cancellationToken = default);
    Task RemoveExperienceAsync(Guid staffId, Guid experienceId, CancellationToken cancellationToken = default);

    Task<StaffDocumentDto> AddDocumentAsync(Guid staffId, StaffDocumentInput input, CancellationToken cancellationToken = default);
    Task VerifyDocumentAsync(Guid staffId, Guid documentId, CancellationToken cancellationToken = default);
    Task RemoveDocumentAsync(Guid staffId, Guid documentId, CancellationToken cancellationToken = default);

    Task<StaffLoginDto> CreateLoginAsync(Guid staffId, StaffLoginRequest request, CancellationToken cancellationToken = default);
    Task SetRolesAsync(Guid staffId, SetStaffRolesRequest request, CancellationToken cancellationToken = default);
    Task<string> ResetPasswordAsync(Guid staffId, CancellationToken cancellationToken = default);
}

// ---------- Validators ----------

public sealed class StaffProfileValidator : AbstractValidator<StaffProfile>
{
    private const string PhonePattern = @"^\+?[0-9]{10,15}$";

    public StaffProfileValidator(TimeProvider clock)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.MiddleName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
        RuleFor(x => x.Designation).MaximumLength(100);
        RuleFor(x => x.HighestQualification).MaximumLength(100);
        RuleFor(x => x.Specialization).MaximumLength(150);

        RuleFor(x => x.DateOfBirth)
            .Must(dob => dob!.Value <= today.AddYears(-18)).When(x => x.DateOfBirth.HasValue)
            .WithMessage("Employee must be at least 18 years old.");
        RuleFor(x => x.JoiningDate).NotEmpty().LessThanOrEqualTo(today.AddMonths(6))
            .WithMessage("Joining date cannot be more than 6 months in the future.");
        RuleFor(x => x.ProbationEndDate).GreaterThanOrEqualTo(x => x.JoiningDate).When(x => x.ProbationEndDate.HasValue);
        RuleFor(x => x.ConfirmationDate).GreaterThanOrEqualTo(x => x.JoiningDate).When(x => x.ConfirmationDate.HasValue);
        RuleFor(x => x.PriorExperienceYears).InclusiveBetween(0, 60).When(x => x.PriorExperienceYears.HasValue);

        RuleFor(x => x.Phone).Matches(PhonePattern).When(x => !string.IsNullOrWhiteSpace(x.Phone)).WithMessage("Enter a valid phone number.");
        RuleFor(x => x.AlternatePhone).Matches(PhonePattern).When(x => !string.IsNullOrWhiteSpace(x.AlternatePhone));
        RuleFor(x => x.EmergencyContactPhone).Matches(PhonePattern).When(x => !string.IsNullOrWhiteSpace(x.EmergencyContactPhone));
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.PersonalEmail).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.PersonalEmail));

        RuleFor(x => x.NationalIdNumber).Matches(@"^\d{12}$").When(x => !string.IsNullOrWhiteSpace(x.NationalIdNumber))
            .WithMessage("Aadhaar number must be 12 digits.");
        RuleFor(x => x.PanNumber).Matches(@"^[A-Z]{5}[0-9]{4}[A-Z]$").When(x => !string.IsNullOrWhiteSpace(x.PanNumber))
            .WithMessage("PAN must look like ABCDE1234F.");
        RuleFor(x => x.Bank!.IfscCode).Matches(@"^[A-Z]{4}0[A-Z0-9]{6}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Bank?.IfscCode)).WithMessage("IFSC must look like HDFC0001234.");
        RuleFor(x => x.Bank!.AccountNumber).Matches(@"^[0-9]{6,20}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Bank?.AccountNumber)).WithMessage("Bank account number must be 6-20 digits.");
        RuleFor(x => x.Statutory!.Uan).Matches(@"^\d{12}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Statutory?.Uan)).WithMessage("UAN must be 12 digits.");

        RuleFor(x => x.CustomFields).Must(BeJsonObject).When(x => !string.IsNullOrWhiteSpace(x.CustomFields))
            .WithMessage("Custom fields must be a JSON object.");
    }

    private static bool BeJsonObject(string? json)
    {
        try
        {
            using var document = JsonDocument.Parse(json!);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed class QualificationInputValidator : AbstractValidator<QualificationInput>
{
    public QualificationInputValidator(TimeProvider clock)
    {
        RuleFor(x => x.Degree).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Institution).MaximumLength(200);
        RuleFor(x => x.BoardOrUniversity).MaximumLength(200);
        RuleFor(x => x.YearOfPassing).InclusiveBetween(1950, clock.GetUtcNow().Year).When(x => x.YearOfPassing.HasValue);
        RuleFor(x => x.Score).MaximumLength(20);
    }
}

public sealed class ExperienceInputValidator : AbstractValidator<ExperienceInput>
{
    public ExperienceInputValidator()
    {
        RuleFor(x => x.Organization).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Designation).MaximumLength(100);
        RuleFor(x => x.ToDate).GreaterThanOrEqualTo(x => x.FromDate).When(x => x.ToDate.HasValue);
    }
}

public sealed class StaffDocumentInputValidator : AbstractValidator<StaffDocumentInput>
{
    public StaffDocumentInputValidator()
    {
        RuleFor(x => x.DocumentType).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.FileUrl).NotEmpty().MaximumLength(1000)
            .Must(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http")
            .WithMessage("File URL must be an absolute http(s) URL.");
        RuleFor(x => x.SizeBytes).InclusiveBetween(0, 25L * 1024 * 1024).WithMessage("Files must be 25 MB or smaller.");
    }
}

public sealed class StaffLoginRequestValidator : AbstractValidator<StaffLoginRequest>
{
    public StaffLoginRequestValidator()
    {
        RuleFor(x => x.UserName).Matches(@"^[a-zA-Z0-9._@+-]{3,64}$").When(x => !string.IsNullOrWhiteSpace(x.UserName))
            .WithMessage("Username: 3-64 characters, letters, digits and . _ @ + - only.");
        RuleFor(x => x.Password).MinimumLength(8).When(x => !string.IsNullOrEmpty(x.Password));
        RuleFor(x => x.Roles).NotEmpty().WithMessage("Assign at least one role.");
    }
}

public sealed class CreateStaffRequestValidator : AbstractValidator<CreateStaffRequest>
{
    public CreateStaffRequestValidator(TimeProvider clock)
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.EmployeeCode).MaximumLength(30);
        RuleFor(x => x.Profile).NotNull().SetValidator(new StaffProfileValidator(clock));
        RuleForEach(x => x.Qualifications).SetValidator(new QualificationInputValidator(clock));
        RuleForEach(x => x.Experiences).SetValidator(new ExperienceInputValidator());
        RuleFor(x => x.Login!).SetValidator(new StaffLoginRequestValidator()).When(x => x.Login is not null);
    }
}

public sealed class ChangeStaffStatusRequestValidator : AbstractValidator<ChangeStaffStatusRequest>
{
    public ChangeStaffStatusRequestValidator()
    {
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.Reason).NotEmpty()
            .When(x => x.Status is StaffStatus.Resigned or StaffStatus.Terminated or StaffStatus.Suspended)
            .WithMessage("A reason is required for this status.");
    }
}
