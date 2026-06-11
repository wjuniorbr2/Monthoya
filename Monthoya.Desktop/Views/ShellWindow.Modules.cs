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
            SetModuleNotice("Nenhuma locação encontrada para a pesquisa atual.");
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
        ModuleSubtitleText.Visibility = page == ShellPage.Locacoes ? Visibility.Collapsed : Visibility.Visible;
        ModulePanel.Margin = page == ShellPage.Locacoes ? new Thickness(0, -10, 0, 0) : new Thickness(0);
        SetModuleNotice(page == ShellPage.Locacoes ? string.Empty : definition.Notice);
        ModulePrimaryActionButton.Content = definition.ActionText;
        ModulePrimaryActionButton.Style = (Style)FindResource(page == ShellPage.Locacoes ? "PrimaryButtonSmall" : "SecondaryButton");
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
                ? "Nenhuma locação cadastrada."
                : page == ShellPage.Locacoes ? string.Empty : definition.Notice);
            ApplyModuleFilter();
        }
        catch (Exception ex)
        {
            _moduleItems = [];
            ModuleGrid.ItemsSource = Array.Empty<object>();
            SetModuleNotice($"Não foi possível carregar este módulo. {ex.Message}");
        }
    }

    private async void ModulePrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeModulePage == ShellPage.Configuracoes)
        {
            MessageBox.Show(this, "Escolha uma das opções de configuração abaixo.", "Configurações", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_activeModulePage == ShellPage.Locacoes)
        {
            await ShowCreateLocacaoInlineAsync();
            return;
        }

        var message = _activeModulePage switch
        {
            ShellPage.Boletos => "Integração bancária ainda não configurada.",
            ShellPage.NotasFiscais => "Integração automática com NFS-e ainda não configurada. Use o fluxo manual/semi-manual.",
            ShellPage.Dimob => "Exportação oficial DIMOB pendente de confirmação do layout vigente da Receita Federal.",
            ShellPage.Documentos => "Modelos iniciais criados como pendentes de revisão. A redação final deve ser confirmada com o cliente.",
            _ => "CRUD completo deste módulo será implementado em uma próxima etapa."
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
                $"Locação: {summary.Codigo ?? "-"}\nStatus: {summary.Status}\nImóvel: {summary.ImovelResumo}\nLocatário: {summary.LocatarioPrincipalNome}\nProprietário: {summary.ProprietarioPrincipalNome}\n\nO formulário de edição completo será implementado em uma próxima etapa.",
                "Locação",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Não foi possível abrir a locação. {ex.Message}", "Locação", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        AddModuleTextColumn("Código", "Codigo", 0.7);
        AddModuleTextColumn("Status", "Status", 1);
        AddModuleTextColumn("Imóvel", "ImovelResumo", 1.8);
        AddModuleTextColumn("Locatário", "LocatarioPrincipalNome", 1.3);
        AddModuleTextColumn("Proprietário", "ProprietarioPrincipalNome", 1.3);
        AddModuleTextColumn("Aluguel", "ValorAluguelAtual", 0.9, "R$ {0:N2}");
        AddModuleTextColumn("Venc.", "DiaVencimentoLocatario", 0.55);
        AddModuleTextColumn("Início", "DataInicioLocacao", 0.8, "dd/MM/yyyy");
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

