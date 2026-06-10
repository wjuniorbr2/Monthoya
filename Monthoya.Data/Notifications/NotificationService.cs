using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Notifications;

public sealed partial class NotificationService(
    MonthoyaDbContext dbContext,
    IEmailNotificationSender emailSender,
    IWhatsAppNotificationSender whatsAppSender) : INotificationService
{
    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await dbContext.NotificationRecipients
            .AsNoTracking()
            .CountAsync(x => x.UserId == userId && x.ReadAtUtc == null && x.DismissedAtUtc == null && !x.NotificationMessage!.IsArchived, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationSummary>> GetRecentForUserAsync(Guid userId, int take, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var filter = new NotificationFilter();
        return (await QueryForUser(userId, filter)
            .OrderByDescending(x => x.NotificationMessage!.CreatedAtUtc)
            .Take(Math.Clamp(take, 1, 50))
            .ToListAsync(cancellationToken))
            .Select(ToSummary)
            .ToList();
    }

    public async Task<IReadOnlyList<NotificationSummary>> GetAllForUserAsync(Guid userId, NotificationFilter filter, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return (await ApplyFilter(QueryForUser(userId, filter), filter)
            .OrderByDescending(x => x.NotificationMessage!.CreatedAtUtc)
            .ToListAsync(cancellationToken))
            .Select(ToSummary)
            .ToList();
    }

    public async Task<IReadOnlyList<NotificationSummary>> GetHistoryForUserAsync(Guid userId, NotificationFilter filter, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return (await ApplyFilter(QueryHistoryForUser(userId), filter)
            .OrderByDescending(x => x.NotificationMessage!.CreatedAtUtc)
            .ToListAsync(cancellationToken))
            .Select(ToSummary)
            .ToList();
    }

    public async Task<IReadOnlyList<NotificationSummary>> GetRequiredUnreadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return (await QueryForUser(userId, new NotificationFilter(UnreadOnly: true))
            .Where(x => x.NotificationMessage!.RequiresAcknowledgement && x.AcknowledgedAtUtc == null)
            .OrderByDescending(x => x.NotificationMessage!.Priority)
            .ThenBy(x => x.NotificationMessage!.CreatedAtUtc)
            .ToListAsync(cancellationToken))
            .Select(ToSummary)
            .ToList();
    }

    public async Task<NotificationDetails?> GetDetailsAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var recipient = await QueryForUserIncludingHistory(userId)
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
    }

    public async Task<NotificationSummary> CreateManualMessageAsync(CreateManualNotificationRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
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
    }

    public async Task<NotificationSummary> CreateSystemNotificationAsync(CreateSystemNotificationRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
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
    }

    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var recipient = await GetRecipientAsync(notificationId, userId, cancellationToken);
        recipient.ReadAtUtc ??= DateTimeOffset.UtcNow;
        recipient.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var unread = await dbContext.NotificationRecipients
            .Where(x => x.UserId == userId && x.ReadAtUtc == null && x.DismissedAtUtc == null && !x.NotificationMessage!.IsArchived)
            .ToListAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var recipient in unread)
        {
            recipient.ReadAtUtc = now;
            recipient.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AcknowledgeAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var recipient = await GetRecipientAsync(notificationId, userId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        recipient.ReadAtUtc ??= now;
        recipient.AcknowledgedAtUtc ??= now;
        recipient.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DismissAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var recipient = await GetRecipientAsync(notificationId, userId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        recipient.ReadAtUtc ??= now;
        recipient.DismissedAtUtc ??= now;
        recipient.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveKeyOverdueNotificationAsync(Guid keyMovementId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var messages = await dbContext.NotificationMessages
            .Include(x => x.Recipients)
            .Where(x => x.Category == NotificationCategory.KeyOverdue
                && x.RelatedEntityType == "ImovelChaveMovimento"
                && x.RelatedEntityId == keyMovementId
                && !x.IsArchived)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var message in messages)
        {
            message.IsArchived = true;
            message.UpdatedAtUtc = now;

            foreach (var recipient in message.Recipients)
            {
                recipient.ReadAtUtc ??= now;
                recipient.DismissedAtUtc ??= now;
                recipient.UpdatedAtUtc = now;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessDueScheduledNotificationsAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
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
    }

    public async Task CheckAndCreateKeyOverdueNotificationsAsync(CancellationToken cancellationToken = default)
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
                "Chave com devoluÃ§Ã£o em atraso",
                BuildKeyOverdueBody(movimento, now),
                recipientIds,
                NotificationCategory.KeyOverdue,
                NotificationPriority.High,
                createdByUserId: null,
                requiresAcknowledgement: false,
                isSystemGenerated: true,
                sendEmail: false,
                sendWhatsApp: false,
                scheduledForUtc: null,
                triggeredAtUtc: now,
                relatedEntityType: "ImovelChaveMovimento",
                relatedEntityId: movimento.Id,
                actionLabel: "Abrir chaves",
                actionTarget: "chaves",
                cancellationToken);
        }
    }

        
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

        var lines = new List<string>
        {
            $"CÃ³digo da chave: {movimento.ChaveCodigo ?? imovel?.ChaveCodigo ?? "-"}",
            $"ImÃ³vel: {endereco}",
            $"ProprietÃ¡rio: {imovel?.Proprietario?.NomeDisplay ?? "-"}",
            $"Retirado por: {movimento.RetiradoPorNome ?? "-"}",
            $"Telefone: {movimento.RetiradoPorTelefone ?? "-"}",
            $"Retirado em: {FormatDateTime(movimento.RetiradoEm)}",
            $"PrevisÃ£o de devoluÃ§Ã£o: {FormatDateTime(previsao)}",
            $"Tempo em atraso: {atrasoTexto}",
            $"Motivo: {movimento.Motivo ?? "-"}"
        };

        if (!string.IsNullOrWhiteSpace(movimento.Observacoes))
        {
            lines.Add($"ObservaÃƒÂ§ÃƒÂµes: {movimento.Observacoes}");
        }

        return string.Join(Environment.NewLine, lines);
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
            throw new InvalidOperationException("Informe o tÃ­tulo da notificaÃ§Ã£o.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("Informe a mensagem da notificaÃ§Ã£o.");
        }

        var recipients = recipientUserIds.Distinct().Where(x => x != Guid.Empty).ToList();
        if (recipients.Count == 0)
        {
            throw new InvalidOperationException("Selecione pelo menos um destinatÃ¡rio.");
        }

        var users = await dbContext.Users
            .Where(x => recipients.Contains(x.Id) && x.IsActive)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            throw new InvalidOperationException("Nenhum destinatÃ¡rio ativo foi encontrado.");
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
            ?? throw new InvalidOperationException("NotificaÃ§Ã£o nÃ£o encontrada para este usuÃ¡rio.");
    }

    private IQueryable<NotificationRecipient> QueryForUser(Guid userId, NotificationFilter filter)
    {
        return dbContext.NotificationRecipients
            .AsNoTracking()
            .Include(x => x.NotificationMessage)!.ThenInclude(x => x!.Deliveries)
            .Where(x => x.UserId == userId && x.DismissedAtUtc == null && !x.NotificationMessage!.IsArchived);
    }

    private IQueryable<NotificationRecipient> QueryForUserIncludingHistory(Guid userId)
    {
        return dbContext.NotificationRecipients
            .AsNoTracking()
            .Include(x => x.NotificationMessage)!.ThenInclude(x => x!.Deliveries)
            .Where(x => x.UserId == userId);
    }

    private IQueryable<NotificationRecipient> QueryHistoryForUser(Guid userId)
    {
        return QueryForUserIncludingHistory(userId)
            .Where(x => x.ReadAtUtc != null || x.DismissedAtUtc != null || x.NotificationMessage!.IsArchived);
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
