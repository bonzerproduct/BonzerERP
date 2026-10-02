using Bonzer.Api.Authorization;
using Bonzer.Application.Features.Roles;
using Bonzer.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace Bonzer.Api.Controllers;

/// <summary>Roles of the institution: built-in roles (read-only) plus the institution's own custom roles.</summary>
[HasPermission(Permissions.Users.ManageRoles)]
public sealed class RolesController(IRoleService roles) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetAll(CancellationToken ct) => Ok(await roles.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RoleDto>> GetById(Guid id, CancellationToken ct) => Ok(await roles.GetByIdAsync(id, ct));

    /// <summary>All permissions grouped by module, for the role editor.</summary>
    [HttpGet("permissions")]
    public ActionResult<IReadOnlyList<PermissionGroupDto>> GetPermissions() => Ok(roles.GetPermissionCatalogue());

    /// <summary>Create a custom role, optionally copying another role's permissions. Using a built-in name overrides it for this institution.</summary>
    [HttpPost]
    public async Task<ActionResult<RoleDto>> Create(CreateRoleRequest request, CancellationToken ct)
    {
        var created = await roles.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<RoleDto>> Update(Guid id, UpdateRoleRequest request, CancellationToken ct) =>
        Ok(await roles.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await roles.DeleteAsync(id, ct);
        return NoContent();
    }
}
