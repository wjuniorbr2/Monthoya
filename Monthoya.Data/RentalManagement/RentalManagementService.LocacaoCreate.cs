using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<LocacaoDetails> CreateLocacaoAsync(CreateLocacaoRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var normalized = await ValidateAndNormalizeLocacaoAsync(request, currentLocacaoId: null, cancellationToken);

        var locacao = new Locacao();
        ApplyLocacaoRequest(locacao, normalized);
        ApplyLocacaoChildren(locacao, normalized);
        await EnsurePessoaRolesForLocacaoPartesAsync(normalized.Partes, cancellationToken);
        locacao.ValoresHistoricos.Add(new LocacaoValorHistorico
        {
            DataVigencia = request.DataInicioLocacao ?? normalized.DataInicioCobranca ?? normalized.DataCadastro,
            ValorAnterior = null,
            ValorNovo = normalized.ValorAluguelAtual,
            Motivo = "Valor inicial da locação",
            Usuario = "sistema"
        });
        locacao.Historicos.Add(new LocacaoHistorico
        {
            Acao = "Criada",
            Motivo = "Criação da locação"
        });

        dbContext.Locacoes.Add(locacao);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetLocacaoDetailsCoreAsync(locacao.Id, cancellationToken);
    }

    private static void ApplyLocacaoChildren(Locacao locacao, LocacaoValidationResult normalized)
    {
        locacao.Partes.Clear();
        foreach (var parte in normalized.Partes)
        {
            locacao.Partes.Add(ToLocacaoParte(parte));
        }

        locacao.Garantias.Clear();
        if (normalized.Request.Garantia is not null)
        {
            locacao.Garantias.Add(ToLocacaoGarantia(normalized.Request.Garantia));
        }

        locacao.EncargosRecorrentes.Clear();
        foreach (var encargo in normalized.Request.Encargos ?? [])
        {
            locacao.EncargosRecorrentes.Add(ToLocacaoEncargo(encargo));
        }

        locacao.Lancamentos.Clear();
        foreach (var lancamento in normalized.Request.Lancamentos ?? [])
        {
            locacao.Lancamentos.Add(ToLocacaoLancamento(lancamento));
        }
    }
}
