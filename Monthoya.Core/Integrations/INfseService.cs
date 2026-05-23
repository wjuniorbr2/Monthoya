namespace Monthoya.Core.Integrations;

public interface INfseService
{
    Task<IntegrationResult> IssueAsync(Guid contractId, CancellationToken cancellationToken = default);
}
