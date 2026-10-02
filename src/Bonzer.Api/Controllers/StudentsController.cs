using Bonzer.Api.Authorization;
using Bonzer.Application.Common.Models;
using Bonzer.Application.Features.Students;
using Bonzer.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace Bonzer.Api.Controllers;

public sealed class StudentsController(IStudentService students) : ApiControllerBase
{
    /// <summary>Search and page students of a branch, optionally filtered by year / class / section / status.</summary>
    [HttpGet]
    [HasPermission(Permissions.Students.View)]
    public async Task<ActionResult<PagedResult<StudentListItemDto>>> Get([FromQuery] StudentQuery query,
        CancellationToken cancellationToken) =>
        Ok(await students.GetPagedAsync(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Students.View)]
    public async Task<ActionResult<StudentDetailDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await students.GetByIdAsync(id, cancellationToken));

    /// <summary>Admit a student: creates the student, links / reuses guardians and enrolls into the section.</summary>
    [HttpPost]
    [HasPermission(Permissions.Students.Create)]
    public async Task<IActionResult> Create(CreateStudentRequest request, CancellationToken cancellationToken)
    {
        var id = await students.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Students.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await students.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
