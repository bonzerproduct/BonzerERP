using Bonzer.Application.Common.Caching;
using Bonzer.Domain.Entities.Academics;

namespace Bonzer.Application.Features.AcademicYears;

public sealed class AcademicYearService(
    IApplicationDbContext db,
    ICurrentUser currentUser,
    ICacheService cache,
    IValidator<CreateAcademicYearRequest> validator) : IAcademicYearService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(6);

    public async Task<IReadOnlyList<AcademicYearDto>> GetByBranchAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        currentUser.EnsureBranchAccess(branchId);
        await EnsureBranchExistsAsync(branchId, cancellationToken);

        return await cache.GetOrCreateAsync<IReadOnlyList<AcademicYearDto>>(
            CacheKeys.AcademicYears(branchId),
            async ct => await db.AcademicYears.AsNoTracking()
                .Where(a => a.BranchId == branchId)
                .OrderByDescending(a => a.StartDate)
                .Select(a => new AcademicYearDto(a.Id, a.BranchId, a.Name, a.StartDate, a.EndDate, a.IsCurrent, a.Status, a.IsAttendanceLocked))
                .ToListAsync(ct),
            CacheTtl, cancellationToken);
    }

    public async Task<AcademicYearDto> CreateAsync(CreateAcademicYearRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        currentUser.EnsureBranchAccess(request.BranchId);
        await EnsureBranchExistsAsync(request.BranchId, cancellationToken);

        var overlaps = await db.AcademicYears.AnyAsync(a =>
            a.BranchId == request.BranchId && a.StartDate <= request.EndDate && a.EndDate >= request.StartDate, cancellationToken);
        if (overlaps)
            throw new ConflictException("The dates overlap an existing academic year of this branch.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        if (request.IsCurrent)
            await ClearCurrentAsync(request.BranchId, cancellationToken);

        var year = new AcademicYear
        {
            BranchId = request.BranchId,
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsCurrent = request.IsCurrent,
            Status = request.IsCurrent ? AcademicYearStatus.Active : AcademicYearStatus.Planned
        };
        db.AcademicYears.Add(year);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await cache.RemoveAsync(CacheKeys.AcademicYears(request.BranchId), cancellationToken);

        return new AcademicYearDto(year.Id, year.BranchId, year.Name, year.StartDate, year.EndDate, year.IsCurrent, year.Status, year.IsAttendanceLocked);
    }

    public async Task SetCurrentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var year = await db.AcademicYears.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                   ?? throw new NotFoundException(nameof(AcademicYear), id);
        currentUser.EnsureBranchAccess(year.BranchId);
        if (year.IsCurrent) return;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await ClearCurrentAsync(year.BranchId, cancellationToken);
        year.IsCurrent = true;
        year.Status = AcademicYearStatus.Active;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await cache.RemoveAsync(CacheKeys.AcademicYears(year.BranchId), cancellationToken);
    }

    // Runs as its own UPDATE before the new current year is saved: the "one current year per branch"
    // unique index is checked per statement, so the old flag must be cleared first (same transaction).
    private async Task ClearCurrentAsync(Guid branchId, CancellationToken cancellationToken) =>
        await db.AcademicYears
            .Where(a => a.BranchId == branchId && a.IsCurrent)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.IsCurrent, false), cancellationToken);

    private async Task EnsureBranchExistsAsync(Guid branchId, CancellationToken cancellationToken)
    {
        // Branches are tenant-filtered, so this also blocks access to another tenant's branch.
        if (!await db.Branches.AnyAsync(b => b.Id == branchId, cancellationToken))
            throw new NotFoundException("Branch", branchId);
    }
}
