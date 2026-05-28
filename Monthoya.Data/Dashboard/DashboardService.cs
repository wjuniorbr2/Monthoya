using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Dashboard;

public sealed class DashboardService(MonthoyaDbContext dbContext) : IDashboardService
{
    public async Task<HomeDashboardSummary> GetHomeSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var totalProperties = await dbContext.Properties.CountAsync(cancellationToken);
        var availableRentals = await dbContext.Properties.CountAsync(
            x => x.AvailabilityStatus == PropertyAvailabilityStatus.DisponivelParaLocacao,
            cancellationToken);
        var activeContracts = await dbContext.Contracts.CountAsync(
            x => x.Status == ContractStatus.Active,
            cancellationToken);
        var pendingRentAmount = await dbContext.RentInstallments
            .Where(x => x.Status == RentInstallmentStatus.Pending || x.Status == RentInstallmentStatus.Overdue)
            .SumAsync(x => x.Amount, cancellationToken);

        var mapItems = await dbContext.Properties
            .Where(x =>
                x.AvailabilityStatus == PropertyAvailabilityStatus.DisponivelParaLocacao &&
                x.Latitude.HasValue &&
                x.Longitude.HasValue)
            .OrderBy(x => x.Code)
            .Select(x => new PropertyMapItem(
                x.Id,
                x.Code,
                x.AddressLine,
                x.City,
                x.State,
                x.RentalPrice,
                x.Latitude!.Value,
                x.Longitude!.Value))
            .ToListAsync(cancellationToken);

        return new HomeDashboardSummary(
            totalProperties,
            availableRentals,
            activeContracts,
            pendingRentAmount,
            mapItems);
    }
}
