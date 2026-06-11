using Monthoya.Core.Entities;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    private async Task SavePessoaOcrChangesAsync(Pessoa pessoa, IReadOnlyCollection<string> filledFields, CancellationToken cancellationToken)
    {
        if (filledFields.Count == 0)
        {
            return;
        }

        pessoa.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsResidencePessoaDocumento(string documentoTipo) =>
        documentoTipo.Equals("residencia", StringComparison.OrdinalIgnoreCase)
        || documentoTipo.Equals("endereco_residencia", StringComparison.OrdinalIgnoreCase);
}
