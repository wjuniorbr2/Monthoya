using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<IReadOnlyList<LocacaoSummary>> GetLocacoesAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var locacoes = await LocacoesWithDetails()
            .AsNoTracking()
            .AsSplitQuery()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return locacoes.Select(ToLocacaoSummary).ToList();
    }

    public async Task<LocacaoDetails> GetLocacaoAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var locacao = await LocacoesWithDetails()
            .AsNoTracking()
            .AsSplitQuery()
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Locação não encontrada.");

        return ToLocacaoDetails(locacao);
    }

    private async Task<LocacaoDetails> GetLocacaoDetailsCoreAsync(Guid id, CancellationToken cancellationToken)
    {
        var locacao = await LocacoesWithDetails()
            .AsNoTracking()
            .AsSplitQuery()
            .SingleAsync(x => x.Id == id, cancellationToken);

        return ToLocacaoDetails(locacao);
    }
}
