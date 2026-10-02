namespace Bonzer.Domain.Entities.Academics;

public class Subject : BranchEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public SubjectType Type { get; set; } = SubjectType.Core;

    /// <summary>Credit hours for colleges / universities; null for schools.</summary>
    public decimal? Credits { get; set; }
    public bool IsActive { get; set; } = true;
}
