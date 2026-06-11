using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    private const decimal DefaultTaxaAdministracaoPercentual = 12m;
    private const decimal DefaultMetaComissaoPrimeiroAluguelPercentual = 50m;

    private IQueryable<Locacao> LocacoesWithDetails() =>
        dbContext.Locacoes
            .Include(x => x.Imovel)
            .Include(x => x.Proprietario)
            .Include(x => x.Locatario)
            .Include(x => x.Partes)
                .ThenInclude(x => x.Pessoa)
            .Include(x => x.Garantias)
            .Include(x => x.EncargosRecorrentes)
            .Include(x => x.Lancamentos)
            .Include(x => x.Cobrancas)
                .ThenInclude(x => x.Itens)
            .Include(x => x.Historicos);

    private static LocacaoSummary ToLocacaoSummary(Locacao locacao)
    {
        var proprietario = FindPrincipalParte(locacao.Partes, TipoParteLocacao.Proprietario)?.Pessoa?.NomeDisplay
            ?? locacao.Proprietario?.NomeDisplay
            ?? "-";
        var locatario = FindPrincipalParte(locacao.Partes, TipoParteLocacao.Locatario)?.Pessoa?.NomeDisplay
            ?? locacao.Locatario?.NomeDisplay
            ?? "-";
        var alertas = GetLocacaoAlertas(locacao, proprietario, locatario);

        return new LocacaoSummary(
            locacao.Id,
            locacao.Codigo,
            GetEnumLabel(locacao.Status),
            GetEnumLabel(locacao.TipoLocacao),
            locacao.ImovelId,
            FormatImovelAddress(locacao.Imovel),
            locatario,
            proprietario,
            locacao.ValorAluguelAtual ?? locacao.ValorAluguelInicial ?? locacao.ValorAluguel,
            locacao.DiaVencimentoLocatario ?? locacao.VencimentoLocatarioDia,
            CalculateProximaDataVencimento(locacao.DiaVencimentoLocatario ?? locacao.VencimentoLocatarioDia),
            locacao.DataInicioLocacao,
            locacao.DataFimPrevista ?? locacao.DataFim,
            alertas);
    }

    private static LocacaoDetails ToLocacaoDetails(Locacao locacao) =>
        new(
            ToLocacaoSummary(locacao),
            ToLocacaoRequest(locacao),
            locacao.Partes
                .OrderBy(x => x.TipoParte)
                .ThenByDescending(x => x.IsPrincipal)
                .ThenBy(x => x.Pessoa!.NomeDisplay)
                .Select(ToLocacaoParteSummary)
                .ToList(),
            locacao.Garantias.Where(x => x.Ativa).OrderByDescending(x => x.CreatedAtUtc).Select(ToLocacaoGarantiaSummary).FirstOrDefault(),
            locacao.EncargosRecorrentes.OrderBy(x => x.TipoEncargo).Select(ToLocacaoEncargoSummary).ToList(),
            locacao.Lancamentos.OrderByDescending(x => x.CreatedAtUtc).Select(ToLocacaoLancamentoSummary).ToList(),
            locacao.Cobrancas.OrderByDescending(x => x.Competencia).Select(ToLocacaoCobrancaSummary).ToList(),
            locacao.Historicos.OrderByDescending(x => x.DataHoraUtc).Select(ToLocacaoHistoricoSummary).ToList());

    private static CreateLocacaoRequest ToLocacaoRequest(Locacao locacao) =>
        new(
            locacao.ImovelId,
            locacao.Partes.Select(ToLocacaoParteRequest).ToList(),
            locacao.Codigo,
            locacao.TipoLocacao,
            locacao.Status,
            locacao.ResponsavelUsuarioId,
            locacao.ResponsavelNome,
            locacao.DataCadastro,
            locacao.DataAssinaturaContrato,
            locacao.DataInicioLocacao,
            locacao.DataEntregaChaves,
            locacao.DataInicioCobranca,
            locacao.DiaBase,
            locacao.DiaVencimentoLocatario ?? locacao.VencimentoLocatarioDia,
            locacao.DiaRepasseProprietario ?? locacao.VencimentoProprietarioDia,
            locacao.PrazoMeses ?? locacao.PeriodoMeses,
            locacao.DataFimPrevista ?? locacao.DataFim,
            locacao.DataEncerramento,
            locacao.DataDesocupacao,
            locacao.MotivoEncerramento,
            locacao.ValorAluguelInicial ?? locacao.ValorAluguel,
            locacao.ValorAluguelAtual ?? locacao.ValorAluguel,
            locacao.AluguelAntecipado,
            locacao.CalculoProporcionalPrimeiroMes,
            locacao.MetodoCalculoProporcional,
            locacao.TemDescontoPontualidade,
            locacao.TipoDescontoPontualidade,
            locacao.ValorDescontoPontualidade,
            locacao.DescontoValidoAteVencimento,
            locacao.MultaAtrasoTipo,
            locacao.MultaAtrasoValor,
            locacao.JurosMoraPercentualMes,
            locacao.DiasTolerancia,
            locacao.CorrecaoMonetariaAtraso,
            locacao.IndiceCorrecaoAtraso,
            locacao.TemReajuste,
            locacao.IndiceReajusteId,
            locacao.PeriodicidadeReajusteMeses,
            locacao.DataBaseReajuste,
            locacao.ProximaDataReajuste,
            locacao.ModoReajuste,
            locacao.ReajusteRequerAprovacao,
            locacao.TaxaAdministracaoPercentual,
            locacao.MetaComissaoPrimeiroAluguelPercentual,
            locacao.TaxaContratoPercentual,
            locacao.TaxaContratoManualOverride,
            locacao.CobrarTaxaContratoInicio,
            locacao.CobrarTaxaContratoRenovacao,
            locacao.CobrarTaxaContratoReajuste,
            locacao.ModoCobrancaTaxaContrato,
            locacao.Garantias.Where(x => x.Ativa).OrderByDescending(x => x.CreatedAtUtc).Select(ToLocacaoGarantiaRequest).FirstOrDefault(),
            locacao.EncargosRecorrentes.OrderBy(x => x.TipoEncargo).Select(ToLocacaoEncargoRequest).ToList(),
            locacao.Lancamentos.OrderByDescending(x => x.CreatedAtUtc).Select(ToLocacaoLancamentoRequest).ToList(),
            locacao.Observacoes,
            locacao.ObservacoesInternas);

    private static void ApplyLocacaoRequest(Locacao locacao, LocacaoValidationResult normalized)
    {
        var request = normalized.Request;
        locacao.ImovelId = request.ImovelId;
        locacao.Codigo = TrimOrNull(request.Codigo);
        locacao.TipoLocacao = request.TipoLocacao;
        locacao.Status = normalized.Status;
        locacao.ResponsavelUsuarioId = request.ResponsavelUsuarioId;
        locacao.ResponsavelNome = TrimOrNull(request.ResponsavelNome);
        locacao.DataCadastro = normalized.DataCadastro;
        locacao.DataAssinaturaContrato = request.DataAssinaturaContrato;
        locacao.DataInicioLocacao = request.DataInicioLocacao;
        locacao.DataEntregaChaves = request.DataEntregaChaves;
        locacao.DataInicioCobranca = normalized.DataInicioCobranca;
        locacao.DiaBase = normalized.DiaBase;
        locacao.DiaVencimentoLocatario = normalized.DiaVencimentoLocatario;
        locacao.DiaRepasseProprietario = normalized.DiaRepasseProprietario;
        locacao.VencimentoLocatarioDia = normalized.DiaVencimentoLocatario;
        locacao.VencimentoProprietarioDia = normalized.DiaRepasseProprietario;
        locacao.PrazoMeses = request.PrazoMeses;
        locacao.PeriodoMeses = request.PrazoMeses;
        locacao.DataFimPrevista = request.DataFimPrevista;
        locacao.DataFim = request.DataFimPrevista;
        locacao.DataInicio = request.DataInicioLocacao ?? normalized.DataInicioCobranca ?? normalized.DataCadastro;
        locacao.DataEncerramento = request.DataEncerramento;
        locacao.DataDesocupacao = request.DataDesocupacao;
        locacao.MotivoEncerramento = TrimOrNull(request.MotivoEncerramento);
        locacao.ValorAluguelInicial = request.ValorAluguelInicial;
        locacao.ValorAluguelAtual = normalized.ValorAluguelAtual;
        locacao.ValorAluguel = normalized.ValorAluguelAtual;
        locacao.AluguelAntecipado = request.AluguelAntecipado;
        locacao.CalculoProporcionalPrimeiroMes = request.CalculoProporcionalPrimeiroMes;
        locacao.MetodoCalculoProporcional = request.MetodoCalculoProporcional;
        locacao.TemDescontoPontualidade = request.TemDescontoPontualidade;
        locacao.TipoDescontoPontualidade = request.TipoDescontoPontualidade;
        locacao.ValorDescontoPontualidade = request.ValorDescontoPontualidade;
        locacao.DescontoValidoAteVencimento = request.DescontoValidoAteVencimento;
        locacao.MultaAtrasoTipo = request.MultaAtrasoTipo;
        locacao.MultaAtrasoValor = request.MultaAtrasoValor;
        locacao.JurosMoraPercentualMes = request.JurosMoraPercentualMes;
        locacao.DiasTolerancia = request.DiasTolerancia;
        locacao.CorrecaoMonetariaAtraso = request.CorrecaoMonetariaAtraso;
        locacao.IndiceCorrecaoAtraso = TrimOrNull(request.IndiceCorrecaoAtraso);
        locacao.TemReajuste = request.TemReajuste;
        locacao.IndiceReajusteId = request.IndiceReajusteId;
        locacao.PeriodicidadeReajusteMeses = request.PeriodicidadeReajusteMeses;
        locacao.DataBaseReajuste = request.DataBaseReajuste;
        locacao.ProximaDataReajuste = request.ProximaDataReajuste;
        locacao.DataProximoReajuste = request.ProximaDataReajuste;
        locacao.ModoReajuste = request.ModoReajuste;
        locacao.ReajusteRequerAprovacao = request.ReajusteRequerAprovacao;
        locacao.TaxaAdministracaoPercentual = normalized.TaxaAdministracaoPercentual;
        locacao.MetaComissaoPrimeiroAluguelPercentual = normalized.MetaComissaoPrimeiroAluguelPercentual;
        locacao.TaxaContratoPercentual = normalized.TaxaContratoPercentual;
        locacao.TaxaContratoManualOverride = request.TaxaContratoManualOverride;
        locacao.CobrarTaxaContratoInicio = request.CobrarTaxaContratoInicio;
        locacao.CobrarTaxaContratoRenovacao = request.CobrarTaxaContratoRenovacao;
        locacao.CobrarTaxaContratoReajuste = request.CobrarTaxaContratoReajuste;
        locacao.ModoCobrancaTaxaContrato = request.ModoCobrancaTaxaContrato;
        locacao.Observacoes = TrimOrNull(request.Observacoes);
        locacao.ObservacoesInternas = TrimOrNull(request.ObservacoesInternas);

        var proprietarioPrincipal = normalized.Partes.First(x => x.TipoParte == TipoParteLocacao.Proprietario && x.IsPrincipal);
        var locatarioPrincipal = normalized.Partes.First(x => x.TipoParte == TipoParteLocacao.Locatario && x.IsPrincipal);
        locacao.ProprietarioId = proprietarioPrincipal.PessoaId;
        locacao.LocatarioId = locatarioPrincipal.PessoaId;
    }

    private static LocacaoParte ToLocacaoParte(LocacaoParteRequest request) => new()
    {
        PessoaId = request.PessoaId,
        TipoParte = request.TipoParte,
        IsPrincipal = request.IsPrincipal,
        PercentualParticipacao = request.PercentualParticipacao,
        RecebeCobranca = request.RecebeCobranca,
        RecebeRepasse = request.RecebeRepasse,
        RecebeNotificacao = request.RecebeNotificacao,
        PercentualRepasse = request.PercentualRepasse,
        Observacoes = TrimOrNull(request.Observacoes)
    };

    private static LocacaoGarantia ToLocacaoGarantia(LocacaoGarantiaRequest request) => new()
    {
        TipoGarantia = request.TipoGarantia,
        Valor = request.Valor,
        DataValidade = request.DataValidade,
        Ativa = request.Ativa,
        Observacoes = TrimOrNull(request.Observacoes),
        ObservacoesDocumento = TrimOrNull(request.ObservacoesDocumento)
    };

    private static LocacaoEncargoRecorrente ToLocacaoEncargo(LocacaoEncargoRequest request) => new()
    {
        TipoEncargo = request.TipoEncargo,
        ControladoPelaImobiliaria = request.ControladoPelaImobiliaria,
        CobradoComAluguel = request.CobradoComAluguel,
        PagoDiretoPeloLocatario = request.PagoDiretoPeloLocatario,
        PagoPeloProprietario = request.PagoPeloProprietario,
        Valor = request.Valor,
        Fixo = request.Fixo,
        NumeroParcelas = request.NumeroParcelas,
        DiaVencimento = request.DiaVencimento,
        RequerAtualizacao = request.RequerAtualizacao,
        Observacoes = TrimOrNull(request.Observacoes),
        Ativo = request.Ativo
    };

    private static LocacaoLancamento ToLocacaoLancamento(LocacaoLancamentoRequest request) => new()
    {
        TipoLancamento = request.TipoLancamento,
        Descricao = TrimOrNull(request.Descricao) ?? string.Empty,
        Valor = request.Valor,
        Competencia = request.Competencia,
        DataVencimento = request.DataVencimento,
        AfetaCobrancaLocatario = request.AfetaCobrancaLocatario,
        AfetaRepasseProprietario = request.AfetaRepasseProprietario,
        RequerAprovacao = request.RequerAprovacao,
        Status = request.Status,
        Observacoes = TrimOrNull(request.Observacoes)
    };

    private static LocacaoParteRequest ToLocacaoParteRequest(LocacaoParte parte) =>
        new(parte.PessoaId, parte.TipoParte, parte.IsPrincipal, parte.PercentualParticipacao, parte.RecebeCobranca, parte.RecebeRepasse, parte.RecebeNotificacao, parte.PercentualRepasse, parte.Observacoes);

    private static LocacaoGarantiaRequest ToLocacaoGarantiaRequest(LocacaoGarantia garantia) =>
        new(garantia.TipoGarantia, garantia.Valor, garantia.DataValidade, garantia.Ativa, garantia.Observacoes, garantia.ObservacoesDocumento);

    private static LocacaoEncargoRequest ToLocacaoEncargoRequest(LocacaoEncargoRecorrente encargo) =>
        new(encargo.TipoEncargo, encargo.ControladoPelaImobiliaria, encargo.CobradoComAluguel, encargo.PagoDiretoPeloLocatario, encargo.PagoPeloProprietario, encargo.Valor, encargo.Fixo, encargo.NumeroParcelas, encargo.DiaVencimento, encargo.RequerAtualizacao, encargo.Observacoes, encargo.Ativo);

    private static LocacaoLancamentoRequest ToLocacaoLancamentoRequest(LocacaoLancamento lancamento) =>
        new(lancamento.TipoLancamento, lancamento.Descricao, lancamento.Valor, lancamento.Competencia, lancamento.DataVencimento, lancamento.AfetaCobrancaLocatario, lancamento.AfetaRepasseProprietario, lancamento.RequerAprovacao, lancamento.Status, lancamento.Observacoes);

    private static LocacaoParteSummary ToLocacaoParteSummary(LocacaoParte parte) =>
        new(parte.Id, parte.PessoaId, parte.Pessoa?.NomeDisplay ?? "-", parte.TipoParte, parte.IsPrincipal, parte.PercentualParticipacao, parte.RecebeCobranca, parte.RecebeRepasse, parte.RecebeNotificacao, parte.PercentualRepasse, parte.Observacoes);

    private static LocacaoGarantiaSummary ToLocacaoGarantiaSummary(LocacaoGarantia garantia) =>
        new(garantia.Id, garantia.TipoGarantia, garantia.Valor, garantia.DataValidade, garantia.Ativa, garantia.Observacoes, garantia.ObservacoesDocumento);

    private static LocacaoEncargoSummary ToLocacaoEncargoSummary(LocacaoEncargoRecorrente encargo) =>
        new(encargo.Id, encargo.TipoEncargo, encargo.ControladoPelaImobiliaria, encargo.CobradoComAluguel, encargo.PagoDiretoPeloLocatario, encargo.PagoPeloProprietario, encargo.Valor, encargo.Fixo, encargo.NumeroParcelas, encargo.DiaVencimento, encargo.RequerAtualizacao, encargo.Observacoes, encargo.Ativo);

    private static LocacaoLancamentoSummary ToLocacaoLancamentoSummary(LocacaoLancamento lancamento) =>
        new(lancamento.Id, lancamento.TipoLancamento, lancamento.Descricao, lancamento.Valor, lancamento.Competencia, lancamento.DataVencimento, lancamento.AfetaCobrancaLocatario, lancamento.AfetaRepasseProprietario, lancamento.RequerAprovacao, lancamento.Status, lancamento.Observacoes);

    private static LocacaoCobrancaSummary ToLocacaoCobrancaSummary(LocacaoCobranca cobranca) =>
        new(cobranca.Id, cobranca.TipoCobranca, cobranca.Competencia, cobranca.PeriodoInicio, cobranca.PeriodoFim, cobranca.DataVencimento, cobranca.Status, cobranca.ValorTotal, cobranca.Itens.OrderBy(x => x.CreatedAtUtc).Select(ToLocacaoCobrancaItemSummary).ToList());

    private static LocacaoCobrancaItemSummary ToLocacaoCobrancaItemSummary(LocacaoCobrancaItem item) =>
        new(item.Id, item.TipoItem, item.Descricao, item.Valor, item.ReferenciaId, item.Observacoes);

    private static LocacaoHistoricoSummary ToLocacaoHistoricoSummary(LocacaoHistorico historico) =>
        new(historico.Id, historico.DataHoraUtc, historico.Usuario, historico.Acao, historico.Campo, historico.ValorAnterior, historico.ValorNovo, historico.Motivo);

    private static LocacaoParte? FindPrincipalParte(IEnumerable<LocacaoParte> partes, TipoParteLocacao tipo) =>
        partes.Where(x => x.TipoParte == tipo)
            .OrderByDescending(x => x.IsPrincipal)
            .ThenBy(x => x.CreatedAtUtc)
            .FirstOrDefault();

    private static string FormatImovelAddress(Imovel? imovel) =>
        imovel is null ? "-" : $"{imovel.Rua}, {imovel.Numero}".Trim().Trim(',');

    private static IReadOnlyList<string> GetLocacaoAlertas(Locacao locacao, string proprietario, string locatario)
    {
        var alertas = new List<string>();
        if (proprietario == "-") alertas.Add("Sem proprietário principal");
        if (locatario == "-") alertas.Add("Sem locatário principal");
        if (locacao.AluguelAntecipado && locacao.Garantias.Any(x => x.Ativa && x.TipoGarantia != TipoGarantiaLocacao.Nenhuma))
        {
            alertas.Add("Aluguel antecipado com garantia ativa");
        }

        return alertas;
    }

    private static DateOnly? CalculateProximaDataVencimento(int? diaVencimento)
    {
        if (!diaVencimento.HasValue || diaVencimento.Value < 1 || diaVencimento.Value > 31)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var due = SafeDate(today.Year, today.Month, diaVencimento.Value);
        return due >= today ? due : SafeDate(today.AddMonths(1).Year, today.AddMonths(1).Month, diaVencimento.Value);
    }

    private static DateOnly SafeDate(int year, int month, int day) =>
        new(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));
}
