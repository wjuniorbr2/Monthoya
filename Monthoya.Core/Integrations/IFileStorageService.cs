namespace Monthoya.Core.Integrations;

public interface IFileStorageService
{
    Task<string> SaveAsync(
        Stream content,
        string fileName,
        string? contentType = null,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);
}
