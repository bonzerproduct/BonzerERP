namespace Bonzer.Domain.Entities.HumanResources;

/// <summary>
/// Uploaded HR document: resume, appointment letter, ID proof, degree certificate, police verification,
/// driving licence (drivers), medical fitness... The file lives in blob storage; only the URL is stored.
/// </summary>
public class StaffDocument : TenantEntity
{
    public Guid StaffId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }

    /// <summary>For documents that must be renewed (driving licence, police verification).</summary>
    public DateOnly? ExpiresOn { get; set; }

    public bool IsVerified { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public Staff Staff { get; set; } = null!;
}
