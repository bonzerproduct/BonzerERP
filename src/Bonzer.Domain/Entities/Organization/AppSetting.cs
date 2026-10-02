namespace Bonzer.Domain.Entities.Organization;

/// <summary>
/// Key/value settings per tenant, optionally overridden per branch
/// (e.g. "attendance.editWindowDays" = "2", "fees.allowPartialPayment" = "true").
/// </summary>
public class AppSetting : TenantEntity
{
    /// <summary>Null = applies to every branch of the tenant.</summary>
    public Guid? BranchId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
