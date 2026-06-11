using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<ImovelChaveMovimentoSummary> CreateImovelChaveMovimentoAsync(CreateImovelChaveMovimentoRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.ImovelId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione o imóvel da chave.");
        }

        var imovel = await dbContext.Imoveis.SingleOrDefaultAsync(x => x.Id == request.ImovelId, cancellationToken)
            ?? throw new InvalidOperationException("Imóvel não encontrado.");

        if (request.Tipo == ImovelChaveMovimentoTipo.Retirada)
        {
            if (string.IsNullOrWhiteSpace(request.RetiradoPorNome))
            {
                throw new InvalidOperationException("Informe quem retirou a chave.");
            }

            if (!request.PrevisaoDevolucaoEm.HasValue)
            {
                throw new InvalidOperationException("Informe a previsão de devolução da chave.");
            }
        }

        var movimento = new ImovelChaveMovimento
        {
            ImovelId = request.ImovelId,
            ChaveCodigo = TrimOrNull(request.ChaveCodigo) ?? imovel.ChaveCodigo,
            Tipo = request.Tipo,
            RetiradoPorNome = TrimOrNull(request.RetiradoPorNome),
            RetiradoPorTelefone = DigitsOrNull(request.RetiradoPorTelefone),
            RetiradoPorDocumento = TrimOrNull(request.RetiradoPorDocumento),
            RetiradoPorRelacao = TrimOrNull(request.RetiradoPorRelacao),
            Motivo = TrimOrNull(request.Motivo),
            RetiradoEm = request.RetiradoEm ?? DateTimeOffset.Now,
            PrevisaoDevolucaoEm = request.PrevisaoDevolucaoEm,
            Status = request.Tipo == ImovelChaveMovimentoTipo.Retirada
                ? ImovelChaveMovimentoStatus.Retirada
                : ImovelChaveMovimentoStatus.ComImobiliaria,
            Observacoes = TrimOrNull(request.Observacoes)
        };

        dbContext.ImovelChaveMovimentos.Add(movimento);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetImovelChaveMovimentosAsync(request.ImovelId, cancellationToken)).Single(x => x.Id == movimento.Id);
    }
}
