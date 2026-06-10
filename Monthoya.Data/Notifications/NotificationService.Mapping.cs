using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Notifications;

public sealed partial class NotificationService
{
    private static NotificationSummary ToSummary(NotificationRecipient recipient)
    {
        var message = recipient.NotificationMessage ?? throw new InvalidOperationException("NotificaÃ§Ã£o sem mensagem.");
        return new NotificationSummary(
            message.Id,
            message.Title,
            message.Body,
            BuildBodyPreview(message, recipient),
            GetCategoryLabel(message.Category),
            message.Category,
            GetPriorityLabel(message.Priority),
            message.Priority,
            message.CreatedAtUtc,
            message.ScheduledForUtc,
            message.TriggeredAtUtc,
            message.RequiresAcknowledgement,
            message.IsSystemGenerated,
            message.RelatedEntityType,
            message.RelatedEntityId,
            message.ActionLabel,
            message.ActionTarget,
            recipient.ReadAtUtc.HasValue,
            recipient.AcknowledgedAtUtc.HasValue,
            recipient.DismissedAtUtc.HasValue,
            BuildDeliverySummary(message.Deliveries));
    }

    private static NotificationDeliverySummary ToDeliverySummary(NotificationDelivery delivery) =>
        new(
            delivery.Id,
            GetChannelLabel(delivery.Channel),
            delivery.Channel,
            delivery.Destination,
            GetDeliveryStatusLabel(delivery.Status),
            delivery.Status,
            delivery.Attempts,
            delivery.LastAttemptAtUtc,
            delivery.ErrorMessage);

    private static string BuildBodyPreview(NotificationMessage message, NotificationRecipient recipient)
    {
        if (message.Category == NotificationCategory.KeyOverdue)
        {
            var code = ExtractBodyValue(message.Body, "CÃ³digo da chave:", "CÃƒÂ³digo da chave:");
            var property = ExtractBodyValue(message.Body, "ImÃ³vel:", "ImÃƒÂ³vel:");
            var street = property.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? property;
            return $"CÃ³d.: {FallbackDash(code)} | {FallbackDash(street)}";
        }

        var firstLine = message.Body
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? message.Body;

        return firstLine.Length <= 110 ? firstLine : string.Concat(firstLine.AsSpan(0, 107), "...");
    }
}
