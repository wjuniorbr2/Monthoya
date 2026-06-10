using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Monthoya.Core.Entities;
using Monthoya.Core.Integrations;
using Monthoya.Core.Services;
using Monthoya.Data.Storage;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<IReadOnlyList<ImovelSummary>> GetImoveisAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await GetImoveisCoreAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ImovelSummary>> GetImoveisCoreAsync(CancellationToken cancellationToken)
    {
        var imoveis = await dbContext.Imoveis
            .AsNoTracking()
            .OrderBy(x => x.Rua)
            .ThenBy(x => x.Numero)
            .Select(x => new
            {
                x.Id,
                x.Rua,
                x.Numero,
                x.Bairro,
                Proprietario = x.Proprietario != null ? x.Proprietario.NomeDisplay : "-",
                x.TipoImovel,
                x.Finalidade,
                x.Status,
                x.ChavePosse,
                x.PublicarNoSite,
                x.PublicarNoApp,
                x.Destaque,
                x.ValorAluguel,
                x.ValorVenda,
                x.ChaveCodigo
            })
            .ToListAsync(cancellationToken);

        return imoveis.Select(x => new ImovelSummary(
            x.Id,
            $"{x.Rua}, {x.Numero}".Trim().Trim(','),
            x.Bairro,
            x.Proprietario,
            x.TipoImovel,
            GetImovelFinalidadeLabel(x.Finalidade),
            GetImovelStatusLabel(x.Status),
            GetImovelChavePosseLabel(x.ChavePosse),
            GetImovelPublicacaoLabel(x.PublicarNoSite, x.PublicarNoApp, x.Destaque),
            x.ValorAluguel,
            x.ValorVenda,
            x.ChaveCodigo)).ToList();
    }

    public async Task<ImovelDetails?> GetImovelAsync(Guid imovelId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var imovel = await dbContext.Imoveis
            .AsNoTracking()
            .Include(x => x.Proprietario)
            .SingleOrDefaultAsync(x => x.Id == imovelId, cancellationToken);

        if (imovel is null)
        {
            return null;
        }

        var summary = new ImovelSummary(
            imovel.Id,
            $"{imovel.Rua}, {imovel.Numero}".Trim().Trim(','),
            imovel.Bairro,
            imovel.Proprietario?.NomeDisplay ?? "-",
            imovel.TipoImovel,
            GetImovelFinalidadeLabel(imovel.Finalidade),
            GetImovelStatusLabel(imovel.Status),
            GetImovelChavePosseLabel(imovel.ChavePosse),
            GetImovelPublicacaoLabel(imovel),
            imovel.ValorAluguel,
            imovel.ValorVenda,
            imovel.ChaveCodigo);

        return new ImovelDetails(summary, ToImovelRequest(imovel));
    }

    public async Task<ImovelSummary> CreateImovelAsync(CreateImovelRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.ProprietarioId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione um proprietário.");
        }

        if (string.IsNullOrWhiteSpace(request.Rua))
        {
            throw new InvalidOperationException("Informe a rua do imóvel.");
        }

        var proprietario = await dbContext.Pessoas
            .SingleOrDefaultAsync(x => x.Id == request.ProprietarioId, cancellationToken)
            ?? throw new InvalidOperationException("Proprietário não encontrado.");

        var imovel = new Imovel();
        ApplyImovelRequest(imovel, request, proprietario.Id);

        dbContext.Imoveis.Add(imovel);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetImoveisCoreAsync(cancellationToken)).Single(x => x.Id == imovel.Id);
    }

    public async Task<ImovelSummary> UpdateImovelAsync(UpdateImovelRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione o imóvel para editar.");
        }

        ValidateImovelRequest(request.Imovel);

        var proprietario = await dbContext.Pessoas
            .SingleOrDefaultAsync(x => x.Id == request.Imovel.ProprietarioId, cancellationToken)
            ?? throw new InvalidOperationException("Proprietário não encontrado.");

        var imovel = await dbContext.Imoveis
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Imóvel não encontrado.");

        ApplyImovelRequest(imovel, request.Imovel, proprietario.Id);
        imovel.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetImoveisCoreAsync(cancellationToken)).Single(x => x.Id == imovel.Id);
    }

    public async Task SetImovelActiveAsync(Guid imovelId, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var imovel = await dbContext.Imoveis.SingleOrDefaultAsync(x => x.Id == imovelId, cancellationToken)
            ?? throw new InvalidOperationException("Imóvel não encontrado.");

        imovel.Status = isActive ? ImovelStatus.Disponivel : ImovelStatus.Inativo;
        imovel.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
