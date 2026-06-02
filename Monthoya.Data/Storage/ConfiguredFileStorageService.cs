using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Monthoya.Core.Integrations;

namespace Monthoya.Data.Storage;

public sealed class ConfiguredFileStorageService(IConfiguration configuration) : IFileStorageService
{
    private static readonly HttpClient HttpClient = new();

    public Task<string> SaveAsync(
        Stream content,
        string fileName,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        var request = new FileStorageSaveRequest(
            GetDefaultBucket(),
            $"manual/{Guid.NewGuid():N}/{SanitizeFileName(fileName)}",
            fileName,
            contentType);

        return SaveAndReturnPathAsync(content, request, cancellationToken);
    }

    public async Task<StoredFile> SaveAsync(
        Stream content,
        FileStorageSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        request = ResolveConfiguredBucket(request);

        if (UseSupabase())
        {
            await SaveToSupabaseAsync(content, request, cancellationToken);
        }
        else
        {
            await SaveToLocalStorageAsync(content, request, cancellationToken);
        }

        return new StoredFile(request.Bucket, NormalizeObjectPath(request.ObjectPath), request.FileName, request.ContentType);
    }

    public async Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            throw new InvalidOperationException("Caminho do documento não informado.");
        }

        if (Uri.TryCreate(storagePath, UriKind.Absolute, out var absoluteUri)
            && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
        {
            return await HttpClient.GetStreamAsync(absoluteUri, cancellationToken);
        }

        if (Path.IsPathRooted(storagePath))
        {
            return File.OpenRead(storagePath);
        }

        if (UseSupabase())
        {
            var signedUrl = await CreateSignedReadUrlAsync(storagePath, null, cancellationToken);
            return await HttpClient.GetStreamAsync(signedUrl.Url, cancellationToken);
        }

        return File.OpenRead(GetLocalStoragePath(storagePath));
    }

    public async Task<FileStorageSignedUrl> CreateSignedReadUrlAsync(
        string storagePath,
        TimeSpan? expiresIn = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveExpiresIn = expiresIn ?? TimeSpan.FromMinutes(GetSignedUrlMinutes());

        if (!UseSupabase())
        {
            return new FileStorageSignedUrl(GetLocalStoragePath(storagePath), DateTimeOffset.UtcNow.Add(effectiveExpiresIn));
        }

        var (bucket, objectPath) = SplitStoragePath(storagePath);
        using var request = CreateSupabaseRequest(HttpMethod.Post, $"object/sign/{bucket}/{objectPath}");
        request.Content = JsonContent.Create(new SupabaseSignedUrlRequest((int)Math.Ceiling(effectiveExpiresIn.TotalSeconds)));

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SupabaseSignedUrlResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Supabase não retornou URL assinada.");

        var signedUrl = result.SignedUrl;
        if (string.IsNullOrWhiteSpace(signedUrl))
        {
            throw new InvalidOperationException("Supabase não retornou URL assinada.");
        }

        if (signedUrl.StartsWith("/", StringComparison.Ordinal))
        {
            signedUrl = $"{GetSupabaseUrl().TrimEnd('/')}/storage/v1{signedUrl}";
        }

        return new FileStorageSignedUrl(signedUrl, DateTimeOffset.UtcNow.Add(effectiveExpiresIn));
    }

    public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storagePath)
            || Uri.TryCreate(storagePath, UriKind.Absolute, out _)
            || Path.IsPathRooted(storagePath))
        {
            return;
        }

        if (UseSupabase())
        {
            await DeleteFromSupabaseAsync(storagePath, cancellationToken);
            return;
        }

        DeleteFromLocalStorage(storagePath);
    }

    public static string SanitizeFileName(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = $"{Guid.NewGuid():N}.bin";
        }

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            safeFileName = safeFileName.Replace(invalidChar, '-');
        }

        return safeFileName.Replace(" ", "-", StringComparison.Ordinal).ToLowerInvariant();
    }

    private async Task<string> SaveAndReturnPathAsync(
        Stream content,
        FileStorageSaveRequest request,
        CancellationToken cancellationToken)
    {
        var storedFile = await SaveAsync(content, request, cancellationToken);
        return $"{storedFile.Bucket}/{storedFile.ObjectPath}";
    }

    private async Task SaveToSupabaseAsync(
        Stream content,
        FileStorageSaveRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = CreateSupabaseRequest(
            HttpMethod.Post,
            $"object/{request.Bucket}/{NormalizeObjectPath(request.ObjectPath)}");
        httpRequest.Headers.TryAddWithoutValidation("x-upsert", "true");
        httpRequest.Content = new StreamContent(content);
        httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType);

        using var response = await HttpClient.SendAsync(httpRequest, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task SaveToLocalStorageAsync(
        Stream content,
        FileStorageSaveRequest request,
        CancellationToken cancellationToken)
    {
        var localPath = GetLocalStoragePath($"{request.Bucket}/{NormalizeObjectPath(request.ObjectPath)}");
        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
        await using var output = File.Create(localPath);
        await content.CopyToAsync(output, cancellationToken);
    }

    private async Task DeleteFromSupabaseAsync(string storagePath, CancellationToken cancellationToken)
    {
        var (bucket, objectPath) = SplitStoragePath(storagePath);
        using var request = CreateSupabaseRequest(HttpMethod.Delete, $"object/{bucket}/{objectPath}");
        using var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private void DeleteFromLocalStorage(string storagePath)
    {
        var localPath = GetLocalStoragePath(storagePath);
        if (!File.Exists(localPath))
        {
            return;
        }

        File.Delete(localPath);
    }

    private HttpRequestMessage CreateSupabaseRequest(HttpMethod method, string relativePath)
    {
        var key = configuration["Storage:SupabaseKey"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Configure Storage:SupabaseKey via user secrets ou variáveis de ambiente.");
        }

        var request = new HttpRequestMessage(method, $"{GetSupabaseUrl().TrimEnd('/')}/storage/v1/{relativePath.TrimStart('/')}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Headers.TryAddWithoutValidation("apikey", key);
        return request;
    }

    private bool UseSupabase() =>
        configuration["Storage:Provider"]?.Equals("Supabase", StringComparison.OrdinalIgnoreCase) == true;

    private string GetSupabaseUrl() =>
        configuration["Storage:SupabaseUrl"]
        ?? throw new InvalidOperationException("Configure Storage:SupabaseUrl via user secrets ou variáveis de ambiente.");

    private string GetDefaultBucket() => configuration["Storage:DocumentsBucket"] ?? "monthoya-documents";

    private FileStorageSaveRequest ResolveConfiguredBucket(FileStorageSaveRequest request)
    {
        var configuredBucket = request.Bucket switch
        {
            "monthoya-documents" => configuration["Storage:DocumentsBucket"],
            "monthoya-property-images" => configuration["Storage:PropertyImagesBucket"],
            _ when request.ObjectPath.StartsWith("pessoas/", StringComparison.OrdinalIgnoreCase) => configuration["Storage:DocumentsBucket"],
            _ when request.ObjectPath.StartsWith("imoveis/", StringComparison.OrdinalIgnoreCase) => configuration["Storage:PropertyImagesBucket"],
            _ => null
        };

        return string.IsNullOrWhiteSpace(configuredBucket)
            ? request
            : request with { Bucket = configuredBucket };
    }

    private int GetSignedUrlMinutes() =>
        int.TryParse(configuration["Storage:SignedUrlMinutes"], out var minutes) && minutes > 0 ? minutes : 15;

    private string GetLocalStoragePath(string storagePath)
    {
        var configuredRoot = configuration["Storage:LocalRootPath"];
        var rootPath = string.IsNullOrWhiteSpace(configuredRoot)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Monthoya", "storage")
            : configuredRoot;

        return Path.Combine(rootPath, storagePath.Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal));
    }

    private static string NormalizeObjectPath(string objectPath) =>
        objectPath.Trim().TrimStart('/').Replace("\\", "/", StringComparison.Ordinal);

    private (string Bucket, string ObjectPath) SplitStoragePath(string storagePath)
    {
        var normalized = NormalizeObjectPath(storagePath);
        var parts = normalized.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            return (parts[0], parts[1]);
        }

        return (GetDefaultBucket(), normalized);
    }

    private sealed record SupabaseSignedUrlRequest([property: JsonPropertyName("expiresIn")] int ExpiresIn);
    private sealed record SupabaseSignedUrlResponse([property: JsonPropertyName("signedURL")] string? SignedUrl);
}
