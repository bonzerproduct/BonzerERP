using Bonzer.Api.Authorization;
using Bonzer.Application.Features.AcademicYears;
using Bonzer.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace Bonzer.Api.Controllers;

public sealed class AcademicYearsController(IAcademicYearService academicYears) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Academics.View)]
    public async Task<ActionResult<IReadOnlyList<AcademicYearDto>>> GetByBranch([FromQuery] Guid branchId,
        CancellationToken cancellationToken) =>
        Ok(await academicYears.GetByBranchAsync(branchId, cancellationToken));

    [HttpPost]
    [HasPermission(Permissions.Academics.Manage)]
    public async Task<ActionResult<AcademicYearDto>> Create(CreateAcademicYearRequest request, CancellationToken cancellationToken)
    {
        var created = await academicYears.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByBranch), new { branchId = created.BranchId }, created);
    }

    [HttpPut("{id:guid}/set-current")]
    [HasPermission(Permissions.Academics.Manage)]
    public async Task<IActionResult> SetCurrent(Guid id, CancellationToken cancellationToken)
    {
        await academicYears.SetCurrentAsync(id, cancellationToken);
        return NoContent();
    }
}
