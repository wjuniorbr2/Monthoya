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
            throw new InvalidOperationException("Selecione o imÃ³vel da locaÃ§Ã£o.");
        }

        var imovelExists = await dbContext.Imoveis.AnyAsync(x => x.Id == request.ImovelId, cancellationToken);
        if (!imovelExists)
        {
            throw new InvalidOperationException("ImÃ³vel nÃ£o encontrado.");
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
                throw new InvalidOperationException("Este imÃ³vel jÃ¡ possui uma locaÃ§Ã£o ativa.");
            }
        }

        var dataCadastro = request.DataCadastro ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var dataInicioCobranca = request.DataInicioCobranca ?? request.DataInicioLocacao;
        var diaBase = request.DiaBase ?? dataInicioCobranca?.Day ?? 1;
        var diaVencimentoLocatario = request.DiaVencimentoLocatario ?? diaBase;
        var diaRepasseProprietario = request.DiaRepasseProprietario ?? diaVencimentoLocatario;

        ValidateDateRange(dataCadastro, "Data de cadastro");
        ValidateOptionalDateRange(request.DataAssinaturaContrato, "Data de assinatura do contrato");
        ValidateOptionalDateRange(request.DataInicioLocacao, "Data inÃ­cio locaÃ§Ã£o");
        ValidateOptionalDateRange(request.DataEntregaChaves, "Data entrega das chaves");
        ValidateOptionalDateRange(dataInicioCobranca, "Data inÃ­cio cobranÃ§a");
        ValidateOptionalDateRange(request.DataFimPrevista, "Data fim prevista");
        ValidateOptionalDateRange(request.DataEncerramento, "Data de encerramento");
        ValidateOptionalDateRange(request.DataDesocupacao, "Data de desocupaÃ§Ã£o");
        ValidateOptionalDateRange(request.DataBaseReajuste, "Data base do reajuste");
        ValidateOptionalDateRange(request.ProximaDataReajuste, "PrÃ³xima data de reajuste");

        ValidateDay(diaBase, "Dia base");
        ValidateDay(diaVencimentoLocatario, "Dia de vencimento do locatÃ¡rio");
        ValidateDay(diaRepasseProprietario, "Dia de repasse ao proprietÃ¡rio");

        if (request.ValorAluguelInicial < 0)
        {
            throw new InvalidOperationException("O valor inicial do aluguel nÃ£o pode ser negativo.");
        }

        var valorAluguelAtual = request.ValorAluguelAtual ?? request.ValorAluguelInicial;
        if (valorAluguelAtual < 0)
        {
            throw new InvalidOperationException("O valor atual do aluguel nÃ£o pode ser negativo.");
        }

        var taxaAdministracao = request.TaxaAdministracaoPercentual ?? DefaultTaxaAdministracaoPercentual;
        var metaComissao = request.MetaComissaoPrimeiroAluguelPercentual ?? DefaultMetaComissaoPrimeiroAluguelPercentual;
        if (taxaAdministracao < 0)
        {
            throw new InvalidOperationException("A taxa de administraÃ§Ã£o nÃ£o pode ser negativa.");
        }

        if (metaComissao < 0)
        {
            throw new InvalidOperationException("A meta de comissÃ£o do primeiro aluguel nÃ£o pode ser negativa.");
        }

        var taxaContrato = request.TaxaContratoManualOverride
            ? request.TaxaContratoPercentual ?? 0m
            : Math.Max(0m, metaComissao - taxaAdministracao);
        if (taxaContrato < 0)
        {
            throw new InvalidOperationException("A taxa de contrato nÃ£o pode ser negativa.");
        }

        var partes = NormalizePartes(request.Partes);
        if (partes.Count == 0)
        {
            throw new InvalidOperationException("Informe ao menos um proprietÃ¡rio e um locatÃ¡rio para salvar a locaÃ§Ã£o.");
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

        if (indexes.Count == 0)
        {
            return;
        }

        if (indexes.Count == 1 && !indexes[0].parte.IsPrincipal)
        {
            partes[indexes[0].index] = indexes[0].parte with { IsPrincipal = true };
            return;
        }

        var firstPrimary = indexes.FirstOrDefault(x => x.parte.IsPrincipal);
        if (firstPrimary is null)
        {
            partes[indexes[0].index] = indexes[0].parte with { IsPrincipal = true };
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
            throw new InvalidOperationException("Uma ou mais pessoas vinculadas Ã  locaÃ§Ã£o nÃ£o foram encontradas.");
        }
    }

    private static void ValidatePartes(IReadOnlyList<LocacaoParteRequest> partes, LocacaoStatus status)
    {
        var proprietarios = partes.Where(x => x.TipoParte == TipoParteLocacao.Proprietario).ToList();
        var locatarios = partes.Where(x => x.TipoParte == TipoParteLocacao.Locatario).ToList();

        var pessoaComMaisDeUmPapel = partes
            .GroupBy(x => x.PessoaId)
            .FirstOrDefault(x => x.Select(parte => parte.TipoParte).Distinct().Count() > 1);
        if (pessoaComMaisDeUmPapel is not null)
        {
            throw new InvalidOperationException("A mesma pessoa nÃ£o pode ocupar mais de um papel na mesma locaÃ§Ã£o.");
        }

        if (proprietarios.Count == 0)
        {
            throw new InvalidOperationException("Informe ao menos um proprietÃ¡rio para a locaÃ§Ã£o.");
        }

        if (locatarios.Count == 0)
        {
            throw new InvalidOperationException("Informe ao menos um locatÃ¡rio para a locaÃ§Ã£o.");
        }

        if (IsActiveLocacaoStatus(status))
        {
            if (!proprietarios.Any(x => x.IsPrincipal))
            {
                throw new InvalidOperationException("Defina o proprietÃ¡rio principal antes de ativar a locaÃ§Ã£o.");
            }

            if (!locatarios.Any(x => x.IsPrincipal))
            {
                throw new InvalidOperationException("Defina o locatÃ¡rio principal antes de ativar a locaÃ§Ã£o.");
            }
        }

        foreach (var parte in partes)
        {
            if (parte.PercentualParticipacao < 0 || parte.PercentualRepasse < 0)
            {
                throw new InvalidOperationException("Percentuais da locaÃ§Ã£o nÃ£o podem ser negativos.");
            }
        }

        if (proprietarios.Count > 1 && proprietarios.All(x => x.PercentualParticipacao.HasValue))
        {
            var total = proprietarios.Sum(x => x.PercentualParticipacao!.Value);
            if (Math.Abs(total - 100m) > 0.01m)
            {
                throw new InvalidOperationException("A participaÃ§Ã£o dos proprietÃ¡rios deve somar 100% quando todos os percentuais forem informados.");
            }
        }
    }

    private static void ValidateGarantia(LocacaoGarantiaRequest? garantia, bool aluguelAntecipado, LocacaoStatus status)
    {
        if (garantia is null)
        {
            return;
        }

        ValidateOptionalDateRange(garantia.DataValidade, "Data de validade da garantia");

        if (garantia.Valor < 0)
        {
            throw new InvalidOperationException("O valor da garantia nÃ£o pode ser negativo.");
        }

        var hasActiveGuarantee = garantia.Ativa && garantia.TipoGarantia != TipoGarantiaLocacao.Nenhuma;
        if (IsActiveLocacaoStatus(status) && aluguelAntecipado && hasActiveGuarantee)
        {
            throw new InvalidOperationException("NÃ£o Ã© permitido ativar locaÃ§Ã£o com aluguel antecipado e garantia ativa.");
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
                throw new InvalidOperationException("O valor do encargo nÃ£o pode ser negativo.");
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
                throw new InvalidOperationException("Informe a descriÃ§Ã£o do lanÃ§amento.");
            }

            ValidateOptionalDateRange(lancamento.Competencia, "CompetÃªncia do lanÃ§amento");
            ValidateOptionalDateRange(lancamento.DataVencimento, "Data de vencimento do lanÃ§amento");

            if (lancamento.Valor < 0)
            {
                throw new InvalidOperationException("O valor do lanÃ§amento nÃ£o pode ser negativo. Use o tipo do lanÃ§amento para representar desconto ou reembolso.");
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

    private static void ValidateOptionalDateRange(DateOnly? date, string label)
    {
        if (date.HasValue)
        {
            ValidateDateRange(date.Value, label);
        }
    }

    private static void ValidateDateRange(DateOnly date, string label)
    {
        if (date.Year is < 1900 or > 2100)
        {
            throw new InvalidOperationException($"{label} deve ficar entre 1900 e 2100.");
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
