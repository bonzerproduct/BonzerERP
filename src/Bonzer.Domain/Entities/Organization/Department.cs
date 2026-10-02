namespace Bonzer.Domain.Entities.Organization;

public class Department : BranchEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public DepartmentType Type { get; set; } = DepartmentType.Academic;
    public Guid? HeadStaffId { get; set; }
    public bool IsActive { get; set; } = true;
}
