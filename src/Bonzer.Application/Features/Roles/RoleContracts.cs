using Bonzer.Application.Security;

namespace Bonzer.Application.Features.Roles;

public sealed record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsTenantOverride,
    int UserCount,
    IReadOnlyList<string> Permissions);

public sealed record PermissionDto(string Code, string Name);

public sealed record PermissionGroupDto(string Module, IReadOnlyList<PermissionDto> Permissions);

/// <param name="CopyFromRoleId">Start from another role's permissions (e.g. clone "Teacher" into "Senior Teacher").</param>
public sealed record CreateRoleRequest(string Name, string? Description, IReadOnlyList<string>? Permissions, Guid? CopyFromRoleId);

public sealed record UpdateRoleRequest(string Name, string? Description, IReadOnlyList<string> Permissions);

/// <summary>
/// Role administration for a tenant. Built-in roles are shared by all tenants and are read-only here;
/// to change one for a single school, create a tenant role with the same name (it overrides the built-in).
/// </summary>
public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<RoleDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    IReadOnlyList<PermissionGroupDto> GetPermissionCatalogue();
    Task<RoleDto> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);
    Task<RoleDto> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class CreateRoleRequestValidator : AbstractValidator<CreateRoleRequest>
{
    public CreateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64).Matches(@"^[A-Za-z][A-Za-z0-9 _-]*$")
            .WithMessage("Role name must start with a letter and contain only letters, digits, spaces, '-' or '_'.");
        RuleFor(x => x.Description).MaximumLength(250);
        RuleForEach(x => x.Permissions).Must(BeKnownTenantPermission).WithMessage("Unknown permission '{PropertyValue}'.");
    }

    internal static bool BeKnownTenantPermission(string permission) =>
        Security.Permissions.All.Contains(permission) && !Security.Permissions.IsPlatformOnly(permission);
}

public sealed class UpdateRoleRequestValidator : AbstractValidator<UpdateRoleRequest>
{
    public UpdateRoleRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64).Matches(@"^[A-Za-z][A-Za-z0-9 _-]*$");
        RuleFor(x => x.Description).MaximumLength(250);
        RuleFor(x => x.Permissions).NotNull();
        RuleForEach(x => x.Permissions).Must(CreateRoleRequestValidator.BeKnownTenantPermission)
            .WithMessage("Unknown permission '{PropertyValue}'.");
    }
}
