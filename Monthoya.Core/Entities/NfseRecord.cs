namespace Monthoya.Core.Entities;

public sealed class NfseRecord : BaseEntity
{
    public Guid? ContractId { get; set; }

    public Contract? Contract { get; set; }

    public string? ExternalId { get; set; }

    public string Status { get; set; } = "Pending";

    public string ServiceDescription { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}
