using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Dashboard;

public sealed class DashboardService(MonthoyaDbContext dbContext) : IDashboardService
{
    public async Task<HomeDashboardSummary> GetHomeSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);

        var totalProperties = await dbContext.Imoveis.CountAsync(cancellationToken);
        var availableRentals = await dbContext.Imoveis.CountAsync(
            x => x.Status == ImovelStatus.Disponivel &&
                 (x.Finalidade == ImovelFinalidade.Locacao || x.Finalidade == ImovelFinalidade.Ambos),
            cancellationToken);
        var activeContracts = await dbContext.Locacoes.CountAsync(
            x => x.Status == LocacaoStatus.Ativa,
            cancellationToken);
        var pendingRentAmount = await dbContext.LancamentosFinanceiros
            .Where(x => x.Status == FinanceiroStatus.Pendente || x.Status == FinanceiroStatus.Atrasado)
            .SumAsync(x => x.Valor, cancellationToken);

        var mapItems = await dbContext.Imoveis
            .Where(x =>
                x.Status == ImovelStatus.Disponivel &&
                (x.Finalidade == ImovelFinalidade.Locacao || x.Finalidade == ImovelFinalidade.Ambos) &&
                x.Latitude.HasValue &&
                x.Longitude.HasValue)
            .OrderBy(x => x.Rua)
            .ThenBy(x => x.Numero)
            .Select(x => new PropertyMapItem(
                x.Id,
                x.Id.ToString("N")[..8].ToUpperInvariant(),
                string.IsNullOrWhiteSpace(x.Numero) ? x.Rua : x.Rua + ", " + x.Numero,
                x.Cidade,
                x.Estado,
                x.ValorAluguel,
                x.Latitude!.Value,
                x.Longitude!.Value))
            .ToListAsync(cancellationToken);

        return new HomeDashboardSummary(
            totalProperties,
            availableRentals,
            activeContracts,
            pendingRentAmount,
            mapItems);
    }
}