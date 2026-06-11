using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<PessoaDocumentoSummary> CreatePessoaDocumentoAsync(CreatePessoaDocumentoRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.PessoaId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione a pessoa do documento.");
        }

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            throw new InvalidOperationException("Informe o nome do documento.");
        }

        if (string.IsNullOrWhiteSpace(request.StoragePath))
        {
            throw new InvalidOperationException("Informe o caminho do arquivo digitalizado.");
        }

        var pessoaExists = await dbContext.Pessoas.AnyAsync(x => x.Id == request.PessoaId, cancellationToken);
        if (!pessoaExists)
        {
            throw new InvalidOperationException("Pessoa não encontrada.");
        }

        var documento = new PessoaDocumento
        {
            PessoaId = request.PessoaId,
            Tipo = string.IsNullOrWhiteSpace(request.Tipo) ? "outros" : request.Tipo.Trim(),
            DocumentoDe = string.IsNullOrWhiteSpace(request.DocumentoDe) ? "pessoa" : request.DocumentoDe.Trim(),
            Nome = request.Nome.Trim(),
            ContentType = TrimOrNull(request.ContentType),
            DataValidade = request.DataValidade,
            Observacoes = TrimOrNull(request.Observacoes),
            SkipOcrAutofill = !request.ApplyOcrToPessoa
        };
        documento.StoragePath = await StorePessoaDocumentoAsync(documento.Id, request.PessoaId, request.StoragePath.Trim(), documento.ContentType, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.OcrTextoExtraido))
        {
            documento.OcrTextoExtraido = TrimOrNull(request.OcrTextoExtraido);
            documento.OcrProcessadoEmUtc = DateTimeOffset.UtcNow;
            documento.OcrStatus = DocumentoOcrStatus.Processado;
        }
        else if (documentOcrService is not null)
        {
            var ocrResult = await documentOcrService.ExtractTextAsync(documento.StoragePath, documento.ContentType, cancellationToken);
            documento.OcrTextoExtraido = TrimOrNull(ocrResult.ExtractedText);
            documento.OcrProcessadoEmUtc = DateTimeOffset.UtcNow;
            documento.OcrStatus = ocrResult.Succeeded ? DocumentoOcrStatus.Processado : DocumentoOcrStatus.Erro;
            documento.OcrErroMensagem = TrimOrNull(ocrResult.ErrorMessage);

            if (request.ApplyOcrToPessoa && ocrResult.Succeeded && !string.IsNullOrWhiteSpace(ocrResult.ExtractedText))
            {
                var filledFields = await ApplyPessoaOcrFieldsAsync(request.PessoaId, documento.Tipo, documento.DocumentoDe, ocrResult.ExtractedText, cancellationToken);
                documento.OcrCamposAplicados = filledFields.Count == 0 ? null : string.Join(", ", filledFields);
            }
        }

        dbContext.PessoaDocumentos.Add(documento);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetPessoaDocumentosCoreAsync(request.PessoaId, cancellationToken)).Single(x => x.Id == documento.Id);
    }
}
