using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    private async Task<LocacaoValidationResult> ValidateAndNormalizeLocacaoAsync(
        CreateLocacaoRequest request,
        Guid? currentLocacaoId,
        CancellationToken cancellationToken)
    {
        if (request.ImovelId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione o imóvel da locação.");
        }

        var imovelExists = await dbContext.Imoveis.AnyAsync(x => x.Id == request.ImovelId, cancellationToken);
        if (!imovelExists)
        {
            throw new InvalidOperationException("Imóvel não encontrado.");
        }

        var status = request.Status ?? LocacaoStatus.Rascunho;
        var proprietarios = request.Partes.Where(x => x.TipoParte == TipoParteLocacao.Proprietario).ToList();
        var locatarios = request.Partes.Where(x => x.TipoParte == TipoParteLocacao.Locatario).ToList();
        var proprietarioIds = proprietarios.Select(x => x.PessoaId).ToHashSet();
        if (locatarios.Any(x => proprietarioIds.Contains(x.PessoaId)))
        {
            throw new InvalidOperationException("A mesma pessoa nÃ£o pode ser proprietÃ¡rio e locatÃ¡rio na mesma locaÃ§Ã£o.");
        }

        if (IsActiveLocacaoStatus(status))
        {
            var hasActiveLocacaoForImovel = await dbContext.Locacoes.AnyAsync(
                x => x.ImovelId == request.ImovelId &&
                     x.Id != currentLocacaoId &&
                     (x.Status == LocacaoStatus.Ativa || x.Status == LocacaoStatus.EmAtraso || x.Status == LocacaoStatus.EmEncerramento || x.Status == LocacaoStatus.Reaberta),
                cancellationToken);

            if (hasActiveLocacaoForImovel)
            {
                throw new InvalidOperationException("Este imóvel já possui uma locação ativa.");
            }
        }

        var dataCadastro = request.DataCadastro ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var dataInicioCobranca = request.DataInicioCobranca ?? request.DataInicioLocacao;
        var diaBase = request.DiaBase ?? dataInicioCobranca?.Day ?? 1;
        var diaVencimentoLocatario = request.DiaVencimentoLocatario ?? diaBase;
        var diaRepasseProprietario = request.DiaRepasseProprietario ?? diaVencimentoLocatario;

        ValidateDay(diaBase, "Dia base");
        ValidateDay(diaVencimentoLocatario, "Dia de vencimento do locatário");
        ValidateDay(diaRepasseProprietario, "Dia de repasse ao proprietário");

        if (request.ValorAluguelInicial < 0)
        {
            throw new InvalidOperationException("O valor inicial do aluguel não pode ser negativo.");
        }

        var valorAluguelAtual = request.ValorAluguelAtual ?? request.ValorAluguelInicial;
        if (valorAluguelAtual < 0)
        {
            throw new InvalidOperationException("O valor atual do aluguel não pode ser negativo.");
        }

        var taxaAdministracao = request.TaxaAdministracaoPercentual ?? DefaultTaxaAdministracaoPercentual;
        var metaComissao = request.MetaComissaoPrimeiroAluguelPercentual ?? DefaultMetaComissaoPrimeiroAluguelPercentual;
        if (taxaAdministracao < 0)
        {
            throw new InvalidOperationException("A taxa de administração não pode ser negativa.");
        }

        if (metaComissao < 0)
        {
            throw new InvalidOperationException("A meta de comissão do primeiro aluguel não pode ser negativa.");
        }

        var taxaContrato = request.TaxaContratoManualOverride
            ? request.TaxaContratoPercentual ?? 0m
            : Math.Max(0m, metaComissao - taxaAdministracao);
        if (taxaContrato < 0)
        {
            throw new InvalidOperationException("A taxa de contrato não pode ser negativa.");
        }

        var partes = NormalizePartes(request.Partes);
        if (partes.Count == 0)
        {
            throw new InvalidOperationException("Informe ao menos um proprietário e um locatário para salvar a locação.");
        }

        await ValidatePessoasExistAsync(partes, cancellationToken);
        ValidatePartes(partes, status);
        ValidateGarantia(request.Garantia, request.AluguelAntecipado, status);
        ValidateEncargos(request.Encargos);
        ValidateLancamentos(request.Lancamentos);

        return new LocacaoValidationResult(
            request,
            status,
            dataCadastro,
            dataInicioCobranca,
            diaBase,
            diaVencimentoLocatario,
            diaRepasseProprietario,
            valorAluguelAtual,
            taxaAdministracao,
            metaComissao,
            taxaContrato,
            partes);
    }

    private static List<LocacaoParteRequest> NormalizePartes(IReadOnlyList<LocacaoParteRequest> partes)
    {
        var normalized = partes
            .Where(x => x.PessoaId != Guid.Empty)
            .Select(x => x with { Observacoes = TrimOrNull(x.Observacoes) })
            .ToList();

        EnsureSinglePrimary(normalized, TipoParteLocacao.Proprietario);
        EnsureSinglePrimary(normalized, TipoParteLocacao.Locatario);
        EnsureSinglePrimary(normalized, TipoParteLocacao.Fiador);

        return normalized;
    }

    private static void EnsureSinglePrimary(List<LocacaoParteRequest> partes, TipoParteLocacao tipoParte)
    {
        var indexes = partes
            .Select((parte, index) => new { parte, index })
            .Where(x => x.parte.TipoParte == tipoParte)
            .ToList();

        if (indexes.Count == 1 && !indexes[0].parte.IsPrincipal)
        {
            partes[indexes[0].index] = indexes[0].parte with { IsPrincipal = true };
            return;
        }

        var firstPrimary = indexes.FirstOrDefault(x => x.parte.IsPrincipal);
        if (firstPrimary is null)
        {
            return;
        }

        foreach (var item in indexes.Where(x => x.index != firstPrimary.index && x.parte.IsPrincipal))
        {
            partes[item.index] = item.parte with { IsPrincipal = false };
        }
    }

    private async Task ValidatePessoasExistAsync(IReadOnlyList<LocacaoParteRequest> partes, CancellationToken cancellationToken)
    {
        var pessoaIds = partes.Select(x => x.PessoaId).Distinct().ToList();
        var foundIds = await dbContext.Pessoas
            .Where(x => pessoaIds.Contains(x.Id))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var missingIds = pessoaIds.Except(foundIds).ToList();
        if (missingIds.Count > 0)
        {
            throw new InvalidOperationException("Uma ou mais pessoas vinculadas à locação não foram encontradas.");
        }
    }

    private static void ValidatePartes(IReadOnlyList<LocacaoParteRequest> partes, LocacaoStatus status)
    {
        var proprietarios = partes.Where(x => x.TipoParte == TipoParteLocacao.Proprietario).ToList();
        var locatarios = partes.Where(x => x.TipoParte == TipoParteLocacao.Locatario).ToList();

        if (proprietarios.Count == 0)
        {
            throw new InvalidOperationException("Informe ao menos um proprietário para a locação.");
        }

        if (locatarios.Count == 0)
        {
            throw new InvalidOperationException("Informe ao menos um locatário para a locação.");
        }

        if (IsActiveLocacaoStatus(status))
        {
            if (!proprietarios.Any(x => x.IsPrincipal))
            {
                throw new InvalidOperationException("Defina o proprietário principal antes de ativar a locação.");
            }

            if (!locatarios.Any(x => x.IsPrincipal))
            {
                throw new InvalidOperationException("Defina o locatário principal antes de ativar a locação.");
            }
        }

        foreach (var parte in partes)
        {
            if (parte.PercentualParticipacao < 0 || parte.PercentualRepasse < 0)
            {
                throw new InvalidOperationException("Percentuais da locação não podem ser negativos.");
            }
        }

        if (proprietarios.Count > 1 && proprietarios.All(x => x.PercentualParticipacao.HasValue))
        {
            var total = proprietarios.Sum(x => x.PercentualParticipacao!.Value);
            if (Math.Abs(total - 100m) > 0.01m)
            {
                throw new InvalidOperationException("A participação dos proprietários deve somar 100% quando todos os percentuais forem informados.");
            }
        }
    }

    private static void ValidateGarantia(LocacaoGarantiaRequest? garantia, bool aluguelAntecipado, LocacaoStatus status)
    {
        if (garantia is null)
        {
            return;
        }

        if (garantia.Valor < 0)
        {
            throw new InvalidOperationException("O valor da garantia não pode ser negativo.");
        }

        var hasActiveGuarantee = garantia.Ativa && garantia.TipoGarantia != TipoGarantiaLocacao.Nenhuma;
        if (IsActiveLocacaoStatus(status) && aluguelAntecipado && hasActiveGuarantee)
        {
            throw new InvalidOperationException("Não é permitido ativar locação com aluguel antecipado e garantia ativa.");
        }
    }

    private static void ValidateEncargos(IReadOnlyList<LocacaoEncargoRequest>? encargos)
    {
        if (encargos is null)
        {
            return;
        }

        foreach (var encargo in encargos)
        {
            if (encargo.Valor < 0)
            {
                throw new InvalidOperationException("O valor do encargo não pode ser negativo.");
            }

            if (encargo.DiaVencimento.HasValue)
            {
                ValidateDay(encargo.DiaVencimento.Value, "Dia de vencimento do encargo");
            }
        }
    }

    private static void ValidateLancamentos(IReadOnlyList<LocacaoLancamentoRequest>? lancamentos)
    {
        if (lancamentos is null)
        {
            return;
        }

        foreach (var lancamento in lancamentos)
        {
            if (string.IsNullOrWhiteSpace(lancamento.Descricao))
            {
                throw new InvalidOperationException("Informe a descrição do lançamento.");
            }

            if (lancamento.Valor < 0)
            {
                throw new InvalidOperationException("O valor do lançamento não pode ser negativo. Use o tipo do lançamento para representar desconto ou reembolso.");
            }
        }
    }

    private static void ValidateDay(int day, string label)
    {
        if (day is < 1 or > 31)
        {
            throw new InvalidOperationException($"{label} deve ficar entre 1 e 31.");
        }
    }

    private static bool IsActiveLocacaoStatus(LocacaoStatus status) =>
        status is LocacaoStatus.Ativa or LocacaoStatus.EmAtraso or LocacaoStatus.EmEncerramento or LocacaoStatus.Reaberta;

    private sealed record LocacaoValidationResult(
        CreateLocacaoRequest Request,
        LocacaoStatus Status,
        DateOnly DataCadastro,
        DateOnly? DataInicioCobranca,
        int DiaBase,
        int DiaVencimentoLocatario,
        int DiaRepasseProprietario,
        decimal ValorAluguelAtual,
        decimal TaxaAdministracaoPercentual,
        decimal MetaComissaoPrimeiroAluguelPercentual,
        decimal TaxaContratoPercentual,
        IReadOnlyList<LocacaoParteRequest> Partes);
}
