using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<IReadOnlyList<VistoriaSummary>> GetVistoriasAsync(Guid? imovelId = null, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var query = dbContext.Vistorias
            .AsNoTracking()
            .Include(x => x.Imovel)
            .AsQueryable();

        if (imovelId.HasValue)
        {
            query = query.Where(x => x.ImovelId == imovelId.Value);
        }

        return await query.OrderByDescending(x => x.DataVistoria)
            .Select(x => new VistoriaSummary(
                x.Id,
                x.ImovelId,
                x.Imovel == null ? "-" : (x.Imovel.Rua + ", " + x.Imovel.Numero).Trim().Trim(','),
                GetVistoriaTipoLabel(x.Tipo),
                x.DataVistoria,
                x.Responsavel,
                GetVistoriaStatusLabel(x.WorkflowStatus),
                x.Observacoes))
            .ToListAsync(cancellationToken);
    }
}
