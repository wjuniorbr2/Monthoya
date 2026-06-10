using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Notifications;

public sealed partial class NotificationService
{
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
}
