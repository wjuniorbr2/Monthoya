using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ImovelNumericInputGuardsRegistered = RegisterImovelNumericInputGuards();
    private bool _imovelNumericInputGuardsApplied;

    private static bool RegisterImovelNumericInputGuards()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForImovelNumericInputGuards));

        return true;
    }

    private static void OnShellWindowLoadedForImovelNumericInputGuards(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyImovelNumericInputGuards, DispatcherPriority.ContextIdle);
        }
    }

    private void ApplyImovelNumericInputGuards()
    {
        if (_imovelNumericInputGuardsApplied)
        {
            return;
        }

        _imovelNumericInputGuardsApplied = true;

        AttachImovelDecimalInput(ImovelColetaLixoBox);
        AttachImovelIntegerInput(ImovelQuartosBox);
        AttachImovelIntegerInput(ImovelSuitesBox);
        AttachImovelIntegerInput(ImovelBanheirosBox);
        AttachImovelIntegerInput(ImovelVagasBox);
    }

    private void AttachImovelDecimalInput(TextBox textBox)
    {
        textBox.PreviewTextInput += DecimalTextBox_PreviewTextInput;
        DataObject.AddPastingHandler(textBox, DecimalTextBox_OnPaste);
        textBox.LostKeyboardFocus += (_, _) => FormatDecimalTextBox(textBox);
    }

    private void AttachImovelIntegerInput(TextBox textBox)
    {
        textBox.PreviewTextInput += ImovelIntegerTextBox_PreviewTextInput;
        DataObject.AddPastingHandler(textBox, ImovelIntegerTextBox_OnPaste);
        textBox.LostKeyboardFocus += ImovelIntegerTextBox_LostKeyboardFocus;
    }

    private void ImovelIntegerTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (Regex.IsMatch(e.Text, @"^\d+$"))
        {
            return;
        }

        e.Handled = true;
        ImovelErrorText.Text = "Campos de quantidade devem aceitar apenas números inteiros.";
    }

    private void ImovelIntegerTextBox_OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            return;
        }

        var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (Regex.IsMatch(text, @"^\d+$"))
        {
            return;
        }

        e.CancelCommand();
        ImovelErrorText.Text = "Cole apenas números inteiros em campos de quantidade.";
    }

    private void ImovelIntegerTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is not TextBox textBox || string.IsNullOrWhiteSpace(textBox.Text))
        {
            return;
        }

        if (!int.TryParse(textBox.Text, out var value) || value < 0)
        {
            textBox.Clear();
            ImovelErrorText.Text = "Campos de quantidade devem ficar com número inteiro maior ou igual a zero.";
        }
    }
}
