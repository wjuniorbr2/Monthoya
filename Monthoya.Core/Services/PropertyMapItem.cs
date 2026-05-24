namespace Monthoya.Core.Services;

public sealed record PropertyMapItem(
    Guid Id,
    string Code,
    string AddressLine,
    string City,
    string State,
    decimal? RentalPrice,
    decimal Latitude,
    decimal Longitude);
