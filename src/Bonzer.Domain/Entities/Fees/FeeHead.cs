namespace Bonzer.Domain.Entities.Fees;

/// <summary>Tuition Fee, Admission Fee, Transport, Lab, Exam Fee...</summary>
public class FeeHead : BranchEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? AccountCode { get; set; }
    public FeeFrequency Frequency { get; set; } = FeeFrequency.Annual;
    public bool IsRefundable { get; set; }
    public bool IsOptional { get; set; }
    public decimal TaxPercent { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
