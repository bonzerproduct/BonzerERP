using Bonzer.Api.Authorization;
using Bonzer.Application.Common.Exceptions;
using Bonzer.Application.Common.Models;
using Bonzer.Application.Features.Employees;
using Bonzer.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bonzer.Api.Controllers;

/// <summary>Employee master for all teaching and non-teaching staff: profile, documents, login and roles.</summary>
public sealed class StaffController(IEmployeeService employees, IAuthorizationService authorization) : ApiControllerBase
{
    /// <summary>Search employees of a branch (by name, code, phone or designation) with filters.</summary>
    [HttpGet]
    [HasPermission(Permissions.Staff.View)]
    public async Task<ActionResult<PagedResult<StaffListItemDto>>> Get([FromQuery] StaffQuery query, CancellationToken ct) =>
        Ok(await employees.GetPagedAsync(query, ct));

    /// <summary>Full employee record. Aadhaar, PAN and bank account are masked without staff.sensitive.view.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission(Permissions.Staff.View)]
    public async Task<ActionResult<StaffDetailDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await employees.GetByIdAsync(id, ct));

    /// <summary>Add an employee, optionally with qualifications, experience and a login. The employee code is auto-generated when empty.</summary>
    [HttpPost]
    [HasPermission(Permissions.Staff.Create)]
    public async Task<ActionResult<StaffCreatedDto>> Create(CreateStaffRequest request, CancellationToken ct)
    {
        if (request.Login is not null)
            await EnsurePermissionAsync(Permissions.Staff.ManageLogin);
        var created = await employees.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Update the profile. Send masked sensitive values back unchanged to keep them.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.Staff.Edit)]
    public async Task<IActionResult> Update(Guid id, StaffProfile profile, CancellationToken ct)
    {
        await employees.UpdateAsync(id, profile, ct);
        return NoContent();
    }

    /// <summary>Change employment status (on leave, notice, suspended, resigned, terminated, retired, re-join).</summary>
    [HttpPut("{id:guid}/status")]
    [HasPermission(Permissions.Staff.Edit)]
    public async Task<IActionResult> ChangeStatus(Guid id, ChangeStaffStatusRequest request, CancellationToken ct)
    {
        await employees.ChangeStatusAsync(id, request, ct);
        return NoContent();
    }

    /// <summary>Delete a record entered by mistake. Employees with history must be exited via status instead.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.Staff.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await employees.DeleteAsync(id, ct);
        return NoContent();
    }

    // ---- Qualifications ----

    [HttpPost("{id:guid}/qualifications")]
    [HasPermission(Permissions.Staff.Edit)]
    public async Task<ActionResult<QualificationDto>> AddQualification(Guid id, QualificationInput input, CancellationToken ct) =>
        Ok(await employees.AddQualificationAsync(id, input, ct));

    [HttpDelete("{id:guid}/qualifications/{qualificationId:guid}")]
    [HasPermission(Permissions.Staff.Edit)]
    public async Task<IActionResult> RemoveQualification(Guid id, Guid qualificationId, CancellationToken ct)
    {
        await employees.RemoveQualificationAsync(id, qualificationId, ct);
        return NoContent();
    }

    // ---- Experience ----

    [HttpPost("{id:guid}/experiences")]
    [HasPermission(Permissions.Staff.Edit)]
    public async Task<ActionResult<ExperienceDto>> AddExperience(Guid id, ExperienceInput input, CancellationToken ct) =>
        Ok(await employees.AddExperienceAsync(id, input, ct));

    [HttpDelete("{id:guid}/experiences/{experienceId:guid}")]
    [HasPermission(Permissions.Staff.Edit)]
    public async Task<IActionResult> RemoveExperience(Guid id, Guid experienceId, CancellationToken ct)
    {
        await employees.RemoveExperienceAsync(id, experienceId, ct);
        return NoContent();
    }

    // ---- Documents (upload the file to storage first, then register its URL) ----

    [HttpPost("{id:guid}/documents")]
    [HasPermission(Permissions.Staff.Edit)]
    public async Task<ActionResult<StaffDocumentDto>> AddDocument(Guid id, StaffDocumentInput input, CancellationToken ct) =>
        Ok(await employees.AddDocumentAsync(id, input, ct));

    [HttpPut("{id:guid}/documents/{documentId:guid}/verify")]
    [HasPermission(Permissions.Staff.Edit)]
    public async Task<IActionResult> VerifyDocument(Guid id, Guid documentId, CancellationToken ct)
    {
        await employees.VerifyDocumentAsync(id, documentId, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    [HasPermission(Permissions.Staff.Edit)]
    public async Task<IActionResult> RemoveDocument(Guid id, Guid documentId, CancellationToken ct)
    {
        await employees.RemoveDocumentAsync(id, documentId, ct);
        return NoContent();
    }

    // ---- Login & roles ----

    /// <summary>Create a login. The temporary password (if generated) is returned only in this response.</summary>
    [HttpPost("{id:guid}/login")]
    [HasPermission(Permissions.Staff.ManageLogin)]
    public async Task<ActionResult<StaffLoginDto>> CreateLogin(Guid id, StaffLoginRequest request, CancellationToken ct) =>
        Ok(await employees.CreateLoginAsync(id, request, ct));

    [HttpPut("{id:guid}/roles")]
    [HasPermission(Permissions.Staff.ManageLogin)]
    public async Task<IActionResult> SetRoles(Guid id, SetStaffRolesRequest request, CancellationToken ct)
    {
        await employees.SetRolesAsync(id, request, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-password")]
    [HasPermission(Permissions.Staff.ManageLogin)]
    public async Task<ActionResult<object>> ResetPassword(Guid id, CancellationToken ct) =>
        Ok(new { temporaryPassword = await employees.ResetPasswordAsync(id, ct) });

    private async Task EnsurePermissionAsync(string permission)
    {
        var result = await authorization.AuthorizeAsync(User, HasPermissionAttribute.PolicyPrefix + permission);
        if (!result.Succeeded)
            throw new ForbiddenAccessException($"Creating a login requires the '{permission}' permission.");
    }
}
