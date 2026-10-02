namespace Bonzer.Domain.Entities.HumanResources;

public class StaffAttendance : BranchEntity
{
    public Guid StaffId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    public AttendanceStatus Status { get; set; }
    public TimeOnly? CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
    public AttendanceSource Source { get; set; } = AttendanceSource.Manual;
    public string? Remark { get; set; }

    public Staff Staff { get; set; } = null!;
}
