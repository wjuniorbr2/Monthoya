using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task SetPessoaActiveAsync(Guid pessoaId, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var pessoa = await dbContext.Pessoas.SingleOrDefaultAsync(x => x.Id == pessoaId, cancellationToken)
            ?? throw new InvalidOperationException("Pessoa não encontrada.");

        pessoa.Status = isActive ? RegistroStatus.Ativo : RegistroStatus.Inativo;
        pessoa.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
