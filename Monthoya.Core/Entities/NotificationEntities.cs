namespace Monthoya.Core.Entities;

public enum NotificationCategory
{
    ManualMessage = 0,
    SystemAlert = 1,
    ScheduledReminder = 2,
    TaskRequired = 3,
    Info = 4,
    Warning = 5,
    AdminAnnouncement = 6,
    KeyOverdue = 7
}

public enum NotificationPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

public enum NotificationChannel
{
    InApp = 0,
    Email = 1,
    WhatsApp = 2
}

public enum NotificationDeliveryStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2,
    Skipped = 3
}

public sealed class NotificationMessage : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public NotificationCategory Category { get; set; } = NotificationCategory.Info;

    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    public Guid? CreatedByUserId { get; set; }

    public DateTimeOffset? ScheduledForUtc { get; set; }

    public DateTimeOffset? TriggeredAtUtc { get; set; }

    public bool RequiresAcknowledgement { get; set; }

    public bool IsSystemGenerated { get; set; }

    public string? RelatedEntityType { get; set; }

    public Guid? RelatedEntityId { get; set; }

    public string? ActionLabel { get; set; }

    public string? ActionTarget { get; set; }

    public bool IsArchived { get; set; }

    public AppUser? CreatedByUser { get; set; }

    public ICollection<NotificationRecipient> Recipients { get; set; } = new List<NotificationRecipient>();

    public ICollection<NotificationDelivery> Deliveries { get; set; } = new List<NotificationDelivery>();
}

public sealed class NotificationRecipient : BaseEntity
{
    public Guid NotificationMessageId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset? ReadAtUtc { get; set; }

    public DateTimeOffset? AcknowledgedAtUtc { get; set; }

    public DateTimeOffset? DismissedAtUtc { get; set; }

    public NotificationMessage? NotificationMessage { get; set; }

    public AppUser? User { get; set; }
}

public sealed class NotificationDelivery : BaseEntity
{
    public Guid NotificationMessageId { get; set; }

    public Guid? RecipientUserId { get; set; }

    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;

    public string? Destination { get; set; }

    public NotificationDeliveryStatus Status { get; set; } = NotificationDeliveryStatus.Pending;

    public int Attempts { get; set; }

    public DateTimeOffset? LastAttemptAtUtc { get; set; }

    public string? ErrorMessage { get; set; }

    public NotificationMessage? NotificationMessage { get; set; }

    public AppUser? RecipientUser { get; set; }
}

public sealed class NotificationEmailSettings : BaseEntity
{
    public bool IsEnabled { get; set; }

    public string? SenderDisplayName { get; set; }

    public string? SenderEmail { get; set; }

    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 587;

    public bool UseSslTls { get; set; } = true;

    public string? SmtpUsername { get; set; }

    public string? SmtpPasswordSecret { get; set; }

    public string? ReplyToEmail { get; set; }
}
