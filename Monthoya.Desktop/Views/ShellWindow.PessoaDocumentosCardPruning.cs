using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoaDocumentosCardPruningApplied;

    private static readonly bool PessoaDocumentosCardPruningClassHandlerRegistered = RegisterPessoaDocumentosCardPruningClassHandler();

    private static bool RegisterPessoaDocumentosCardPruningClassHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler((sender, _) => ((ShellWindow)sender).ApplyPessoaDocumentosCardPruning()));

        return true;
    }

    private void ApplyPessoaDocumentosCardPruning()
    {
        _ = PessoaDocumentosCardPruningClassHandlerRegistered;

        if (_pessoaDocumentosCardPruningApplied)
        {
            return;
        }

        _pessoaDocumentosCardPruningApplied = true;
        PrunePessoaDocumentosCard();

        PessoasGrid.SelectionChanged += (_, _) => Dispatcher.BeginInvoke(
            PrunePessoaDocumentosCard,
            DispatcherPriority.Background);

        PessoaTipoBox.SelectionChanged += (_, _) => Dispatcher.BeginInvoke(
            PrunePessoaDocumentosCard,
            DispatcherPriority.Background);

        Dispatcher.BeginInvoke(PrunePessoaDocumentosCard, DispatcherPriority.Background);
    }

    private void PrunePessoaDocumentosCard()
    {
        if (PessoaDocumentosTitleText is not null)
        {
            PessoaDocumentosTitleText.Text = "Documentos anexos";
        }

        RenamePessoaDocumentosCardTextBlock("Documentos anexos:", "Anexar mais documentos");
        RemovePessoaDocumentosCardTextBlockStartingWith("Selecione uma pessoa na lista para vincular documentos");
        RemovePessoaDocumentosCardTextBlockStartingWith("OCR local será tentado ao registrar");
        RemovePessoaDocumentosCardLabelBefore(PessoaDocumentoPessoaText, "Pessoa selecionada");
        RemovePessoaDocumentosCardControl(PessoaDocumentoPessoaText);
    }

    private void RenamePessoaDocumentosCardTextBlock(string currentText, string newText)
    {
        foreach (var block in FindPessoaDocumentosCardVisualChildren<TextBlock>(this))
        {
            if (string.Equals(block.Text, currentText, StringComparison.Ordinal))
            {
                block.Text = newText;
            }
        }
    }

    private void RemovePessoaDocumentosCardTextBlockStartingWith(string textStart)
    {
        foreach (var block in FindPessoaDocumentosCardVisualChildren<TextBlock>(this).ToList())
        {
            if ((block.Text ?? string.Empty).StartsWith(textStart, StringComparison.OrdinalIgnoreCase))
            {
                RemovePessoaDocumentosCardElement(block);
            }
        }
    }

    private static void RemovePessoaDocumentosCardLabelBefore(FrameworkElement control, string expectedLabel)
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

        parent.Children.Remove(label);
    }

    private static void RemovePessoaDocumentosCardControl(FrameworkElement control)
    {
        RemovePessoaDocumentosCardElement(control);
    }

    private static void RemovePessoaDocumentosCardElement(FrameworkElement element)
    {
        if (element.Parent is Panel parent)
        {
            parent.Children.Remove(element);
        }
    }

    private static IEnumerable<T> FindPessoaDocumentosCardVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var childIndex = 0; childIndex < VisualTreeHelper.GetChildrenCount(parent); childIndex++)
        {
            var child = VisualTreeHelper.GetChild(parent, childIndex);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindPessoaDocumentosCardVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
