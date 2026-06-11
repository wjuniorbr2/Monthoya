using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<IReadOnlyList<ImovelChaveMovimentoSummary>> GetImovelChaveMovimentosAsync(Guid? imovelId = null, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var query = dbContext.ImovelChaveMovimentos
            .AsNoTracking()
            .AsQueryable();

        if (imovelId.HasValue)
        {
            query = query.Where(x => x.ImovelId == imovelId.Value);
        }

        var now = DateTimeOffset.UtcNow;
        var movimentos = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.ImovelId,
                ImovelRua = x.Imovel != null ? x.Imovel.Rua : null,
                ImovelNumero = x.Imovel != null ? x.Imovel.Numero : null,
                x.Tipo,
                x.Status,
                x.ChaveCodigo,
                x.RetiradoPorNome,
                x.RetiradoPorTelefone,
                x.RetiradoPorDocumento,
                x.RetiradoPorRelacao,
                x.Motivo,
                x.RetiradoEm,
                x.PrevisaoDevolucaoEm,
                x.DevolvidoEm,
                x.DevolvidoParaNome,
                x.Observacoes
            })
            .ToListAsync(cancellationToken);

        return movimentos.Select(x =>
        {
            var status = x.Status == ImovelChaveMovimentoStatus.Retirada
                && x.PrevisaoDevolucaoEm.HasValue
                && x.PrevisaoDevolucaoEm.Value < now
                && !x.DevolvidoEm.HasValue
                    ? "Em atraso"
                    : GetEnumLabel(x.Status);

            return new ImovelChaveMovimentoSummary(
                x.Id,
                x.ImovelId,
                string.IsNullOrWhiteSpace(x.ImovelRua) ? "-" : $"{x.ImovelRua}, {x.ImovelNumero}".Trim().Trim(','),
                GetImovelChaveMovimentoTipoLabel(x.Tipo),
                status,
                x.ChaveCodigo,
                x.RetiradoPorNome,
                FormatPhoneForDisplay(x.RetiradoPorTelefone),
                x.RetiradoPorDocumento,
                x.RetiradoPorRelacao,
                x.Motivo,
                x.RetiradoEm,
                x.PrevisaoDevolucaoEm,
                x.DevolvidoEm,
                x.DevolvidoParaNome,
                x.Observacoes);
        }).ToList();
    }
}
