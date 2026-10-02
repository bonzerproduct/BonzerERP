using Bonzer.Application.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bonzer.Infrastructure.Persistence.Seed;

/// <summary>
/// Startup tasks: apply migrations (optional), sync built-in roles and permissions, create the platform
/// super admin and, in development, a demo school. Every step is idempotent and safe to run on each start.
/// </summary>
public sealed class DbInitializer(
    BonzerDbContext db,
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IPermissionService permissionService,
    IConfiguration configuration,
    TimeProvider clock,
    ILogger<DbInitializer> logger)
{
    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        if (configuration.GetValue("Database:MigrateOnStartup", false))
        {
            logger.LogInformation("Applying database migrations");
            await db.Database.MigrateAsync(cancellationToken);
        }

        await SeedRolesAsync(cancellationToken);
        await SeedSuperAdminAsync();

        if (configuration.GetValue("Seed:DemoData", false))
            await DemoDataSeeder.SeedAsync(db, userManager, configuration, clock, logger, cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken)
    {
        foreach (var (roleName, permissions) in AppRoles.DefaultPermissions)
        {
            var description = AppRoles.Descriptions.GetValueOrDefault(roleName);
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName && r.TenantId == null, cancellationToken);
            if (role is null)
            {
                role = new ApplicationRole { Name = roleName, IsSystem = true, Description = description };
                EnsureSucceeded(await roleManager.CreateAsync(role), $"create role {roleName}");
                logger.LogInformation("Created built-in role {Role}", roleName);
            }
            role.Description = description;

            // Built-in roles are defined in code (AppRoles) and kept exactly in sync. Schools customise by
            // creating their own role with the same name, which overrides the built-in one for them.
            var existing = await db.RoleClaims
                .Where(c => c.RoleId == role.Id && c.ClaimType == AppClaimTypes.Permission).ToListAsync(cancellationToken);
            var target = permissions.ToHashSet();
            var missing = target.Where(p => existing.All(c => c.ClaimValue != p)).ToList();
            var extra = existing.Where(c => !target.Contains(c.ClaimValue!)).ToList();

            db.RoleClaims.RemoveRange(extra);
            db.RoleClaims.AddRange(missing.Select(p => new IdentityRoleClaim<Guid>
            {
                RoleId = role.Id, ClaimType = AppClaimTypes.Permission, ClaimValue = p
            }));
            await db.SaveChangesAsync(cancellationToken);

            if (missing.Count > 0 || extra.Count > 0)
            {
                await permissionService.InvalidateRoleAsync(null, roleName, cancellationToken);
                logger.LogInformation("Synced role {Role}: +{Added} -{Removed} permissions", roleName, missing.Count, extra.Count);
            }
        }

        // Custom (tenant) roles may still hold permissions that were renamed or removed from the catalogue.
        var known = Permissions.All.ToList();
        var stale = await db.RoleClaims
            .Where(c => c.ClaimType == AppClaimTypes.Permission && !known.Contains(c.ClaimValue!))
            .ToListAsync(cancellationToken);
        if (stale.Count > 0)
        {
            db.RoleClaims.RemoveRange(stale);
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Removed {Count} obsolete permission grants from custom roles", stale.Count);
        }
    }

    private async Task SeedSuperAdminAsync()
    {
        var email = configuration["Seed:SuperAdmin:Email"];
        var password = configuration["Seed:SuperAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("Seed:SuperAdmin is not configured; skipping platform administrator creation");
            return;
        }

        if (await userManager.FindByNameAsync("superadmin") is not null) return;

        var admin = new ApplicationUser
        {
            UserName = "superadmin",
            Email = email,
            EmailConfirmed = true,
            FirstName = "Platform",
            LastName = "Administrator",
            UserType = UserType.PlatformAdmin,
            MustChangePassword = true
        };
        EnsureSucceeded(await userManager.CreateAsync(admin, password), "create super admin");
        EnsureSucceeded(await userManager.AddToRoleAsync(admin, AppRoles.SuperAdmin), "assign SuperAdmin role");
        logger.LogInformation("Created platform administrator {Email}", email);
    }

    internal static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to {action}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
    }
}
