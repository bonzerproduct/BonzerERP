namespace Bonzer.Domain.Entities.Exams;

/// <summary>Unit Test, Mid Term, Final, Semester End, Mock Test...</summary>
public class ExamType : BranchEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }

    /// <summary>Contribution of this exam type to the final / annual result, if weighted.</summary>
    public decimal? WeightagePercent { get; set; }
}

/// <summary>Theory, Practical, Oral, Internal Assessment, Project...</summary>
public class ExamComponentType : BranchEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
}
