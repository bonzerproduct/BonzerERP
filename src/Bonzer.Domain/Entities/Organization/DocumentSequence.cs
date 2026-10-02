namespace Bonzer.Domain.Entities.Organization;

/// <summary>
/// Gap-free, concurrency-safe running numbers for admission numbers, receipts, TCs, etc.
/// Format tokens: {PREFIX}, {YEAR} (academic year name), {SEQ} (zero padded).
/// </summary>
public class DocumentSequence : BranchEntity
{
    public DocumentType DocumentType { get; set; }

    /// <summary>Null = sequence never resets; otherwise restarts every academic year.</summary>
    public Guid? AcademicYearId { get; set; }

    public string Prefix { get; set; } = string.Empty;
    public string Format { get; set; } = "{PREFIX}/{YEAR}/{SEQ}";
    public int Padding { get; set; } = 5;
    public long NextValue { get; set; } = 1;
}
