using Bonzer.Application.Features.Roles;
using Bonzer.Application.Security;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Bonzer.Infrastructure.Identity;

public sealed class RoleService(
    BonzerDbContext db,
    RoleManager<ApplicationRole> roleManager,
    RoleResolver resolver,
    IPermissionService permissionService,
    ICurrentUser currentUser,
    IValidator<CreateRoleRequest> createValidator,
    IValidator<UpdateRoleRequest> updateValidator,
    ILogger<RoleService> logger) : IRoleService
{
    public async Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tenantId = currentUser.TenantId;
        var roles = await db.Roles.AsNoTracking()
            .Where(r => r.TenantId == null || r.TenantId == tenantId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        // Hide built-in roles the tenant has overridden, and the platform-only role for tenant users.
        var overridden = roles.Where(r => r.TenantId != null).Select(r => r.NormalizedName).ToHashSet();
        roles = roles.Where(r => r.TenantId != null || !overridden.Contains(r.NormalizedName))
            .Where(r => currentUser.IsPlatformAdmin || r.Name != AppRoles.SuperAdmin)
            .ToList();

        var ids = roles.Select(r => r.Id).ToList();
        var claims = await db.RoleClaims.AsNoTracking()
            .Where(c => ids.Contains(c.RoleId) && c.ClaimType == AppClaimTypes.Permission)
            .Select(c => new { c.RoleId, c.ClaimValue })
            .ToListAsync(cancellationToken);
        var userCounts = await CountUsersAsync(ids, cancellationToken);

        return roles.Select(r => ToDto(r,
                claims.Where(c => c.RoleId == r.Id).Select(c => c.ClaimValue!).Order().ToList(),
                userCounts.GetValueOrDefault(r.Id), overridden.Contains(r.NormalizedName) && r.TenantId != null))
            .ToList();
    }

    public async Task<RoleDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await LoadVisibleAsync(id, cancellationToken);
        return await ToDtoAsync(role, cancellationToken);
    }

    public IReadOnlyList<PermissionGroupDto> GetPermissionCatalogue() =>
        Permissions.Catalogue
            .Where(p => currentUser.IsPlatformAdmin || !Permissions.IsPlatformOnly(p.Code))
            .GroupBy(p => p.Module)
            .Select(g => new PermissionGroupDto(g.Key, g.Select(p => new PermissionDto(p.Code, p.Name)).ToList()))
            .ToList();

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var tenantId = currentUser.TenantId
                       ?? throw new BusinessRuleException("Custom roles belong to an institution; sign in as an institution user.");

        var permissions = new HashSet<string>(request.Permissions ?? [], StringComparer.Ordinal);
        if (request.CopyFromRoleId is { } sourceId)
        {
            var source = await LoadVisibleAsync(sourceId, cancellationToken);
            permissions.UnionWith(await GetRolePermissionsAsync(source.Id, cancellationToken));
            permissions.RemoveWhere(Permissions.IsPlatformOnly);
        }
        await resolver.EnsureCanGrantAsync(permissions, [request.Name], cancellationToken);

        var role = new ApplicationRole { Name = request.Name.Trim(), Description = request.Description, TenantId = tenantId };
        ThrowIfFailed(await roleManager.CreateAsync(role));

        db.RoleClaims.AddRange(permissions.Select(p => new IdentityRoleClaim<Guid>
        {
            RoleId = role.Id, ClaimType = AppClaimTypes.Permission, ClaimValue = p
        }));
        await db.SaveChangesAsync(cancellationToken);
        await permissionService.InvalidateRoleAsync(tenantId, role.Name, cancellationToken);

        logger.LogInformation("Role {Role} created for tenant {TenantId} with {Count} permissions", role.Name, tenantId, permissions.Count);
        return await ToDtoAsync(role, cancellationToken);
    }

    public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var role = await LoadEditableAsync(id, cancellationToken);
        var oldName = role.Name!;

        var existing = await db.RoleClaims.Where(c => c.RoleId == id && c.ClaimType == AppClaimTypes.Permission).ToListAsync(cancellationToken);
        var target = request.Permissions.ToHashSet(StringComparer.Ordinal);
        var added = target.Where(p => existing.All(c => c.ClaimValue != p)).ToList();
        var removed = existing.Where(c => !target.Contains(c.ClaimValue!)).ToList();

        // Adding and removing both change what holders can do, so the caller must hold all touched permissions.
        await resolver.EnsureCanGrantAsync(added.Concat(removed.Select(c => c.ClaimValue!)), [oldName, request.Name], cancellationToken);

        if (!string.Equals(oldName, request.Name.Trim(), StringComparison.Ordinal))
        {
            role.Name = request.Name.Trim();
            ThrowIfFailed(await roleManager.UpdateAsync(role)); // validates per-tenant uniqueness
        }
        role.Description = request.Description;

        db.RoleClaims.RemoveRange(removed);
        db.RoleClaims.AddRange(added.Select(p => new IdentityRoleClaim<Guid> { RoleId = id, ClaimType = AppClaimTypes.Permission, ClaimValue = p }));
        await db.SaveChangesAsync(cancellationToken);

        await permissionService.InvalidateRoleAsync(role.TenantId, oldName, cancellationToken);
        await permissionService.InvalidateRoleAsync(role.TenantId, role.Name!, cancellationToken);
        logger.LogInformation("Role {Role} updated: +{Added} -{Removed} permissions", role.Name, added.Count, removed.Count);
        return await ToDtoAsync(role, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await LoadEditableAsync(id, cancellationToken);
        if (await db.UserRoles.AnyAsync(ur => ur.RoleId == id, cancellationToken))
            throw new BusinessRuleException("This role is assigned to users. Move them to another role first.");

        db.RoleClaims.RemoveRange(db.RoleClaims.Where(c => c.RoleId == id));
        db.Roles.Remove(role);
        await db.SaveChangesAsync(cancellationToken);
        await permissionService.InvalidateRoleAsync(role.TenantId, role.Name!, cancellationToken);
    }

    private async Task<ApplicationRole> LoadVisibleAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id && (r.TenantId == null || r.TenantId == tenantId), cancellationToken);
        if (role is null || (!currentUser.IsPlatformAdmin && role.Name == AppRoles.SuperAdmin))
            throw new NotFoundException("Role", id);
        return role;
    }

    /// <summary>
    /// Built-in roles are shared by every institution and defined in code (AppRoles, synced at startup),
    /// so they are read-only here. Only an institution's own roles can be edited or deleted.
    /// </summary>
    private async Task<ApplicationRole> LoadEditableAsync(Guid id, CancellationToken cancellationToken)
    {
        var role = await LoadVisibleAsync(id, cancellationToken);
        if (role.TenantId is null)
            throw new BusinessRuleException("Built-in roles cannot be changed. Create a role with the same name to override it for your institution.");
        return role;
    }

    private async Task<IReadOnlyList<string>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken) =>
        await db.RoleClaims.Where(c => c.RoleId == roleId && c.ClaimType == AppClaimTypes.Permission)
            .Select(c => c.ClaimValue!).OrderBy(c => c).ToListAsync(cancellationToken);

    private async Task<Dictionary<Guid, int>> CountUsersAsync(List<Guid> roleIds, CancellationToken cancellationToken)
    {
        var tenantId = currentUser.TenantId;
        // Count only users of the caller's institution, never other tenants sharing a built-in role.
        return await db.UserRoles
            .Where(ur => roleIds.Contains(ur.RoleId))
            .Join(db.Users, ur => ur.UserId, u => u.Id, (ur, u) => new { ur.RoleId, u.TenantId })
            .Where(x => currentUser.IsPlatformAdmin || x.TenantId == tenantId)
            .GroupBy(x => x.RoleId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
    }

    private async Task<RoleDto> ToDtoAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        var counts = await CountUsersAsync([role.Id], cancellationToken);
        var isOverride = role.TenantId != null &&
                         await db.Roles.AnyAsync(r => r.TenantId == null && r.NormalizedName == role.NormalizedName, cancellationToken);
        return ToDto(role, await GetRolePermissionsAsync(role.Id, cancellationToken), counts.GetValueOrDefault(role.Id), isOverride);
    }

    private static RoleDto ToDto(ApplicationRole role, IReadOnlyList<string> permissions, int userCount, bool isOverride) =>
        new(role.Id, role.Name!, role.Description, role.TenantId is null, isOverride, userCount, permissions);

    private static void ThrowIfFailed(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new ValidationException(result.Errors.Select(e => new ValidationFailure("Name", e.Description)));
    }
}
