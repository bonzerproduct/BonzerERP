namespace Bonzer.Domain.Entities.Students;

public class StudentGuardian : TenantEntity
{
    public Guid StudentId { get; set; }
    public Guid GuardianId { get; set; }
    public GuardianRelation Relation { get; set; }

    public bool IsPrimaryContact { get; set; }
    public bool CanPickUp { get; set; } = true;
    public bool ReceivesNotifications { get; set; } = true;

    public Student Student { get; set; } = null!;
    public Guardian Guardian { get; set; } = null!;
}
