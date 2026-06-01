using System.Windows.Controls;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private void ApplyPessoaDocumentoOcrIdentityFallbackToForm(string documentoDe, string? text)
    {
        var values = PessoaDocumentoOcrParser.ExtractIdentityFields(text);
        ApplyPessoaDocumentoFallbackName(documentoDe, values.Nome);

        switch (documentoDe)
        {
            case "pessoa":
                FillIfBlank(PessoaDocumentoBox, values.Cpf);
                ReplaceRecentDate(PessoaDataNascimentoBox, values.DataNascimento);
                break;
            case "conjuge":
                FillIfBlank(PessoaConjugeCpfBox, values.Cpf);
                ReplaceRecentDate(PessoaConjugeDataNascimentoBox, values.DataNascimento);
                break;
            case "responsavel":
                FillIfBlank(PessoaResponsavelCpfBox, values.Cpf);
                ReplaceRecentDate(PessoaResponsavelDataNascimentoBox, values.DataNascimento);
                break;
            default:
                break;
        }
    }

    private static void ReplaceRecentDate(DatePicker? datePicker, DateOnly? value)
    {
        if (datePicker is null || !value.HasValue)
        {
            return;
        }

        if (!datePicker.SelectedDate.HasValue || datePicker.SelectedDate.Value.Year >= DateTime.Today.Year - 15)
        {
            datePicker.SelectedDate = value.Value.ToDateTime(TimeOnly.MinValue);
        }
    }

    private void ApplyPessoaDocumentoFallbackName(string documentoDe, string? fallbackName)
    {
        var box = documentoDe switch
        {
            "pessoa" => PessoaNomeBox,
            "conjuge" => PessoaConjugeNomeBox,
            "responsavel" => PessoaResponsavelNomeBox,
            _ => null
        };

        FillIfBlank(box, fallbackName);
    }
}
