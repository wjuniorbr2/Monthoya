using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<string> GetPessoaDocumentoOpenTargetAsync(Guid documentoId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var documento = await dbContext.PessoaDocumentos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == documentoId, cancellationToken)
            ?? throw new InvalidOperationException("Documento não encontrado.");

        if (Path.IsPathRooted(documento.StoragePath) || fileStorageService is null)
        {
            return documento.StoragePath;
        }

        var signedUrl = await fileStorageService.CreateSignedReadUrlAsync(documento.StoragePath, null, cancellationToken);
        return signedUrl.Url;
    }
}
