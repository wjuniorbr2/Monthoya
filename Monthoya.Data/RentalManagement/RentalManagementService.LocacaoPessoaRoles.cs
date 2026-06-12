using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    private async Task EnsurePessoaRoleAsync(Guid pessoaId, PessoaRoleTipo role, CancellationToken cancellationToken)
    {
        if (pessoaId == Guid.Empty)
        {
            return;
        }

        var exists = await dbContext.PessoaRoles
            .AnyAsync(x => x.PessoaId == pessoaId && x.Role == role, cancellationToken);

        if (!exists)
        {
            dbContext.PessoaRoles.Add(new PessoaRole
            {
                PessoaId = pessoaId,
                Role = role
            });
        }
    }

    private async Task EnsurePessoaRolesForLocacaoPartesAsync(
        IReadOnlyList<LocacaoParteRequest> partes,
        CancellationToken cancellationToken)
    {
        var requiredRoles = partes
            .Where(x => x.PessoaId != Guid.Empty)
            .Where(x => x.TipoParte is TipoParteLocacao.Locatario or TipoParteLocacao.Fiador)
            .Select(x => new
            {
                x.PessoaId,
                Role = ToPessoaRoleTipo(x.TipoParte)
            })
            .Distinct()
            .ToList();

        if (requiredRoles.Count == 0)
        {
            return;
        }

        var pessoaIds = requiredRoles.Select(x => x.PessoaId).Distinct().ToList();
        var existingRoles = await dbContext.PessoaRoles
            .Where(x => pessoaIds.Contains(x.PessoaId))
            .Select(x => new { x.PessoaId, x.Role })
            .ToListAsync(cancellationToken);

        foreach (var role in requiredRoles)
        {
            if (existingRoles.Any(x => x.PessoaId == role.PessoaId && x.Role == role.Role))
            {
                continue;
            }

            dbContext.PessoaRoles.Add(new PessoaRole
            {
                PessoaId = role.PessoaId,
                Role = role.Role
            });
        }
    }

    private static PessoaRoleTipo ToPessoaRoleTipo(TipoParteLocacao tipoParte) =>
        tipoParte switch
        {
            TipoParteLocacao.Locatario => PessoaRoleTipo.Locatario,
            TipoParteLocacao.Fiador => PessoaRoleTipo.Fiador,
            _ => throw new InvalidOperationException("Tipo de parte da locação não suportado para marcação automática.")
        };
}
