namespace Monthoya.Core.Integrations;

public interface IDocumentOcrService
{
    Task<DocumentOcrResult> ExtractTextAsync(
        string storagePath,
        string? contentType = null,
        CancellationToken cancellationToken = default);
}

public sealed record DocumentOcrResult(
    bool Succeeded,
    string? ExtractedText,
    string? ErrorMessage = null);
