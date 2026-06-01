namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static bool IsResidencePessoaDocumento(string? documentoTipo)
    {
        if (string.IsNullOrWhiteSpace(documentoTipo))
        {
            return false;
        }

        return documentoTipo.Contains("residencia", StringComparison.OrdinalIgnoreCase)
            || documentoTipo.Contains("residência", StringComparison.OrdinalIgnoreCase)
            || documentoTipo.Contains("endereco", StringComparison.OrdinalIgnoreCase)
            || documentoTipo.Contains("endereço", StringComparison.OrdinalIgnoreCase)
            || documentoTipo.Contains("comprovante", StringComparison.OrdinalIgnoreCase);
    }
}
