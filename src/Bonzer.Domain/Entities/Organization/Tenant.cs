namespace Bonzer.Domain.Entities.Organization;

/// <summary>
/// A customer of the platform: a single school, a trust / group running many campuses,
/// a university or a coaching chain. Every business row carries a TenantId (replaces "school_groups").
/// </summary>
public class Tenant : AuditableEntity, ISoftDeletable
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Short unique code, e.g. "DPS". Used as a prefix for usernames and document numbers.</summary>
    public string Code { get; set; } = string.Empty;

    public string? LegalName { get; set; }
    public string? Subdomain { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? LogoUrl { get; set; }

    public SubscriptionPlan Plan { get; set; } = SubscriptionPlan.Trial;
    public DateOnly? SubscriptionEndsOn { get; set; }

    public string TimeZone { get; set; } = "Asia/Kolkata";
    public string Currency { get; set; } = "INR";
    public string Locale { get; set; } = "en-IN";

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    public ICollection<Branch> Branches { get; set; } = new List<Branch>();
}
