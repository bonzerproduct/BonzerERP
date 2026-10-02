using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bonzer.Infrastructure.Services;

/// <summary>
/// Issues the next number with a single atomic "UPDATE ... RETURNING" (row lock), so two cashiers can never
/// get the same receipt number. Runs inside the caller's transaction: a rollback also returns the number.
/// The sequence row is created on first use with a sensible default prefix.
/// </summary>
public sealed class DocumentNumberGenerator(BonzerDbContext db, TimeProvider clock) : IDocumentNumberGenerator
{
    private const string NextSql = """
        UPDATE document_sequences
           SET next_value = next_value + 1, updated_at = @now
         WHERE tenant_id = @tenant AND branch_id = @branch AND document_type = @type
           AND academic_year_id IS NOT DISTINCT FROM @year AND is_deleted = false
        RETURNING next_value - 1, prefix, format, padding
        """;

    private const string CreateSql = """
        INSERT INTO document_sequences
            (id, tenant_id, branch_id, document_type, academic_year_id, prefix, format, padding, next_value, created_at, is_deleted)
        VALUES (@id, @tenant, @branch, @type, @year, @prefix, @format, 5, 1, @now, false)
        ON CONFLICT DO NOTHING
        """;

    public async Task<string> NextAsync(Guid branchId, DocumentType documentType, Guid? academicYearId,
        CancellationToken cancellationToken = default)
    {
        var branch = await db.Branches.AsNoTracking()
                         .Where(b => b.Id == branchId)
                         .Select(b => new { b.TenantId, b.Code })
                         .FirstOrDefaultAsync(cancellationToken)
                     ?? throw new NotFoundException(nameof(Branch), branchId);

        var yearName = academicYearId is null
            ? null
            : await db.AcademicYears.AsNoTracking().Where(a => a.Id == academicYearId).Select(a => a.Name).FirstOrDefaultAsync(cancellationToken);

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var next = await TryNextAsync(branch.TenantId, branchId, documentType, academicYearId, cancellationToken);
            if (next is not null)
                return Format(next.Value, yearName, branch.Code);

            var prefix = $"{branch.Code}-{DefaultPrefix(documentType)}";
            var format = academicYearId is null ? "{PREFIX}/{SEQ}" : "{PREFIX}/{YEAR}/{SEQ}";
            await ExecuteAsync(CreateSql, cancellationToken,
                ("id", UuidV7.NewGuid()), ("tenant", branch.TenantId), ("branch", branchId), ("type", documentType.ToString()),
                ("year", academicYearId), ("prefix", prefix), ("format", format), ("now", Now));
        }

        throw new InvalidOperationException($"Could not allocate a {documentType} number.");
    }

    private async Task<(long Value, string Prefix, string Format, int Padding)?> TryNextAsync(
        Guid tenantId, Guid branchId, DocumentType type, Guid? yearId, CancellationToken cancellationToken)
    {
        await using var command = await CreateCommandAsync(NextSql, cancellationToken,
            ("now", Now), ("tenant", tenantId), ("branch", branchId), ("type", type.ToString()), ("year", yearId));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return (reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3));
    }

    private static string Format((long Value, string Prefix, string Format, int Padding) seq, string? yearName, string branchCode) =>
        seq.Format
            .Replace("{PREFIX}", seq.Prefix)
            .Replace("{YEAR}", yearName ?? string.Empty)
            .Replace("{BRANCH}", branchCode)
            .Replace("{SEQ}", seq.Value.ToString().PadLeft(seq.Padding, '0'))
            .Replace("//", "/");

    private static string DefaultPrefix(DocumentType type) => type switch
    {
        DocumentType.Admission => "ADM",
        DocumentType.Enrollment => "ENR",
        DocumentType.Invoice => "INV",
        DocumentType.Receipt => "RCP",
        DocumentType.Refund => "RFD",
        DocumentType.TransferCertificate => "TC",
        DocumentType.BonafideCertificate => "BON",
        DocumentType.CharacterCertificate => "CHR",
        DocumentType.HallTicket => "HT",
        DocumentType.StaffCode => "EMP",
        _ => type.ToString().ToUpperInvariant()
    };

    private async Task ExecuteAsync(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        await using var command = await CreateCommandAsync(sql, cancellationToken, parameters);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<DbCommand> CreateCommandAsync(string sql, CancellationToken cancellationToken, params (string Name, object? Value)[] parameters)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await db.Database.OpenConnectionAsync(cancellationToken);

        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            if (name == "year") parameter.DbType = DbType.Guid; // typed even when null
            command.Parameters.Add(parameter);
        }
        return command;
    }

    private DateTime Now => clock.GetUtcNow().UtcDateTime;
}
