namespace Monthoya.Core.Entities;

public sealed class Payment : BaseEntity
{
    public Guid RentInstallmentId { get; set; }

    public RentInstallment? RentInstallment { get; set; }

    public DateTimeOffset PaidAtUtc { get; set; }

    public decimal Amount { get; set; }

    public string? Method { get; set; }

    public string? Notes { get; set; }
}
