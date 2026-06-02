using System.IO;
using System.Net.Http;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly HttpClient PessoaDocumentoDownloadHttpClient = new();

    private async Task<PessoaDocumentoLocalFile> PreparePessoaDocumentoLocalFileAsync(PessoaDocumentoSummary document)
    {
        var openTarget = document.Id == Guid.Empty
            ? document.StoragePath
            : await _rentalManagementService.GetPessoaDocumentoOpenTargetAsync(document.Id);

        if (Path.IsPathRooted(openTarget))
        {
            if (!File.Exists(openTarget))
            {
                throw new FileNotFoundException("O arquivo do documento não foi encontrado no computador.", openTarget);
            }

            return new PessoaDocumentoLocalFile(openTarget, false);
        }

        if (!Uri.TryCreate(openTarget, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Não foi possível localizar o arquivo do documento.");
        }

        var tempPath = Path.Combine(
            Path.GetTempPath(),
            "Monthoya",
            "documentos",
            $"{Guid.NewGuid():N}{Path.GetExtension(document.Arquivo)}");
        Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

        await using var source = await PessoaDocumentoDownloadHttpClient.GetStreamAsync(uri);
        await using var target = File.Create(tempPath);
        await source.CopyToAsync(target);
        return new PessoaDocumentoLocalFile(tempPath, true);
    }

    private static void TryDeletePessoaDocumentoLocalFile(PessoaDocumentoLocalFile localFile)
    {
        if (!localFile.IsTemporary)
        {
            return;
        }

        try
        {
            File.Delete(localFile.Path);
        }
        catch
        {
            // Temporary cleanup should not interrupt the document workflow.
        }
    }

    private sealed record PessoaDocumentoLocalFile(string Path, bool IsTemporary);
}
