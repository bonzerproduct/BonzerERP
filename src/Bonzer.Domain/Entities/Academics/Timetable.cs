namespace Bonzer.Domain.Entities.Academics;

/// <summary>Bell schedule of a branch: "Period 1 08:00-08:40", "Lunch Break"...</summary>
public class TimetableSlot : BranchEntity
{
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SortOrder { get; set; }
    public bool IsBreak { get; set; }
}

/// <summary>One cell of a section's weekly timetable. Unique indexes prevent section and teacher clashes.</summary>
public class TimetableEntry : BranchEntity
{
    public Guid AcademicYearId { get; set; }
    public Guid SectionId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public Guid SlotId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? StaffId { get; set; }
    public string? Room { get; set; }

    public AcademicYear AcademicYear { get; set; } = null!;
    public Section Section { get; set; } = null!;
    public TimetableSlot Slot { get; set; } = null!;
    public Subject? Subject { get; set; }
    public Staff? Staff { get; set; }
}
