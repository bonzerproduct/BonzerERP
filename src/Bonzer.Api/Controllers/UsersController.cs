using Bonzer.Api.Authorization;
using Bonzer.Application.Common.Interfaces;
using Bonzer.Application.Common.Models;
using Bonzer.Application.Features.Users;
using Bonzer.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace Bonzer.Api.Controllers;

/// <summary>
/// User administration for the whole institution: staff, parent and student logins.
/// Staff logins are created from the employee record (POST /api/v1/staff/{id}/login).
/// </summary>
public sealed class UsersController(IUserAdminService users) : ApiControllerBase
{
    /// <summary>Search users by name, username, email or phone; filter by type, role, branch, status.</summary>
    [HttpGet]
    [HasPermission(Permissions.Users.View)]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> Get([FromQuery] UserQuery query, CancellationToken ct) =>
        Ok(await users.GetPagedAsync(query, ct));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Users.View)]
    public async Task<ActionResult<UserDetailDto>> GetById(Guid id, CancellationToken ct) => Ok(await users.GetByIdAsync(id, ct));

    /// <summary>Replace the user's roles. You can only grant roles whose permissions you hold.</summary>
    [HttpPut("{id:guid}/roles")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<IActionResult> SetRoles(Guid id, SetUserRolesRequest request, CancellationToken ct)
    {
        await users.SetRolesAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>Activate or deactivate. Deactivation signs the user out everywhere immediately.</summary>
    [HttpPut("{id:guid}/status")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<IActionResult> SetStatus(Guid id, SetUserStatusRequest request, CancellationToken ct)
    {
        await users.SetStatusAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>Extra branches a user may work in (multi-campus staff).</summary>
    [HttpPut("{id:guid}/branches")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<IActionResult> SetBranches(Guid id, SetUserBranchesRequest request, CancellationToken ct)
    {
        await users.SetBranchesAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/unlock")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<IActionResult> Unlock(Guid id, CancellationToken ct)
    {
        await users.UnlockAsync(id, ct);
        return NoContent();
    }

    /// <summary>Set a temporary password (returned once); the user must change it at next sign-in.</summary>
    [HttpPost("{id:guid}/reset-password")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<ActionResult<TemporaryPasswordResponse>> ResetPassword(Guid id, CancellationToken ct) =>
        Ok(await users.ResetPasswordAsync(id, ct));

    [HttpPost("{id:guid}/logout-all")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<IActionResult> LogoutEverywhere(Guid id, CancellationToken ct)
    {
        await users.LogoutEverywhereAsync(id, ct);
        return NoContent();
    }

    /// <summary>Create a parent portal login (role Parent). Username defaults to the parent's phone number.</summary>
    [HttpPost("guardians/{guardianId:guid}/login")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<ActionResult<CreatedUserAccount>> CreateGuardianLogin(Guid guardianId, CreatePortalLoginRequest request,
        CancellationToken ct) =>
        Ok(await users.CreateGuardianLoginAsync(guardianId, request, ct));

    /// <summary>Create a student portal login (role Student). Username defaults to the admission number.</summary>
    [HttpPost("students/{studentId:guid}/login")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<ActionResult<CreatedUserAccount>> CreateStudentLogin(Guid studentId, CreatePortalLoginRequest request,
        CancellationToken ct) =>
        Ok(await users.CreateStudentLoginAsync(studentId, request, ct));
}
