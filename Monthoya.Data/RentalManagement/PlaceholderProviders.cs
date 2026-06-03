using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed class LocalBoletoProvider : IBoletoProvider
{
    private const string NotConfigured = "Integração bancária ainda não configurada.";

    public Task<BoletoProviderResult> GenerateBoletoAsync(Boleto boleto, CancellationToken cancellationToken = default) =>
        Task.FromResult(Failed());

    public Task<BoletoProviderResult> RegisterBoletoAsync(Boleto boleto, CancellationToken cancellationToken = default) =>
        Task.FromResult(Failed());

    public Task<BoletoProviderResult> QueryStatusAsync(Boleto boleto, CancellationToken cancellationToken = default) =>
        Task.FromResult(Failed());

    public Task<BoletoProviderResult> CancelAsync(Boleto boleto, CancellationToken cancellationToken = default) =>
        Task.FromResult(Failed());

    // Backward-compatible aliases used by older call sites.
    public Task<BoletoProviderResult> CancelBoletoAsync(Boleto boleto, CancellationToken cancellationToken = default) =>
        CancelAsync(boleto, cancellationToken);

    public Task<BoletoProviderResult> GetBoletoStatusAsync(Boleto boleto, CancellationToken cancellationToken = default) =>
        QueryStatusAsync(boleto, cancellationToken);

    public Task<BoletoProviderResult> DownloadBoletoPdfAsync(Boleto boleto, CancellationToken cancellationToken = default) =>
        Task.FromResult(Failed());

    private static BoletoProviderResult Failed() =>
        new(false, null, null, NotConfigured);
}

public sealed class ManualPortalNfseProvider : INfseProvider
{
    private const string NotConfigured = "Integração automática com NFS-e ainda não configurada. Use o fluxo manual/semi-manual.";

    public Task<NfseProviderResult> IssueAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default) =>
        Task.FromResult(Failed());

    public Task<NfseProviderResult> CancelAsync(NotaFiscal notaFiscal, string reason, CancellationToken cancellationToken = default) =>
        Task.FromResult(Failed());

    // Backward-compatible aliases used by existing tests/older call sites.
    public Task<NfseProviderResult> EmitirNotaFiscalAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default) =>
        IssueAsync(notaFiscal, cancellationToken);

    public Task<NfseProviderResult> CancelarNotaFiscalAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default) =>
        CancelAsync(notaFiscal, string.Empty, cancellationToken);

    public Task<NfseProviderResult> BaixarPdfNotaFiscalAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default) =>
        Task.FromResult(Failed());

    private static NfseProviderResult Failed() =>
        new(false, null, null, null, NotConfigured);
}
