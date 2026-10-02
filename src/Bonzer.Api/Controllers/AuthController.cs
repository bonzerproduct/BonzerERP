using Bonzer.Application.Common.Exceptions;
using Bonzer.Application.Common.Interfaces;
using Bonzer.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bonzer.Api.Controllers;

/// <summary>Sign in / out, tokens, own profile, password change and recovery, own sessions.</summary>
public sealed class AuthController(IAuthService auth, ICurrentUser currentUser) : ApiControllerBase
{
    /// <summary>Sign in with username, email or phone and receive an access + refresh token pair.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct) =>
        Ok(await auth.LoginAsync(request, ct));

    /// <summary>Exchange a refresh token for a new token pair (the old refresh token stops working).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest request, CancellationToken ct) =>
        Ok(await auth.RefreshAsync(request, ct));

    /// <summary>Sign out of this device.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken ct)
    {
        await auth.LogoutAsync(request, ct);
        return NoContent();
    }

    /// <summary>Sign out of every device (also invalidates access tokens already issued).</summary>
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        await auth.LogoutAllAsync(UserId, ct);
        return NoContent();
    }

    /// <summary>Profile, roles and effective permissions of the signed-in user (drives the UI menu).</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> Me(CancellationToken ct) => Ok(await auth.GetProfileAsync(UserId, ct));

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMe(UpdateProfileRequest request, CancellationToken ct) =>
        Ok(await auth.UpdateProfileAsync(UserId, request, ct));

    /// <summary>Change own password. Other devices are signed out; this response carries new tokens.</summary>
    [HttpPost("change-password")]
    public async Task<ActionResult<AuthResponse>> ChangePassword(ChangePasswordRequest request, CancellationToken ct) =>
        Ok(await auth.ChangePasswordAsync(UserId, request, ct));

    /// <summary>Send a reset link (email) or a 6-digit code (SMS). Always returns 202 so accounts cannot be discovered.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<MessageResponse>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        await auth.ForgotPasswordAsync(request, ct);
        return Accepted(new MessageResponse("If the account exists, reset instructions have been sent to its email or phone."));
    }

    /// <summary>Set a new password using the emailed token or the SMS code.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        await auth.ResetPasswordAsync(request, ct);
        return NoContent();
    }

    /// <summary>Devices / browsers where this user is signed in.</summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyList<SessionDto>>> Sessions(CancellationToken ct) =>
        Ok(await auth.GetSessionsAsync(UserId, ct));

    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken ct)
    {
        await auth.RevokeSessionAsync(UserId, sessionId, ct);
        return NoContent();
    }

    private Guid UserId => currentUser.UserId ?? throw new UnauthorizedException("Not signed in.");
}
