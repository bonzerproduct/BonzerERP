namespace Bonzer.Domain.Entities.Communication;

/// <summary>Circular / announcement / alert sent over one or more channels.</summary>
public class Notification : TenantEntity
{
    /// <summary>Null = tenant-wide.</summary>
    public Guid? BranchId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationCategory Category { get; set; } = NotificationCategory.General;
    public NotificationChannel Channels { get; set; } = NotificationChannel.InApp;

    public AudienceType AudienceType { get; set; } = AudienceType.Everyone;

    /// <summary>JSON describing the audience (class level ids, section ids, role names, user ids).</summary>
    public string? AudienceFilter { get; set; }
    public string? AttachmentUrl { get; set; }

    public DateTime? ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }

    public ICollection<NotificationRecipient> Recipients { get; set; } = new List<NotificationRecipient>();
}

[AuditIgnore]
public class NotificationRecipient : TenantEntity
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public NotificationChannel Channel { get; set; }
    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? Error { get; set; }

    public Notification Notification { get; set; } = null!;
}
