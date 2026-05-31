using System.Windows;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoaDocumentoOcrFallbackHooked;
    private static readonly bool PessoaDocumentoOcrFallbackClassHandlerRegistered = RegisterPessoaDocumentoOcrFallbackClassHandler();

    private static bool RegisterPessoaDocumentoOcrFallbackClassHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler((sender, _) => ((ShellWindow)sender).HookPessoaDocumentoOcrFallback()));

        return true;
    }

    private void HookPessoaDocumentoOcrFallback()
    {
        _ = PessoaDocumentoOcrFallbackClassHandlerRegistered;

        if (_pessoaDocumentoOcrFallbackHooked)
        {
            return;
        }

        _pessoaDocumentoOcrFallbackHooked = true;
        Dispatcher.BeginInvoke(() =>
        {
            SavePessoaDocumentoButton.Click += SavePessoaDocumentoOcrFallbackButton_Click;
        }, DispatcherPriority.Background);
    }

    private async void SavePessoaDocumentoOcrFallbackButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isPessoaEditing)
        {
            return;
        }

        var documentoDe = PessoaDocumentoDonoBox.SelectedValue as string ?? "pessoa";
        var filePath = PessoaDocumentoArquivoBox.Text?.Trim();
        var contentType = string.IsNullOrWhiteSpace(filePath) ? null : GuessPessoaDocumentoContentType(filePath);
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        try
        {
            var text = await ExtractPessoaDocumentoTextForFormAsync(filePath, contentType);
            ApplyPessoaDocumentoOcrIdentityFallbackToForm(documentoDe, text);
        }
        catch
        {
            // The normal document save flow already reports OCR errors. This fallback must stay silent.
        }
    }

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
