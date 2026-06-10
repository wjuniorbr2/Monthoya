using Monthoya.Core.Entities;
using Monthoya.Core.Integrations;
using Monthoya.Data.Storage;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    private async Task<string> StorePessoaDocumentoAsync(
        Guid documentoId,
        Guid pessoaId,
        string storagePath,
        string? contentType,
        CancellationToken cancellationToken)
    {
        if (fileStorageService is null || !File.Exists(storagePath))
        {
            return NormalizeStoredPath(storagePath);
        }

        var fileName = Path.GetFileName(storagePath);
        var safeFileName = ConfiguredFileStorageService.SanitizeFileName(fileName);
        var objectPath = $"pessoas/{pessoaId}/documentos/{documentoId}/{safeFileName}";
        await using var stream = File.OpenRead(storagePath);
        var stored = await fileStorageService.SaveAsync(
            stream,
            new FileStorageSaveRequest("monthoya-documents", objectPath, fileName, contentType ?? GuessContentType(fileName)),
            cancellationToken);

        return $"{stored.Bucket}/{stored.ObjectPath}";
    }

    private async Task<string> StoreImovelImagemAsync(
        Guid imovelId,
        string storagePath,
        string? contentType,
        ImovelMediaCategory category,
        CancellationToken cancellationToken)
    {
        if (fileStorageService is null || !File.Exists(storagePath))
        {
            return NormalizeStoredPath(storagePath);
        }

        var imageId = Guid.NewGuid();
        var fileName = Path.GetFileName(storagePath);
        var safeFileName = ConfiguredFileStorageService.SanitizeFileName(fileName);
        var folder = category switch
        {
            ImovelMediaCategory.Document => "documentos",
            ImovelMediaCategory.InspectionPhoto => "vistorias",
            ImovelMediaCategory.MaintenancePhoto => "manutencoes",
            ImovelMediaCategory.Other => "outros",
            _ => "fotos"
        };
        var objectPath = $"imoveis/{imovelId}/{folder}/{imageId}/{safeFileName}";
        await using var stream = File.OpenRead(storagePath);
        var stored = await fileStorageService.SaveAsync(
            stream,
            new FileStorageSaveRequest("monthoya-property-images", objectPath, fileName, contentType ?? GuessContentType(fileName)),
            cancellationToken);

        return $"{stored.Bucket}/{stored.ObjectPath}";
    }

    private static string NormalizeStoredPath(string storagePath) =>
        storagePath.Replace("\\", "/", StringComparison.Ordinal).Trim();

    private static string GuessContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
}



