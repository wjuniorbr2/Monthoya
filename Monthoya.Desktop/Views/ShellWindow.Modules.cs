using System.Windows;
using System.Windows.Controls;
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
        ModuleGrid.ItemsSource = _moduleItems
            .Where(item => item switch
            {
                LocacaoSummary locacao => ContainsSearch(query, locacao.Imovel, locacao.Proprietario, locacao.Locatario, locacao.Fiadores, locacao.Status),
                _ => ContainsSearch(query, item.ToString())
            })
            .ToList();
    }

    private async Task LoadGenericModuleAsync(ShellPage page)
    {
        _activeModulePage = page;
        var definition = GetModuleDefinition(page);
        ModuleTitleText.Text = definition.Title;
        ModuleSubtitleText.Text = definition.Subtitle;
        ModuleNoticeText.Text = definition.Notice;
        ModulePrimaryActionButton.Content = definition.ActionText;
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
            ShellPage.Configuracoes => (await _rentalManagementService.GetIndicesReajusteAsync()).Cast<object>(),
            _ => []
        };
        _moduleItems = items.ToList();
        ApplyModuleFilter();
    }

    private void ModulePrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeModulePage == ShellPage.Configuracoes)
        {
            ShowAiSettingsDialog();
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
