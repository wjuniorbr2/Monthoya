using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Monthoya.Core.Entities;
using Monthoya.Core.Integrations;
using Monthoya.Core.Services;
using Monthoya.Data.Storage;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{

    public async Task<VistoriaSummary> CreateVistoriaAsync(CreateVistoriaRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.ImovelId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione o imóvel da vistoria.");
        }

        var imovelExists = await dbContext.Imoveis.AnyAsync(x => x.Id == request.ImovelId, cancellationToken);
        if (!imovelExists)
        {
            throw new InvalidOperationException("Imóvel não encontrado.");
        }

        var vistoria = new Vistoria
        {
            ImovelId = request.ImovelId,
            LocacaoId = request.LocacaoId,
            Tipo = request.Tipo,
            DataVistoria = request.DataVistoria,
            Responsavel = TrimOrNull(request.Responsavel),
            WorkflowStatus = request.WorkflowStatus,
            Status = GetVistoriaStatusLabel(request.WorkflowStatus),
            Descricao = TrimOrNull(request.DescricaoGeral),
            DescricaoGeral = TrimOrNull(request.DescricaoGeral),
            Observacoes = TrimOrNull(request.Observacoes)
        };

        dbContext.Vistorias.Add(vistoria);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetVistoriasAsync(request.ImovelId, cancellationToken)).Single(x => x.Id == vistoria.Id);
    }
}
