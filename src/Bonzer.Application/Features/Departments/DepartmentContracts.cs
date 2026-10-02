namespace Bonzer.Application.Features.Departments;

public sealed record DepartmentDto(
    Guid Id, Guid BranchId, string Name, string? Code, DepartmentType Type, Guid? HeadStaffId, string? HeadStaffName,
    bool IsActive, int StaffCount);

public sealed record SaveDepartmentRequest(Guid BranchId, string Name, string? Code, DepartmentType Type, Guid? HeadStaffId, bool IsActive = true);

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> GetByBranchAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<DepartmentDto> CreateAsync(SaveDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentDto> UpdateAsync(Guid id, SaveDepartmentRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class SaveDepartmentRequestValidator : AbstractValidator<SaveDepartmentRequest>
{
    public SaveDepartmentRequestValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Code).MaximumLength(30);
    }
}
