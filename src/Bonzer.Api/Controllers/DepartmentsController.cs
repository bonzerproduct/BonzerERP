using Bonzer.Api.Authorization;
using Bonzer.Application.Features.Departments;
using Bonzer.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace Bonzer.Api.Controllers;

public sealed class DepartmentsController(IDepartmentService departments) : ApiControllerBase
{
    [HttpGet]
    [HasPermission(Permissions.Staff.View)]
    public async Task<ActionResult<IReadOnlyList<DepartmentDto>>> GetByBranch([FromQuery] Guid branchId, CancellationToken ct) =>
        Ok(await departments.GetByBranchAsync(branchId, ct));

    [HttpPost]
    [HasPermission(Permissions.Staff.ManageDepartments)]
    public async Task<ActionResult<DepartmentDto>> Create(SaveDepartmentRequest request, CancellationToken ct)
    {
        var created = await departments.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetByBranch), new { branchId = created.BranchId }, created);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Staff.ManageDepartments)]
    public async Task<ActionResult<DepartmentDto>> Update(Guid id, SaveDepartmentRequest request, CancellationToken ct) =>
        Ok(await departments.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Staff.ManageDepartments)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await departments.DeleteAsync(id, ct);
        return NoContent();
    }
}
