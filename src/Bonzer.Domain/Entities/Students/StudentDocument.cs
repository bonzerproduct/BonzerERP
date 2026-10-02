namespace Bonzer.Domain.Entities.Students;

/// <summary>Uploaded document (birth certificate, previous TC, caste certificate...). File lives in blob storage.</summary>
public class StudentDocument : TenantEntity
{
    public Guid StudentId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }

    public bool IsVerified { get; set; }
    public Guid? VerifiedBy { get; set; }
    public DateTime? VerifiedAt { get; set; }

    public Student Student { get; set; } = null!;
}
