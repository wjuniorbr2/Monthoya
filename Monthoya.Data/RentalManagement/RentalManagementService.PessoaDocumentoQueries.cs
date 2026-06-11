using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<IReadOnlyList<PessoaDocumentoSummary>> GetPessoaDocumentosAsync(Guid? pessoaId = null, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await GetPessoaDocumentosCoreAsync(pessoaId, cancellationToken);
    }

    private async Task<IReadOnlyList<PessoaDocumentoSummary>> GetPessoaDocumentosCoreAsync(Guid? pessoaId, CancellationToken cancellationToken)
    {
        var query = dbContext.PessoaDocumentos
            .AsNoTracking()
            .Include(x => x.Pessoa)
            .AsQueryable();

        if (pessoaId.HasValue)
        {
            query = query.Where(x => x.PessoaId == pessoaId.Value);
        }

        var documentos = await query
            .OrderBy(x => x.Pessoa!.NomeDisplay)
            .ThenBy(x => x.Tipo)
            .ToListAsync(cancellationToken);

        return documentos.Select(x => new PessoaDocumentoSummary(
            x.Id,
            x.PessoaId,
            x.Pessoa?.NomeDisplay ?? "-",
            GetPessoaDocumentoTipoLabel(x.Tipo),
            GetDocumentoDeLabel(x.DocumentoDe),
            x.Nome,
            x.StoragePath,
            x.DataValidade,
            x.Status == RegistroStatus.Ativo ? "Ativo" : "Inativo",
            GetEnumLabel(x.OcrStatus),
            x.OcrTextoExtraido,
            x.OcrProcessadoEmUtc,
            x.OcrErroMensagem,
            x.OcrCamposAplicados)).ToList();
    }

    public async Task<PessoaContratoAutofillContext?> GetPessoaContratoAutofillContextAsync(Guid pessoaId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var pessoa = (await GetPessoasCoreAsync(cancellationToken)).SingleOrDefault(x => x.Id == pessoaId);
        if (pessoa is null)
        {
            return null;
        }

        var documentos = await GetPessoaDocumentosCoreAsync(pessoaId, cancellationToken);
        var textoOcr = string.Join(
            Environment.NewLine,
            documentos
                .Where(x => !string.IsNullOrWhiteSpace(x.OcrTextoExtraido))
                .Select(x => $"[{x.Tipo} - {x.Nome}]{Environment.NewLine}{x.OcrTextoExtraido}"));

        return new PessoaContratoAutofillContext(pessoa, documentos, textoOcr);
    }
}
