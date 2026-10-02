using Microsoft.AspNetCore.Identity;

namespace Bonzer.Infrastructure.Persistence.Configurations.Identity;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> b)
    {
        b.ToTable("users");
        b.Property(x => x.FirstName).HasMaxLength(100);
        b.Property(x => x.LastName).HasMaxLength(100);
        b.Property(x => x.PhoneNumber).HasMaxLength(20);
        b.Property(x => x.ProfilePhotoUrl).HasMaxLength(1000);
        b.Property(x => x.UserType).HasMaxLength(50);
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.TenantId);
        b.HasIndex(x => x.PhoneNumber);
    }
}

internal sealed class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> b)
    {
        b.ToTable("roles");
        b.Property(x => x.Description).HasMaxLength(250);
        b.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);

        // Identity makes role names globally unique; make them unique per tenant instead.
        b.HasIndex(x => x.NormalizedName).HasDatabaseName("RoleNameIndex").IsUnique(false);
        b.HasIndex(x => new { x.TenantId, x.NormalizedName }, "ux_roles_tenant_name").HasDatabaseName("ux_roles_tenant_name").IsUnique().AreNullsDistinct(false);
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> b) => b.ToTable("user_roles");
}

internal sealed class UserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> b) => b.ToTable("user_claims");
}

internal sealed class RoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> b)
    {
        b.ToTable("role_claims");
        b.HasIndex(x => new { x.RoleId, x.ClaimType });
    }
}

internal sealed class UserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> b) => b.ToTable("user_logins");
}

internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> b) => b.ToTable("user_tokens");
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.Property(x => x.TokenHash).HasMaxLength(64);
        b.Property(x => x.ReplacedByTokenHash).HasMaxLength(64);
        b.Property(x => x.CreatedByIp).HasMaxLength(45);
        b.Property(x => x.RevokedByIp).HasMaxLength(45);
        b.Property(x => x.UserAgent).HasMaxLength(500);
        b.Property(x => x.ReasonRevoked).HasMaxLength(100);
        b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.HasIndex(x => new { x.UserId, x.RevokedAt });
    }
}

internal sealed class UserBranchAccessConfiguration : IEntityTypeConfiguration<UserBranchAccess>
{
    public void Configure(EntityTypeBuilder<UserBranchAccess> b)
    {
        b.ToTable("user_branch_access");
        b.HasKey(x => new { x.UserId, x.BranchId });
        b.HasOne(x => x.User).WithMany(u => u.BranchAccess).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Branch).WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
    }
}
