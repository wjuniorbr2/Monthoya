namespace Monthoya.Core.Integrations;

public interface IBoletoService
{
    Task<IntegrationResult> IssueAsync(Guid rentInstallmentId, CancellationToken cancellationToken = default);
}
