using Bonzer.Application.Common.Interfaces;
using Serilog.Context;

namespace Bonzer.Api.Middleware;

/// <summary>Adds TenantId / UserId to every log event of the request, so logs can be filtered per school.</summary>
public sealed class LogEnrichmentMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser)
    {
        using (LogContext.PushProperty("TenantId", currentUser.TenantId))
        using (LogContext.PushProperty("UserId", currentUser.UserId))
        {
            await next(context);
        }
    }
}
