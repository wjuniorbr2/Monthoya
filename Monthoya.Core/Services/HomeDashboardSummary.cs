namespace Monthoya.Core.Services;

public sealed record HomeDashboardSummary(
    int TotalProperties,
    int AvailableRentals,
    int ActiveContracts,
    decimal PendingRentAmount,
    IReadOnlyList<PropertyMapItem> AvailableRentalProperties);
