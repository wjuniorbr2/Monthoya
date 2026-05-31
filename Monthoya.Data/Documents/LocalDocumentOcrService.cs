using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Monthoya.Core.Integrations;

namespace Monthoya.Data.Documents;

public sealed class LocalDocumentOcrService(
    IFileStorageService fileStorageService,
    IConfiguration configuration) : IDocumentOcrService
{
    public async Task<DocumentOcrResult> ExtractTextAsync(
        string storagePath,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        if (IsPlainText(storagePath, contentType))
        {
            return await ExtractPlainTextAsync(storagePath, cancellationToken);
        }

        if (!IsImage(storagePath, contentType))
        {
            return new DocumentOcrResult(
                false,
                null,
                "OCR local configurado como base. Para PDFs, converta a página digitalizada para imagem ou configure um motor OCR local com suporte a PDF.");
        }

        return await ExtractImageTextAsync(storagePath, cancellationToken);
    }

    private async Task<DocumentOcrResult> ExtractPlainTextAsync(string storagePath, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await fileStorageService.OpenReadAsync(storagePath, cancellationToken);
            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync(cancellationToken);

            return string.IsNullOrWhiteSpace(text)
                ? new DocumentOcrResult(false, null, "O arquivo de texto está vazio.")
                : new DocumentOcrResult(true, text.Trim());
        }
        catch (Exception ex)
        {
            return new DocumentOcrResult(false, null, $"Não foi possível ler o documento para OCR: {ex.Message}");
        }
    }

    private async Task<DocumentOcrResult> ExtractImageTextAsync(string storagePath, CancellationToken cancellationToken)
    {
        var tesseractCommand = configuration["Ocr:TesseractCommand"];
        if (string.IsNullOrWhiteSpace(tesseractCommand))
        {
            tesseractCommand = "tesseract";
        }

        var language = configuration["Ocr:Language"];
        if (string.IsNullOrWhiteSpace(language))
        {
            language = "por+eng";
        }

        var tempFile = await CreateReadableTempFileAsync(storagePath, cancellationToken);
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = tesseractCommand,
                Arguments = $"\"{tempFile}\" stdout -l {language} --psm 1",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new DocumentOcrResult(false, null, "Não foi possível iniciar o OCR local.");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                return new DocumentOcrResult(false, null, string.IsNullOrWhiteSpace(error)
                    ? "OCR local terminou com erro."
                    : error.Trim());
            }

            return string.IsNullOrWhiteSpace(output)
                ? new DocumentOcrResult(false, null, "OCR local não encontrou texto no documento.")
                : new DocumentOcrResult(true, output.Trim());
        }
        catch (Exception ex)
        {
            return new DocumentOcrResult(false, null, $"OCR local indisponível ou falhou: {ex.Message}");
        }
        finally
        {
            TryDeleteTempFile(tempFile, storagePath);
        }
    }

    private async Task<string> CreateReadableTempFileAsync(string storagePath, CancellationToken cancellationToken)
    {
        if (Path.IsPathRooted(storagePath) && File.Exists(storagePath))
        {
            return storagePath;
        }

        var extension = Path.GetExtension(Uri.TryCreate(storagePath, UriKind.Absolute, out var uri) ? uri.AbsolutePath : storagePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }

        var tempFile = Path.Combine(Path.GetTempPath(), $"monthoya-ocr-{Guid.NewGuid():N}{extension}");
        await using var source = await fileStorageService.OpenReadAsync(storagePath, cancellationToken);
        await using var destination = File.Create(tempFile);
        await source.CopyToAsync(destination, cancellationToken);
        return tempFile;
    }

    private static void TryDeleteTempFile(string tempFile, string originalStoragePath)
    {
        if (Path.IsPathRooted(originalStoragePath)
            && Path.GetFullPath(tempFile).Equals(Path.GetFullPath(originalStoragePath), StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            File.Delete(tempFile);
        }
        catch
        {
            // Best effort cleanup only.
        }
    }

    private static bool IsPlainText(string storagePath, string? contentType)
    {
        if (contentType?.StartsWith("text/", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        var extension = Path.GetExtension(storagePath);
        return extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".md", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImage(string storagePath, string? contentType)
    {
        if (contentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        var extension = Path.GetExtension(storagePath);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".tif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
    }
}
