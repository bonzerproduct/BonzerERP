namespace Bonzer.Domain.Enums;

/// <summary>Daily (schools) or per period / lecture (colleges, universities, tuition batches).</summary>
public enum AttendanceMode { Daily, PeriodWise }

public enum AttendanceStatus { Present, Absent, Late, HalfDay, Leave, Holiday, Excused }

public enum AttendanceLockScope { AcademicYear, Date, SectionDate }

public enum ApplicantType { Student, Staff }

public enum RequestStatus { Pending, Approved, Rejected, Cancelled }

public enum AttendanceSource { Manual, Biometric, Rfid, MobileApp, Import }
