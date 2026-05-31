using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoaDocumentoUiPolishApplied;
    private object? _pessoaDocumentoAddButtonOriginalContent;
    private bool _pessoaDocumentoProcessingVisualActive;

    private static readonly bool PessoaDocumentoUiPolishClassHandlerRegistered = RegisterPessoaDocumentoUiPolishClassHandler();

    private static bool RegisterPessoaDocumentoUiPolishClassHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler((sender, _) => ((ShellWindow)sender).ApplyPessoaDocumentoUiPolish()));

        return true;
    }

    private void ApplyPessoaDocumentoUiPolish()
    {
        _ = PessoaDocumentoUiPolishClassHandlerRegistered;

        if (_pessoaDocumentoUiPolishApplied)
        {
            return;
        }

        _pessoaDocumentoUiPolishApplied = true;

        SavePessoaDocumentoButton.Click += (_, _) => QueuePessoaDocumentoProcessingVisual();
        PessoaDocumentoArquivoBox.TextChanged += (_, _) =>
        {
            ClearPessoaDocumentoProcessingVisual();
            QueueRemovePessoaLegacyCpfLabels();
        };
        PessoaDocumentoNomeBox.TextChanged += (_, _) => ClearPessoaDocumentoProcessingVisual();
        PessoaDocumentosGrid.TargetUpdated += (_, _) => ClearPessoaDocumentoProcessingVisual();
        PessoaDocumentosGrid.Items.CurrentChanged += (_, _) => ClearPessoaDocumentoProcessingVisual();
        PessoaDocumentoArquivoBox.Loaded += (_, _) => QueueRemovePessoaLegacyCpfLabels();
        PessoaDataNascimentoBox.Loaded += (_, _) => QueueRemovePessoaLegacyCpfLabels();
        PessoaFisicaFieldsPanel.Loaded += (_, _) => QueueRemovePessoaLegacyCpfLabels();
        PessoasPanel.IsVisibleChanged += (_, _) => QueueRemovePessoaLegacyCpfLabels();

        QueueRemovePessoaLegacyCpfLabels();
    }

    private void QueuePessoaDocumentoProcessingVisual()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (_pessoaDocumentoProcessingVisualActive)
            {
                return;
            }

            _pessoaDocumentoProcessingVisualActive = true;
            _pessoaDocumentoAddButtonOriginalContent ??= SavePessoaDocumentoButton.Content;
            SavePessoaDocumentoButton.Content = BuildPessoaDocumentoProcessingContent();
            SavePessoaDocumentoButton.IsEnabled = false;

            _ = Dispatcher.BeginInvoke(async () =>
            {
                await Task.Delay(30000);
                ClearPessoaDocumentoProcessingVisual();
            }, DispatcherPriority.ApplicationIdle);
        }, DispatcherPriority.Background);
    }

    private void ClearPessoaDocumentoProcessingVisual()
    {
        if (!_pessoaDocumentoProcessingVisualActive)
        {
            return;
        }

        _pessoaDocumentoProcessingVisualActive = false;
        SavePessoaDocumentoButton.Content = _pessoaDocumentoAddButtonOriginalContent ?? "Adicionar documento";
        SavePessoaDocumentoButton.IsEnabled = true;
    }

    private object BuildPessoaDocumentoProcessingContent()
    {
        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        stack.Children.Add(new ProgressBar
        {
            Width = 28,
            Height = 8,
            IsIndeterminate = true,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        stack.Children.Add(new TextBlock
        {
            Text = "Processando...",
            VerticalAlignment = VerticalAlignment.Center
        });

        return stack;
    }

    private void QueueRemovePessoaLegacyCpfLabels()
    {
        Dispatcher.BeginInvoke(RemovePessoaLegacyCpfLabels, DispatcherPriority.ApplicationIdle);
        Dispatcher.BeginInvoke(RemovePessoaLegacyCpfLabels, DispatcherPriority.ContextIdle);
    }

    private void RemovePessoaLegacyCpfLabels()
    {
        foreach (var block in FindPessoaUiPolishVisualChildren<TextBlock>(PessoaFisicaFieldsPanel).ToList())
        {
            if (!string.Equals(block.Text?.Trim(), "CPF", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IsLabelForControl(block, PessoaDocumentoBox))
            {
                continue;
            }

            if (block.Parent is Panel parent)
            {
                parent.Children.Remove(block);
            }
        }
    }

    private static bool IsLabelForControl(TextBlock label, Control control)
    {
        if (label.Parent is not Panel parent)
        {
            return false;
        }

        return ReferenceEquals(parent, control.Parent) || parent.Children.Contains(control);
    }

    private static IEnumerable<T> FindPessoaUiPolishVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var childIndex = 0; childIndex < VisualTreeHelper.GetChildrenCount(parent); childIndex++)
        {
            var child = VisualTreeHelper.GetChild(parent, childIndex);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindPessoaUiPolishVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
