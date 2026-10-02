namespace Bonzer.Domain.Entities.Fees;

/// <summary>Fee demand raised to a student (usually one per installment).</summary>
public class Invoice : BranchEntity, IHasRowVersion
{
    public Guid StudentId { get; set; }
    public Guid? EnrollmentId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid? FeeInstallmentId { get; set; }

    public string InvoiceNo { get; set; } = string.Empty;
    public DateOnly InvoiceDate { get; set; }
    public DateOnly DueDate { get; set; }

    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LateFeeAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal BalanceAmount { get; set; }

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;
    public string? Notes { get; set; }

    public uint RowVersion { get; set; }

    public Student Student { get; set; } = null!;
    public StudentEnrollment? Enrollment { get; set; }
    public AcademicYear AcademicYear { get; set; } = null!;
    public FeeInstallment? FeeInstallment { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}

public class InvoiceItem : TenantEntity
{
    public Guid InvoiceId { get; set; }

    /// <summary>Null for ad-hoc charges (fine, lost ID card...).</summary>
    public Guid? FeeHeadId { get; set; }
    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public Invoice Invoice { get; set; } = null!;
    public FeeHead? FeeHead { get; set; }
}
