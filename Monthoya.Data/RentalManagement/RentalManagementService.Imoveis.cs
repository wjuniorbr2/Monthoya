using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Monthoya.Core.Entities;
using Monthoya.Core.Integrations;
using Monthoya.Core.Services;
using Monthoya.Data.Storage;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{

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
