using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private void ModuleSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyModuleFilter();
        SaveActiveTabState();
    }

    private void ApplyModuleFilter()
    {
        var query = ModuleSearchBox.Text;
        var filteredItems = _moduleItems
            .Where(item => item switch
            {
                LocacaoSummary locacao => ContainsSearch(
                    query,
                    locacao.Codigo,
                    locacao.Status,
                    locacao.TipoLocacao,
                    locacao.ImovelResumo,
                    locacao.LocatarioPrincipalNome,
                    locacao.ProprietarioPrincipalNome,
                    locacao.AlertasTexto),
                _ => ContainsSearch(query, item.ToString())
            })
            .ToList();

        ModuleGrid.ItemsSource = filteredItems;
        ModuleOpenButton.IsEnabled = _activeModulePage == ShellPage.Locacoes && ModuleGrid.SelectedItem is LocacaoSummary;

        if (_activeModulePage == ShellPage.Locacoes && _moduleItems.Count > 0 && filteredItems.Count == 0)
        {
            SetModuleNotice("Nenhuma locaÃ§Ã£o encontrada para a pesquisa atual.");
        }
        else if (_activeModulePage == ShellPage.Locacoes && _moduleItems.Count > 0)
        {
            SetModuleNotice(string.Empty);
        }
    }

    private async Task LoadGenericModuleAsync(ShellPage page)
    {
        _activeModulePage = page;
        var definition = GetModuleDefinition(page);
        ModuleTitleText.Text = definition.Title;
        ModuleSubtitleText.Text = definition.Subtitle;
        SetModuleNotice(page == ShellPage.Locacoes ? string.Empty : definition.Notice);
        ModulePrimaryActionButton.Content = definition.ActionText;
        ModuleOpenButton.Visibility = Visibility.Collapsed;
        ModuleOpenButton.IsEnabled = false;
        ClearModuleDetails();
        ConfigureModuleGrid(page);

        if (page == ShellPage.Configuracoes)
        {
            ShowSettingsMenuButtons();
            _moduleItems = [];
            ModuleGrid.ItemsSource = Array.Empty<object>();
            return;
        }

        HideSettingsMenuButtons();
        try
        {
            IEnumerable<object> items = page switch
            {
                ShellPage.Locacoes => (await _rentalManagementService.GetLocacoesAsync()).Cast<object>(),
                ShellPage.Financeiro => (await _rentalManagementService.GetLancamentosFinanceirosAsync()).Cast<object>(),
                ShellPage.Boletos => (await _rentalManagementService.GetBoletosAsync()).Cast<object>(),
                ShellPage.NotasFiscais => (await _rentalManagementService.GetNotasFiscaisAsync()).Cast<object>(),
                ShellPage.Documentos => (await _rentalManagementService.GetPessoaDocumentosAsync()).Cast<object>(),
                ShellPage.Relatorios => (await _rentalManagementService.GetImoveisAsync()).Cast<object>(),
                ShellPage.Dimob => (await _rentalManagementService.GetDimobDeclaracoesAsync()).Cast<object>(),
                ShellPage.Manutencoes => (await _rentalManagementService.GetManutencoesAsync()).Cast<object>(),
                ShellPage.Vistorias => (await _rentalManagementService.GetVistoriasAsync()).Cast<object>(),
                _ => []
            };

            _moduleItems = items.ToList();
            SetModuleNotice(page == ShellPage.Locacoes && _moduleItems.Count == 0
                ? "Nenhuma locaÃ§Ã£o cadastrada."
                : page == ShellPage.Locacoes ? string.Empty : definition.Notice);
            ApplyModuleFilter();
        }
        catch (Exception ex)
        {
            _moduleItems = [];
            ModuleGrid.ItemsSource = Array.Empty<object>();
            SetModuleNotice($"NÃ£o foi possÃ­vel carregar este mÃ³dulo. {ex.Message}");
        }
    }

    private async void ModulePrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeModulePage == ShellPage.Configuracoes)
        {
            MessageBox.Show(this, "Escolha uma das opÃ§Ãµes de configuraÃ§Ã£o abaixo.", "ConfiguraÃ§Ãµes", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_activeModulePage == ShellPage.Locacoes)
        {
            await ShowCreateLocacaoInlineAsync();
            return;
        }

        var message = _activeModulePage switch
        {
            ShellPage.Boletos => "IntegraÃ§Ã£o bancÃ¡ria ainda nÃ£o configurada.",
            ShellPage.NotasFiscais => "IntegraÃ§Ã£o automÃ¡tica com NFS-e ainda nÃ£o configurada. Use o fluxo manual/semi-manual.",
            ShellPage.Dimob => "ExportaÃ§Ã£o oficial DIMOB pendente de confirmaÃ§Ã£o do layout vigente da Receita Federal.",
            ShellPage.Documentos => "Modelos iniciais criados como pendentes de revisÃ£o. A redaÃ§Ã£o final deve ser confirmada com o cliente.",
            _ => "CRUD completo deste mÃ³dulo serÃ¡ implementado em uma prÃ³xima etapa."
        };

        MessageBox.Show(this, message, "Monthoya", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void ModuleReloadButton_Click(object sender, RoutedEventArgs e) =>
        await LoadGenericModuleAsync(_activeModulePage);

    private void ModuleGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ModuleOpenButton.IsEnabled = _activeModulePage == ShellPage.Locacoes && ModuleGrid.SelectedItem is LocacaoSummary;
        if (_activeModulePage == ShellPage.Locacoes)
        {
            ShowLocacaoSelectionDetails();
        }
    }

    private async void ModuleOpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeModulePage != ShellPage.Locacoes || ModuleGrid.SelectedItem is not LocacaoSummary locacao)
        {
            return;
        }

        await Task.CompletedTask;
        ShowLocacaoDetails(locacao);
        ModuleDetailsBorder.Focus();
        if (ModuleDetailsBorder.Visibility == Visibility.Visible)
        {
            return;
        }

        try
        {
            var details = await _rentalManagementService.GetLocacaoAsync(locacao.Id);
            var summary = details.Summary;
            MessageBox.Show(
                this,
                $"LocaÃ§Ã£o: {summary.Codigo ?? "-"}\nStatus: {summary.Status}\nImÃ³vel: {summary.ImovelResumo}\nLocatÃ¡rio: {summary.LocatarioPrincipalNome}\nProprietÃ¡rio: {summary.ProprietarioPrincipalNome}\n\nO formulÃ¡rio de ediÃ§Ã£o completo serÃ¡ implementado em uma prÃ³xima etapa.",
                "LocaÃ§Ã£o",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"NÃ£o foi possÃ­vel abrir a locaÃ§Ã£o. {ex.Message}", "LocaÃ§Ã£o", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ConfigureModuleGrid(ShellPage page)
    {
        ModuleGrid.Columns.Clear();
        ModuleGrid.AutoGenerateColumns = page != ShellPage.Locacoes;

        if (page != ShellPage.Locacoes)
        {
            return;
        }

        AddModuleTextColumn("CÃ³digo", "Codigo", 0.7);
        AddModuleTextColumn("Status", "Status", 1);
        AddModuleTextColumn("ImÃ³vel", "ImovelResumo", 1.8);
        AddModuleTextColumn("LocatÃ¡rio", "LocatarioPrincipalNome", 1.3);
        AddModuleTextColumn("ProprietÃ¡rio", "ProprietarioPrincipalNome", 1.3);
        AddModuleTextColumn("Aluguel", "ValorAluguelAtual", 0.9, "R$ {0:N2}");
        AddModuleTextColumn("Venc.", "DiaVencimentoLocatario", 0.55);
        AddModuleTextColumn("InÃ­cio", "DataInicioLocacao", 0.8, "dd/MM/yyyy");
        AddModuleTextColumn("Fim previsto", "DataFimPrevista", 0.9, "dd/MM/yyyy");
        AddModuleTextColumn("Alertas", "AlertasTexto", 1.4);
    }

    private void AddModuleTextColumn(string header, string bindingPath, double width, string? stringFormat = null)
    {
        ModuleGrid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = new Binding(bindingPath) { StringFormat = stringFormat },
            Width = new DataGridLength(width, DataGridLengthUnitType.Star)
        });
    }

    private void ClearModuleDetails()
    {
        ModuleDetailsHost.Content = null;
        ModuleDetailsBorder.Visibility = Visibility.Collapsed;
    }

    private void SetModuleNotice(string? message)
    {
        ModuleNoticeText.Text = message ?? string.Empty;
        ModuleNoticeBorder.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
    }

    private ModulePageState CaptureModulePageState() =>
        new(ModuleSearchBox.Text, TryGetItemId(ModuleGrid.SelectedItem));

    private Task RestoreModulePageStateAsync(ModulePageState state)
    {
        ModuleSearchBox.Text = state.SearchText;
        ApplyModuleFilter();
        RestoreDataGridSelection(ModuleGrid, state.SelectedItemId);
        return Task.CompletedTask;
    }

    private sealed record ModulePageState(string SearchText, Guid? SelectedItemId) : IShellPageState
    {
        public static ModulePageState Default { get; } = new("", null);
    }
}

