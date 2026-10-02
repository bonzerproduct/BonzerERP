namespace Bonzer.Domain.Entities.Exams;

public class HallTicket : BranchEntity
{
    public Guid ExamId { get; set; }
    public Guid StudentId { get; set; }
    public Guid EnrollmentId { get; set; }

    public string TicketNumber { get; set; } = string.Empty;
    public string? SeatNumber { get; set; }
    public string? ExamCentre { get; set; }
    public DateTime GeneratedAt { get; set; }
    public bool IsIssued { get; set; }

    public Exam Exam { get; set; } = null!;
    public Student Student { get; set; } = null!;
    public StudentEnrollment Enrollment { get; set; } = null!;
}
