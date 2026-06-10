using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Monthoya.Core.Entities;
using Monthoya.Core.Integrations;
using Monthoya.Core.Services;
using Monthoya.Data.Storage;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<ImovelImagemSummary> CreateImovelImagemAsync(CreateImovelImagemRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.ImovelId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione o imóvel da foto.");
        }

        if (string.IsNullOrWhiteSpace(request.StoragePath))
        {
            throw new InvalidOperationException("Informe o caminho da foto do imóvel.");
        }

        var imovelExists = await dbContext.Imoveis.AnyAsync(x => x.Id == request.ImovelId, cancellationToken);
        if (!imovelExists)
        {
            throw new InvalidOperationException("Imóvel não encontrado.");
        }

        var isPublic = request.MediaCategory == ImovelMediaCategory.InspectionPhoto
            ? false
            : request.IsPublic;

        if (request.IsCover)
        {
            var currentCovers = await dbContext.ImovelImagens
                .Where(x => x.ImovelId == request.ImovelId && x.IsCover)
                .ToListAsync(cancellationToken);

            foreach (var currentCover in currentCovers)
            {
                currentCover.IsCover = false;
                currentCover.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
        }

        var imagem = new ImovelImagem
        {
            ImovelId = request.ImovelId,
            FileName = string.IsNullOrWhiteSpace(request.FileName)
                ? Path.GetFileName(request.StoragePath)
                : request.FileName.Trim(),
            StoragePath = await StoreImovelImagemAsync(request.ImovelId, request.StoragePath.Trim(), request.ContentType, request.MediaCategory, cancellationToken),
            ContentType = TrimOrNull(request.ContentType),
            DisplayOrder = request.DisplayOrder,
            Caption = TrimOrNull(request.Caption),
            IsCover = request.IsCover,
            IsPublic = isPublic,
            MediaCategory = request.MediaCategory,
            Source = request.Source
        };

        dbContext.Set<ImovelImagem>().Add(imagem);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetImovelImagensCoreAsync(request.ImovelId, cancellationToken)).Single(x => x.Id == imagem.Id);
    }

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
