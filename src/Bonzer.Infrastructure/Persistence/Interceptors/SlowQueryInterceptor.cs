using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bonzer.Infrastructure.Persistence.Interceptors;

public sealed class DatabaseLoggingOptions
{
    public const string SectionName = "Database";

    /// <summary>Commands slower than this are logged as warnings with their SQL (parameter values are not logged).</summary>
    public int SlowQueryThresholdMs { get; init; } = 500;
}

/// <summary>
/// Logs every SQL command that takes longer than <see cref="DatabaseLoggingOptions.SlowQueryThresholdMs"/>,
/// so missing indexes and N+1 queries show up in Seq per tenant and request (the log context carries
/// TenantId, UserId and CorrelationId). Stateless: registered as a singleton.
/// </summary>
public sealed class SlowQueryInterceptor(IOptions<DatabaseLoggingOptions> options, ILogger<SlowQueryInterceptor> logger)
    : DbCommandInterceptor
{
    private readonly TimeSpan _threshold = TimeSpan.FromMilliseconds(Math.Max(1, options.Value.SlowQueryThresholdMs));

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Check(command, eventData);
        return result;
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        DbDataReader result, CancellationToken cancellationToken = default)
    {
        Check(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Check(command, eventData);
        return result;
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        Check(command, eventData);
        return ValueTask.FromResult(result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        Check(command, eventData);
        return result;
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result,
        CancellationToken cancellationToken = default)
    {
        Check(command, eventData);
        return ValueTask.FromResult(result);
    }

    private void Check(DbCommand command, CommandExecutedEventData eventData)
    {
        if (eventData.Duration < _threshold) return;

        logger.LogWarning("Slow query ({ElapsedMs} ms, threshold {ThresholdMs} ms): {Sql}",
            (long)eventData.Duration.TotalMilliseconds, (long)_threshold.TotalMilliseconds, command.CommandText);
    }
}
