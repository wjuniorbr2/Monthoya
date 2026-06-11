using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Notifications;

public sealed partial class NotificationService
{
    private async Task<NotificationMessage> CreateNotificationAsync(
        string title,
        string body,
        IReadOnlyList<Guid> recipientUserIds,
        NotificationCategory category,
        NotificationPriority priority,
        Guid? createdByUserId,
        bool requiresAcknowledgement,
        bool isSystemGenerated,
        bool sendEmail,
        bool sendWhatsApp,
        DateTimeOffset? scheduledForUtc,
        DateTimeOffset? triggeredAtUtc,
        string? relatedEntityType,
        Guid? relatedEntityId,
        string? actionLabel,
        string? actionTarget,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Informe o título da notificação.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("Informe a mensagem da notificação.");
        }

        var recipients = recipientUserIds.Distinct().Where(x => x != Guid.Empty).ToList();
        if (recipients.Count == 0)
        {
            throw new InvalidOperationException("Selecione pelo menos um destinatário.");
        }

        var users = await dbContext.Users
            .Where(x => recipients.Contains(x.Id) && x.IsActive)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            throw new InvalidOperationException("Nenhum destinatário ativo foi encontrado.");
        }

        var message = new NotificationMessage
        {
            Title = title.Trim(),
            Body = body.Trim(),
            Category = category,
            Priority = priority,
            CreatedByUserId = createdByUserId,
            ScheduledForUtc = scheduledForUtc,
            TriggeredAtUtc = triggeredAtUtc,
            RequiresAcknowledgement = requiresAcknowledgement,
            IsSystemGenerated = isSystemGenerated,
            RelatedEntityType = TrimOrNull(relatedEntityType),
            RelatedEntityId = relatedEntityId,
            ActionLabel = TrimOrNull(actionLabel),
            ActionTarget = TrimOrNull(actionTarget)
        };

        foreach (var user in users)
        {
            message.Recipients.Add(new NotificationRecipient { UserId = user.Id });
            message.Deliveries.Add(new NotificationDelivery
            {
                RecipientUserId = user.Id,
                Channel = NotificationChannel.InApp,
                Status = NotificationDeliveryStatus.Sent,
                Attempts = 1,
                LastAttemptAtUtc = DateTimeOffset.UtcNow
            });

            if (sendEmail)
            {
                message.Deliveries.Add(new NotificationDelivery
                {
                    RecipientUserId = user.Id,
                    Channel = NotificationChannel.Email,
                    Destination = user.Email,
                    Status = NotificationDeliveryStatus.Pending
                });
            }

            if (sendWhatsApp)
            {
                message.Deliveries.Add(new NotificationDelivery
                {
                    RecipientUserId = user.Id,
                    Channel = NotificationChannel.WhatsApp,
                    Status = NotificationDeliveryStatus.Skipped,
                    ErrorMessage = "WhatsApp provider not configured."
                });
            }
        }

        dbContext.NotificationMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (triggeredAtUtc.HasValue)
        {
            await ProcessPendingDeliveriesAsync(message.Id, cancellationToken);
        }

        return message;
    }

}

