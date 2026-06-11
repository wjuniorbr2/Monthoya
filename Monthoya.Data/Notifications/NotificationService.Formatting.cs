using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Notifications;

public sealed partial class NotificationService
{
    private static string ExtractBodyValue(string body, params string[] labels)
    {
        foreach (var line in body.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var label in labels)
            {
                if (line.StartsWith(label, StringComparison.OrdinalIgnoreCase))
                {
                    return line[label.Length..].Trim();
                }
            }
        }

        return string.Empty;
    }

    private static string FallbackDash(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();

    private static string BuildDeliverySummary(IEnumerable<NotificationDelivery> deliveries) =>
        string.Join(", ", deliveries
            .OrderBy(x => x.Channel)
            .GroupBy(x => x.Channel)
            .Select(group => $"{GetChannelLabel(group.Key)}: {GetDeliveryStatusLabel(group.OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).First().Status)}"));

    private static string GetCategoryLabel(NotificationCategory category) =>
        category switch
        {
            NotificationCategory.ManualMessage => "Mensagem manual",
            NotificationCategory.SystemAlert => "Alerta do sistema",
            NotificationCategory.ScheduledReminder => "Lembrete agendado",
            NotificationCategory.TaskRequired => "Ação necessária",
            NotificationCategory.Info => "Informação",
            NotificationCategory.Warning => "Aviso",
            NotificationCategory.AdminAnnouncement => "Comunicado administrativo",
            NotificationCategory.KeyOverdue => "Chave em atraso",
            _ => category.ToString()
        };

    private static string GetPriorityLabel(NotificationPriority priority) =>
        priority switch
        {
            NotificationPriority.Low => "Baixa",
            NotificationPriority.Normal => "Normal",
            NotificationPriority.High => "Alta",
            NotificationPriority.Critical => "Crítica",
            _ => priority.ToString()
        };

    private static string GetChannelLabel(NotificationChannel channel) =>
        channel switch
        {
            NotificationChannel.InApp => "No sistema",
            NotificationChannel.Email => "E-mail",
            NotificationChannel.WhatsApp => "WhatsApp",
            _ => channel.ToString()
        };

    private static string GetDeliveryStatusLabel(NotificationDeliveryStatus status) =>
        status switch
        {
            NotificationDeliveryStatus.Pending => "Pendente",
            NotificationDeliveryStatus.Sent => "Enviado",
            NotificationDeliveryStatus.Failed => "Falhou",
            NotificationDeliveryStatus.Skipped => "Ignorado",
            _ => status.ToString()
        };

    private static string FormatDateTime(DateTimeOffset? value) =>
        value?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "-";
}

