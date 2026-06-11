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

        // The desktop app keeps the service alive while the screen is open.
        // After a failed SaveChanges, tracked entities can remain in a bad state.
        // Phase 5 edits only basic Locação fields, so start from a clean tracker.
        dbContext.ChangeTracker.Clear();

        var normalized = await ValidateAndNormalizeLocacaoAsync(request.Locacao, request.Id, cancellationToken);

        var locacao = await dbContext.Locacoes
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Locação não encontrada.");

        var oldRent = locacao.ValorAluguelAtual ?? locacao.ValorAluguelInicial ?? locacao.ValorAluguel;

        ApplyLocacaoRequest(locacao, normalized);
        locacao.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (oldRent != normalized.ValorAluguelAtual)
        {
            dbContext.LocacaoValoresHistoricos.Add(new LocacaoValorHistorico
            {
                LocacaoId = locacao.Id,
                DataVigencia = request.Locacao.DataInicioLocacao ?? normalized.DataInicioCobranca ?? normalized.DataCadastro,
                ValorAnterior = oldRent,
                ValorNovo = normalized.ValorAluguelAtual,
                Motivo = "Atualização do valor da locação",
                Usuario = "sistema"
            });
        }

        dbContext.LocacaoHistoricos.Add(new LocacaoHistorico
        {
            LocacaoId = locacao.Id,
            Acao = "Atualizada",
            Motivo = "Atualização básica da locação"
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            dbContext.ChangeTracker.Clear();
            throw new InvalidOperationException("Não foi possível salvar a locação porque os dados locais estavam desatualizados. Atualize a lista e tente novamente.", ex);
        }

        dbContext.ChangeTracker.Clear();
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
