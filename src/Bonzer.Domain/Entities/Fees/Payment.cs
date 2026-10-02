namespace Bonzer.Domain.Entities.Fees;

/// <summary>Money received. One payment can settle several invoices / items via allocations.</summary>
public class Payment : BranchEntity, IHasRowVersion
{
    public Guid StudentId { get; set; }

    public string ReceiptNo { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Completed;

    public string? TransactionReference { get; set; }
    public string? BankName { get; set; }
    public string? ChequeNo { get; set; }
    public DateOnly? ChequeDate { get; set; }

    // Online payments (Razorpay, PayU, Stripe...)
    public string? GatewayName { get; set; }
    public string? GatewayOrderId { get; set; }
    public string? GatewayPaymentId { get; set; }

    public Guid? CollectedByUserId { get; set; }
    public string? Remarks { get; set; }

    public uint RowVersion { get; set; }

    public Student Student { get; set; } = null!;
    public ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
}

/// <summary>How much of a payment went to which invoice (item). Replaces "payment_fee_lines".</summary>
public class PaymentAllocation : TenantEntity
{
    public Guid PaymentId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? InvoiceItemId { get; set; }
    public decimal Amount { get; set; }

    public Payment Payment { get; set; } = null!;
    public Invoice Invoice { get; set; } = null!;
    public InvoiceItem? InvoiceItem { get; set; }
}
