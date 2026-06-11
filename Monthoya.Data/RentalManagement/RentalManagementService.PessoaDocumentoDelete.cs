using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task DeletePessoaDocumentoAsync(Guid documentoId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var documento = await dbContext.PessoaDocumentos.SingleOrDefaultAsync(x => x.Id == documentoId, cancellationToken)
            ?? throw new InvalidOperationException("Documento não encontrado.");

        if (fileStorageService is not null)
        {
            await fileStorageService.DeleteAsync(documento.StoragePath, cancellationToken);
        }

        dbContext.PessoaDocumentos.Remove(documento);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
