using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<LocacaoDetails> UpdateLocacaoAsync(UpdateLocacaoRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione a locação.");
        }

        var locacao = await LocacoesWithDetails()
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Locação não encontrada.");

        var oldRent = locacao.ValorAluguelAtual ?? locacao.ValorAluguelInicial ?? locacao.ValorAluguel;
        var normalized = await ValidateAndNormalizeLocacaoAsync(request.Locacao, request.Id, cancellationToken);

        ApplyLocacaoRequest(locacao, normalized);
        ReplaceLocacaoChildren(locacao, normalized);
        locacao.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (oldRent != normalized.ValorAluguelAtual)
        {
            locacao.ValoresHistoricos.Add(new()
            {
                DataVigencia = request.Locacao.DataInicioLocacao ?? normalized.DataInicioCobranca ?? normalized.DataCadastro,
                ValorAnterior = oldRent,
                ValorNovo = normalized.ValorAluguelAtual,
                Motivo = "Atualização do valor da locação",
                Usuario = "sistema"
            });
        }

        locacao.Historicos.Add(new()
        {
            Acao = "Atualizada",
            Motivo = "Atualização da locação"
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetLocacaoDetailsCoreAsync(locacao.Id, cancellationToken);
    }

    private void ReplaceLocacaoChildren(Locacao locacao, LocacaoValidationResult normalized)
    {
        dbContext.LocacaoPartes.RemoveRange(locacao.Partes);
        dbContext.LocacaoGarantias.RemoveRange(locacao.Garantias);
        dbContext.LocacaoEncargosRecorrentes.RemoveRange(locacao.EncargosRecorrentes);
        dbContext.LocacaoLancamentos.RemoveRange(locacao.Lancamentos);

        locacao.Partes = normalized.Partes.Select(ToLocacaoParte).ToList();
        locacao.Garantias = normalized.Request.Garantia is null ? [] : [ToLocacaoGarantia(normalized.Request.Garantia)];
        locacao.EncargosRecorrentes = (normalized.Request.Encargos ?? []).Select(ToLocacaoEncargo).ToList();
        locacao.Lancamentos = (normalized.Request.Lancamentos ?? []).Select(ToLocacaoLancamento).ToList();
    }
}
