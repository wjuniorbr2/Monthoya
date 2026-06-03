using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Integrations;
using Monthoya.Core.Services;
using Monthoya.Data.RentalManagement;
using Monthoya.Data.Storage;
using Monthoya.Data.Users;

namespace Monthoya.Tests;

public class AuthAndUserTests
{
    [Fact]
    public async Task PlaceholderProviders_ReportPendingConfiguration()
    {
        var boletoProvider = new LocalBoletoProvider();
        var nfseProvider = new ManualPortalNfseProvider();

        var boletoResult = await boletoProvider.GenerateBoletoAsync(new Boleto
        {
            Valor = 1500m,
            DataVencimento = new DateOnly(2026, 6, 10)
        });

        var nfseResult = await nfseProvider.EmitirNotaFiscalAsync(new NotaFiscal
        {
            ValorServico = 150m,
            Provider = "manual_portal"
        });

        Assert.False(boletoResult.Success);
        Assert.Equal("Integração bancária ainda não configurada.", boletoResult.ErrorMessage);
        Assert.False(nfseResult.Success);
        Assert.Equal("Integração automática com NFS-e ainda não configurada. Use o fluxo manual/semi-manual.", nfseResult.ErrorMessage);
    }

    private static MonthoyaDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MonthoyaDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MonthoyaDbContext(options);
    }

    private sealed class FakeDocumentOcrService(string text) : IDocumentOcrService
    {
        public Task<DocumentOcrResult> ExtractTextAsync(
            string storagePath,
            string? contentType = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(DocumentOcrResult.Success(text));
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public Task<FileStorageResult> SaveAsync(
            Stream content,
            string fileName,
            string? contentType = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new FileStorageResult(fileName, fileName, contentType));

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream());

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
