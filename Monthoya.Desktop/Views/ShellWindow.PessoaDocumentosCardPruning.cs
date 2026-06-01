using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoaDocumentosCardPruningApplied;
    private Button? _selecionarPessoaDocumentoArquivoButton;
    private TextBlock? _pessoaDocumentoArquivoTiposText;

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

        PessoasGrid.SelectionChanged += (_, _) => QueuePessoaDocumentosCardPruning();
        PessoaTipoBox.SelectionChanged += (_, _) => QueuePessoaDocumentosCardPruning();
        PessoaDocumentosGrid.Loaded += (_, _) => QueuePessoaDocumentosCardPruning();
        PessoasPanel.IsVisibleChanged += (_, _) => QueuePessoaDocumentosCardPruning();
        PessoasNavButton.Click += (_, _) => QueuePessoaDocumentosCardPruning();
        SavePessoaDocumentoButton.Loaded += (_, _) => QueuePessoaDocumentosCardPruning();

        QueuePessoaDocumentosCardPruning();
        _ = QueueDelayedPessoaDocumentosCardPruningAsync();
    }

    private void QueuePessoaDocumentosCardPruning()
    {
        Dispatcher.BeginInvoke(PrunePessoaDocumentosCard, DispatcherPriority.Background);
        Dispatcher.BeginInvoke(PrunePessoaDocumentosCard, DispatcherPriority.ApplicationIdle);
    }

    private async Task QueueDelayedPessoaDocumentosCardPruningAsync()
    {
        foreach (var delay in new[] { 250, 750, 1500, 3000 })
        {
            await Task.Delay(delay);
            await Dispatcher.InvokeAsync(PrunePessoaDocumentosCard, DispatcherPriority.ApplicationIdle);
        }
    }

    private void PrunePessoaDocumentosCard()
    {
        PessoaDocumentosTitleText.Text = "Documentos anexos";

        RenamePessoaDocumentosCardTextBlock("Documentos da pessoa selecionada", "Documentos anexos");
        RemovePessoaDocumentosCardTextBlockStartingWith("Nenhum documento cadastrado para esta pessoa");
        RenamePessoaDocumentosCardTextBlock("Documentos anexos:", "Anexar mais documentos");
        RenamePessoaDocumentosCardTextBlock("Caminho no Supabase Storage ou arquivo local", "Arquivo digitalizado");

        RemovePessoaDocumentosCardTextBlockStartingWith("Selecione uma pessoa na lista para vincular documentos");
        RemovePessoaDocumentosCardTextBlockStartingWith("OCR local será tentado ao registrar");
        RemovePessoaDocumentosCardLabelBefore(PessoaDocumentoPessoaText, "Pessoa selecionada");
        RemovePessoaDocumentosCardControl(PessoaDocumentoPessoaText);

        RemovePessoaDocumentosCardLabelBefore(PessoaDocumentoTipoBox, "Tipo de documento");
        RemovePessoaDocumentosCardControl(PessoaDocumentoTipoBox);
        if (PessoaDocumentoTipoBox.SelectedValue is null)
        {
            PessoaDocumentoTipoBox.SelectedIndex = 0;
        }

        RemovePessoaDocumentosGridColumn("OCR");
        RemovePessoaDocumentosGridColumn("Caminho");

        ConfigurePessoaDocumentoArquivoSelector();
        EnsurePessoaDocumentosBatchUseDataButton();
        UpdatePessoaDocumentoEditorAvailability();
    }

    private void ConfigurePessoaDocumentoArquivoSelector()
    {
        PessoaDocumentoArquivoBox.IsReadOnly = true;
        PessoaDocumentoArquivoBox.ToolTip = "Arquivo local selecionado. O sistema envia o arquivo para o armazenamento configurado ao adicionar o documento.";

        if (PessoaDocumentoArquivoBox.Parent is not Panel parent)
        {
            return;
        }

        var index = parent.Children.IndexOf(PessoaDocumentoArquivoBox);
        if (index < 0)
        {
            return;
        }

        if (_selecionarPessoaDocumentoArquivoButton is null)
        {
            _selecionarPessoaDocumentoArquivoButton = new Button
            {
                Content = "Selecionar arquivo",
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 0, 0, 6),
                ToolTip = "Escolha uma imagem para OCR inteligente ou um TXT para fallback local."
            };
            _selecionarPessoaDocumentoArquivoButton.Click += SelecionarPessoaDocumentoArquivoButton_Click;
            parent.Children.Insert(index + 1, _selecionarPessoaDocumentoArquivoButton);
        }

        _selecionarPessoaDocumentoArquivoButton.Style = TryFindResource("PrimaryButtonSmall") as Style ?? TryFindResource("PrimaryButton") as Style;
        SavePessoaDocumentoButton.Style = TryFindResource("PrimaryButton") as Style;

        if (_pessoaDocumentoArquivoTiposText is null)
        {
            _pessoaDocumentoArquivoTiposText = new TextBlock
            {
                Foreground = TryFindResource("MutedBrush") as Brush,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            };
            parent.Children.Insert(parent.Children.IndexOf(_selecionarPessoaDocumentoArquivoButton) + 1, _pessoaDocumentoArquivoTiposText);
        }

        _pessoaDocumentoArquivoTiposText.Text = "Arquivos aceitos: PNG, JPG, JPEG, BMP, TIF, TIFF e TXT.";
    }

    private void SelecionarPessoaDocumentoArquivoButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isPessoaEditing)
        {
            PessoaDocumentoErrorText.Text = "Clique em Editar para selecionar arquivos.";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Selecionar documento digitalizado",
            Filter = "Documentos aceitos|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.txt|Imagens|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|Texto|*.txt|Todos os arquivos|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        PessoaDocumentoArquivoBox.Text = dialog.FileName;

        if (string.IsNullOrWhiteSpace(PessoaDocumentoNomeBox.Text))
        {
            PessoaDocumentoNomeBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
        }
    }

    private void RemovePessoaDocumentosGridColumn(string header)
    {
        var column = PessoaDocumentosGrid.Columns
            .FirstOrDefault(x => string.Equals(x.Header?.ToString(), header, StringComparison.OrdinalIgnoreCase));

        if (column is not null)
        {
            PessoaDocumentosGrid.Columns.Remove(column);
        }
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
