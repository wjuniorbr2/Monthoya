namespace Monthoya.Core.Integrations;

public sealed record IntegrationResult(
    bool Success,
    string? ExternalId = null,
    string? Message = null);
