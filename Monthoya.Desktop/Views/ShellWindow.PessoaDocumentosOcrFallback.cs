using System.Text.RegularExpressions;
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
        CleanPessoaDocumentoSuspiciousName(documentoDe);
        if (string.IsNullOrWhiteSpace(values.Cpf) && !values.DataNascimento.HasValue)
        {
            return;
        }

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

    private static void ReplaceRecentDate(System.Windows.Controls.DatePicker? datePicker, DateOnly? value)
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

    private void CleanPessoaDocumentoSuspiciousName(string documentoDe)
    {
        var box = documentoDe switch
        {
            "pessoa" => PessoaNomeBox,
            "conjuge" => PessoaConjugeNomeBox,
            "responsavel" => PessoaResponsavelNomeBox,
            _ => null
        };

        if (box is not null && IsSuspiciousOcrName(box.Text))
        {
            box.Clear();
        }
    }

    private static bool IsSuspiciousOcrName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var cleaned = Regex.Replace(value.Trim(), @"\s+", " ");
        var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 3)
        {
            return false;
        }

        var shortWords = words.Count(word => word.Length <= 2);
        var noCommonPortugueseNameParts = !Regex.IsMatch(cleaned, @"\b(DE|DA|DO|DAS|DOS|SILVA|SOUZA|SOUSA|SANTOS|OLIVEIRA|PEREIRA|CARVALHO|JUNIOR|JÚNIOR|FILHO|NETO)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var tooManyOddLetters = words.Count(word => Regex.IsMatch(word, @"[WVY]{2,}|[QKX]{1,}")) >= 2;

        return noCommonPortugueseNameParts && (shortWords >= 1 || tooManyOddLetters);
    }
}
