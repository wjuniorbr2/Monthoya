namespace Monthoya.Core.Entities;

public sealed class BoletoRecord : BaseEntity
{
    public Guid RentInstallmentId { get; set; }

    public RentInstallment? RentInstallment { get; set; }

    public string? ExternalId { get; set; }

    public string Status { get; set; } = "Pending";

    public string? Barcode { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal Amount { get; set; }
}
