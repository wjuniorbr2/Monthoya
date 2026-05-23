namespace Monthoya.Core.Entities;

public sealed class Property : BaseEntity
{
    public string Code { get; set; } = string.Empty;

    public string AddressLine { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string? PostalCode { get; set; }

    public Guid? OwnerId { get; set; }

    public Person? Owner { get; set; }

    public decimal? ListingPrice { get; set; }

    public decimal? RentalPrice { get; set; }

    public string? Notes { get; set; }

    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
}
