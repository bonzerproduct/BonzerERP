namespace Bonzer.Domain.Entities.Certificates;

/// <summary>Printable template (HTML with {{placeholders}}) per certificate type and branch.</summary>
public class CertificateTemplate : BranchEntity
{
    public CertificateType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}

/// <summary>
/// Any issued certificate: TC, bonafide, character, migration... Replaces the separate
/// "tc_certificates" and "bonafide_certificates" tables; type-specific fields (conduct,
/// fees cleared, valid-from/to...) are kept as a JSON snapshot in <see cref="Data"/>.
/// </summary>
public class IssuedCertificate : BranchEntity
{
    public CertificateType Type { get; set; }
    public Guid? TemplateId { get; set; }
    public Guid StudentId { get; set; }
    public Guid? EnrollmentId { get; set; }
    public Guid AcademicYearId { get; set; }

    public string CertificateNo { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public string? Purpose { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }

    /// <summary>JSON snapshot of the printed values, so reprints match the original.</summary>
    public string? Data { get; set; }

    public bool IsDuplicate { get; set; }
    public Guid? OriginalCertificateId { get; set; }
    public CertificateStatus Status { get; set; } = CertificateStatus.Issued;
    public Guid? IssuedBy { get; set; }
    public string? Remarks { get; set; }

    public CertificateTemplate? Template { get; set; }
    public Student Student { get; set; } = null!;
    public StudentEnrollment? Enrollment { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
    public IssuedCertificate? OriginalCertificate { get; set; }
}
