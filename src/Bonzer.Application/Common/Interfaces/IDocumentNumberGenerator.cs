namespace Bonzer.Application.Common.Interfaces;

/// <summary>Concurrency-safe running numbers (admission no, receipt no, TC no...). Call inside the business transaction.</summary>
public interface IDocumentNumberGenerator
{
    Task<string> NextAsync(Guid branchId, DocumentType documentType, Guid? academicYearId,
        CancellationToken cancellationToken = default);
}
