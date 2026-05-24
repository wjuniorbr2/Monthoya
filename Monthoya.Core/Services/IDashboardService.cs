namespace Monthoya.Core.Services;

public interface IDashboardService
{
    Task<HomeDashboardSummary> GetHomeSummaryAsync(CancellationToken cancellationToken = default);
}
