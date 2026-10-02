using Bonzer.Application.Common.Interfaces;
using Serilog;
using Serilog.Context;

namespace Bonzer.Api.Middleware;

/// <summary>
/// Adds TenantId / UserId / UserType to every log event of the request, and to the request-completion log
/// line (written by the outer request-logging middleware), so logs can be filtered per school and per user.
/// Runs after authentication.
/// </summary>
public sealed class LogEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser, IDiagnosticContext diagnosticContext)
    {
        if (currentUser.IsAuthenticated)
        {
            diagnosticContext.Set("TenantId", currentUser.TenantId);
            diagnosticContext.Set("UserId", currentUser.UserId);
            diagnosticContext.Set("UserType", currentUser.UserType?.ToString());
        }

        using (LogContext.PushProperty("TenantId", currentUser.TenantId))
        using (LogContext.PushProperty("UserId", currentUser.UserId))
        {
            await next(context);
        }
    }
}
