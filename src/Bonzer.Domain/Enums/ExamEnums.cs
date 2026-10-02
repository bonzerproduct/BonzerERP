namespace Bonzer.Domain.Enums;

/// <summary>Percentage (most schools), GradePoint (CGPA / university GPA) or Descriptive (pre-primary).</summary>
public enum GradingType { Percentage, GradePoint, Descriptive }

public enum ExamStatus { Draft, Scheduled, Ongoing, MarksEntry, Completed, Published, Cancelled }

public enum MarkStatus { Present, Absent, Medical, Exempt }

public enum ResultStatus { Pass, Fail, Promoted, Detained, Withheld, Absent, Compartment }
