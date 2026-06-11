using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed class ChavesSafeRentalManagementService(
    RentalManagementService inner,
    MonthoyaDbContext dbContext) : IRentalManagementService
{
    public Task<IReadOnlyList<PessoaSummary>> GetPessoasAsync(CancellationToken cancellationToken = default) => inner.GetPessoasAsync(cancellationToken);
    public Task<PessoaDetails?> GetPessoaAsync(Guid pessoaId, CancellationToken cancellationToken = default) => inner.GetPessoaAsync(pessoaId, cancellationToken);
    public Task<IReadOnlyList<string>> GetStreetSuggestionsAsync(CancellationToken cancellationToken = default) => inner.GetStreetSuggestionsAsync(cancellationToken);
    public Task<PessoaSummary> CreatePessoaAsync(CreatePessoaRequest request, CancellationToken cancellationToken = default) => inner.CreatePessoaAsync(request, cancellationToken);
    public Task<PessoaSummary> UpdatePessoaAsync(UpdatePessoaRequest request, CancellationToken cancellationToken = default) => inner.UpdatePessoaAsync(request, cancellationToken);
    public Task SetPessoaActiveAsync(Guid pessoaId, bool isActive, CancellationToken cancellationToken = default) => inner.SetPessoaActiveAsync(pessoaId, isActive, cancellationToken);
    public Task<PessoaDocumentoSummary> CreatePessoaDocumentoAsync(CreatePessoaDocumentoRequest request, CancellationToken cancellationToken = default) => inner.CreatePessoaDocumentoAsync(request, cancellationToken);
    public Task<IReadOnlyList<PessoaDocumentoSummary>> GetPessoaDocumentosAsync(Guid? pessoaId = null, CancellationToken cancellationToken = default) => inner.GetPessoaDocumentosAsync(pessoaId, cancellationToken);
    public Task<PessoaDocumentoSummary> UpdatePessoaDocumentoOcrAsync(UpdatePessoaDocumentoOcrRequest request, CancellationToken cancellationToken = default) => inner.UpdatePessoaDocumentoOcrAsync(request, cancellationToken);
    public Task DeletePessoaDocumentoAsync(Guid documentoId, CancellationToken cancellationToken = default) => inner.DeletePessoaDocumentoAsync(documentoId, cancellationToken);
    public Task<string> GetPessoaDocumentoOpenTargetAsync(Guid documentoId, CancellationToken cancellationToken = default) => inner.GetPessoaDocumentoOpenTargetAsync(documentoId, cancellationToken);
    public Task<PessoaContratoAutofillContext?> GetPessoaContratoAutofillContextAsync(Guid pessoaId, CancellationToken cancellationToken = default) => inner.GetPessoaContratoAutofillContextAsync(pessoaId, cancellationToken);
    public Task<IReadOnlyList<ImovelSummary>> GetImoveisAsync(CancellationToken cancellationToken = default) => inner.GetImoveisAsync(cancellationToken);
    public Task<ImovelDetails?> GetImovelAsync(Guid imovelId, CancellationToken cancellationToken = default) => inner.GetImovelAsync(imovelId, cancellationToken);
    public Task<ImovelSummary> CreateImovelAsync(CreateImovelRequest request, CancellationToken cancellationToken = default) => inner.CreateImovelAsync(request, cancellationToken);
    public Task<ImovelSummary> UpdateImovelAsync(UpdateImovelRequest request, CancellationToken cancellationToken = default) => ((IRentalManagementService)inner).UpdateImovelAsync(request, cancellationToken);
    public Task SetImovelActiveAsync(Guid imovelId, bool isActive, CancellationToken cancellationToken = default) => inner.SetImovelActiveAsync(imovelId, isActive, cancellationToken);
    public Task<ImovelImagemSummary> CreateImovelImagemAsync(CreateImovelImagemRequest request, CancellationToken cancellationToken = default) => inner.CreateImovelImagemAsync(request, cancellationToken);
    public Task<IReadOnlyList<ImovelImagemSummary>> GetImovelImagensAsync(Guid imovelId, CancellationToken cancellationToken = default) => inner.GetImovelImagensAsync(imovelId, cancellationToken);
    public Task<IReadOnlyList<LocacaoSummary>> GetLocacoesAsync(CancellationToken cancellationToken = default) => inner.GetLocacoesAsync(cancellationToken);
    public Task<LocacaoDetails> GetLocacaoAsync(Guid id, CancellationToken cancellationToken = default) => inner.GetLocacaoAsync(id, cancellationToken);
    public Task<LocacaoDetails> CreateLocacaoAsync(CreateLocacaoRequest request, CancellationToken cancellationToken = default) => inner.CreateLocacaoAsync(request, cancellationToken);
    public Task<LocacaoDetails> UpdateLocacaoAsync(UpdateLocacaoRequest request, CancellationToken cancellationToken = default) => inner.UpdateLocacaoAsync(request, cancellationToken);
    public Task<IReadOnlyList<IndiceReajusteSummary>> GetIndicesReajusteAsync(CancellationToken cancellationToken = default) => inner.GetIndicesReajusteAsync(cancellationToken);
    public Task<IReadOnlyList<FinanceiroSummary>> GetLancamentosFinanceirosAsync(CancellationToken cancellationToken = default) => inner.GetLancamentosFinanceirosAsync(cancellationToken);
    public Task<IReadOnlyList<BoletoSummary>> GetBoletosAsync(CancellationToken cancellationToken = default) => inner.GetBoletosAsync(cancellationToken);
    public Task<IReadOnlyList<NotaFiscalSummary>> GetNotasFiscaisAsync(CancellationToken cancellationToken = default) => inner.GetNotasFiscaisAsync(cancellationToken);
    public Task<IReadOnlyList<DocumentoModeloSummary>> GetDocumentoModelosAsync(CancellationToken cancellationToken = default) => inner.GetDocumentoModelosAsync(cancellationToken);
    public Task<IReadOnlyList<DimobDeclaracaoSummary>> GetDimobDeclaracoesAsync(CancellationToken cancellationToken = default) => inner.GetDimobDeclaracoesAsync(cancellationToken);
    public Task<IReadOnlyList<ManutencaoSummary>> GetManutencoesAsync(CancellationToken cancellationToken = default) => inner.GetManutencoesAsync(cancellationToken);
    public Task<IReadOnlyList<VistoriaSummary>> GetVistoriasAsync(Guid? imovelId = null, CancellationToken cancellationToken = default) => inner.GetVistoriasAsync(imovelId, cancellationToken);
    public Task<VistoriaSummary> CreateVistoriaAsync(CreateVistoriaRequest request, CancellationToken cancellationToken = default) => inner.CreateVistoriaAsync(request, cancellationToken);

    public async Task<IReadOnlyList<ImovelChaveMovimentoSummary>> GetImovelChaveMovimentosAsync(Guid? imovelId = null, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await GetImovelChaveMovimentosCoreAsync(imovelId, cancellationToken);
    }

    public async Task<ImovelChaveMovimentoSummary> CreateImovelChaveMovimentoAsync(CreateImovelChaveMovimentoRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.ImovelId == Guid.Empty) throw new InvalidOperationException("Selecione o imóvel da chave.");

        var imovel = await dbContext.Imoveis.SingleOrDefaultAsync(x => x.Id == request.ImovelId, cancellationToken)
            ?? throw new InvalidOperationException("Imóvel não encontrado.");

        var alreadyTaken = await dbContext.ImovelChaveMovimentos.AnyAsync(x => x.ImovelId == request.ImovelId && !x.DevolvidoEm.HasValue, cancellationToken);
        if (alreadyTaken) throw new InvalidOperationException("Esta chave já está retirada.");

        if (string.IsNullOrWhiteSpace(request.RetiradoPorNome)) throw new InvalidOperationException("Informe quem retirou a chave.");
        if (!request.PrevisaoDevolucaoEm.HasValue) throw new InvalidOperationException("Informe a previsão de devolução da chave.");

        var retiradoEm = ToUtc(request.RetiradoEm ?? DateTimeOffset.UtcNow);
        var previsaoDevolucaoEm = ToUtc(request.PrevisaoDevolucaoEm.Value);
        if (previsaoDevolucaoEm < retiradoEm) throw new InvalidOperationException("A previsão de devolução não pode ser anterior à retirada da chave.");

        var movimento = new ImovelChaveMovimento
        {
            ImovelId = request.ImovelId,
            ChaveCodigo = TrimOrNull(request.ChaveCodigo) ?? imovel.ChaveCodigo,
            Tipo = ImovelChaveMovimentoTipo.Retirada,
            RetiradoPorNome = TrimOrNull(request.RetiradoPorNome),
            RetiradoPorTelefone = DigitsOrNull(request.RetiradoPorTelefone),
            RetiradoPorDocumento = TrimOrNull(request.RetiradoPorDocumento),
            RetiradoPorRelacao = TrimOrNull(request.RetiradoPorRelacao),
            Motivo = TrimOrNull(request.Motivo),
            RetiradoEm = retiradoEm,
            PrevisaoDevolucaoEm = previsaoDevolucaoEm,
            Status = ImovelChaveMovimentoStatus.Retirada,
            Observacoes = TrimOrNull(request.Observacoes)
        };

        dbContext.ImovelChaveMovimentos.Add(movimento);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetImovelChaveMovimentosCoreAsync(request.ImovelId, cancellationToken)).Single(x => x.Id == movimento.Id);
    }

    public async Task<ImovelChaveMovimentoSummary> ReturnImovelChaveMovimentoAsync(ReturnImovelChaveMovimentoRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.MovimentoId == Guid.Empty) throw new InvalidOperationException("Selecione a retirada de chave.");

        var movimento = await dbContext.ImovelChaveMovimentos.SingleOrDefaultAsync(x => x.Id == request.MovimentoId, cancellationToken)
            ?? throw new InvalidOperationException("Movimentação de chave não encontrada.");

        if (movimento.DevolvidoEm.HasValue) throw new InvalidOperationException("Esta chave já foi devolvida.");

        var devolvidoEm = ToUtc(request.DevolvidoEm ?? DateTimeOffset.UtcNow);
        if (movimento.RetiradoEm.HasValue && devolvidoEm < ToUtc(movimento.RetiradoEm.Value))
        {
            throw new InvalidOperationException("A data de devolução não pode ser anterior à retirada da chave.");
        }

        movimento.Tipo = ImovelChaveMovimentoTipo.Devolucao;
        movimento.Status = ImovelChaveMovimentoStatus.ComImobiliaria;
        movimento.DevolvidoEm = devolvidoEm;
        movimento.DevolvidoParaNome = TrimOrNull(request.DevolvidoParaNome);
        movimento.Observacoes = MergeNotes(movimento.Observacoes, request.Observacoes);
        movimento.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetImovelChaveMovimentosCoreAsync(movimento.ImovelId, cancellationToken)).Single(x => x.Id == movimento.Id);
    }

    public async Task DeleteImovelChaveMovimentoAsync(Guid movimentoId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var movimento = await dbContext.ImovelChaveMovimentos.SingleOrDefaultAsync(x => x.Id == movimentoId, cancellationToken)
            ?? throw new InvalidOperationException("Entrada do histórico de chaves não encontrada.");

        dbContext.ImovelChaveMovimentos.Remove(movimento);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ImovelChaveMovimentoSummary>> GetImovelChaveMovimentosCoreAsync(Guid? imovelId, CancellationToken cancellationToken)
    {
        var query = dbContext.ImovelChaveMovimentos.AsNoTracking().Include(x => x.Imovel).AsQueryable();
        if (imovelId.HasValue) query = query.Where(x => x.ImovelId == imovelId.Value);

        var now = DateTimeOffset.UtcNow;
        var movimentos = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);

        return movimentos.Select(x =>
        {
            var status = x.Status == ImovelChaveMovimentoStatus.Retirada && x.PrevisaoDevolucaoEm.HasValue && x.PrevisaoDevolucaoEm.Value < now && !x.DevolvidoEm.HasValue
                ? "Em atraso"
                : GetChavesStatusLabel(x.Status);

            return new ImovelChaveMovimentoSummary(
                x.Id,
                x.ImovelId,
                x.Imovel is null ? "-" : $"{x.Imovel.Rua}, {x.Imovel.Numero}".Trim().Trim(','),
                GetChavesTipoLabel(x.Tipo),
                status,
                x.ChaveCodigo,
                x.RetiradoPorNome,
                FormatPhoneForDisplay(x.RetiradoPorTelefone),
                x.RetiradoPorDocumento,
                x.RetiradoPorRelacao,
                x.Motivo,
                ToLocalOrNull(x.RetiradoEm),
                ToLocalOrNull(x.PrevisaoDevolucaoEm),
                ToLocalOrNull(x.DevolvidoEm),
                x.DevolvidoParaNome,
                x.Observacoes);
        }).ToList();
    }

    private static DateTimeOffset ToUtc(DateTimeOffset value) => value.Offset == TimeSpan.Zero ? value : value.ToUniversalTime();
    private static DateTimeOffset? ToLocalOrNull(DateTimeOffset? value) => value.HasValue ? value.Value.ToLocalTime() : null;
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? DigitsOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    private static string? FormatPhoneForDisplay(string? value)
    {
        var digits = DigitsOrNull(value);
        return digits switch
        {
            null => null,
            { Length: 11 } => $"({digits[..2]}) {digits[2..7]}-{digits[7..]}",
            { Length: 10 } => $"({digits[..2]}) {digits[2..6]}-{digits[6..]}",
            _ => value ?? string.Empty
        };
    }

    private static string GetChavesTipoLabel(ImovelChaveMovimentoTipo tipo) => tipo switch
    {
        ImovelChaveMovimentoTipo.Retirada => "Retirada",
        ImovelChaveMovimentoTipo.Devolucao => "Devolução",
        ImovelChaveMovimentoTipo.Transferencia => "Transferência",
        ImovelChaveMovimentoTipo.MarcadaPerdida => "Marcada perdida",
        _ => "Outro"
    };

    private static string GetChavesStatusLabel(ImovelChaveMovimentoStatus status) => status switch
    {
        ImovelChaveMovimentoStatus.ComImobiliaria => "Na imobiliária",
        ImovelChaveMovimentoStatus.Retirada => "Retirada",
        ImovelChaveMovimentoStatus.EmAtraso => "Em atraso",
        ImovelChaveMovimentoStatus.Perdida => "Perdida",
        ImovelChaveMovimentoStatus.Inativa => "Inativa",
        _ => status.ToString()
    };

    private static string? MergeNotes(string? current, string? next)
    {
        current = TrimOrNull(current);
        next = TrimOrNull(next);
        if (string.IsNullOrWhiteSpace(current)) return next;
        if (string.IsNullOrWhiteSpace(next)) return current;
        return $"{current}{Environment.NewLine}{next}";
    }
}
