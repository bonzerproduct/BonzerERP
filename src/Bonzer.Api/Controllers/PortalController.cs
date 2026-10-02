using Bonzer.Api.Authorization;
using Bonzer.Application.Features.Students;
using Bonzer.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace Bonzer.Api.Controllers;

/// <summary>
/// Parent / student self-service (mobile app). Data is scoped to the signed-in person via the
/// guardian_id / student_id claims, never by ids passed from the client.
/// </summary>
public sealed class PortalController(IStudentService students) : ApiControllerBase
{
    [HttpGet("children")]
    [HasPermission(Permissions.Portal.Access)]
    public async Task<ActionResult<IReadOnlyList<StudentListItemDto>>> MyChildren(CancellationToken cancellationToken) =>
        Ok(await students.GetMyChildrenAsync(cancellationToken));
}
