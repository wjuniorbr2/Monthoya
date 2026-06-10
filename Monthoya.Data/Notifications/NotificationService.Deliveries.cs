using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Notifications;

public sealed partial class NotificationService
{
    private async Task ProcessPendingDeliveriesAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var message = await dbContext.NotificationMessages
            .Include(x => x.Deliveries)
            .Include(x => x.Recipients).ThenInclude(x => x.User)
            .SingleAsync(x => x.Id == messageId, cancellationToken);

        foreach (var delivery in message.Deliveries.Where(x => x.Status == NotificationDeliveryStatus.Pending).ToList())
        {
            var recipient = message.Recipients.FirstOrDefault(x => x.UserId == delivery.RecipientUserId);
            var destination = delivery.Destination ?? recipient?.User?.Email ?? string.Empty;
            var recipientName = recipient?.User?.DisplayName;
            var sendMessage = new EmailNotificationMessage(message.Title, message.Body, destination, recipientName);
            var result = delivery.Channel switch
            {
                NotificationChannel.Email => await emailSender.SendAsync(sendMessage, cancellationToken),
                NotificationChannel.WhatsApp => await whatsAppSender.SendAsync(sendMessage, cancellationToken),
                _ => new NotificationSendResult(true, false, null)
            };

            delivery.Attempts += 1;
            delivery.LastAttemptAtUtc = DateTimeOffset.UtcNow;
            delivery.ErrorMessage = result.ErrorMessage;
            delivery.Status = result.Sent
                ? NotificationDeliveryStatus.Sent
                : result.Skipped
                    ? NotificationDeliveryStatus.Skipped
                    : NotificationDeliveryStatus.Failed;
            delivery.UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
