using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<IReadOnlyList<IndiceReajusteSummary>> GetIndicesReajusteAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await dbContext.IndicesReajuste.AsNoTracking().OrderBy(x => x.Nome)
            .Select(x => new IndiceReajusteSummary(x.Id, x.Nome, x.Codigo, x.Tipo == ReajusteTipo.Oficial ? "Oficial" : "Custom/manual", x.Percentual, x.Ativo ? "Ativo" : "Inativo"))
            .ToListAsync(cancellationToken);
    }


    public async Task<IReadOnlyList<DocumentoModeloSummary>> GetDocumentoModelosAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await dbContext.DocumentosModelos.AsNoTracking().OrderBy(x => x.Tipo)
            .Select(x => new DocumentoModeloSummary(x.Id, x.Tipo, x.Nome, x.StatusRevisao.ToString(), x.Ativo ? "Ativo" : "Inativo"))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DimobDeclaracaoSummary>> GetDimobDeclaracoesAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await dbContext.DimobDeclaracoes.AsNoTracking().OrderByDescending(x => x.AnoCalendario)
            .Select(x => new DimobDeclaracaoSummary(x.Id, x.AnoCalendario, x.Status.ToString(), x.Observacoes))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManutencaoSummary>> GetManutencoesAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await dbContext.ManutencoesImovel.AsNoTracking().OrderByDescending(x => x.DataSolicitacao)
            .Select(x => new ManutencaoSummary(x.Id, x.Descricao, x.Status.ToString(), x.DataSolicitacao, x.Valor))
            .ToListAsync(cancellationToken);
    }
}
