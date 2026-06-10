using System.Globalization;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{

    private async Task LoadImoveisAsync()
    {
        _imoveis = await _rentalManagementService.GetImoveisAsync();
        ApplyImoveisFilter();

        _pessoas = await _rentalManagementService.GetPessoasAsync();
        RefreshImovelProprietarioOptions();
        await UpdateImovelOverdueKeysIndicatorAsync();
        if (_selectedImovelId.HasValue)
        {
            await LoadImovelImagensAsync(_selectedImovelId.Value);
            await LoadImovelVistoriasAsync(_selectedImovelId.Value);
        }
    }

    private async Task UpdateImovelOverdueKeysIndicatorAsync()
    {
        var movimentos = await _rentalManagementService.GetImovelChaveMovimentosAsync();
        var overdueCount = movimentos.Count(x => string.Equals(x.Status, "Em atraso", StringComparison.OrdinalIgnoreCase));
        ImovelOverdueKeysText.Text = overdueCount == 0
            ? string.Empty
            : overdueCount == 1
                ? "1 chave em atraso"
                : $"{overdueCount} chaves em atraso";
    }

    private async void ReloadImoveisButton_Click(object sender, RoutedEventArgs e) => await LoadImoveisAsync();

    private void ImovelProprietarioBox_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Up or Key.Down or Key.Left or Key.Right or Key.Enter or Key.Escape or Key.Tab)
        {
            return;
        }

        RefreshImovelProprietarioOptions(ImovelProprietarioBox.Text, openDropDown: true);
    }

    private void RefreshImovelProprietarioOptions(string? query = null, bool openDropDown = false)
    {
        var text = query ?? string.Empty;
        var owners = _pessoas
            .Where(x => x.Status == "Ativo")
            .Where(x => string.IsNullOrWhiteSpace(text) || ContainsSearch(text, x.Nome, x.Documento, x.Telefone))
            .OrderBy(x => x.Nome)
            .Take(50)
            .ToList();

        ImovelProprietarioBox.ItemsSource = owners;

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        ImovelProprietarioBox.Text = text;
        ImovelProprietarioBox.IsDropDownOpen = openDropDown && owners.Count > 0;
        Dispatcher.BeginInvoke(() =>
        {
            if (ImovelProprietarioBox.Template.FindName("PART_EditableTextBox", ImovelProprietarioBox) is TextBox textBox)
            {
                textBox.SelectionStart = textBox.Text.Length;
                textBox.SelectionLength = 0;
            }
        });
    }

    private void ImoveisSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ImoveisFilter_Changed(sender, e);
    }

    private void ImoveisFilter_Changed(object sender, EventArgs e)
    {
        ApplyImoveisFilter();
        SaveActiveTabState();
    }

    private void ApplyImoveisFilter()
    {
        var query = ImoveisSearchBox.Text;
        var statusFilter = ImoveisStatusFilterBox.SelectedValue as string ?? "ativos";
        var finalidadeFilter = ImoveisFinalidadeFilterBox.SelectedValue as ImovelFinalidade?;
        var publicacaoFilter = ImoveisPublicacaoFilterBox.SelectedValue as string ?? "todos";
        ImoveisGrid.ItemsSource = _imoveis
            .Where(x => ContainsSearch(query, x.Endereco, x.Bairro, x.Proprietario, x.TipoImovel, x.Finalidade, x.Status))
            .Where(x => statusFilter switch
            {
                "ativos" => !string.Equals(x.Status, "Inativo", StringComparison.OrdinalIgnoreCase),
                "todos" => true,
                _ => NormalizeSearch(x.Status).Contains(NormalizeSearch(statusFilter), StringComparison.OrdinalIgnoreCase)
            })
            .Where(x => !finalidadeFilter.HasValue || string.Equals(x.Finalidade, GetImovelFinalidadeLabel(finalidadeFilter.Value), StringComparison.OrdinalIgnoreCase))
            .Where(x => publicacaoFilter switch
            {
                "privado" => string.Equals(x.Publicacao, "Privado", StringComparison.OrdinalIgnoreCase),
                "site" => x.Publicacao.Contains("Site", StringComparison.OrdinalIgnoreCase),
                "app" => x.Publicacao.Contains("App", StringComparison.OrdinalIgnoreCase),
                "destaque" => x.Publicacao.Contains("destaque", StringComparison.OrdinalIgnoreCase),
                _ => true
            })
            .ToList();
    }

    private void NewImovelButton_Click(object sender, RoutedEventArgs e)
    {
        ImoveisGrid.SelectedItem = null;
        _selectedImovelId = null;
        _selectedImovelDetails = null;
        ClearImovelForm();
        SetImovelEditMode(true, isNew: true);
    }

    private async void SaveImovelButton_Click(object sender, RoutedEventArgs e)
    {
        ImovelErrorText.Text = string.Empty;

        try
        {
            var request = BuildImovelRequestFromForm();
            ImovelSummary saved;
            if (_selectedImovelId.HasValue)
            {
                saved = await _rentalManagementService.UpdateImovelAsync(new UpdateImovelRequest(_selectedImovelId.Value, request));
            }
            else
            {
                saved = await _rentalManagementService.CreateImovelAsync(request);
            }

            var savedImovelId = saved.Id;
            await SavePendingImovelMediaAsync(savedImovelId);
            ClearImovelForm();
            await LoadImoveisAsync();
            RestoreDataGridSelection(ImoveisGrid, savedImovelId);
        }
        catch (Exception ex)
        {
            ImovelErrorText.Text = GetImovelExceptionMessage(ex);
        }
    }

    private async void ImoveisGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRestoringTabState)
        {
            return;
        }

        if (TryGetItemId(ImoveisGrid.SelectedItem) is not Guid imovelId)
        {
            _selectedImovelId = null;
            _selectedImovelDetails = null;
            _pendingImovelMedia.Clear();
            _imovelImagens = [];
            RefreshImovelMediaGrid();
            _imovelVistorias = [];
            ImovelVistoriasGrid.ItemsSource = _imovelVistorias;
            SetImovelEditMode(true, isNew: true);
            return;
        }

        await LoadSelectedImovelAsync(imovelId);
        SaveActiveTabState();
    }

    private async Task LoadSelectedImovelAsync(Guid imovelId)
    {
        ImovelErrorText.Text = string.Empty;
        var details = await _rentalManagementService.GetImovelAsync(imovelId);
        if (details is null)
        {
            _selectedImovelId = null;
            _selectedImovelDetails = null;
            SetImovelEditMode(true, isNew: true);
            return;
        }

        _selectedImovelId = imovelId;
        _selectedImovelDetails = details;
        _pendingImovelMedia.Clear();
        SetImovelForm(details.Dados);
        ImovelFormTitleText.Text = details.Summary.Endereco;
        SetActiveImovelTabLabel(details.Summary.Endereco);
        await LoadImovelImagensAsync(imovelId);
        await LoadImovelVistoriasAsync(imovelId);
        SetImovelEditMode(false, isNew: false);
    }

    private void EditImovelButton_Click(object sender, RoutedEventArgs e) => SetImovelEditMode(true, isNew: false);

    private void CancelImovelEditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedImovelDetails is not null)
        {
            SetImovelForm(_selectedImovelDetails.Dados);
            ImovelFormTitleText.Text = _selectedImovelDetails.Summary.Endereco;
            SetActiveImovelTabLabel(_selectedImovelDetails.Summary.Endereco);
            SetImovelEditMode(false, isNew: false);
            return;
        }

        ClearImovelForm();
        SetImovelEditMode(true, isNew: true);
    }

    private async void DeactivateImovelButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedImovelId.HasValue || _selectedImovelDetails is null)
        {
            return;
        }

        var confirm = MessageBox.Show(
            this,
            "Remover este imóvel apenas altera o status para inativo. Deseja continuar?",
            "Remover imóvel",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var isCurrentlyInactive = _selectedImovelDetails.Dados.Status == ImovelStatus.Inativo;
            await _rentalManagementService.SetImovelActiveAsync(_selectedImovelId.Value, isCurrentlyInactive);
            var selectedId = _selectedImovelId.Value;
            await LoadImoveisAsync();
            RestoreDataGridSelection(ImoveisGrid, selectedId);
        }
        catch (Exception ex)
        {
            ImovelErrorText.Text = ex.Message;
        }
    }

    private async void SaveImovelVistoriaButton_Click(object sender, RoutedEventArgs e)
    {
        ImovelVistoriaErrorText.Text = string.Empty;
        if (!_selectedImovelId.HasValue)
        {
            ImovelVistoriaErrorText.Text = "Selecione ou salve o imóvel antes de adicionar vistorias.";
            return;
        }

        try
        {
            var tipo = ImovelVistoriaTipoBox.SelectedValue is VistoriaTipo selectedTipo
                ? selectedTipo
                : VistoriaTipo.InicialProprietario;
            var status = ImovelVistoriaStatusBox.SelectedValue is VistoriaStatus selectedStatus
                ? selectedStatus
                : VistoriaStatus.Draft;

            await _rentalManagementService.CreateVistoriaAsync(new CreateVistoriaRequest(
                _selectedImovelId.Value,
                null,
                tipo,
                ToDateOnly(ImovelVistoriaDataBox.SelectedDate) ?? DateOnly.FromDateTime(DateTime.Today),
                ImovelVistoriaResponsavelBox.Text,
                status,
                ImovelVistoriaDescricaoBox.Text,
                ImovelVistoriaObservacoesBox.Text));

            ClearImovelVistoriaForm();
            await LoadImovelVistoriasAsync(_selectedImovelId.Value);
        }
        catch (Exception ex)
        {
            ImovelVistoriaErrorText.Text = ex.Message;
        }
    }


    private void SetActiveImovelTabLabel(string label)
    {
        if (_activeTab?.Page != ShellPage.Imoveis)
        {
            return;
        }

        _activeTab.SelectedImovelName = label;
        RenderTabs();
    }

    private void ClearImovelImagemForm()
    {
        ImovelImagemArquivoBox.Clear();
        ImovelImagemLegendaBox.Clear();
        ImovelImagemOrdemBox.Clear();
        ImovelImagemPublicaBox.IsChecked = false;
        ImovelImagemCapaBox.IsChecked = false;
        ImovelMediaCategoryBox.SelectedValue = ImovelMediaCategory.PropertyPhoto;
        ImovelImagemErrorText.Text = string.Empty;
    }

    private ImovelMediaCategory GetSelectedImovelMediaCategory() =>
        ImovelMediaCategoryBox.SelectedValue is ImovelMediaCategory selectedCategory
            ? selectedCategory
            : ImovelMediaCategory.PropertyPhoto;

    private static string GetImovelMediaCategoryLabel(ImovelMediaCategory category) =>
        category switch
        {
            ImovelMediaCategory.PropertyPhoto => "Foto pública do imóvel",
            ImovelMediaCategory.Document => "Documento",
            ImovelMediaCategory.InspectionPhoto => "Foto de vistoria",
            ImovelMediaCategory.MaintenancePhoto => "Foto de manutenção",
            ImovelMediaCategory.Other => "Foto privada",
            _ => category.ToString()
        };

    private static string? GetImagePreviewPath(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && Path.IsPathRooted(path)
        && File.Exists(path)
        && IsImageFile(path)
            ? path
            : null;

    private static string GetFileKindLabel(string fileName) =>
        IsImageFile(fileName) ? string.Empty : Path.GetExtension(fileName).Trim('.').ToUpperInvariant();

    private static bool IsImageFile(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".webp";

    private void UpdateImovelChaveFieldsVisibility()
    {
        var posse = ImovelChavePosseBox.SelectedValue is ImovelChavePosse selected
            ? selected
            : ImovelChavePosse.NaoCadastrada;

        var hasExternalHolder = posse is ImovelChavePosse.Proprietario
            or ImovelChavePosse.Locatario
            or ImovelChavePosse.Terceiro
            or ImovelChavePosse.Outro;
        var hasRealEstateKey = posse == ImovelChavePosse.Imobiliaria;

        ImovelChaveCodigoPanel.Visibility = hasRealEstateKey ? Visibility.Visible : Visibility.Collapsed;
        ImovelChaveAutorizacaoBox.Visibility = posse == ImovelChavePosse.NaoCadastrada ? Visibility.Collapsed : Visibility.Visible;
        ImovelChaveContatoPanel.Visibility = hasExternalHolder ? Visibility.Visible : Visibility.Collapsed;
        ImovelChaveRetiradaPanel.Visibility = hasExternalHolder ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ClearImovelVistoriaForm()
    {
        ImovelVistoriaTipoBox.SelectedValue = VistoriaTipo.InicialProprietario;
        ImovelVistoriaDataBox.SelectedDate = DateTime.Today;
        ImovelVistoriaResponsavelBox.Clear();
        ImovelVistoriaStatusBox.SelectedValue = VistoriaStatus.Draft;
        ImovelVistoriaDescricaoBox.Clear();
        ImovelVistoriaObservacoesBox.Clear();
        ImovelVistoriaErrorText.Text = string.Empty;
    }

    private Guid ResolveImovelProprietarioId()
    {
        if (ImovelProprietarioBox.SelectedValue is Guid selectedOwnerId)
        {
            return selectedOwnerId;
        }

        var typed = ImovelProprietarioBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(typed))
        {
            return Guid.Empty;
        }

        var owners = _pessoas.Where(x => x.Status == "Ativo");
        var normalizedTyped = NormalizeSearch(typed);
        var exact = owners.FirstOrDefault(owner =>
            string.Equals(NormalizeSearch(owner.Nome), normalizedTyped, StringComparison.OrdinalIgnoreCase));

        if (exact is not null)
        {
            ImovelProprietarioBox.SelectedValue = exact.Id;
            return exact.Id;
        }

        var matches = owners
            .Where(owner => ContainsSearch(typed, owner.Nome, owner.Documento, owner.Telefone))
            .Take(2)
            .ToList();

        if (matches.Count == 1)
        {
            ImovelProprietarioBox.SelectedValue = matches[0].Id;
            return matches[0].Id;
        }

        return Guid.Empty;
    }

    private static int? ParseNullableInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.GetCultureInfo("pt-BR"), out var parsed) ? parsed : null;

    private static string GuessImovelMediaContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

    private static string GetImovelFinalidadeLabel(ImovelFinalidade finalidade) =>
        finalidade switch
        {
            ImovelFinalidade.Locacao => "Locação",
            ImovelFinalidade.Venda => "Venda",
            ImovelFinalidade.Ambos => "Ambos",
            _ => finalidade.ToString()
        };

    private sealed record PendingImovelMedia(
        string StoragePath,
        string? Caption,
        int DisplayOrder,
        ImovelMediaCategory MediaCategory,
        bool IsCover,
        bool IsPublic);

    private sealed record ImovelMediaListItem(
        string FileName,
        string MediaCategory,
        string? Caption,
        bool IsCover,
        bool IsPublic,
        string Source,
        string Status,
        string? PreviewPath,
        string FileKind);
}




