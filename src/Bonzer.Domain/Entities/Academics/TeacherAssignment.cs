namespace Bonzer.Domain.Entities.Academics;

/// <summary>
/// Which staff member teaches which subject to which section in a year.
/// Merges the old "class_teacher_map" and "teacher_subject_map" tables.
/// </summary>
public class TeacherAssignment : BranchEntity
{
    public Guid StaffId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid SectionId { get; set; }

    /// <summary>Null when the row only marks the class teacher.</summary>
    public Guid? SubjectId { get; set; }
    public bool IsClassTeacher { get; set; }

    public Staff Staff { get; set; } = null!;
    public AcademicYear AcademicYear { get; set; } = null!;
    public Section Section { get; set; } = null!;
    public Subject? Subject { get; set; }
}
