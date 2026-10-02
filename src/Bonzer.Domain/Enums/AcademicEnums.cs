namespace Bonzer.Domain.Enums;

/// <summary>Education level of a program. Lets one model cover pre-primary through university and tuition.</summary>
public enum EducationLevel
{
    PrePrimary, Primary, MiddleSchool, Secondary, HigherSecondary,
    Diploma, UnderGraduate, PostGraduate, Doctorate, Tuition, Vocational, Other
}

/// <summary>How a program's academic year is divided.</summary>
public enum TermStructure { Annual, Semester, Trimester, Quarterly, Monthly, Custom }

public enum AcademicYearStatus { Planned, Active, Closed }

public enum SubjectType { Core, Elective, Language, Practical, Activity, CoScholastic, Project }

public enum HolidayType { Public, School, Vacation, Exam, Emergency }
