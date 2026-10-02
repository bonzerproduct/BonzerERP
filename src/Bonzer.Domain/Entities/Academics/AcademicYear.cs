namespace Bonzer.Domain.Entities.Academics;

/// <summary>Academic session of a branch, e.g. "2026-27". Replaces the separate attendance year-lock table.</summary>
public class AcademicYear : BranchEntity
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    /// <summary>Only one current year per branch (enforced by a filtered unique index).</summary>
    public bool IsCurrent { get; set; }
    public AcademicYearStatus Status { get; set; } = AcademicYearStatus.Planned;

    /// <summary>When true no attendance can be changed for the whole year.</summary>
    public bool IsAttendanceLocked { get; set; }

    public ICollection<AcademicTerm> Terms { get; set; } = new List<AcademicTerm>();
}
