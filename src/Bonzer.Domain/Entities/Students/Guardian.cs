namespace Bonzer.Domain.Entities.Students;

/// <summary>
/// Parent / guardian. Tenant-wide (not per branch) so a parent with children in two campuses
/// has one record and one login. Replaces the flattened father_*/mother_*/guardian_* columns
/// and the separate "parent_users" table.
/// </summary>
public class Guardian : TenantEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }
    public string? Occupation { get; set; }
    public string? Qualification { get; set; }
    public decimal? AnnualIncome { get; set; }

    [Encrypted]
    public string? NationalIdNumber { get; set; }

    public Address Address { get; set; } = new();

    /// <summary>Parent portal / mobile app login.</summary>
    public Guid? UserId { get; set; }

    public ICollection<StudentGuardian> Students { get; set; } = new List<StudentGuardian>();

    public string FullName => string.IsNullOrWhiteSpace(LastName) ? FirstName : $"{FirstName} {LastName}";
}
