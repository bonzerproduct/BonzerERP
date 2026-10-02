using System.Diagnostics;
using Serilog.Context;

namespace Bonzer.Api.Middleware;

/// <summary>
/// One id per request, end to end. Accepts <c>X-Correlation-Id</c> from the caller (web app, mobile app, API
/// gateway) or uses the W3C trace id, then:
///  - sets <see cref="HttpContext.TraceIdentifier"/>, so ProblemDetails, audit_logs and request logs share it,
///  - pushes "CorrelationId" onto every log event of the request,
///  - echoes it in the response header, so a user can quote it to support.
/// Must be the first middleware.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";
    private const int MaxLength = 64;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Sanitize(context.Request.Headers[HeaderName])
                            ?? Activity.Current?.TraceId.ToHexString()
                            ?? context.TraceIdentifier;

        context.TraceIdentifier = correlationId;
        // OnStarting survives the exception handler clearing the response headers.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
            await next(context);
    }

    /// <summary>Only short, plain ids are trusted; anything else (log injection, huge values) is replaced.</summary>
    internal static string? Sanitize(string? value) =>
        !string.IsNullOrEmpty(value) && value.Length <= MaxLength
                                     && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':')
            ? value
            : null;
}
