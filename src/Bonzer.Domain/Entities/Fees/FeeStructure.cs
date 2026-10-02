namespace Bonzer.Domain.Entities.Fees;

/// <summary>
/// Fee plan for a year, applied to a whole program or one class level. Replaces the overlapping
/// "fee_structures", "class_fee_map" and "fee_installments" tables with one consistent model:
/// structure -> installments (due dates, late fee rules) -> items (fee head amount per installment).
/// </summary>
public class FeeStructure : BranchEntity
{
    public Guid AcademicYearId { get; set; }
    public Guid? ProgramId { get; set; }
    public Guid? ClassLevelId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public AcademicYear AcademicYear { get; set; } = null!;
    public AcademicProgram? Program { get; set; }
    public ClassLevel? ClassLevel { get; set; }
    public ICollection<FeeInstallment> Installments { get; set; } = new List<FeeInstallment>();
    public ICollection<FeeStructureItem> Items { get; set; } = new List<FeeStructureItem>();
}

public class FeeInstallment : TenantEntity
{
    public Guid FeeStructureId { get; set; }
    public int InstallmentNo { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public int GraceDays { get; set; }

    public LateFeeType LateFeeType { get; set; } = LateFeeType.None;
    public decimal LateFeeValue { get; set; }
    public decimal? LateFeeCap { get; set; }

    public FeeStructure FeeStructure { get; set; } = null!;
}

public class FeeStructureItem : TenantEntity
{
    public Guid FeeStructureId { get; set; }
    public Guid FeeHeadId { get; set; }
    public Guid InstallmentId { get; set; }
    public decimal Amount { get; set; }

    public FeeStructure FeeStructure { get; set; } = null!;
    public FeeHead FeeHead { get; set; } = null!;
    public FeeInstallment Installment { get; set; } = null!;
}
