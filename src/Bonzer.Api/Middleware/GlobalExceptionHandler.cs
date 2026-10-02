using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Bonzer.Api.Middleware;

/// <summary>
/// The single place where exceptions become HTTP responses (RFC 7807 ProblemDetails with a stable "code").
/// Mapping lives in <see cref="ExceptionMapper"/>; this class logs and writes the response.
/// Unexpected errors never leak internals, except in Development where the exception is included.
/// </summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetails,
    IDiagnosticContext diagnosticContext,
    IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var error = ExceptionMapper.Map(exception, httpContext.RequestAborted.IsCancellationRequested);

        // Lets the request-completion log line carry the error code and exception type.
        diagnosticContext.Set("ErrorCode", error.Code);
        diagnosticContext.Set("ExceptionType", exception.GetType().Name);

        logger.Log(error.LogLevel, error.LogException ? exception : null,
            "{Method} {Path} failed with {StatusCode} {ErrorCode}: {Reason}",
            httpContext.Request.Method, httpContext.Request.Path.Value, error.Status, error.Code, exception.Message);

        if (error.Status == StatusCodes.Status499ClientClosedRequest || httpContext.Response.HasStarted)
        {
            if (!httpContext.Response.HasStarted) httpContext.Response.StatusCode = error.Status;
            return true; // nobody is listening, or headers are already sent
        }

        var problem = new ProblemDetails
        {
            Status = error.Status,
            Title = error.Title,
            Detail = error.Detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["code"] = error.Code;

        if (exception is ValidationException validation)
            problem.Extensions["errors"] = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        if (environment.IsDevelopment())
            problem.Extensions["exception"] = new
            {
                type = exception.GetType().FullName,
                message = exception.Message,
                inner = exception.InnerException?.Message,
                stackTrace = exception.StackTrace?.Split(Environment.NewLine, StringSplitOptions.TrimEntries)
            };

        if (error.RetryAfterSeconds is { } retryAfter)
            httpContext.Response.Headers.RetryAfter = retryAfter.ToString();

        httpContext.Response.StatusCode = error.Status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
