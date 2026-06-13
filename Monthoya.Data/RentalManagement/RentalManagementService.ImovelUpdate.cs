using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<ImovelSummary> UpdateImovelAsync(UpdateImovelRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione o imÃƒÂ³vel para editar.");
        }

        ValidateImovelRequest(request.Imovel);

        var proprietario = await dbContext.Pessoas
            .SingleOrDefaultAsync(x => x.Id == request.Imovel.ProprietarioId, cancellationToken)
            ?? throw new InvalidOperationException("ProprietÃƒÂ¡rio nÃƒÂ£o encontrado.");

        var imovel = await dbContext.Imoveis
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException("ImÃƒÂ³vel nÃƒÂ£o encontrado.");

        var oldProprietarioId = imovel.ProprietarioId;
        ApplyImovelRequest(imovel, request.Imovel, proprietario.Id);
        imovel.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await SyncPessoaProprietarioRoleForImovelAsync(
            proprietario.Id,
            imovel.Id,
            imovel.Status != ImovelStatus.Inativo,
            cancellationToken);

        if (oldProprietarioId != proprietario.Id)
        {
            await SyncPessoaProprietarioRoleForImovelAsync(
                oldProprietarioId,
                imovel.Id,
                currentImovelCountsAsActive: false,
                cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetImoveisCoreAsync(cancellationToken)).Single(x => x.Id == imovel.Id);
    }
}
