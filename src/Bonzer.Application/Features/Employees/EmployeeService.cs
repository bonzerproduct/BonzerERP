using System.Text.RegularExpressions;
using Bonzer.Application.Common;
using Bonzer.Application.Security;
using Bonzer.Domain.Entities.HumanResources;

namespace Bonzer.Application.Features.Employees;

public sealed partial class EmployeeService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IUserAccountService accounts,
    IPermissionService permissions,
    IDocumentNumberGenerator numbers,
    TimeProvider clock,
    IValidator<CreateStaffRequest> createValidator,
    IValidator<StaffProfile> profileValidator,
    IValidator<ChangeStaffStatusRequest> statusValidator,
    IValidator<QualificationInput> qualificationValidator,
    IValidator<ExperienceInput> experienceValidator,
    IValidator<StaffDocumentInput> documentValidator,
    IValidator<StaffLoginRequest> loginValidator) : IEmployeeService
{
    private static readonly StaffStatus[] LoginDisabledStatuses =
        [StaffStatus.Suspended, StaffStatus.Resigned, StaffStatus.Terminated, StaffStatus.Retired];

    private static readonly StaffStatus[] ExitStatuses = [StaffStatus.Resigned, StaffStatus.Terminated, StaffStatus.Retired];

    // ---------------- Queries ----------------

    public async Task<PagedResult<StaffListItemDto>> GetPagedAsync(StaffQuery query, CancellationToken cancellationToken = default)
    {
        currentUser.EnsureBranchAccess(query.BranchId);

        var staff = db.Staff.AsNoTracking().Where(s => s.BranchId == query.BranchId);

        if (query.DepartmentId.HasValue) staff = staff.Where(s => s.DepartmentId == query.DepartmentId);
        if (query.StaffType.HasValue) staff = staff.Where(s => s.StaffType == query.StaffType);
        if (query.Status.HasValue) staff = staff.Where(s => s.Status == query.Status);
        if (query.EmploymentType.HasValue) staff = staff.Where(s => s.EmploymentType == query.EmploymentType);
        if (query.Current == true) staff = staff.Where(s => !ExitStatuses.Contains(s.Status));
        if (query.Current == false) staff = staff.Where(s => ExitStatuses.Contains(s.Status));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            staff = staff.Where(s =>
                s.EmployeeCode.ToLower().Contains(term) ||
                s.FirstName.ToLower().Contains(term) ||
                (s.LastName != null && s.LastName.ToLower().Contains(term)) ||
                (s.Phone != null && s.Phone.Contains(term)) ||
                (s.Designation != null && s.Designation.ToLower().Contains(term)));
        }

        return await staff
            .OrderBy(s => s.FirstName).ThenBy(s => s.LastName)
            .Select(s => new StaffListItemDto(
                s.Id, s.EmployeeCode,
                s.FirstName + (s.LastName == null ? "" : " " + s.LastName),
                s.Designation,
                s.Department == null ? null : s.Department.Name,
                s.StaffType, s.EmploymentType, s.Status, s.Phone, s.Email, s.JoiningDate, s.PhotoUrl,
                s.UserId != null))
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<StaffDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var s = await db.Staff.AsNoTracking()
                    .Include(x => x.Department)
                    .Include(x => x.ReportingTo)
                    .Include(x => x.Qualifications)
                    .Include(x => x.Experiences)
                    .Include(x => x.Documents)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new NotFoundException(nameof(Staff), id);
        currentUser.EnsureBranchAccess(s.BranchId);

        var reveal = await CanSeeSensitiveAsync(cancellationToken);
        var login = s.UserId is { } userId ? await accounts.GetAsync(userId, cancellationToken) : null;

        var profile = new StaffProfile(
            s.FirstName, s.MiddleName, s.LastName, s.Gender, s.DateOfBirth, s.BloodGroup, s.MaritalStatus,
            s.Nationality, s.Religion, s.Category,
            Masking.Mask(s.NationalIdNumber, reveal), Masking.Mask(s.PanNumber, reveal),
            s.Phone, s.AlternatePhone, s.Email, s.PersonalEmail, s.Address, s.PermanentAddress, s.PhotoUrl,
            s.EmergencyContactName, s.EmergencyContactRelation, s.EmergencyContactPhone,
            s.StaffType, s.DepartmentId, s.Designation, s.EmploymentType, s.JoiningDate, s.ProbationEndDate,
            s.ConfirmationDate, s.ReportingToStaffId, s.HighestQualification, s.Specialization,
            s.PriorExperienceYears, s.ProfessionalRegistrationNo,
            Bank: null, // returned separately (masked) in StaffDetailDto.Bank
            s.Statutory, s.CustomFields);

        return new StaffDetailDto(
            s.Id, s.BranchId, s.EmployeeCode, s.FullName, profile,
            new BankDetailsDto(s.Bank.AccountHolderName, Masking.Mask(s.Bank.AccountNumber, reveal), s.Bank.IfscCode,
                s.Bank.BankName, s.Bank.BranchName),
            s.Status, s.LeavingDate, s.ExitReason, s.Department?.Name, s.ReportingTo?.FullName,
            s.Qualifications.OrderByDescending(q => q.YearOfPassing).Select(ToDto).ToList(),
            s.Experiences.OrderByDescending(e => e.FromDate).Select(ToDto).ToList(),
            s.Documents.OrderBy(d => d.DocumentType).Select(ToDto).ToList(),
            login, s.RowVersion);
    }

    // ---------------- Commands ----------------

    public async Task<StaffCreatedDto> CreateAsync(CreateStaffRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        currentUser.EnsureBranchAccess(request.BranchId);

        var tenantId = await db.Branches.Where(b => b.Id == request.BranchId).Select(b => (Guid?)b.TenantId)
                           .FirstOrDefaultAsync(cancellationToken)
                       ?? throw new NotFoundException("Branch", request.BranchId);
        await ValidateReferencesAsync(request.BranchId, null, request.Profile, cancellationToken);
        await EnsureNoDuplicateIdentityAsync(null, request.Profile, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var code = string.IsNullOrWhiteSpace(request.EmployeeCode)
            ? await numbers.NextAsync(request.BranchId, DocumentType.StaffCode, null, cancellationToken)
            : request.EmployeeCode.Trim();
        if (await db.Staff.AnyAsync(s => s.BranchId == request.BranchId && s.EmployeeCode == code, cancellationToken))
            throw new ConflictException($"Employee code '{code}' is already used.");

        var staff = new Staff { TenantId = tenantId, BranchId = request.BranchId, EmployeeCode = code };
        Apply(staff, request.Profile, keepSensitiveWhenMasked: false);

        foreach (var q in request.Qualifications ?? []) staff.Qualifications.Add(ToEntity(q, tenantId));
        foreach (var e in request.Experiences ?? []) staff.Experiences.Add(ToEntity(e, tenantId));
        NormalizeHighest(staff);

        db.Staff.Add(staff);
        await db.SaveChangesAsync(cancellationToken);

        CreatedUserAccount? login = null;
        if (request.Login is not null)
            login = await CreateLoginCoreAsync(staff, request.Login, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return new StaffCreatedDto(staff.Id, staff.EmployeeCode, login);
    }

    public async Task UpdateAsync(Guid id, StaffProfile profile, CancellationToken cancellationToken = default)
    {
        await profileValidator.ValidateAndThrowAsync(profile, cancellationToken);
        var staff = await LoadAsync(id, cancellationToken);
        await ValidateReferencesAsync(staff.BranchId, staff.Id, profile, cancellationToken);
        await EnsureNoDuplicateIdentityAsync(staff.Id, profile, cancellationToken);

        Apply(staff, profile, keepSensitiveWhenMasked: true);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangeStatusAsync(Guid id, ChangeStaffStatusRequest request, CancellationToken cancellationToken = default)
    {
        await statusValidator.ValidateAndThrowAsync(request, cancellationToken);
        var staff = await LoadAsync(id, cancellationToken);
        if (staff.Status == request.Status) return;

        var effective = request.EffectiveDate ?? Today;
        if (ExitStatuses.Contains(request.Status))
        {
            if (effective < staff.JoiningDate)
                throw new BusinessRuleException("Leaving date cannot be before the joining date.");
            staff.LeavingDate = effective;
            staff.ExitReason = request.Reason;
        }
        else if (ExitStatuses.Contains(staff.Status))
        {
            // Re-joining: clear the previous exit.
            staff.LeavingDate = null;
            staff.ExitReason = null;
        }
        staff.Status = request.Status;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (staff.UserId is { } userId)
            await accounts.SetActiveAsync(userId, !LoginDisabledStatuses.Contains(request.Status), cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var staff = await LoadAsync(id, cancellationToken);

        // Records with history must be exited (status change), not deleted.
        var hasHistory = await db.StaffAttendances.AnyAsync(a => a.StaffId == id, cancellationToken)
                         || await db.TeacherAssignments.AnyAsync(a => a.StaffId == id, cancellationToken)
                         || await db.Homeworks.AnyAsync(h => h.AssignedByStaffId == id, cancellationToken);
        if (hasHistory)
            throw new BusinessRuleException("This employee has attendance or teaching history. Mark them Resigned / Terminated instead of deleting.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Staff.Remove(staff); // soft delete
        await db.SaveChangesAsync(cancellationToken);
        if (staff.UserId is { } userId)
            await accounts.SetActiveAsync(userId, false, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    // ---------------- Qualifications / experience / documents ----------------

    public async Task<QualificationDto> AddQualificationAsync(Guid staffId, QualificationInput input, CancellationToken cancellationToken = default)
    {
        await qualificationValidator.ValidateAndThrowAsync(input, cancellationToken);
        var staff = await LoadAsync(staffId, cancellationToken, includeQualifications: true);

        var qualification = ToEntity(input, staff.TenantId);
        staff.Qualifications.Add(qualification);
        NormalizeHighest(staff, qualification);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(qualification);
    }

    public async Task RemoveQualificationAsync(Guid staffId, Guid qualificationId, CancellationToken cancellationToken = default)
    {
        var staff = await LoadAsync(staffId, cancellationToken, includeQualifications: true);
        var qualification = staff.Qualifications.FirstOrDefault(q => q.Id == qualificationId)
                            ?? throw new NotFoundException(nameof(StaffQualification), qualificationId);

        db.StaffQualifications.Remove(qualification); // soft delete
        if (qualification.IsHighest)
            staff.HighestQualification = staff.Qualifications
                .Where(q => q.Id != qualificationId).OrderByDescending(q => q.YearOfPassing).FirstOrDefault()?.Degree;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ExperienceDto> AddExperienceAsync(Guid staffId, ExperienceInput input, CancellationToken cancellationToken = default)
    {
        await experienceValidator.ValidateAndThrowAsync(input, cancellationToken);
        var staff = await LoadAsync(staffId, cancellationToken);

        var experience = ToEntity(input, staff.TenantId);
        staff.Experiences.Add(experience);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(experience);
    }

    public async Task RemoveExperienceAsync(Guid staffId, Guid experienceId, CancellationToken cancellationToken = default)
    {
        await LoadAsync(staffId, cancellationToken);
        var experience = await db.StaffExperiences
                             .FirstOrDefaultAsync(e => e.Id == experienceId && e.StaffId == staffId, cancellationToken)
                         ?? throw new NotFoundException(nameof(StaffExperience), experienceId);
        db.StaffExperiences.Remove(experience);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<StaffDocumentDto> AddDocumentAsync(Guid staffId, StaffDocumentInput input, CancellationToken cancellationToken = default)
    {
        await documentValidator.ValidateAndThrowAsync(input, cancellationToken);
        var staff = await LoadAsync(staffId, cancellationToken);

        var document = new StaffDocument
        {
            TenantId = staff.TenantId,
            DocumentType = input.DocumentType.Trim(),
            FileName = input.FileName.Trim(),
            FileUrl = input.FileUrl.Trim(),
            ContentType = input.ContentType,
            SizeBytes = input.SizeBytes,
            ExpiresOn = input.ExpiresOn
        };
        staff.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(document);
    }

    public async Task VerifyDocumentAsync(Guid staffId, Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await LoadDocumentAsync(staffId, documentId, cancellationToken);
        document.IsVerified = true;
        document.VerifiedBy = currentUser.UserId;
        document.VerifiedAt = clock.GetUtcNow().UtcDateTime;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveDocumentAsync(Guid staffId, Guid documentId, CancellationToken cancellationToken = default)
    {
        db.StaffDocuments.Remove(await LoadDocumentAsync(staffId, documentId, cancellationToken));
        await db.SaveChangesAsync(cancellationToken);
    }

    // ---------------- Login & roles ----------------

    public async Task<StaffLoginDto> CreateLoginAsync(Guid staffId, StaffLoginRequest request, CancellationToken cancellationToken = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);
        var staff = await LoadAsync(staffId, cancellationToken);
        if (staff.UserId is not null)
            throw new ConflictException("This employee already has a login.");
        if (LoginDisabledStatuses.Contains(staff.Status))
            throw new BusinessRuleException($"Cannot create a login for an employee with status {staff.Status}.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var account = await CreateLoginCoreAsync(staff, request, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new StaffLoginDto(account);
    }

    public async Task SetRolesAsync(Guid staffId, SetStaffRolesRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Roles is not { Count: > 0 })
            throw new ValidationException("Assign at least one role.");
        var staff = await LoadAsync(staffId, cancellationToken);
        var userId = staff.UserId ?? throw new BusinessRuleException("Create a login for this employee first.");
        await accounts.SetRolesAsync(userId, request.Roles, cancellationToken);
    }

    public async Task<string> ResetPasswordAsync(Guid staffId, CancellationToken cancellationToken = default)
    {
        var staff = await LoadAsync(staffId, cancellationToken);
        var userId = staff.UserId ?? throw new BusinessRuleException("This employee has no login.");
        return await accounts.ResetPasswordAsync(userId, cancellationToken);
    }

    // ---------------- Helpers ----------------

    private async Task<CreatedUserAccount> CreateLoginCoreAsync(Staff staff, StaffLoginRequest request, CancellationToken cancellationToken)
    {
        var branches = new List<Guid> { staff.BranchId };
        foreach (var branchId in request.AdditionalBranchIds ?? [])
        {
            currentUser.EnsureBranchAccess(branchId);
            if (!await db.Branches.AnyAsync(b => b.Id == branchId, cancellationToken))
                throw new NotFoundException("Branch", branchId);
            if (!branches.Contains(branchId)) branches.Add(branchId);
        }

        var userName = string.IsNullOrWhiteSpace(request.UserName)
            ? UserNameFromCode(staff.EmployeeCode)
            : request.UserName.Trim();

        var account = await accounts.CreateAsync(new CreateUserAccount(
            staff.TenantId, userName, staff.Email, staff.Phone, staff.FirstName, staff.LastName,
            UserType.Staff, request.Roles, branches, request.Password), cancellationToken);

        staff.UserId = account.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return account;
    }

    private async Task<Staff> LoadAsync(Guid id, CancellationToken cancellationToken, bool includeQualifications = false)
    {
        var query = db.Staff.AsQueryable();
        if (includeQualifications) query = query.Include(s => s.Qualifications);
        var staff = await query.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
                    ?? throw new NotFoundException(nameof(Staff), id);
        currentUser.EnsureBranchAccess(staff.BranchId);
        return staff;
    }

    private async Task<StaffDocument> LoadDocumentAsync(Guid staffId, Guid documentId, CancellationToken cancellationToken)
    {
        await LoadAsync(staffId, cancellationToken);
        return await db.StaffDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.StaffId == staffId, cancellationToken)
               ?? throw new NotFoundException(nameof(StaffDocument), documentId);
    }

    private async Task ValidateReferencesAsync(Guid branchId, Guid? staffId, StaffProfile profile, CancellationToken cancellationToken)
    {
        if (profile.DepartmentId is { } departmentId &&
            !await db.Departments.AnyAsync(d => d.Id == departmentId && d.BranchId == branchId, cancellationToken))
            throw new BusinessRuleException("The department does not belong to this branch.");

        if (profile.ReportingToStaffId is { } managerId)
        {
            if (managerId == staffId)
                throw new BusinessRuleException("An employee cannot report to themselves.");
            if (!await db.Staff.AnyAsync(s => s.Id == managerId, cancellationToken))
                throw new BusinessRuleException("The reporting manager was not found.");
        }
    }

    /// <summary>The same person must not be on the rolls twice in one tenant (checked by phone and email).</summary>
    private async Task EnsureNoDuplicateIdentityAsync(Guid? staffId, StaffProfile profile, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(profile.Phone) &&
            await db.Staff.AnyAsync(s => s.Phone == profile.Phone && s.Id != staffId && !ExitStatuses.Contains(s.Status), cancellationToken))
            throw new ConflictException($"Another current employee already uses phone {profile.Phone}.");

        if (!string.IsNullOrWhiteSpace(profile.Email) &&
            await db.Staff.AnyAsync(s => s.Email == profile.Email && s.Id != staffId && !ExitStatuses.Contains(s.Status), cancellationToken))
            throw new ConflictException($"Another current employee already uses email {profile.Email}.");
    }

    private async Task<bool> CanSeeSensitiveAsync(CancellationToken cancellationToken) =>
        currentUser.IsPlatformAdmin ||
        (await permissions.GetPermissionsAsync(currentUser.TenantId, currentUser.Roles, cancellationToken))
        .Contains(Permissions.Staff.ViewSensitive);

    /// <param name="keepSensitiveWhenMasked">
    /// On update the client sends back what it received; a masked value ("XXXXXXXX1234") means "unchanged".
    /// </param>
    private static void Apply(Staff s, StaffProfile p, bool keepSensitiveWhenMasked)
    {
        s.FirstName = p.FirstName.Trim();
        s.MiddleName = p.MiddleName?.Trim();
        s.LastName = p.LastName?.Trim();
        s.Gender = p.Gender;
        s.DateOfBirth = p.DateOfBirth;
        s.BloodGroup = p.BloodGroup;
        s.MaritalStatus = p.MaritalStatus;
        s.Nationality = p.Nationality;
        s.Religion = p.Religion;
        s.Category = p.Category;
        s.NationalIdNumber = Sensitive(s.NationalIdNumber, p.NationalIdNumber, keepSensitiveWhenMasked);
        s.PanNumber = Sensitive(s.PanNumber, p.PanNumber?.ToUpperInvariant(), keepSensitiveWhenMasked);
        s.Phone = p.Phone;
        s.AlternatePhone = p.AlternatePhone;
        s.Email = p.Email?.Trim().ToLowerInvariant();
        s.PersonalEmail = p.PersonalEmail?.Trim().ToLowerInvariant();
        CopyAddress(s.Address, p.Address);
        CopyAddress(s.PermanentAddress, p.PermanentAddress);
        s.PhotoUrl = p.PhotoUrl;
        s.EmergencyContactName = p.EmergencyContactName;
        s.EmergencyContactRelation = p.EmergencyContactRelation;
        s.EmergencyContactPhone = p.EmergencyContactPhone;
        s.StaffType = p.StaffType;
        s.DepartmentId = p.DepartmentId;
        s.Designation = p.Designation?.Trim();
        s.EmploymentType = p.EmploymentType;
        s.JoiningDate = p.JoiningDate;
        s.ProbationEndDate = p.ProbationEndDate;
        s.ConfirmationDate = p.ConfirmationDate;
        s.ReportingToStaffId = p.ReportingToStaffId;
        s.HighestQualification = p.HighestQualification;
        s.Specialization = p.Specialization;
        s.PriorExperienceYears = p.PriorExperienceYears;
        s.ProfessionalRegistrationNo = p.ProfessionalRegistrationNo;
        s.CustomFields = p.CustomFields;

        // Owned value objects are updated in place so the audit log records only real changes.
        if (p.Bank is not null)
        {
            s.Bank.AccountHolderName = p.Bank.AccountHolderName;
            s.Bank.AccountNumber = Sensitive(s.Bank.AccountNumber, p.Bank.AccountNumber, keepSensitiveWhenMasked);
            s.Bank.IfscCode = p.Bank.IfscCode?.ToUpperInvariant();
            s.Bank.BankName = p.Bank.BankName;
            s.Bank.BranchName = p.Bank.BranchName;
        }
        if (p.Statutory is not null)
        {
            s.Statutory.Uan = p.Statutory.Uan;
            s.Statutory.PfNumber = p.Statutory.PfNumber;
            s.Statutory.EsiNumber = p.Statutory.EsiNumber;
            s.Statutory.IsPfApplicable = p.Statutory.IsPfApplicable;
            s.Statutory.IsEsiApplicable = p.Statutory.IsEsiApplicable;
        }
    }

    private static void CopyAddress(Address target, Address? source)
    {
        source ??= new Address();
        target.Line1 = source.Line1;
        target.Line2 = source.Line2;
        target.City = source.City;
        target.District = source.District;
        target.State = source.State;
        target.Country = source.Country;
        target.PostalCode = source.PostalCode;
    }

    private static string? Sensitive(string? current, string? incoming, bool keepWhenMasked) =>
        keepWhenMasked && incoming is not null && incoming.StartsWith('X') ? current : incoming;

    /// <summary>Exactly one qualification is flagged "highest"; it also feeds Staff.HighestQualification.</summary>
    private static void NormalizeHighest(Staff staff, StaffQualification? preferred = null)
    {
        var highest = preferred is { IsHighest: true } ? preferred : staff.Qualifications.LastOrDefault(q => q.IsHighest);
        if (highest is null) return;
        foreach (var q in staff.Qualifications) q.IsHighest = ReferenceEquals(q, highest);
        staff.HighestQualification = highest.Degree;
    }

    private static string UserNameFromCode(string employeeCode) =>
        NonUserNameChars().Replace(employeeCode.ToLowerInvariant(), "-").Trim('-');

    [GeneratedRegex("[^a-z0-9._@+-]+")]
    private static partial Regex NonUserNameChars();

    private DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

    private static StaffQualification ToEntity(QualificationInput q, Guid tenantId) => new()
    {
        TenantId = tenantId, Degree = q.Degree.Trim(), Specialization = q.Specialization, Institution = q.Institution,
        BoardOrUniversity = q.BoardOrUniversity, YearOfPassing = q.YearOfPassing, Score = q.Score, IsHighest = q.IsHighest
    };

    private static StaffExperience ToEntity(ExperienceInput e, Guid tenantId) => new()
    {
        TenantId = tenantId, Organization = e.Organization.Trim(), Designation = e.Designation, FromDate = e.FromDate,
        ToDate = e.ToDate, Responsibilities = e.Responsibilities, LeavingReason = e.LeavingReason
    };

    private static QualificationDto ToDto(StaffQualification q) =>
        new(q.Id, q.Degree, q.Specialization, q.Institution, q.BoardOrUniversity, q.YearOfPassing, q.Score, q.IsHighest);

    private static ExperienceDto ToDto(StaffExperience e) =>
        new(e.Id, e.Organization, e.Designation, e.FromDate, e.ToDate, e.Responsibilities, e.LeavingReason);

    private static StaffDocumentDto ToDto(StaffDocument d) =>
        new(d.Id, d.DocumentType, d.FileName, d.FileUrl, d.ContentType, d.SizeBytes, d.ExpiresOn, d.IsVerified, d.VerifiedAt);
}
