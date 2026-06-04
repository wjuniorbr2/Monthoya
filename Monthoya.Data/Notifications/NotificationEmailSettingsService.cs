using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Notifications;

public sealed class NotificationEmailSettingsService(MonthoyaDbContext dbContext) : INotificationEmailSettingsService
{
    public async Task<NotificationEmailSettingsSummary> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        return ToSummary(settings);
    }

    public async Task<NotificationEmailSettingsSummary> SaveAsync(SaveNotificationEmailSettingsRequest request, CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        settings.IsEnabled = request.IsEnabled;
        settings.SenderDisplayName = TrimOrNull(request.SenderDisplayName);
        settings.SenderEmail = TrimOrNull(request.SenderEmail);
        settings.SmtpHost = TrimOrNull(request.SmtpHost);
        settings.SmtpPort = request.SmtpPort <= 0 ? 587 : request.SmtpPort;
        settings.UseSslTls = request.UseSslTls;
        settings.SmtpUsername = TrimOrNull(request.SmtpUsername);
        settings.ReplyToEmail = TrimOrNull(request.ReplyToEmail);
        if (!string.IsNullOrWhiteSpace(request.SmtpPassword))
        {
            settings.SmtpPasswordSecret = request.SmtpPassword.Trim();
        }

        settings.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToSummary(settings);
    }

    public async Task<NotificationSendResult> SendTestAsync(string? destinationEmail, CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        var destination = TrimOrNull(destinationEmail) ?? settings.SenderEmail;
        if (string.IsNullOrWhiteSpace(destination))
        {
            return new NotificationSendResult(false, true, "Informe um e-mail de destino para o teste.");
        }

        return await SmtpEmailNotificationSender.SendWithSettingsAsync(
            settings,
            new EmailNotificationMessage(
                "Teste de envio do Monthoya",
                "Este é um teste das configurações SMTP do Monthoya.",
                destination,
                "Teste"),
            cancellationToken);
    }

    private async Task<NotificationEmailSettings> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.NotificationEmailSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is not null)
        {
            return settings;
        }

        settings = new NotificationEmailSettings();
        dbContext.NotificationEmailSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static NotificationEmailSettingsSummary ToSummary(NotificationEmailSettings settings) =>
        new(
            settings.IsEnabled,
            settings.SenderDisplayName,
            settings.SenderEmail,
            settings.SmtpHost,
            settings.SmtpPort,
            settings.UseSslTls,
            settings.SmtpUsername,
            !string.IsNullOrWhiteSpace(settings.SmtpPasswordSecret),
            settings.ReplyToEmail);

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class SmtpEmailNotificationSender(MonthoyaDbContext dbContext) : IEmailNotificationSender
{
    public async Task<NotificationSendResult> SendAsync(EmailNotificationMessage message, CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.NotificationEmailSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return settings is null
            ? new NotificationSendResult(false, true, "E-mail de envio não configurado.")
            : await SendWithSettingsAsync(settings, message, cancellationToken);
    }

    internal static async Task<NotificationSendResult> SendWithSettingsAsync(
        NotificationEmailSettings settings,
        EmailNotificationMessage message,
        CancellationToken cancellationToken)
    {
        if (!settings.IsEnabled)
        {
            return new NotificationSendResult(false, true, "Envio de e-mail está desativado.");
        }

        if (string.IsNullOrWhiteSpace(settings.SenderEmail)
            || string.IsNullOrWhiteSpace(settings.SmtpHost)
            || string.IsNullOrWhiteSpace(settings.SmtpUsername)
            || string.IsNullOrWhiteSpace(settings.SmtpPasswordSecret))
        {
            return new NotificationSendResult(false, true, "Configuração SMTP incompleta.");
        }

        try
        {
            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(settings.SenderDisplayName ?? "Monthoya", settings.SenderEmail));
            mimeMessage.To.Add(MailboxAddress.Parse(message.Destination));
            if (!string.IsNullOrWhiteSpace(settings.ReplyToEmail))
            {
                mimeMessage.ReplyTo.Add(MailboxAddress.Parse(settings.ReplyToEmail));
            }

            mimeMessage.Subject = message.Title;
            mimeMessage.Body = new TextPart("plain") { Text = message.Body };

            using var smtp = new SmtpClient();
            var secureSocketOptions = settings.UseSslTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await smtp.ConnectAsync(settings.SmtpHost, settings.SmtpPort, secureSocketOptions, cancellationToken);
            await smtp.AuthenticateAsync(settings.SmtpUsername, settings.SmtpPasswordSecret, cancellationToken);
            await smtp.SendAsync(mimeMessage, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);
            return new NotificationSendResult(true, false, null);
        }
        catch (Exception ex)
        {
            return new NotificationSendResult(false, false, ex.GetBaseException().Message);
        }
    }
}

public sealed class StubWhatsAppNotificationSender : IWhatsAppNotificationSender
{
    public Task<NotificationSendResult> SendAsync(EmailNotificationMessage message, CancellationToken cancellationToken = default) =>
        Task.FromResult(new NotificationSendResult(false, true, "WhatsApp provider not configured."));
}
