using System.Windows.Controls;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private void ApplyPessoaDocumentoOcrIdentityFallbackToForm(string documentoDe, string? text)
    {
        var values = PessoaDocumentoOcrParser.ExtractIdentityFields(text);
        if (string.IsNullOrWhiteSpace(values.Cpf) && !values.DataNascimento.HasValue)
        {
            return;
        }

        switch (documentoDe)
        {
            case "pessoa":
                FillIfBlank(PessoaDocumentoBox, values.Cpf);
                FillDateIfBlank(PessoaDataNascimentoBox, values.DataNascimento);
                break;
            case "conjuge":
                FillIfBlank(PessoaConjugeCpfBox, values.Cpf);
                FillDateIfBlank(PessoaConjugeDataNascimentoBox, values.DataNascimento);
                break;
            case "responsavel":
                FillIfBlank(PessoaResponsavelCpfBox, values.Cpf);
                FillDateIfBlank(PessoaResponsavelDataNascimentoBox, values.DataNascimento);
                break;
            default:
                break;
        }
    }
}
