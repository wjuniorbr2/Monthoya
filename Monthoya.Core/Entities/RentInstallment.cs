namespace Monthoya.Core.Entities;

public sealed class RentInstallment : BaseEntity
{
    public Guid ContractId { get; set; }

    public Contract? Contract { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal Amount { get; set; }

    public RentInstallmentStatus Status { get; set; } = RentInstallmentStatus.Pending;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
