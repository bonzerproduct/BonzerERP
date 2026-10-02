namespace Bonzer.Domain.Entities.Organization;

/// <summary>
/// Tenant-configurable dropdown values (religion, caste category, blood group, medium, leave type,
/// document type...). Lets each school change master data without code changes or migrations.
/// </summary>
public class LookupValue : TenantEntity
{
    /// <summary>Group key, e.g. "Religion", "Category", "BloodGroup", "LeaveType". See LookupCategories.</summary>
    public string Category { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Seeded by the platform; can be hidden but not deleted.</summary>
    public bool IsSystem { get; set; }
}

public static class LookupCategories
{
    public const string Religion = "Religion";
    public const string Caste = "Caste";
    public const string Category = "Category";
    public const string BloodGroup = "BloodGroup";
    public const string Nationality = "Nationality";
    public const string MotherTongue = "MotherTongue";
    public const string LeaveType = "LeaveType";
    public const string StudentDocumentType = "StudentDocumentType";
    public const string ConcessionCategory = "ConcessionCategory";
    public const string Designation = "Designation";
    public const string StaffDocumentType = "StaffDocumentType";
}
