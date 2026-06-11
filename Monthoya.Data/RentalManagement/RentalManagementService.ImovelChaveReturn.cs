using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<ImovelChaveMovimentoSummary> ReturnImovelChaveMovimentoAsync(ReturnImovelChaveMovimentoRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.MovimentoId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione a retirada de chave.");
        }

        var movimento = await dbContext.ImovelChaveMovimentos
            .SingleOrDefaultAsync(x => x.Id == request.MovimentoId, cancellationToken)
            ?? throw new InvalidOperationException("Movimentação de chave não encontrada.");

        if (movimento.DevolvidoEm.HasValue)
        {
            throw new InvalidOperationException("Esta chave já foi devolvida.");
        }

        movimento.Tipo = ImovelChaveMovimentoTipo.Devolucao;
        movimento.Status = ImovelChaveMovimentoStatus.ComImobiliaria;
        movimento.DevolvidoEm = DateTimeOffset.Now;
        movimento.DevolvidoParaNome = TrimOrNull(request.DevolvidoParaNome);
        movimento.Observacoes = MergeNotes(movimento.Observacoes, request.Observacoes);
        movimento.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetImovelChaveMovimentosAsync(movimento.ImovelId, cancellationToken)).Single(x => x.Id == movimento.Id);
    }
}
