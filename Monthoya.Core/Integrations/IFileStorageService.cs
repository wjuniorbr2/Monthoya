namespace Monthoya.Core.Integrations;

public interface IFileStorageService
{
    Task<string> SaveAsync(
        Stream content,
        string fileName,
        string? contentType = null,
        CancellationToken cancellationToken = default);

    Task<StoredFile> SaveAsync(
        Stream content,
        FileStorageSaveRequest request,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);

    Task<FileStorageSignedUrl> CreateSignedReadUrlAsync(
        string storagePath,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}

public sealed record FileStorageSaveRequest(
    string Bucket,
    string ObjectPath,
    string FileName,
    string? ContentType = null);

public sealed record StoredFile(
    string Bucket,
    string ObjectPath,
    string FileName,
    string? ContentType);

public sealed record FileStorageSignedUrl(
    string Url,
    DateTimeOffset ExpiresAtUtc);
