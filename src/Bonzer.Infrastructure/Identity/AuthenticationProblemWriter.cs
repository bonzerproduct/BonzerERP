using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Bonzer.Infrastructure.Identity;

/// <summary>Raised by the per-request session check (deactivated user, changed security stamp).</summary>
public sealed class SessionRevokedException() : SecurityTokenException("Session is no longer valid.");

/// <summary>
/// Replaces the empty 401 of JwtBearer with a ProblemDetails body whose "code" tells the client what to do:
/// <c>token_expired</c> → call /auth/refresh, <c>session_revoked</c> / <c>token_invalid</c> / <c>unauthenticated</c> → sign in.
/// </summary>
internal static class AuthenticationProblemWriter
{
    public static async Task WriteChallengeAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse(); // suppress the default empty response

        var (code, detail, header) = context.AuthenticateFailure switch
        {
            null => (ErrorCodes.Unauthenticated, "Sign in to continue.", "Bearer"),
            SecurityTokenExpiredException => (ErrorCodes.TokenExpired, "The access token has expired. Refresh it and retry.",
                "Bearer error=\"invalid_token\", error_description=\"The token expired\""),
            SessionRevokedException => (ErrorCodes.SessionRevoked,
                "Your session has ended (signed out, password or role changed, or account disabled). Please sign in again.",
                "Bearer error=\"invalid_token\", error_description=\"The session was revoked\""),
            _ => (ErrorCodes.TokenInvalid, "The access token is invalid. Please sign in again.", "Bearer error=\"invalid_token\"")
        };

        var http = context.HttpContext;
        http.Response.StatusCode = StatusCodes.Status401Unauthorized;
        http.Response.Headers.WWWAuthenticate = header;

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = detail,
            Instance = http.Request.Path
        };
        problem.Extensions["code"] = code;

        var writer = http.RequestServices.GetService<IProblemDetailsService>();
        if (writer is null || !await writer.TryWriteAsync(new ProblemDetailsContext { HttpContext = http, ProblemDetails = problem }))
            await http.Response.WriteAsJsonAsync(problem, http.RequestAborted);
    }
}
