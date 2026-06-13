using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    private async Task SyncPessoaProprietarioRoleForImovelAsync(
        Guid pessoaId,
        Guid currentImovelId,
        bool currentImovelCountsAsActive,
        CancellationToken cancellationToken)
    {
        if (pessoaId == Guid.Empty)
        {
            return;
        }

        var hasOtherActiveImovel = await dbContext.Imoveis.AnyAsync(
            x => x.Id != currentImovelId &&
                 x.ProprietarioId == pessoaId &&
                 x.Status != ImovelStatus.Inativo,
            cancellationToken);

        if (currentImovelCountsAsActive || hasOtherActiveImovel)
        {
            await EnsurePessoaRoleAsync(pessoaId, PessoaRoleTipo.Proprietario, cancellationToken);
            return;
        }

        var role = await dbContext.PessoaRoles
            .FirstOrDefaultAsync(
                x => x.PessoaId == pessoaId && x.Role == PessoaRoleTipo.Proprietario,
                cancellationToken);

        if (role is not null)
        {
            dbContext.PessoaRoles.Remove(role);
        }
    }
}