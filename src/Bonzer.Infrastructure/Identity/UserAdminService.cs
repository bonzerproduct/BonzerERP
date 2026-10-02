using Bonzer.Application.Common.Models;
using Bonzer.Application.Features.Users;
using Bonzer.Application.Security;
using FluentValidation;

namespace Bonzer.Infrastructure.Identity;

public sealed class UserAdminService(
    BonzerDbContext db,
    IUserAccountService accounts,
    ICurrentUser currentUser,
    TimeProvider clock,
    IValidator<CreatePortalLoginRequest> portalLoginValidator,
    IValidator<SetUserRolesRequest> rolesValidator) : IUserAdminService
{
    public async Task<PagedResult<UserListItemDto>> GetPagedAsync(UserQuery query, CancellationToken cancellationToken = default)
    {
        var users = VisibleUsers();

        if (query.UserType.HasValue) users = users.Where(u => u.UserType == query.UserType);
        if (query.IsActive.HasValue) users = users.Where(u => u.IsActive == query.IsActive);
        if (query.BranchId is { } branchId)
        {
            currentUser.EnsureBranchAccess(branchId);
            users = users.Where(u => u.BranchAccess.Any(b => b.BranchId == branchId)
                                     || db.Staff.Any(s => s.UserId == u.Id && s.BranchId == branchId)
                                     || db.Students.Any(s => s.UserId == u.Id && s.BranchId == branchId));
        }
        if (!string.IsNullOrWhiteSpace(query.Role))
        {
            var role = query.Role.Trim().ToUpperInvariant();
            users = users.Where(u => db.UserRoles.Any(ur => ur.UserId == u.Id &&
                                                            db.Roles.Any(r => r.Id == ur.RoleId && r.NormalizedName == role)));
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            users = users.Where(u =>
                u.UserName!.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                (u.LastName != null && u.LastName.ToLower().Contains(term)) ||
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
        }

        var now = clock.GetUtcNow();
        var page = await users
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Select(u => new
            {
                u.Id, u.UserName, u.FirstName, u.LastName, u.Email, u.PhoneNumber, u.UserType, u.IsActive,
                LockedOut = u.LockoutEnd != null && u.LockoutEnd > now, u.LastLoginAt
            })
            .ToPagedResultAsync(query, cancellationToken);

        var roleMap = await RolesOfAsync(page.Items.Select(u => u.Id).ToList(), cancellationToken);
        var items = page.Items.Select(u => new UserListItemDto(
                u.Id, u.UserName!, string.IsNullOrWhiteSpace(u.LastName) ? u.FirstName : $"{u.FirstName} {u.LastName}",
                u.Email, u.PhoneNumber, u.UserType, u.IsActive, u.LockedOut, u.LastLoginAt,
                roleMap.GetValueOrDefault(u.Id) ?? []))
            .ToList();

        return new PagedResult<UserListItemDto>(items, page.Page, page.PageSize, page.TotalCount);
    }

    public async Task<UserDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await VisibleUsers().FirstOrDefaultAsync(u => u.Id == id, cancellationToken)
                   ?? throw new NotFoundException("User", id);

        var roles = (await RolesOfAsync([id], cancellationToken)).GetValueOrDefault(id) ?? [];
        var branches = await db.UserBranchAccess.Where(b => b.UserId == id)
            .Join(db.Branches, b => b.BranchId, br => br.Id, (_, br) => new BranchRefDto(br.Id, br.Name))
            .ToListAsync(cancellationToken);

        var now = clock.GetUtcNow();
        var activeSessions = await db.RefreshTokens.CountAsync(
            t => t.UserId == id && t.RevokedAt == null && t.ExpiresAt > now.UtcDateTime, cancellationToken);

        return new UserDetailDto(
            user.Id, user.UserName!, user.FullName, user.Email, user.PhoneNumber, user.UserType, user.IsActive,
            user.LockoutEnd > now, user.LockoutEnd > now ? user.LockoutEnd : null, user.AccessFailedCount,
            user.MustChangePassword, user.LastLoginAt, user.CreatedAt, roles, branches,
            await LinkedProfileAsync(user, cancellationToken), activeSessions);
    }

    public async Task SetRolesAsync(Guid id, SetUserRolesRequest request, CancellationToken cancellationToken = default)
    {
        await rolesValidator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureVisibleAsync(id, cancellationToken);
        await accounts.SetRolesAsync(id, request.Roles, cancellationToken);
    }

    public async Task SetStatusAsync(Guid id, SetUserStatusRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureVisibleAsync(id, cancellationToken);
        if (id == currentUser.UserId && !request.IsActive)
            throw new BusinessRuleException("You cannot deactivate your own account.");
        await accounts.SetActiveAsync(id, request.IsActive, cancellationToken);
    }

    public async Task SetBranchesAsync(Guid id, SetUserBranchesRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureVisibleAsync(id, cancellationToken);
        await accounts.SetBranchesAsync(id, request.BranchIds ?? [], cancellationToken);
    }

    public async Task UnlockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureVisibleAsync(id, cancellationToken);
        await accounts.UnlockAsync(id, cancellationToken);
    }

    public async Task<TemporaryPasswordResponse> ResetPasswordAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureVisibleAsync(id, cancellationToken);
        if (id == currentUser.UserId)
            throw new BusinessRuleException("Use change password for your own account.");
        return new TemporaryPasswordResponse(await accounts.ResetPasswordAsync(id, cancellationToken));
    }

    public async Task LogoutEverywhereAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureVisibleAsync(id, cancellationToken);
        await accounts.EndAllSessionsAsync(id, "Signed out by administrator", cancellationToken);
    }

    public async Task<CreatedUserAccount> CreateGuardianLoginAsync(Guid guardianId, CreatePortalLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        await portalLoginValidator.ValidateAndThrowAsync(request, cancellationToken);
        var guardian = await db.Guardians.FirstOrDefaultAsync(g => g.Id == guardianId, cancellationToken)
                       ?? throw new NotFoundException(nameof(Guardian), guardianId);
        if (guardian.UserId is not null)
            throw new ConflictException("This parent already has a login.");

        // Parents may have children in several branches; the caller needs access to at least one of them.
        var branches = await db.StudentGuardians.Where(sg => sg.GuardianId == guardianId)
            .Select(sg => sg.Student.BranchId).Distinct().ToListAsync(cancellationToken);
        if (branches.Count == 0)
            throw new BusinessRuleException("Link this parent to a student before creating a login.");
        if (!branches.Any(currentUser.HasBranchAccess))
            throw new ForbiddenAccessException("You do not have access to this parent's branch.");

        var account = await accounts.CreateAsync(new CreateUserAccount(
            guardian.TenantId,
            string.IsNullOrWhiteSpace(request.UserName) ? guardian.Phone : request.UserName.Trim(),
            guardian.Email, guardian.Phone, guardian.FirstName, guardian.LastName, UserType.Guardian,
            [AppRoles.Parent], [], request.Password), cancellationToken);

        guardian.UserId = account.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<CreatedUserAccount> CreateStudentLoginAsync(Guid studentId, CreatePortalLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        await portalLoginValidator.ValidateAndThrowAsync(request, cancellationToken);
        var student = await db.Students.FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken)
                      ?? throw new NotFoundException(nameof(Student), studentId);
        currentUser.EnsureBranchAccess(student.BranchId);
        if (student.UserId is not null)
            throw new ConflictException("This student already has a login.");
        if (student.Status != StudentStatus.Active)
            throw new BusinessRuleException($"Cannot create a login for a student with status {student.Status}.");

        var userName = string.IsNullOrWhiteSpace(request.UserName)
            ? new string(student.AdmissionNo.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-')
            : request.UserName.Trim();

        var account = await accounts.CreateAsync(new CreateUserAccount(
            student.TenantId, userName, student.Email, student.Phone, student.FirstName, student.LastName,
            UserType.Student, [AppRoles.Student], [student.BranchId], request.Password), cancellationToken);

        student.UserId = account.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return account;
    }

    /// <summary>Users of the caller's institution (platform admins see everyone).</summary>
    private IQueryable<ApplicationUser> VisibleUsers()
    {
        var users = db.Users.AsNoTracking();
        if (currentUser.IsPlatformAdmin) return users;

        var tenantId = currentUser.TenantId;
        users = users.Where(u => u.TenantId == tenantId);

        // Branch-level administrators only see users attached to their branches.
        if (!currentUser.HasTenantWideAccess)
        {
            var branchIds = currentUser.BranchIds.ToList();
            users = users.Where(u =>
                u.BranchAccess.Any(b => branchIds.Contains(b.BranchId)) ||
                db.Staff.Any(s => s.UserId == u.Id && branchIds.Contains(s.BranchId)) ||
                db.Students.Any(s => s.UserId == u.Id && branchIds.Contains(s.BranchId)) ||
                db.Guardians.Any(g => g.UserId == u.Id && g.Students.Any(sg => branchIds.Contains(sg.Student.BranchId))));
        }
        return users;
    }

    private async Task EnsureVisibleAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!await VisibleUsers().AnyAsync(u => u.Id == id, cancellationToken))
            throw new NotFoundException("User", id);
    }

    private async Task<Dictionary<Guid, IReadOnlyList<string>>> RolesOfAsync(List<Guid> userIds, CancellationToken cancellationToken)
    {
        var rows = await db.UserRoles.Where(ur => userIds.Contains(ur.UserId))
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync(cancellationToken);
        return rows.GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(r => r.Name!).Order().ToList());
    }

    private async Task<LinkedProfileDto?> LinkedProfileAsync(ApplicationUser user, CancellationToken cancellationToken) =>
        user.UserType switch
        {
            UserType.Staff => await db.Staff.Where(s => s.UserId == user.Id)
                .Select(s => new LinkedProfileDto("Staff", s.Id, s.FirstName + " " + (s.LastName ?? "") + " (" + s.EmployeeCode + ")"))
                .FirstOrDefaultAsync(cancellationToken),
            UserType.Guardian => await db.Guardians.Where(g => g.UserId == user.Id)
                .Select(g => new LinkedProfileDto("Guardian", g.Id, g.FirstName + " " + (g.LastName ?? "")))
                .FirstOrDefaultAsync(cancellationToken),
            UserType.Student => await db.Students.Where(s => s.UserId == user.Id)
                .Select(s => new LinkedProfileDto("Student", s.Id, s.FirstName + " " + (s.LastName ?? "") + " (" + s.AdmissionNo + ")"))
                .FirstOrDefaultAsync(cancellationToken),
            _ => null
        };
}
