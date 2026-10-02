namespace Bonzer.Application.Security;

/// <summary>Custom JWT claim names. Standard ones (sub, name, email, role, jti) use JwtRegisteredClaimNames.</summary>
public static class AppClaimTypes
{
    public const string TenantId = "tenant_id";
    public const string BranchId = "branch_id";
    public const string UserType = "user_type";
    public const string StaffId = "staff_id";
    public const string GuardianId = "guardian_id";
    public const string StudentId = "student_id";
    public const string Role = "role";
    public const string Permission = "permission";

    /// <summary>Hash of the user's security stamp. Changes on password reset, role change or "log out everywhere".</summary>
    public const string SecurityStamp = "sstamp";

    /// <summary>Present while the user must change a temporary password; only auth endpoints are allowed.</summary>
    public const string PasswordChangeRequired = "pwd_change";
}
