using Monthoya.Core.Entities;

namespace Monthoya.Core.Services;

public sealed record NotificationFilter(
    string? SearchText = null,
    bool UnreadOnly = false,
    NotificationCategory? Category = null,
    NotificationPriority? Priority = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null);

public sealed record CreateManualNotificationRequest(
    string Title,
    string Body,
    IReadOnlyList<Guid> RecipientUserIds,
    Guid? CreatedByUserId,
    NotificationPriority Priority = NotificationPriority.Normal,
    NotificationCategory Category = NotificationCategory.ManualMessage,
    bool RequiresAcknowledgement = false,
    bool SendEmail = false,
    bool SendWhatsApp = false,
    DateTimeOffset? ScheduledForUtc = null,
    string? RelatedEntityType = null,
    Guid? RelatedEntityId = null,
    string? ActionLabel = null,
    string? ActionTarget = null);

public sealed record CreateSystemNotificationRequest(
    string Title,
    string Body,
    IReadOnlyList<Guid> RecipientUserIds,
    NotificationCategory Category,
    NotificationPriority Priority = NotificationPriority.Normal,
    bool RequiresAcknowledgement = false,
    bool SendEmail = false,
    bool SendWhatsApp = false,
    DateTimeOffset? ScheduledForUtc = null,
    DateTimeOffset? TriggeredAtUtc = null,
    string? RelatedEntityType = null,
    Guid? RelatedEntityId = null,
    string? ActionLabel = null,
    string? ActionTarget = null);

public sealed record NotificationSummary(
    Guid Id,
    string Title,
    string Body,
    string Category,
    NotificationCategory CategoryValue,
    string Priority,
    NotificationPriority PriorityValue,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ScheduledForUtc,
    DateTimeOffset? TriggeredAtUtc,
    bool RequiresAcknowledgement,
    bool IsSystemGenerated,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    string? ActionLabel,
    string? ActionTarget,
    bool IsRead,
    bool IsAcknowledged,
    bool IsDismissed,
    string DeliverySummary);

public sealed record NotificationDetails(
    NotificationSummary Summary,
    IReadOnlyList<NotificationDeliverySummary> Deliveries);

public sealed record NotificationDeliverySummary(
    Guid Id,
    string Channel,
    NotificationChannel ChannelValue,
    string? Destination,
    string Status,
    NotificationDeliveryStatus StatusValue,
    int Attempts,
    DateTimeOffset? LastAttemptAtUtc,
    string? ErrorMessage);

public interface INotificationService
{
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationSummary>> GetRecentForUserAsync(Guid userId, int take, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationSummary>> GetAllForUserAsync(Guid userId, NotificationFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotificationSummary>> GetRequiredUnreadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationDetails?> GetDetailsAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    Task<NotificationSummary> CreateManualMessageAsync(CreateManualNotificationRequest request, CancellationToken cancellationToken = default);
    Task<NotificationSummary> CreateSystemNotificationAsync(CreateSystemNotificationRequest request, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AcknowledgeAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    Task DismissAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default);
    Task ProcessDueScheduledNotificationsAsync(CancellationToken cancellationToken = default);
    Task CheckAndCreateKeyOverdueNotificationsAsync(CancellationToken cancellationToken = default);
}

public sealed record EmailNotificationMessage(
    string Title,
    string Body,
    string Destination,
    string? RecipientName);

public sealed record NotificationSendResult(
    bool Sent,
    bool Skipped,
    string? ErrorMessage);

public interface IEmailNotificationSender
{
    Task<NotificationSendResult> SendAsync(EmailNotificationMessage message, CancellationToken cancellationToken = default);
}

public interface IWhatsAppNotificationSender
{
    Task<NotificationSendResult> SendAsync(EmailNotificationMessage message, CancellationToken cancellationToken = default);
}

public sealed record NotificationEmailSettingsSummary(
    bool IsEnabled,
    string? SenderDisplayName,
    string? SenderEmail,
    string? SmtpHost,
    int SmtpPort,
    bool UseSslTls,
    string? SmtpUsername,
    bool HasPassword,
    string? ReplyToEmail);

public sealed record SaveNotificationEmailSettingsRequest(
    bool IsEnabled,
    string? SenderDisplayName,
    string? SenderEmail,
    string? SmtpHost,
    int SmtpPort,
    bool UseSslTls,
    string? SmtpUsername,
    string? SmtpPassword,
    string? ReplyToEmail);

public interface INotificationEmailSettingsService
{
    Task<NotificationEmailSettingsSummary> GetAsync(CancellationToken cancellationToken = default);
    Task<NotificationEmailSettingsSummary> SaveAsync(SaveNotificationEmailSettingsRequest request, CancellationToken cancellationToken = default);
    Task<NotificationSendResult> SendTestAsync(string? destinationEmail, CancellationToken cancellationToken = default);
}
