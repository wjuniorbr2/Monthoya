using System.Globalization;
using System.Windows.Controls;
using System.Windows.Input;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private void PessoaDatePicker_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not DatePicker datePicker)
        {
            return;
        }

        e.Handled = true;
        Dispatcher.BeginInvoke(
            () => TryApplyBrazilianDate(datePicker),
            System.Windows.Threading.DispatcherPriority.Background);
    }

    private void PessoaDatePicker_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is DatePicker datePicker)
        {
            TryApplyBrazilianDate(datePicker);
        }
    }

    private bool TryApplyBrazilianDate(DatePicker datePicker)
    {
        return TryApplyBrazilianDate(datePicker, message => PessoaErrorText.Text = message);
    }

    private static bool TryApplyBrazilianDate(DatePicker datePicker, Action<string> setError)
    {
        var text = datePicker.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var digits = OnlyDigits(text);
        if (digits.Length == 6)
        {
            setError("Use o ano com quatro números. Exemplo: 25/04/1998.");
            return false;
        }

        if (digits.Length != 8)
        {
            setError("Data inválida. Use dia/mês/ano no formato brasileiro. Exemplo: 25/04/1998.");
            return false;
        }

        var normalized = $"{digits[..2]}/{digits.Substring(2, 2)}/{digits.Substring(4, 4)}";
        if (!DateTime.TryParseExact(normalized, "dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var parsed))
        {
            setError("Data inválida. Use dia/mês/ano no formato brasileiro. Exemplo: 25/04/1998.");
            return false;
        }

        datePicker.SelectedDate = parsed;
        datePicker.Text = parsed.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"));
        setError(string.Empty);
        return true;
    }
}
