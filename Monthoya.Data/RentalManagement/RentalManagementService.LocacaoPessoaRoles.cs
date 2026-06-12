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

    private async Task RecalculatePessoaRolesForLocacaoPartesAsync(
        IReadOnlyList<LocacaoParteRequest> partes,
        CancellationToken cancellationToken)
    {
        var affectedRoles = partes
            .Where(x => x.PessoaId != Guid.Empty)
            .Where(x => x.TipoParte is TipoParteLocacao.Locatario or TipoParteLocacao.Fiador)
            .Select(x => new
            {
                x.PessoaId,
                TipoParte = x.TipoParte,
                Role = ToPessoaRoleTipo(x.TipoParte)
            })
            .Distinct()
            .ToList();

        foreach (var affectedRole in affectedRoles)
        {
            var stillHasOpenLocacao = await dbContext.LocacaoPartes
                .AnyAsync(parte =>
                    parte.PessoaId == affectedRole.PessoaId &&
                    parte.TipoParte == affectedRole.TipoParte &&
                    dbContext.Locacoes.Any(locacao =>
                        locacao.Id == parte.LocacaoId &&
                        locacao.Status != LocacaoStatus.Cancelada &&
                        locacao.Status != LocacaoStatus.Encerrada),
                    cancellationToken);

            if (stillHasOpenLocacao)
            {
                continue;
            }

            var role = await dbContext.PessoaRoles
                .FirstOrDefaultAsync(x => x.PessoaId == affectedRole.PessoaId && x.Role == affectedRole.Role, cancellationToken);
            if (role is not null)
            {
                dbContext.PessoaRoles.Remove(role);
            }
        }
    }

    private async Task SyncImovelStatusForLocacaoAsync(
        Guid imovelId,
        LocacaoStatus status,
        Guid? currentLocacaoId,
        CancellationToken cancellationToken)
    {
        if (imovelId == Guid.Empty)
        {
            return;
        }

        var imovel = await dbContext.Imoveis
            .SingleOrDefaultAsync(x => x.Id == imovelId, cancellationToken);
        if (imovel is null)
        {
            return;
        }

        if (IsBlockingLocacaoStatus(status))
        {
            imovel.Status = ImovelStatus.Locado;
            return;
        }

        var hasOtherOpenLocacao = await dbContext.Locacoes.AnyAsync(
            x => x.ImovelId == imovelId &&
                 x.Id != currentLocacaoId &&
                 x.Status != LocacaoStatus.Cancelada &&
                 x.Status != LocacaoStatus.Encerrada,
            cancellationToken);

        if (!hasOtherOpenLocacao && imovel.Status == ImovelStatus.Locado)
        {
            imovel.Status = ImovelStatus.Disponivel;
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
