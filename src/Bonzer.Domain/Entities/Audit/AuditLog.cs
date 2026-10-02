namespace Bonzer.Domain.Entities.Audit;

/// <summary>
/// Automatic change history written by the audit interceptor on every SaveChanges:
/// who changed what, old and new values (JSON), from where. Append-only.
/// </summary>
public class AuditLog : BaseEntity
{
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }

    /// <summary>Create, Update, Delete (soft) or a custom action such as Login.</summary>
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }

    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedColumns { get; set; }

    public string? IpAddress { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; }
}
