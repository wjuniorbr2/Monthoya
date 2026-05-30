using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoaDocumentosCardCleanupApplied;

    private static readonly bool PessoaDocumentosCardCleanupClassHandlerRegistered = RegisterPessoaDocumentosCardCleanupClassHandler();

    private static bool RegisterPessoaDocumentosCardCleanupClassHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler((sender, _) => ((ShellWindow)sender).ApplyPessoaDocumentosCardCleanup()));

        return true;
    }

    private void ApplyPessoaDocumentosCardCleanup()
    {
        _ = PessoaDocumentosCardCleanupClassHandlerRegistered;

        if (_pessoaDocumentosCardCleanupApplied)
        {
            return;
        }

        _pessoaDocumentosCardCleanupApplied = true;
        CleanPessoaDocumentosCardText();

        PessoasGrid.SelectionChanged += (_, _) => Dispatcher.BeginInvoke(
            CleanPessoaDocumentosCardText,
            DispatcherPriority.Background);

        PessoaTipoBox.SelectionChanged += (_, _) => Dispatcher.BeginInvoke(
            CleanPessoaDocumentosCardText,
            DispatcherPriority.Background);

        Dispatcher.BeginInvoke(CleanPessoaDocumentosCardText, DispatcherPriority.Background);
    }

    private void CleanPessoaDocumentosCardText()
    {
        if (PessoaDocumentosTitleText is not null)
        {
            PessoaDocumentosTitleText.Text = "Documentos anexos";
        }

        RenameTextBlock("Documentos anexos:", "Anexar mais documentos");
        HideTextBlockStartingWith("Selecione uma pessoa na lista para vincular documentos");
        HideTextBlockStartingWith("O banco salva metadados");
        HideTextBlockStartingWith("OCR local será tentado ao registrar");
        HideTextBlockStartingWith("Sem motor OCR configurado");
        HideLabelBeforeControl(PessoaDocumentoPessoaText, "Pessoa selecionada");
        HideControl(PessoaDocumentoPessoaText);
    }

    private void RenameTextBlock(string currentText, string newText)
    {
        foreach (var block in FindVisualChildren<TextBlock>(this))
        {
            if (string.Equals(block.Text, currentText, StringComparison.Ordinal))
            {
                block.Text = newText;
            }
        }
    }

    private void HideTextBlockStartingWith(string textStart)
    {
        foreach (var block in FindVisualChildren<TextBlock>(this))
        {
            if ((block.Text ?? string.Empty).StartsWith(textStart, StringComparison.OrdinalIgnoreCase))
            {
                block.Visibility = Visibility.Collapsed;
                block.Margin = new Thickness(0);
            }
        }
    }

    private static void HideLabelBeforeControl(Control control, string expectedLabel)
    {
        if (control.Parent is not Panel parent)
        {
            return;
        }

        var index = parent.Children.IndexOf(control);
        if (index <= 0 || parent.Children[index - 1] is not TextBlock label)
        {
            return;
        }

        if (!string.Equals(label.Text, expectedLabel, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        label.Visibility = Visibility.Collapsed;
        label.Margin = new Thickness(0);
    }

    private static void HideControl(Control control)
    {
        control.Visibility = Visibility.Collapsed;
        control.Margin = new Thickness(0);
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is null)
        {
            yield break;
        }

        for (var childIndex = 0; childIndex < VisualTreeHelper.GetChildrenCount(parent); childIndex++)
        {
            var child = VisualTreeHelper.GetChild(parent, childIndex);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
