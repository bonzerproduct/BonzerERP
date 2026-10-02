using Bonzer.Api.Middleware;
using Bonzer.Application.Common.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Bonzer.UnitTests.Api;

public class ExceptionMappingTests
{
    private static PostgresException Pg(string sqlState) => new("db error", "ERROR", "ERROR", sqlState);

    public static TheoryData<Exception, int, string> Cases => new()
    {
        { new ValidationException([new ValidationFailure("Name", "Required")]), 400, ErrorCodes.ValidationFailed },
        { new UnauthorizedException("Locked", ErrorCodes.AccountLocked), 401, ErrorCodes.AccountLocked },
        { new ForbiddenAccessException(), 403, ErrorCodes.Forbidden },
        { new NotFoundException("Student", Guid.Empty), 404, ErrorCodes.NotFound },
        { new ConflictException("Duplicate admission number"), 409, ErrorCodes.Conflict },
        { new BusinessRuleException("Section is full"), 422, ErrorCodes.BusinessRule },
        { new MessageDeliveryException("email", new IOException("smtp down")), 503, ErrorCodes.MessageDeliveryFailed },
        { new DbUpdateConcurrencyException("stale"), 409, ErrorCodes.ConcurrencyConflict },
        { new DbUpdateException("save failed", Pg(PostgresErrorCodes.UniqueViolation)), 409, ErrorCodes.DuplicateRecord },
        { new DbUpdateException("save failed", Pg(PostgresErrorCodes.ForeignKeyViolation)), 409, ErrorCodes.RecordInUse },
        { new DbUpdateException("save failed", Pg(PostgresErrorCodes.StringDataRightTruncation)), 422, ErrorCodes.ConstraintViolation },
        { Pg(PostgresErrorCodes.DeadlockDetected), 503, ErrorCodes.TransientFailure },
        { Pg(PostgresErrorCodes.TooManyConnections), 503, ErrorCodes.DatabaseUnavailable },
        { new TimeoutException(), 503, ErrorCodes.Timeout },
        { new BadHttpRequestException("too big", 413), 413, ErrorCodes.PayloadTooLarge },
        { new InvalidOperationException("bug"), 500, ErrorCodes.InternalError },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Maps_exception_to_status_and_code(Exception exception, int status, string code)
    {
        var error = ExceptionMapper.Map(exception);
        Assert.Equal(status, error.Status);
        Assert.Equal(code, error.Code);
    }

    [Fact]
    public void Internal_errors_never_expose_the_exception_message()
    {
        var error = ExceptionMapper.Map(new InvalidOperationException("Connection string Password=secret"));
        Assert.DoesNotContain("secret", error.Detail);
        Assert.True(error.LogException);
        Assert.Equal(LogLevel.Error, error.LogLevel);
    }

    [Fact]
    public void Client_disconnect_is_not_a_server_error()
    {
        var error = ExceptionMapper.Map(new OperationCanceledException(), requestAborted: true);
        Assert.Equal(StatusCodes.Status499ClientClosedRequest, error.Status);
        Assert.Equal(LogLevel.Information, error.LogLevel);
        Assert.False(error.LogException);
    }

    [Fact]
    public void Transient_database_errors_ask_the_client_to_retry()
    {
        Assert.NotNull(ExceptionMapper.Map(Pg(PostgresErrorCodes.SerializationFailure)).RetryAfterSeconds);
    }

    [Fact]
    public void Expected_client_errors_are_logged_without_stack_traces()
    {
        Assert.False(ExceptionMapper.Map(new NotFoundException("Staff", 1)).LogException);
        Assert.True(ExceptionMapper.Map(new DbUpdateException("x", Pg(PostgresErrorCodes.UniqueViolation))).LogException);
    }

    [Theory]
    [InlineData("web-7f3a9c", "web-7f3a9c")]
    [InlineData("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01", "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01")]
    [InlineData("bad\nInjected log line", null)]
    [InlineData("<script>", null)]
    [InlineData("", null)]
    public void Correlation_id_accepts_only_safe_values(string input, string? expected) =>
        Assert.Equal(expected, CorrelationIdMiddleware.Sanitize(input));

    [Fact]
    public void Correlation_id_rejects_oversized_values() =>
        Assert.Null(CorrelationIdMiddleware.Sanitize(new string('a', 65)));

    [Fact]
    public void Every_status_has_a_default_code()
    {
        Assert.Equal(ErrorCodes.NotFound, ErrorCodes.ForStatus(404));
        Assert.Equal(ErrorCodes.RateLimited, ErrorCodes.ForStatus(429));
        Assert.Equal(ErrorCodes.InternalError, ErrorCodes.ForStatus(502));
    }
}
