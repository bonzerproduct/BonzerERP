using Bonzer.Application.Common.Exceptions;
using Bonzer.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace Bonzer.Api.Middleware;

/// <summary>
/// While a user still has a temporary password (new account or admin reset), only the auth endpoints
/// (change password, profile, logout...) are allowed. Everything else returns 403 "password_change_required".
/// </summary>
public sealed class PasswordChangeRequiredMiddleware(RequestDelegate next)
{
    private static readonly PathString AuthPath = "/api/v1/auth";

    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetails)
    {
        if (context.User.HasClaim(AppClaimTypes.PasswordChangeRequired, "1") && !context.Request.Path.StartsWithSegments(AuthPath))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Password change required",
                Detail = "Change your temporary password (POST /api/v1/auth/change-password) before continuing.",
                Instance = context.Request.Path
            };
            problem.Extensions["code"] = ErrorCodes.PasswordChangeRequired;
            await problemDetails.WriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = problem });
            return;
        }

        await next(context);
    }
}
