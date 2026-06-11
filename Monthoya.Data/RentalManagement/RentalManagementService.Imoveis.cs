using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Monthoya.Core.Entities;
using Monthoya.Core.Integrations;
using Monthoya.Core.Services;
using Monthoya.Data.Storage;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{



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
