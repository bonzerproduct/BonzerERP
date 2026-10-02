using Bonzer.Domain.Entities.Organization;

namespace Bonzer.Application.Features.Departments;

public sealed class DepartmentService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    IValidator<SaveDepartmentRequest> validator) : IDepartmentService
{
    public async Task<IReadOnlyList<DepartmentDto>> GetByBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        currentUser.EnsureBranchAccess(branchId);
        return await Project(db.Departments.AsNoTracking().Where(d => d.BranchId == branchId).OrderBy(d => d.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentDto> CreateAsync(SaveDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        currentUser.EnsureBranchAccess(request.BranchId);
        if (!await db.Branches.AnyAsync(b => b.Id == request.BranchId, cancellationToken))
            throw new NotFoundException("Branch", request.BranchId);
        await EnsureUniqueAsync(request, null, cancellationToken);
        await EnsureHeadAsync(request, cancellationToken);

        var department = new Department { BranchId = request.BranchId };
        Apply(department, request);
        db.Departments.Add(department);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(department.Id, cancellationToken);
    }

    public async Task<DepartmentDto> UpdateAsync(Guid id, SaveDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        var department = await db.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
                         ?? throw new NotFoundException(nameof(Department), id);
        currentUser.EnsureBranchAccess(department.BranchId);
        if (request.BranchId != department.BranchId)
            throw new BusinessRuleException("A department cannot be moved to another branch.");
        await EnsureUniqueAsync(request, id, cancellationToken);
        await EnsureHeadAsync(request, cancellationToken);

        Apply(department, request);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var department = await db.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
                         ?? throw new NotFoundException(nameof(Department), id);
        currentUser.EnsureBranchAccess(department.BranchId);

        if (await db.Staff.AnyAsync(s => s.DepartmentId == id, cancellationToken))
            throw new BusinessRuleException("Move the staff of this department to another department first.");

        db.Departments.Remove(department);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<DepartmentDto> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await Project(db.Departments.AsNoTracking().Where(d => d.Id == id)).FirstAsync(cancellationToken);

    private IQueryable<DepartmentDto> Project(IQueryable<Department> departments) =>
        departments.Select(d => new DepartmentDto(
            d.Id, d.BranchId, d.Name, d.Code, d.Type, d.HeadStaffId,
            db.Staff.Where(s => s.Id == d.HeadStaffId).Select(s => s.FirstName + " " + (s.LastName ?? "")).FirstOrDefault(),
            d.IsActive,
            db.Staff.Count(s => s.DepartmentId == d.Id)));

    private async Task EnsureUniqueAsync(SaveDepartmentRequest request, Guid? id, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (await db.Departments.AnyAsync(d => d.BranchId == request.BranchId && d.Name == name && d.Id != id, cancellationToken))
            throw new ConflictException($"Department '{name}' already exists in this branch.");
    }

    private async Task EnsureHeadAsync(SaveDepartmentRequest request, CancellationToken cancellationToken)
    {
        if (request.HeadStaffId is { } headId &&
            !await db.Staff.AnyAsync(s => s.Id == headId && s.BranchId == request.BranchId, cancellationToken))
            throw new BusinessRuleException("The head of department must be a staff member of the same branch.");
    }

    private static void Apply(Department department, SaveDepartmentRequest request)
    {
        department.Name = request.Name.Trim();
        department.Code = request.Code?.Trim();
        department.Type = request.Type;
        department.HeadStaffId = request.HeadStaffId;
        department.IsActive = request.IsActive;
    }
}
