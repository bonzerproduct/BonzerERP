namespace Bonzer.Domain.Entities.Students;

public class StudentPreviousEducation : TenantEntity
{
    public Guid StudentId { get; set; }
    public string InstitutionName { get; set; } = string.Empty;
    public string? Board { get; set; }
    public string? ClassOrProgram { get; set; }
    public int? PassingYear { get; set; }
    public string? Result { get; set; }
    public decimal? Percentage { get; set; }
    public string? TransferCertificateNo { get; set; }
    public string? ContactNumber { get; set; }

    public Student Student { get; set; } = null!;
}
