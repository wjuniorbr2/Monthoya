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
                SettingsMenuOption option => ContainsSearch(query, option.Opção, option.Descrição),
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
            ShellPage.Configuracoes => GetSettingsMenuOptions().Cast<object>(),
            _ => []
        };
        _moduleItems = items.ToList();
        ApplyModuleFilter();
    }

    private void ModulePrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeModulePage == ShellPage.Configuracoes)
        {
            OpenSelectedSettingsOption();
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

    private void OpenSelectedSettingsOption()
    {
        if (ModuleGrid.SelectedItem is not SettingsMenuOption option)
        {
            MessageBox.Show(this, "Selecione uma opção de configuração.", "Configurações", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        switch (option.Id)
        {
            case "ai":
                ShowAiSettingsDialog();
                break;
            case "indexes":
                MessageBox.Show(this, "Os índices de reajuste serão abertos em tela própria na próxima etapa.", "Índices de reajuste", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
            default:
                MessageBox.Show(this, "Opção de configuração ainda não implementada.", "Configurações", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
        }
    }

    private static IReadOnlyList<SettingsMenuOption> GetSettingsMenuOptions() =>
    [
        new("indexes", "Índices de reajuste", "IGP-M, IPCA, INPC e índices personalizados usados nos contratos."),
        new("ai", "IA / OCR inteligente", "Configuração da chave Gemini para leitura inteligente de documentos digitalizados.")
    ];

    private ModulePageState CaptureModulePageState() =>
        new(ModuleSearchBox.Text, TryGetItemId(ModuleGrid.SelectedItem));

    private Task RestoreModulePageStateAsync(ModulePageState state)
    {
        ModuleSearchBox.Text = state.SearchText;
        ApplyModuleFilter();
        RestoreDataGridSelection(ModuleGrid, state.SelectedItemId);
        return Task.CompletedTask;
    }

    private sealed record SettingsMenuOption(string Id, string Opção, string Descrição);

    private sealed record ModulePageState(string SearchText, Guid? SelectedItemId) : IShellPageState
    {
        public static ModulePageState Default { get; } = new("", null);
    }
}
