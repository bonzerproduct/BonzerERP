namespace Bonzer.Application.Features.AcademicYears;

public sealed record AcademicYearDto(
    Guid Id, Guid BranchId, string Name, DateOnly StartDate, DateOnly EndDate,
    bool IsCurrent, AcademicYearStatus Status, bool IsAttendanceLocked);

public sealed record CreateAcademicYearRequest(Guid BranchId, string Name, DateOnly StartDate, DateOnly EndDate, bool IsCurrent);

public interface IAcademicYearService
{
    Task<IReadOnlyList<AcademicYearDto>> GetByBranchAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<AcademicYearDto> CreateAsync(CreateAcademicYearRequest request, CancellationToken cancellationToken = default);
    Task SetCurrentAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class CreateAcademicYearRequestValidator : AbstractValidator<CreateAcademicYearRequest>
{
    public CreateAcademicYearRequestValidator()
    {
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(20);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");
        RuleFor(x => x).Must(x => x.EndDate.DayNumber - x.StartDate.DayNumber <= 731)
            .WithMessage("An academic year cannot be longer than two years.");
    }
}
