using Monthoya.Core.Entities;
using Monthoya.Data.RentalManagement;

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
}
