using Bonzer.Infrastructure.Persistence.Configurations.Organization;

namespace Bonzer.Infrastructure.Persistence.Configurations.Fees;

internal sealed class FeeHeadConfiguration : IEntityTypeConfiguration<FeeHead>
{
    public void Configure(EntityTypeBuilder<FeeHead> b)
    {
        b.ToTable("fee_heads");
        b.Property(x => x.Name).HasMaxLength(150);
        b.Property(x => x.Code).HasMaxLength(30);
        b.Property(x => x.AccountCode).HasMaxLength(50);
        b.Property(x => x.TaxPercent).HasPrecision(5, 2);
        b.HasIndex(x => new { x.BranchId, x.Code }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class FeeStructureConfiguration : IEntityTypeConfiguration<FeeStructure>
{
    public void Configure(EntityTypeBuilder<FeeStructure> b)
    {
        b.ToTable("fee_structures");
        b.Property(x => x.Name).HasMaxLength(150);
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasIndex(x => new { x.BranchId, x.AcademicYearId });
    }
}

internal sealed class FeeInstallmentConfiguration : IEntityTypeConfiguration<FeeInstallment>
{
    public void Configure(EntityTypeBuilder<FeeInstallment> b)
    {
        b.ToTable("fee_installments");
        b.Property(x => x.Name).HasMaxLength(100);
        b.HasOne(x => x.FeeStructure).WithMany(s => s.Installments).HasForeignKey(x => x.FeeStructureId);
        b.HasIndex(x => new { x.FeeStructureId, x.InstallmentNo }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class FeeStructureItemConfiguration : IEntityTypeConfiguration<FeeStructureItem>
{
    public void Configure(EntityTypeBuilder<FeeStructureItem> b)
    {
        b.ToTable("fee_structure_items", t => t.HasCheckConstraint("ck_fee_structure_items_amount", "amount >= 0"));
        b.HasOne(x => x.FeeStructure).WithMany(s => s.Items).HasForeignKey(x => x.FeeStructureId);
        b.HasIndex(x => new { x.FeeStructureId, x.FeeHeadId, x.InstallmentId }).IsUnique().HasFilter(Sql.NotDeleted);
    }
}

internal sealed class FeeConcessionConfiguration : IEntityTypeConfiguration<FeeConcession>
{
    public void Configure(EntityTypeBuilder<FeeConcession> b)
    {
        b.ToTable("fee_concessions");
        b.Property(x => x.ConcessionCategory).HasMaxLength(50);
        b.Property(x => x.Reason).HasMaxLength(500);
        b.HasIndex(x => new { x.StudentId, x.AcademicYearId });
    }
}

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> b)
    {
        b.ToTable("invoices", t => t.HasCheckConstraint("ck_invoices_paid", "paid_amount >= 0 AND paid_amount <= total_amount"));
        b.Property(x => x.InvoiceNo).HasMaxLength(50);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.HasIndex(x => new { x.TenantId, x.InvoiceNo }).IsUnique().HasFilter(Sql.NotDeleted);
        b.HasIndex(x => new { x.StudentId, x.Status });
        b.HasIndex(x => new { x.BranchId, x.DueDate, x.Status });
    }
}

internal sealed class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> b)
    {
        b.ToTable("invoice_items");
        b.Property(x => x.Description).HasMaxLength(200);
        b.HasOne(x => x.Invoice).WithMany(i => i.Items).HasForeignKey(x => x.InvoiceId);
    }
}

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("payments", t => t.HasCheckConstraint("ck_payments_amount", "amount > 0"));
        b.Property(x => x.ReceiptNo).HasMaxLength(50);
        b.Property(x => x.TransactionReference).HasMaxLength(100);
        b.Property(x => x.BankName).HasMaxLength(100);
        b.Property(x => x.ChequeNo).HasMaxLength(30);
        b.Property(x => x.GatewayName).HasMaxLength(50);
        b.Property(x => x.GatewayOrderId).HasMaxLength(100);
        b.Property(x => x.GatewayPaymentId).HasMaxLength(100);
        b.Property(x => x.Remarks).HasMaxLength(500);

        b.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CollectedByUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => new { x.TenantId, x.ReceiptNo }).IsUnique().HasFilter(Sql.NotDeleted);
        // Idempotency for payment-gateway webhooks.
        b.HasIndex(x => new { x.GatewayName, x.GatewayPaymentId }).IsUnique().HasFilter("gateway_payment_id IS NOT NULL");
        b.HasIndex(x => new { x.BranchId, x.PaymentDate });
    }
}

internal sealed class PaymentAllocationConfiguration : IEntityTypeConfiguration<PaymentAllocation>
{
    public void Configure(EntityTypeBuilder<PaymentAllocation> b)
    {
        b.ToTable("payment_allocations", t => t.HasCheckConstraint("ck_payment_allocations_amount", "amount > 0"));
        b.HasOne(x => x.Payment).WithMany(p => p.Allocations).HasForeignKey(x => x.PaymentId);
    }
}
