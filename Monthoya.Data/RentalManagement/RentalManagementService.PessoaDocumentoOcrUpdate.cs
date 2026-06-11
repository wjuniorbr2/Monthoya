using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<PessoaDocumentoSummary> UpdatePessoaDocumentoOcrAsync(UpdatePessoaDocumentoOcrRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var documento = await dbContext.PessoaDocumentos.SingleOrDefaultAsync(x => x.Id == request.DocumentoId, cancellationToken)
            ?? throw new InvalidOperationException("Documento não encontrado.");

        documento.OcrTextoExtraido = TrimOrNull(request.OcrTextoExtraido);
        documento.OcrStatus = request.Succeeded ? DocumentoOcrStatus.Processado : DocumentoOcrStatus.Erro;
        documento.OcrProcessadoEmUtc = DateTimeOffset.UtcNow;
        documento.OcrErroMensagem = TrimOrNull(request.ErrorMessage);
        documento.OcrCamposAplicados = TrimOrNull(request.CamposAplicados);
        documento.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetPessoaDocumentosCoreAsync(documento.PessoaId, cancellationToken)).Single(x => x.Id == documento.Id);
    }
}
