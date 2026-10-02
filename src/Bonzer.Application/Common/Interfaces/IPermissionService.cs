namespace Bonzer.Application.Common.Interfaces;

/// <summary>
/// Resolves role -> permissions from the database (cached in Redis). Permissions are not baked
/// into the JWT, so changing a role's permissions takes effect without users logging in again.
/// </summary>
public interface IPermissionService
{
    Task<IReadOnlySet<string>> GetPermissionsAsync(Guid? tenantId, IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    Task InvalidateRoleAsync(Guid? tenantId, string role, CancellationToken cancellationToken = default);
}
