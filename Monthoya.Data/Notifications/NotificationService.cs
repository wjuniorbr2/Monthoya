using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Notifications;

public sealed class NotificationService(
    MonthoyaDbContext dbContext,
    IEmailNotificationSender emailSender,
    IWhatsAppNotificationSender whatsAppSender) : INotificationService
{
    public Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default) =>
        ExecuteWithDbContextLockAsync(
            () => dbContext.NotificationRecipients
                .AsNoTracking()
                .CountAsync(x => x.UserId == userId && x.ReadAtUtc == null && x.DismissedAtUtc == null && !x.NotificationMessage!.IsArchived, cancellationToken),
            cancellationToken);

    public Task<IReadOnlyList<NotificationSummary>> GetRecentForUserAsync(Guid userId, int take, CancellationToken cancellationToken = default) =>
        ExecuteWithDbContextLockAsync(async () =>
        {
            var filter = new NotificationFilter();
            return (IReadOnlyList<NotificationSummary>)(await QueryForUser(userId, filter)
                .OrderByDescending(x => x.NotificationMessage!.CreatedAtUtc)
                .Take(Math.Clamp(take, 1, 50))
                .ToListAsync(cancellationToken))
                .Select(ToSummary)
                .ToList();
        }, cancellationToken);

    public Task<IReadOnlyList<NotificationSummary>> GetAllForUserAsync(Guid userId, NotificationFilter filter, CancellationToken cancellationToken = default) =>
        ExecuteWithDbContextLockAsync(async () =>
        {
            return (IReadOnlyList<NotificationSummary>)(await ApplyFilter(QueryForUser(userId, filter), filter)
                .OrderByDescending(x => x.NotificationMessage!.CreatedAtUtc)
                .ToListAsync(cancellationToken))
                .Select(ToSummary)
                .ToList();
        }, cancellationToken);

    public Task<IReadOnlyList<NotificationSummary>> GetRequiredUnreadAsync(Guid userId, CancellationToken cancellationToken = default) =>
        ExecuteWithDbContextLockAsync(async () =>
        {
            return (IReadOnlyList<NotificationSummary>)(await QueryForUser(userId, new NotificationFilter(UnreadOnly: true))
                .Where(x => x.NotificationMessage!.RequiresAcknowledgement && x.AcknowledgedAtUtc == null)
                .OrderByDescending(x => x.NotificationMessage!.Priority)
                .ThenBy(x => x.NotificationMessage!.CreatedAtUtc)
                .ToListAsync(cancellationToken))
                .Select(ToSummary)
                .ToList();
        }, cancellationToken);

    public Task<NotificationDetails?> GetDetailsAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default) =>
        ExecuteWithDbContextLockAsync(async () =>
        {
            var recipient = await QueryForUser(userId, new NotificationFilter())
                .SingleOrDefaultAsync(x => x.NotificationMessageId == notificationId, cancellationToken);

            if (recipient is null)
            {
                return null;
            }

            return new NotificationDetails(
                ToSummary(recipient),
                recipient.NotificationMessage!.Deliveries
                    .OrderBy(x => x.Channel)
                    .Select(ToDeliverySummary)
                    .ToList());
        }, cancellationToken);

    public Task<NotificationSummary> CreateManualMessageAsync(CreateManualNotificationRequest request, CancellationToken cancellationToken = default) =>
        ExecuteWithDbContextLockAsync(async () =>
        {
            var message = await CreateNotificationAsync(
                title: request.Title,
                body: request.Body,
                recipientUserIds: request.RecipientUserIds,
                category: request.Category,
                priority: request.Priority,
                createdByUserId: request.CreatedByUserId,
                requiresAcknowledgement: request.RequiresAcknowledgement,
                isSystemGenerated: false,
                sendEmail: request.SendEmail,
                sendWhatsApp: request.SendWhatsApp,
                scheduledForUtc: request.ScheduledForUtc,
                triggeredAtUtc: request.ScheduledForUtc is null || request.ScheduledForUtc <= DateTimeOffset.UtcNow ? DateTimeOffset.UtcNow : null,
                relatedEntityType: request.RelatedEntityType,
                relatedEntityId: request.RelatedEntityId,
                actionLabel: request.ActionLabel,
                actionTarget: request.ActionTarget,
                cancellationToken);

            return await GetFirstRecipientSummaryAsync(message.Id, request.RecipientUserIds, cancellationToken);
        }, cancellationToken);

    public Task<NotificationSummary> CreateSystemNotificationAsync(CreateSystemNotificationRequest request, CancellationToken cancellationToken = default) =>
        ExecuteWithDbContextLockAsync(async () =>
        {
            var message = await CreateNotificationAsync(
                request.Title,
                request.Body,
                request.RecipientUserIds,
                request.Category,
                request.Priority,
                createdByUserId: null,
                request.RequiresAcknowledgement,
                isSystemGenerated: true,
                request.SendEmail,
                request.SendWhatsApp,
                request.ScheduledForUtc,
                request.TriggeredAtUtc ?? (request.ScheduledForUtc is null || request.ScheduledForUtc <= DateTimeOffset.UtcNow ? DateTimeOffset.UtcNow : null),
                request.RelatedEntityType,
                request.RelatedEntityId,
                request.ActionLabel,
                request.ActionTarget,
                cancellationToken);

            return await GetFirstRecipientSummaryAsync(message.Id, request.RecipientUserIds, cancellationToken);
        }, cancellationToken);

    public Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default) =>
        ExecuteWithDbContextLockAsync(async () =>
        {
            var recipient = await GetRecipientAsync(notificationId, userId, cancellationToken);
            recipient.ReadAtUtc ??= DateTimeOffset.UtcNow;
            recipient.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

    public Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default) =>
        ExecuteWithDbContextLockAsync(async () =>
        {
            var unread = await dbContext.NotificationRecipients
                .Where(x => x.UserId == userId && x.ReadAtUtc == null && x.DismissedAtUtc == null)
                .ToListAsync(cancellationToken);

            var now = DateTimeOffset.UtcNow;
            foreach (var recipient in unread)
            {
                recipient.ReadAtUtc = now;
                recipient.UpdatedAtUtc = now;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

    public Task AcknowledgeAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default) =>
        ExecuteWithDbContextLockAsync(async () =>
        {
            var recipient = await GetRecipientAsync(notificationId, userId, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            recipient.ReadAtUtc ??= now;
            recipient.AcknowledgedAtUtc ??= now;
            recipient.UpdatedAtUtc = now;
            await dbContext.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

    public Task DismissAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default) =>
        ExecuteWithDbContextLockAsync(async () =>
        {
            var recipient = await GetRecipientAsync(notificationId, userId, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            recipient.ReadAtUtc ??= now;
            recipient.DismissedAtUtc ??= now;
            recipient.UpdatedAtUtc = now;
            await dbContext.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

    public Task ProcessDueScheduledNotificationsAsync(CancellationToken cancellationToken = default) =>
        ExecuteWithDbContextLockAsync(async () =>
        {
            var now = DateTimeOffset.UtcNow;
            var due = await dbContext.NotificationMessages
                .Where(x => x.ScheduledForUtc != null && x.ScheduledForUtc <= now && x.TriggeredAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var message in due)
            {
                message.TriggeredAtUtc = now;
                message.UpdatedAtUtc = now;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            foreach (var message in due)
            {
                await ProcessPendingDeliveriesAsync(message.Id, cancellationToken);
            }
        }, cancellationToken);

    public Task CheckAndCreateKeyOverdueNotificationsAsync(CancellationToken cancellationToken = default) =>
        ExecuteWithDbContextLockAsync(async () =>
        {
            var now = DateTimeOffset.UtcNow;
            var overdueMovements = await dbContext.ImovelChaveMovimentos
                .AsNoTracking()
                .Include(x => x.Imovel)!.ThenInclude(x => x!.Proprietario)
                .Where(x => x.DevolvidoEm == null
                    && x.PrevisaoDevolucaoEm != null
                    && x.PrevisaoDevolucaoEm < now)
                .ToListAsync(cancellationToken);

            if (overdueMovements.Count == 0)
            {
                return;
            }

            var recipientIds = await GetKeyOverdueRecipientIdsAsync(cancellationToken);
            if (recipientIds.Count == 0)
            {
                return;
            }

            foreach (var movimento in overdueMovements)
            {
                var alreadyExists = await dbContext.NotificationMessages
                    .AsNoTracking()
                    .AnyAsync(x => x.Category == NotificationCategory.KeyOverdue
                        && x.RelatedEntityType == "ImovelChaveMovimento"
                        && x.RelatedEntityId == movimento.Id
                        && !x.IsArchived,
                        cancellationToken);

                if (alreadyExists)
                {
                    continue;
                }

                await CreateNotificationAsync(
                    "Chave com devolução em atraso",
                    BuildKeyOverdueBody(movimento, now),
                    recipientIds,
                    NotificationCategory.KeyOverdue,
                    NotificationPriority.High,
                    createdByUserId: null,
                    requiresAcknowledgement: true,
                    isSystemGenerated: true,
                    sendEmail: false,
                    sendWhatsApp: false,
                    scheduledForUtc: null,
                    triggeredAtUtc: now,
                    relatedEntityType: "ImovelChaveMovimento",
                    relatedEntityId: movimento.Id,
                    actionLabel: "Abrir tela de chaves",
                    actionTarget: $"chaves:{movimento.Id}",
                    cancellationToken);
            }
        }, cancellationToken);

    private async Task<T> ExecuteWithDbContextLockAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        await using var dbContextOperation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await operation();
    }

    private async Task ExecuteWithDbContextLockAsync(Func<Task> operation, CancellationToken cancellationToken)
    {
        await using var dbContextOperation = await DbContextOperationGate.EnterAsync(cancellationToken);
        await operation();
    }

    private async Task<IReadOnlyList<Guid>> GetKeyOverdueRecipientIdsAsync(CancellationToken cancellationToken)
    {
        var adminUsers = await dbContext.Users
            .AsNoTracking()
            .Where(x => x.IsActive && (x.Role == UserRole.Administrador || x.Role == UserRole.Desenvolvedor))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (adminUsers.Count > 0)
        {
            return adminUsers;
        }

        return await dbContext.Users
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    private static string BuildKeyOverdueBody(ImovelChaveMovimento movimento, DateTimeOffset now)
    {
        var imovel = movimento.Imovel;
        var endereco = imovel is null
            ? "-"
            : $"{imovel.Rua}, {imovel.Numero} - {imovel.Bairro}, {imovel.Cidade}/{imovel.Estado}";
        var previsao = movimento.PrevisaoDevolucaoEm;
        var atraso = previsao.HasValue ? now - previsao.Value : TimeSpan.Zero;
        var atrasoTexto = atraso.TotalDays >= 1
            ? $"{Math.Floor(atraso.TotalDays)} dia(s)"
            : $"{Math.Max(1, Math.Floor(atraso.TotalHours))} hora(s)";

        return string.Join(Environment.NewLine, new[]
        {
            $"Código da chave: {movimento.ChaveCodigo ?? imovel?.ChaveCodigo ?? "-"}",
            $"Imóvel: {endereco}",
            $"Proprietário: {imovel?.Proprietario?.NomeDisplay ?? "-"}",
            $"Retirado por: {movimento.RetiradoPorNome ?? "-"}",
            $"Telefone: {movimento.RetiradoPorTelefone ?? "-"}",
            $"Retirado em: {FormatDateTime(movimento.RetiradoEm)}",
            $"Previsão de devolução: {FormatDateTime(previsao)}",
            $"Tempo em atraso: {atrasoTexto}",
            $"Motivo: {movimento.Motivo ?? "-"}"
        });
    }

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

    private async Task<NotificationSummary> GetFirstRecipientSummaryAsync(Guid messageId, IReadOnlyList<Guid> requestedRecipients, CancellationToken cancellationToken)
    {
        var preferredUserId = requestedRecipients.FirstOrDefault(x => x != Guid.Empty);
        var query = QueryForUser(preferredUserId, new NotificationFilter()).Where(x => x.NotificationMessageId == messageId);
        var recipient = await query.FirstOrDefaultAsync(cancellationToken)
            ?? await dbContext.NotificationRecipients
                .Include(x => x.NotificationMessage)!.ThenInclude(x => x!.Deliveries)
                .AsNoTracking()
                .FirstAsync(x => x.NotificationMessageId == messageId, cancellationToken);
        return ToSummary(recipient);
    }

    private async Task<NotificationRecipient> GetRecipientAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken)
    {
        return await dbContext.NotificationRecipients
            .SingleOrDefaultAsync(x => x.NotificationMessageId == notificationId && x.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException("Notificação não encontrada para este usuário.");
    }

    private IQueryable<NotificationRecipient> QueryForUser(Guid userId, NotificationFilter filter)
    {
        return dbContext.NotificationRecipients
            .AsNoTracking()
            .Include(x => x.NotificationMessage)!.ThenInclude(x => x!.Deliveries)
            .Where(x => x.UserId == userId && !x.NotificationMessage!.IsArchived);
    }

    private static IQueryable<NotificationRecipient> ApplyFilter(IQueryable<NotificationRecipient> query, NotificationFilter filter)
    {
        if (filter.UnreadOnly)
        {
            query = query.Where(x => x.ReadAtUtc == null && x.DismissedAtUtc == null);
        }

        if (filter.Category.HasValue)
        {
            query = query.Where(x => x.NotificationMessage!.Category == filter.Category.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(x => x.NotificationMessage!.Priority == filter.Priority.Value);
        }

        if (filter.FromDate.HasValue)
        {
            var from = new DateTimeOffset(filter.FromDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            query = query.Where(x => x.NotificationMessage!.CreatedAtUtc >= from);
        }

        if (filter.ToDate.HasValue)
        {
            var to = new DateTimeOffset(filter.ToDate.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero);
            query = query.Where(x => x.NotificationMessage!.CreatedAtUtc <= to);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var search = filter.SearchText.Trim().ToUpperInvariant();
            query = query.Where(x =>
                x.NotificationMessage!.Title.ToUpper().Contains(search)
                || x.NotificationMessage.Body.ToUpper().Contains(search));
        }

        return query;
    }

    private static NotificationSummary ToSummary(NotificationRecipient recipient)
    {
        var message = recipient.NotificationMessage ?? throw new InvalidOperationException("Notificação sem mensagem.");
        return new NotificationSummary(
            message.Id,
            message.Title,
            message.Body,
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

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
