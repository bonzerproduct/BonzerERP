using Bonzer.Application.Common.Exceptions;
using Bonzer.Application.Common.Interfaces;
using Bonzer.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bonzer.Api.Controllers;

public sealed class AuthController(IAuthService auth, ICurrentUser currentUser) : ApiControllerBase
{
    /// <summary>Sign in with username, email or phone and receive an access + refresh token pair.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await auth.LoginAsync(request, cancellationToken));

    /// <summary>Exchange a refresh token for a new token pair (the old refresh token stops working).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken) =>
        Ok(await auth.RefreshAsync(request, cancellationToken));

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await auth.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    /// <summary>Profile, roles and effective permissions of the signed-in user (drives the UI menu).</summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> Me(CancellationToken cancellationToken) =>
        Ok(await auth.GetProfileAsync(RequireUserId(), cancellationToken));

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await auth.ChangePasswordAsync(RequireUserId(), request, cancellationToken);
        return NoContent();
    }

    private Guid RequireUserId() => currentUser.UserId ?? throw new UnauthorizedException("Not signed in.");
}
