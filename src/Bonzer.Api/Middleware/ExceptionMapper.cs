using Bonzer.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Bonzer.Api.Middleware;

/// <summary>What the client receives and how loudly it is logged for one exception.</summary>
public sealed record ErrorDescriptor(
    int Status,
    string Code,
    string Title,
    string Detail,
    LogLevel LogLevel,
    int? RetryAfterSeconds = null)
{
    /// <summary>
    /// Server failures, and database constraint errors (a validation rule is missing), are logged with the full
    /// exception: stack trace, inner exceptions, SQL state and constraint name.
    /// </summary>
    public bool LogException => Status >= 500 || Code is ErrorCodes.DuplicateRecord or ErrorCodes.RecordInUse or ErrorCodes.ConstraintViolation;
}

/// <summary>
/// Translates any exception into a stable HTTP status + error code. Pure and side-effect free so it can be
/// unit tested. Rules:
///  - <see cref="AppException"/>s carry their own code and a user-safe message.
///  - PostgreSQL constraint errors that slip past validation become 409 / 422 instead of 500.
///  - Transient database failures (deadlock, failover, pool exhausted) become 503 + Retry-After.
///  - Anything unknown is a 500 with a generic message; details are only logged.
/// </summary>
public static class ExceptionMapper
{
    public static ErrorDescriptor Map(Exception exception, bool requestAborted = false)
    {
        // The client disconnected: nothing to send, not a server error.
        if (requestAborted && exception is OperationCanceledException or IOException)
            return new(StatusCodes.Status499ClientClosedRequest, ErrorCodes.ClientClosedRequest, "Client closed request",
                "The request was cancelled by the client.", LogLevel.Information);

        return exception switch
        {
            ValidationException => new(StatusCodes.Status400BadRequest, ErrorCodes.ValidationFailed, "Validation failed",
                "One or more fields are invalid.", LogLevel.Information),

            UnauthorizedException e => new(StatusCodes.Status401Unauthorized, e.Code, "Unauthorized", e.Message, LogLevel.Information),
            ForbiddenAccessException e => new(StatusCodes.Status403Forbidden, e.Code, "Forbidden", e.Message, LogLevel.Warning),
            NotFoundException e => new(StatusCodes.Status404NotFound, e.Code, "Not found", e.Message, LogLevel.Information),
            ConflictException e => new(StatusCodes.Status409Conflict, e.Code, "Conflict", e.Message, LogLevel.Information),
            BusinessRuleException e => new(StatusCodes.Status422UnprocessableEntity, e.Code, "Business rule violated", e.Message,
                LogLevel.Information),
            ExternalServiceException e => new(StatusCodes.Status503ServiceUnavailable, e.Code, "Service unavailable", e.Message,
                LogLevel.Error, 30),

            DbUpdateConcurrencyException => new(StatusCodes.Status409Conflict, ErrorCodes.ConcurrencyConflict, "Conflict",
                "The record was changed by someone else. Reload and try again.", LogLevel.Information),
            DbUpdateException { InnerException: PostgresException pg } => MapPostgres(pg),
            PostgresException pg => MapPostgres(pg),
            DbUpdateException { InnerException: NpgsqlException { IsTransient: true } } or NpgsqlException { IsTransient: true }
                => DatabaseUnavailable(),

            TimeoutException or OperationCanceledException => new(StatusCodes.Status503ServiceUnavailable, ErrorCodes.Timeout,
                "Timeout", "The operation took too long. Please try again.", LogLevel.Error, 5),

            BadHttpRequestException e => new(e.StatusCode, ErrorCodes.ForStatus(e.StatusCode), "Bad request",
                e.StatusCode == StatusCodes.Status413PayloadTooLarge ? "The request body is too large." : "The request is malformed.",
                LogLevel.Information),

            _ => Internal()
        };
    }

    private static ErrorDescriptor MapPostgres(PostgresException pg) => pg.SqlState switch
    {
        PostgresErrorCodes.UniqueViolation => new(StatusCodes.Status409Conflict, ErrorCodes.DuplicateRecord, "Conflict",
            "A record with the same unique value already exists.", LogLevel.Warning),
        PostgresErrorCodes.ForeignKeyViolation => new(StatusCodes.Status409Conflict, ErrorCodes.RecordInUse, "Conflict",
            "The record is linked to other data, or refers to data that does not exist.", LogLevel.Warning),
        PostgresErrorCodes.ExclusionViolation => new(StatusCodes.Status409Conflict, ErrorCodes.Conflict, "Conflict",
            "The record overlaps with an existing one (for example a timetable clash).", LogLevel.Information),
        PostgresErrorCodes.NotNullViolation or PostgresErrorCodes.CheckViolation or PostgresErrorCodes.StringDataRightTruncation
            or PostgresErrorCodes.NumericValueOutOfRange or PostgresErrorCodes.InvalidTextRepresentation
            => new(StatusCodes.Status422UnprocessableEntity, ErrorCodes.ConstraintViolation, "Invalid data",
                "One or more values are missing, too long or out of range.", LogLevel.Warning),
        PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected
            => new(StatusCodes.Status503ServiceUnavailable, ErrorCodes.TransientFailure, "Temporary failure",
                "The system was busy with a conflicting change. Please try again.", LogLevel.Warning, 1),
        PostgresErrorCodes.QueryCanceled => new(StatusCodes.Status503ServiceUnavailable, ErrorCodes.Timeout, "Timeout",
            "The operation took too long. Please try again.", LogLevel.Error, 5),
        _ when pg.IsTransient => DatabaseUnavailable(),
        _ => Internal()
    };

    private static ErrorDescriptor DatabaseUnavailable() => new(StatusCodes.Status503ServiceUnavailable,
        ErrorCodes.DatabaseUnavailable, "Service unavailable", "The service is temporarily unavailable. Please try again shortly.",
        LogLevel.Error, 10);

    private static ErrorDescriptor Internal() => new(StatusCodes.Status500InternalServerError, ErrorCodes.InternalError,
        "Server error", "An unexpected error occurred. Quote the correlation id when contacting support.", LogLevel.Error);
}
