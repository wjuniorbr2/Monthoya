namespace Monthoya.Core.Entities;

public sealed class Contract : BaseEntity
{
    public Guid PropertyId { get; set; }

    public Property? Property { get; set; }

    public Guid OwnerId { get; set; }

    public Person? Owner { get; set; }

    public Guid TenantId { get; set; }

    public Person? Tenant { get; set; }

    public DateOnly StartsOn { get; set; }

    public DateOnly? EndsOn { get; set; }

    public decimal MonthlyRent { get; set; }

    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    public string? Notes { get; set; }

    public ICollection<RentInstallment> RentInstallments { get; set; } = new List<RentInstallment>();
}
