using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoaDocumentosContextMenuApplied;
    private static readonly bool PessoaDocumentosContextMenuRegistered = RegisterPessoaDocumentosContextMenu();

    private static bool RegisterPessoaDocumentosContextMenu()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler((sender, _) => ((ShellWindow)sender).ApplyPessoaDocumentosContextMenu()));

        return true;
    }

    private void ApplyPessoaDocumentosContextMenu()
    {
        _ = PessoaDocumentosContextMenuRegistered;

        if (_pessoaDocumentosContextMenuApplied)
        {
            return;
        }

        _pessoaDocumentosContextMenuApplied = true;

        PessoaDocumentosGrid.MouseRightButtonDown += PessoaDocumentosGrid_MouseRightButtonDown;
        PessoaDocumentosGrid.ContextMenu = BuildPessoaDocumentosContextMenu();
    }

    private ContextMenu BuildPessoaDocumentosContextMenu()
    {
        var menu = new ContextMenu();

        var useDataItem = new MenuItem { Header = "Usar dados do documento" };
        useDataItem.Click += UsePessoaDocumentoDataMenuItem_Click;
        menu.Items.Add(useDataItem);

        var openItem = new MenuItem { Header = "Abrir documento" };
        openItem.Click += OpenPessoaDocumentoMenuItem_Click;
        menu.Items.Add(openItem);

        menu.Items.Add(new Separator());

        var removeItem = new MenuItem { Header = "Remover documento" };
        removeItem.Click += RemovePessoaDocumentoMenuItem_Click;
        menu.Items.Add(removeItem);

        menu.Opened += (_, _) =>
        {
            var selected = PessoaDocumentosGrid.SelectedItem as PessoaDocumentoSummary;
            var hasSelection = selected is not null;
            useDataItem.IsEnabled = hasSelection && _isPessoaEditing;
            openItem.IsEnabled = hasSelection;
            removeItem.IsEnabled = hasSelection && _isPessoaEditing;
        };

        return menu;
    }

    private void PessoaDocumentosGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var row = FindPessoaDocumentoAncestor<DataGridRow>((DependencyObject)e.OriginalSource);
        if (row is null)
        {
            return;
        }

        row.IsSelected = true;
        row.Focus();
    }

    private async void UsePessoaDocumentoDataMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PessoaDocumentoErrorText.Text = string.Empty;

        if (!_isPessoaEditing)
        {
            PessoaDocumentoErrorText.Text = "Clique em Editar antes de usar dados do documento.";
            return;
        }

        if (PessoaDocumentosGrid.SelectedItem is not PessoaDocumentoSummary document)
        {
            PessoaDocumentoErrorText.Text = "Selecione um documento na lista.";
            return;
        }

        try
        {
            var rawText = document.OcrTextoExtraido;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                rawText = await ExtractPessoaDocumentoTextForFormAsync(document.StoragePath, GuessPessoaDocumentoContentType(document.StoragePath));
            }

            ShowPessoaDocumentoOcrDebugText(document.Nome, NormalizeDocumentoDeFromDisplay(document.DocumentoDe), rawText);
            ApplyPessoaDocumentoOcrTextToForm(document.Tipo, NormalizeDocumentoDeFromDisplay(document.DocumentoDe), rawText);
            ApplyPessoaDocumentoOcrIdentityFallbackToForm(NormalizeDocumentoDeFromDisplay(document.DocumentoDe), rawText);

            if (_selectedPessoaId.HasValue && _isPessoaEditing && !string.IsNullOrWhiteSpace(rawText))
            {
                await _rentalManagementService.UpdatePessoaAsync(new UpdatePessoaRequest(_selectedPessoaId.Value, BuildPessoaRequest()));
                await RefreshSelectedPessoaAfterDocumentoOcrAsync(_selectedPessoaId.Value);
                await LoadPessoaDocumentosAsync(_selectedPessoaId);
            }

            PessoaDocumentoErrorText.Text = "Dados do documento aplicados para revisão.";
        }
        catch (Exception ex)
        {
            PessoaDocumentoErrorText.Text = ex.Message;
        }
    }

    private void OpenPessoaDocumentoMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (PessoaDocumentosGrid.SelectedItem is not PessoaDocumentoSummary document)
        {
            return;
        }

        try
        {
            if (!File.Exists(document.StoragePath))
            {
                PessoaDocumentoErrorText.Text = "O arquivo do documento não foi encontrado no computador.";
                return;
            }

            Process.Start(new ProcessStartInfo(document.StoragePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            PessoaDocumentoErrorText.Text = ex.Message;
        }
    }

    private void RemovePessoaDocumentoMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PessoaDocumentoErrorText.Text = string.Empty;

        if (!_isPessoaEditing)
        {
            PessoaDocumentoErrorText.Text = "Clique em Editar antes de remover documentos.";
            return;
        }

        if (PessoaDocumentosGrid.SelectedItem is not PessoaDocumentoSummary document)
        {
            return;
        }

        if (document.Id != Guid.Empty)
        {
            PessoaDocumentoErrorText.Text = "Remoção de documento já salvo será habilitada na próxima etapa.";
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Deseja remover o documento '{document.Nome}'?",
            "Remover documento",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        RemovePendingPessoaDocumento(document);
        RefreshPendingPessoaDocumentosGrid();
        PessoaDocumentoErrorText.Text = "Documento removido.";
        QueuePessoaDocumentosCardPruning();
    }

    private void RemovePendingPessoaDocumento(PessoaDocumentoSummary document)
    {
        var index = _pendingPessoaDocumentos.FindIndex(draft =>
            string.Equals(draft.Nome, document.Nome, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Path.GetFileName(draft.StoragePath), document.StoragePath, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            _pendingPessoaDocumentos.RemoveAt(index);
        }
    }

    private static string NormalizeDocumentoDeFromDisplay(string? documentoDe)
    {
        if (string.IsNullOrWhiteSpace(documentoDe))
        {
            return "pessoa";
        }

        return documentoDe.Trim().ToLowerInvariant() switch
        {
            "cônjuge" or "conjuge" => "conjuge",
            "responsável" or "responsavel" => "responsavel",
            "empresa" => "empresa",
            "trabalho da pessoa" => "empresa_trabalho",
            "trabalho do cônjuge" or "trabalho do conjuge" => "trabalho_conjuge",
            "trabalho do cônjuge do responsável" or "trabalho do conjuge do responsavel" => "trabalho_conjuge_responsavel",
            "outros" => "outros",
            _ => "pessoa"
        };
    }

    private static T? FindPessoaDocumentoAncestor<T>(DependencyObject source) where T : DependencyObject
    {
        var current = source;
        while (current is not null)
        {
            if (current is T typed)
            {
                return typed;
            }

            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
