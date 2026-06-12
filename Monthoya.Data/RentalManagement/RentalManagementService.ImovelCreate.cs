using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

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

        var hasOwnerRole = await dbContext.PessoaRoles
            .AnyAsync(x => x.PessoaId == proprietario.Id && x.Role == PessoaRoleTipo.Proprietario, cancellationToken);

        if (!hasOwnerRole)
        {
            dbContext.PessoaRoles.Add(new PessoaRole
            {
                PessoaId = proprietario.Id,
                Role = PessoaRoleTipo.Proprietario
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetImoveisCoreAsync(cancellationToken)).Single(x => x.Id == imovel.Id);
    }
}
