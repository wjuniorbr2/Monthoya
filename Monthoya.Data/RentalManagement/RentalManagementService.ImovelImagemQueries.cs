using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<IReadOnlyList<ImovelImagemSummary>> GetImovelImagensAsync(Guid imovelId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await GetImovelImagensCoreAsync(imovelId, cancellationToken);
    }

    private async Task<IReadOnlyList<ImovelImagemSummary>> GetImovelImagensCoreAsync(Guid imovelId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<ImovelImagem>()
            .AsNoTracking()
            .Where(x => x.ImovelId == imovelId)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.FileName)
            .Select(x => new ImovelImagemSummary(
                x.Id,
                x.ImovelId,
                x.FileName,
                x.StoragePath,
                x.ContentType,
                x.DisplayOrder,
                x.Caption,
                x.IsCover,
                x.IsPublic,
                GetImovelMediaCategoryLabel(x.MediaCategory),
                GetImovelMediaSourceLabel(x.Source),
                x.Status == RegistroStatus.Ativo ? "Ativo" : "Inativo"))
            .ToListAsync(cancellationToken);
    }
}
