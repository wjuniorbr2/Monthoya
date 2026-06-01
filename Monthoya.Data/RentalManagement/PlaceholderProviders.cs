using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed class LocalBoletoProvider : IBoletoProvider
{
    private const string NotConfigured = "Integração bancária ainda não configurada.";

    public Task<BoletoProviderResult> GenerateBoletoAsync(Boleto boleto, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BoletoProviderResult(false, NotConfigured));

    public Task<BoletoProviderResult> RegisterBoletoAsync(Boleto boleto, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BoletoProviderResult(false, NotConfigured));

    public Task<BoletoProviderResult> CancelBoletoAsync(Boleto boleto, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BoletoProviderResult(false, NotConfigured));

    public Task<BoletoProviderResult> GetBoletoStatusAsync(Boleto boleto, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BoletoProviderResult(false, NotConfigured));

    public Task<BoletoProviderResult> DownloadBoletoPdfAsync(Boleto boleto, CancellationToken cancellationToken = default) =>
        Task.FromResult(new BoletoProviderResult(false, NotConfigured));
}

public sealed class ManualPortalNfseProvider : INfseProvider
{
    private const string NotConfigured = "Integração automática com NFS-e ainda não configurada. Use o fluxo manual/semi-manual.";

    public Task<NfseProviderResult> IssueNotaFiscalAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default) =>
        Task.FromResult(new NfseProviderResult(false, NotConfigured));

    public Task<NfseProviderResult> CancelNotaFiscalAsync(NotaFiscal notaFiscal, string motivo, CancellationToken cancellationToken = default) =>
        Task.FromResult(new NfseProviderResult(false, NotConfigured));

    public Task<NfseProviderResult> DownloadNotaFiscalPdfAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default) =>
        Task.FromResult(new NfseProviderResult(false, NotConfigured));

    // Backward-compatible aliases used by existing tests/older call sites.
    public Task<NfseProviderResult> EmitirNotaFiscalAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default) =>
        IssueNotaFiscalAsync(notaFiscal, cancellationToken);

    public Task<NfseProviderResult> CancelarNotaFiscalAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default) =>
        CancelNotaFiscalAsync(notaFiscal, string.Empty, cancellationToken);

    public Task<NfseProviderResult> BaixarPdfNotaFiscalAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default) =>
        DownloadNotaFiscalPdfAsync(notaFiscal, cancellationToken);
}
