using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Monthoya.Core.Entities;
using Monthoya.Core.Security;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow : Window
{
    private readonly List<ShellTab> _tabs = [];
    private readonly AuthenticatedUser _currentUser;
    private readonly IUserService _userService;
    private readonly IDashboardService _dashboardService;
    private readonly IRentalManagementService _rentalManagementService;
    private Guid? _editingUserId;
    private ShellTab? _activeTab;
    private ShellPage _activeModulePage;
    private IReadOnlyList<PessoaSummary> _pessoas = [];
    private IReadOnlyList<ImovelSummary> _imoveis = [];
    private IReadOnlyList<object> _moduleItems = [];

    public ShellWindow(
        AuthenticatedUser currentUser,
        IUserService userService,
        IDashboardService dashboardService,
        IRentalManagementService rentalManagementService)
    {
        InitializeComponent();
        _currentUser = currentUser;
        _userService = userService;
        _dashboardService = dashboardService;
        _rentalManagementService = rentalManagementService;

        CurrentUserText.Text = currentUser.DisplayName;
        CurrentRoleText.Text = GetRoleLabel(currentUser.Role);
        UsersNavButton.Visibility = RolePermissions.CanManageUsers(currentUser.Role, currentUser.Access) ? Visibility.Visible : Visibility.Collapsed;
        DiagnosticsNavButton.Visibility = RolePermissions.CanAccessDiagnostics(currentUser.Role) ? Visibility.Visible : Visibility.Collapsed;
        UserRoleBox.ItemsSource = UserRoleOptions;
        UserRoleBox.SelectedValue = UserRole.Usuario;
        PessoaTipoBox.ItemsSource = TipoPessoaOptions;
        PessoaTipoBox.SelectedValuePath = "Tipo";
        PessoaTipoBox.DisplayMemberPath = "Label";
        PessoaTipoBox.SelectedValue = TipoPessoa.Fisica;
        PessoaDocumentoTipoBox.ItemsSource = PessoaDocumentoTipoOptions;
        PessoaDocumentoTipoBox.SelectedValuePath = "Tipo";
        PessoaDocumentoTipoBox.DisplayMemberPath = "Label";
        PessoaDocumentoTipoBox.SelectedValue = "cpf";
        ImovelFinalidadeBox.ItemsSource = ImovelFinalidadeOptions;
        ImovelFinalidadeBox.SelectedValuePath = "Finalidade";
        ImovelFinalidadeBox.DisplayMemberPath = "Label";
        ImovelFinalidadeBox.SelectedValue = ImovelFinalidade.Locacao;
        SetAccessCheckboxes(RolePermissions.DefaultUserAccess);
        UpdateAccessControlState();
        DiagnosticsText.Text = $"Login: {currentUser.LoginName}{Environment.NewLine}E-mail: {currentUser.Email}{Environment.NewLine}Perfil: {GetRoleLabel(currentUser.Role)}{Environment.NewLine}Acessos: {GetAccessLabel(RolePermissions.GetEffectiveAccess(currentUser.Role, currentUser.Access))}{Environment.NewLine}Banco: configurado via secrets/appsettings";

        AddShellTab(ShellPage.Dashboard, "Tela Inicial");
        Loaded += async (_, _) => await ShowPageAsync(ShellPage.Dashboard, true);
    }

    public bool IsLogoutRequested { get; private set; }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsInsideTitleBarButton(e.OriginalSource as DependencyObject))
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleWindowMaximized();
            return;
        }

        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private static bool IsInsideTitleBarButton(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is Button)
            {
                return true;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private void TitleBarMinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void TitleBarMaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleWindowMaximized();
    }

    private void TitleBarCloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ShellWindow_StateChanged(object? sender, EventArgs e)
    {
        UpdateMaximizeButtonIcon();
    }

    private void ToggleWindowMaximized()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        UpdateMaximizeButtonIcon();
    }

    private void UpdateMaximizeButtonIcon()
    {
        if (TitleBarMaximizeButton is not null)
        {
            TitleBarMaximizeButton.Content = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        }
    }

    private async Task LoadDashboardAsync()
    {
        try
        {
            var summary = await _dashboardService.GetHomeSummaryAsync();
            TotalPropertiesText.Text = summary.TotalProperties.ToString(CultureInfo.CurrentCulture);
            AvailableRentalsText.Text = summary.AvailableRentals.ToString(CultureInfo.CurrentCulture);
            ActiveContractsText.Text = summary.ActiveContracts.ToString(CultureInfo.CurrentCulture);
            PendingRentText.Text = summary.PendingRentAmount.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
            MapPropertiesList.ItemsSource = summary.AvailableRentalProperties;
            DashboardStatusText.Text = summary.AvailableRentalProperties.Count == 0
                ? "Nenhum imóvel disponível para locação com coordenadas cadastrado ainda."
                : $"{summary.AvailableRentalProperties.Count} imóvel(is) disponível(is) com localização.";

            await LoadMapAsync(summary.AvailableRentalProperties);
        }
        catch (Exception ex)
        {
            DashboardStatusText.Text = $"Não foi possível carregar a tela inicial: {ex.Message}";
            ShowMapFallback("Mapa indisponível no momento.");
        }
    }

    private async Task LoadMapAsync(IReadOnlyList<PropertyMapItem> properties)
    {
        try
        {
            await PropertyMap.EnsureCoreWebView2Async();
            PropertyMap.Visibility = Visibility.Visible;
            MapFallback.Visibility = Visibility.Collapsed;
            PropertyMap.NavigateToString(BuildMapHtml(properties));
        }
        catch
        {
            ShowMapFallback("Não foi possível iniciar o WebView2. Instale o runtime do Microsoft Edge WebView2 para ver o mapa.");
        }
    }

    private static string BuildMapHtml(IReadOnlyList<PropertyMapItem> properties)
    {
        var json = JsonSerializer.Serialize(properties.Select(x => new
        {
            x.Code,
            x.AddressLine,
            x.City,
            x.State,
            RentalPrice = x.RentalPrice?.ToString("C", CultureInfo.GetCultureInfo("pt-BR")),
            Latitude = x.Latitude,
            Longitude = x.Longitude
        }));

        return MapHtmlTemplate.Replace("__PROPERTIES__", json, StringComparison.Ordinal);
    }

    private void ShowMapFallback(string message)
    {
        PropertyMap.Visibility = Visibility.Collapsed;
        MapFallback.Visibility = Visibility.Visible;
        MapFallbackText.Text = message;
    }

    private async Task LoadUsersAsync()
    {
        UsersGrid.ItemsSource = await _userService.GetUsersAsync();
    }

    private void ShowDashboard()
    {
        SetActiveNavigation(DashboardNavButton);
        DashboardPanel.Visibility = Visibility.Visible;
        UsersPanel.Visibility = Visibility.Collapsed;
        DiagnosticsPanel.Visibility = Visibility.Collapsed;
    }

    private async void DashboardNavButton_Click(object sender, RoutedEventArgs e)
    {
        await UpdateActiveTabAsync(ShellPage.Dashboard, "Tela Inicial", true);
    }

    private async void UsersNavButton_Click(object sender, RoutedEventArgs e)
    {
        if (!RolePermissions.CanManageUsers(_currentUser.Role, _currentUser.Access))
        {
            return;
        }

        await UpdateActiveTabAsync(ShellPage.Users, "Usuários", true);
    }

    private void DiagnosticsNavButton_Click(object sender, RoutedEventArgs e)
    {
        if (!RolePermissions.CanAccessDiagnostics(_currentUser.Role))
        {
            return;
        }

        _ = UpdateActiveTabAsync(ShellPage.Diagnostics, "Diagnósticos", false);
    }

    private void SetActiveNavigation(Button activeButton)
    {
        DashboardNavButton.Style = (Style)FindResource("NavButton");
        UsersNavButton.Style = (Style)FindResource("NavButton");
        PessoasNavButton.Style = (Style)FindResource("NavButton");
        ImoveisNavButton.Style = (Style)FindResource("NavButton");
        LocacoesNavButton.Style = (Style)FindResource("NavButton");
        FinanceiroNavButton.Style = (Style)FindResource("NavButton");
        BoletosNavButton.Style = (Style)FindResource("NavButton");
        NotasFiscaisNavButton.Style = (Style)FindResource("NavButton");
        DocumentosNavButton.Style = (Style)FindResource("NavButton");
        RelatoriosNavButton.Style = (Style)FindResource("NavButton");
        DimobNavButton.Style = (Style)FindResource("NavButton");
        ManutencoesNavButton.Style = (Style)FindResource("NavButton");
        VistoriasNavButton.Style = (Style)FindResource("NavButton");
        ConfiguracoesNavButton.Style = (Style)FindResource("NavButton");
        DiagnosticsNavButton.Style = (Style)FindResource("NavButton");
        activeButton.Style = (Style)FindResource("SelectedNavButton");
    }

    private async void PessoasNavButton_Click(object sender, RoutedEventArgs e) =>
        await UpdateActiveTabAsync(ShellPage.Pessoas, "Pessoas", true);

    private async void ImoveisNavButton_Click(object sender, RoutedEventArgs e) =>
        await UpdateActiveTabAsync(ShellPage.Imoveis, "Imóveis", true);

    private async void LocacoesNavButton_Click(object sender, RoutedEventArgs e) =>
        await UpdateActiveTabAsync(ShellPage.Locacoes, "Locações", true);

    private async void FinanceiroNavButton_Click(object sender, RoutedEventArgs e) =>
        await UpdateActiveTabAsync(ShellPage.Financeiro, "Financeiro", true);

    private async void BoletosNavButton_Click(object sender, RoutedEventArgs e) =>
        await UpdateActiveTabAsync(ShellPage.Boletos, "Boletos", true);

    private async void NotasFiscaisNavButton_Click(object sender, RoutedEventArgs e) =>
        await UpdateActiveTabAsync(ShellPage.NotasFiscais, "Notas Fiscais", true);

    private async void DocumentosNavButton_Click(object sender, RoutedEventArgs e) =>
        await UpdateActiveTabAsync(ShellPage.Documentos, "Documentos", true);

    private async void RelatoriosNavButton_Click(object sender, RoutedEventArgs e) =>
        await UpdateActiveTabAsync(ShellPage.Relatorios, "Relatórios", true);

    private async void DimobNavButton_Click(object sender, RoutedEventArgs e) =>
        await UpdateActiveTabAsync(ShellPage.Dimob, "DIMOB", true);

    private async void ManutencoesNavButton_Click(object sender, RoutedEventArgs e) =>
        await UpdateActiveTabAsync(ShellPage.Manutencoes, "Manutenções", true);

    private async void VistoriasNavButton_Click(object sender, RoutedEventArgs e) =>
        await UpdateActiveTabAsync(ShellPage.Vistorias, "Vistorias", true);

    private async void ConfiguracoesNavButton_Click(object sender, RoutedEventArgs e) =>
        await UpdateActiveTabAsync(ShellPage.Configuracoes, "Configurações", true);

    private async void RefreshDashboardButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadDashboardAsync();
    }

    private async void AddTabButton_Click(object sender, RoutedEventArgs e)
    {
        AddShellTab(ShellPage.Dashboard, "Tela Inicial");
        await ShowPageAsync(ShellPage.Dashboard, true);
    }

    private void AddShellTab(ShellPage page, string title)
    {
        var tab = new ShellTab(Guid.NewGuid(), title, page);
        _tabs.Add(tab);
        _activeTab = tab;
        RenderTabs();
    }

    private async Task UpdateActiveTabAsync(ShellPage page, string title, bool loadData)
    {
        if (_activeTab is null)
        {
            AddShellTab(page, title);
        }
        else
        {
            _activeTab.Page = page;
            _activeTab.Title = title;
            RenderTabs();
        }

        await ShowPageAsync(page, loadData);
    }

    private async Task SelectTabAsync(ShellTab tab)
    {
        _activeTab = tab;
        RenderTabs();
        await ShowPageAsync(tab.Page, true);
    }

    private void RenderTabs()
    {
        TabsPanel.Children.Clear();

        foreach (var tab in _tabs)
        {
            var tabContent = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            tabContent.Children.Add(new TextBlock
            {
                Text = GetShellPageIcon(tab.Page),
                Style = (Style)FindResource("ShellTabIconText"),
                FontFamily = tab.Page == ShellPage.Financeiro
                    ? new FontFamily("Segoe UI Black")
                    : new FontFamily("Segoe MDL2 Assets"),
                FontSize = tab.Page == ShellPage.Financeiro ? 14 : 13
            });

            tabContent.Children.Add(new TextBlock
            {
                Text = tab.Title,
                VerticalAlignment = VerticalAlignment.Center
            });

            var closeButton = new Button
            {
                Content = "×",
                Width = 20,
                Height = 20,
                Padding = new Thickness(0),
                Margin = new Thickness(10, 0, -4, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = Brushes.Gray,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand
            };

            closeButton.Click += async (sender, e) =>
            {
                e.Handled = true;
                await CloseTabAsync(tab);
            };

            tabContent.Children.Add(closeButton);

            var tabButton = new Button
            {
                Content = tabContent,
                Margin = tab == _activeTab
                    ? new Thickness(0, 0, 4, -1)
                    : new Thickness(0, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
                Style = (Style)FindResource(tab == _activeTab ? "ShellTabButtonActive" : "ShellTabButton")
            };

            tabButton.Click += async (_, _) => await SelectTabAsync(tab);
            TabsPanel.Children.Add(tabButton);
        }
    }

    private static string GetShellPageIcon(ShellPage page) =>
        page switch
        {
            ShellPage.Dashboard => "\uE80F",
            ShellPage.Users => "\uE77B",
            ShellPage.Pessoas => "\uE716",
            ShellPage.Imoveis => "\uE80F",
            ShellPage.Locacoes => "\uE8A1",
            ShellPage.Financeiro => "$",
            ShellPage.Boletos => "\uE8C7",
            ShellPage.NotasFiscais => "\uE9D9",
            ShellPage.Documentos => "\uE8A5",
            ShellPage.Relatorios => "\uE9D2",
            ShellPage.Dimob => "\uE9F9",
            ShellPage.Manutencoes => "\uE90F",
            ShellPage.Vistorias => "\uE721",
            ShellPage.Configuracoes => "\uE713",
            ShellPage.Diagnostics => "\uE950",
            _ => "\uE80F"
        };

    private static DateOnly? ToDateOnly(DateTime? value) =>
        value.HasValue ? DateOnly.FromDateTime(value.Value) : null;

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var culture = CultureInfo.GetCultureInfo("pt-BR");
        return decimal.TryParse(value, NumberStyles.Number | NumberStyles.Currency, culture, out var parsed)
            || decimal.TryParse(value, NumberStyles.Number | NumberStyles.Currency, CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : null;
    }

    private static bool ContainsSearch(string? query, params string?[] values)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var normalizedQuery = NormalizeSearch(query);
        return values.Any(value => NormalizeSearch(value).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeSearch(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace(".", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace("/", string.Empty, StringComparison.Ordinal)
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Trim();

    private async Task CloseTabAsync(ShellTab tab)
    {
        if (_tabs.Count == 1)
        {
            await UpdateActiveTabAsync(ShellPage.Dashboard, "Tela Inicial", true);
            return;
        }

        var closedTabIndex = _tabs.IndexOf(tab);
        _tabs.Remove(tab);

        if (_activeTab == tab)
        {
            var nextTabIndex = Math.Clamp(closedTabIndex, 0, _tabs.Count - 1);
            _activeTab = _tabs[nextTabIndex];
            RenderTabs();
            await ShowPageAsync(_activeTab.Page, true);
            return;
        }

        RenderTabs();
    }

    private async Task ShowPageAsync(ShellPage page, bool loadData)
    {
        DashboardPanel.Visibility = page == ShellPage.Dashboard ? Visibility.Visible : Visibility.Collapsed;
        UsersPanel.Visibility = page == ShellPage.Users ? Visibility.Visible : Visibility.Collapsed;
        PessoasPanel.Visibility = page == ShellPage.Pessoas ? Visibility.Visible : Visibility.Collapsed;
        ImoveisPanel.Visibility = page == ShellPage.Imoveis ? Visibility.Visible : Visibility.Collapsed;
        ModulePanel.Visibility = IsGenericModulePage(page) ? Visibility.Visible : Visibility.Collapsed;
        DiagnosticsPanel.Visibility = page == ShellPage.Diagnostics ? Visibility.Visible : Visibility.Collapsed;

        SetActiveNavigation(page switch
        {
            ShellPage.Users => UsersNavButton,
            ShellPage.Pessoas => PessoasNavButton,
            ShellPage.Imoveis => ImoveisNavButton,
            ShellPage.Locacoes => LocacoesNavButton,
            ShellPage.Financeiro => FinanceiroNavButton,
            ShellPage.Boletos => BoletosNavButton,
            ShellPage.NotasFiscais => NotasFiscaisNavButton,
            ShellPage.Documentos => DocumentosNavButton,
            ShellPage.Relatorios => RelatoriosNavButton,
            ShellPage.Dimob => DimobNavButton,
            ShellPage.Manutencoes => ManutencoesNavButton,
            ShellPage.Vistorias => VistoriasNavButton,
            ShellPage.Configuracoes => ConfiguracoesNavButton,
            ShellPage.Diagnostics => DiagnosticsNavButton,
            _ => DashboardNavButton
        });

        if (!loadData)
        {
            return;
        }

        if (page == ShellPage.Dashboard)
        {
            await LoadDashboardAsync();
        }
        else if (page == ShellPage.Users)
        {
            await LoadUsersAsync();
        }
        else if (page == ShellPage.Pessoas)
        {
            await LoadPessoasAsync();
        }
        else if (page == ShellPage.Imoveis)
        {
            await LoadImoveisAsync();
        }
        else if (IsGenericModulePage(page))
        {
            await LoadGenericModuleAsync(page);
        }
    }

    private async Task LoadPessoasAsync()
    {
        _pessoas = await _rentalManagementService.GetPessoasAsync();
        ApplyPessoasFilter();
        PessoaDocumentoPessoaBox.ItemsSource = _pessoas;

        var proprietarios = _pessoas
            .Where(x => x.Roles.Contains("Proprietário", StringComparison.OrdinalIgnoreCase))
            .ToList();
        ImovelProprietarioBox.ItemsSource = proprietarios;
    }

    private async Task LoadImoveisAsync()
    {
        _imoveis = await _rentalManagementService.GetImoveisAsync();
        ApplyImoveisFilter();

        _pessoas = await _rentalManagementService.GetPessoasAsync();
        var proprietarios = _pessoas
            .Where(x => x.Roles.Contains("Proprietário", StringComparison.OrdinalIgnoreCase))
            .ToList();
        ImovelProprietarioBox.ItemsSource = proprietarios;
        PessoaDocumentoPessoaBox.ItemsSource = _pessoas;
    }

    private async void ReloadPessoasButton_Click(object sender, RoutedEventArgs e) => await LoadPessoasAsync();

    private async void ReloadImoveisButton_Click(object sender, RoutedEventArgs e) => await LoadImoveisAsync();

    private void PessoasSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyPessoasFilter();

    private void ImoveisSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyImoveisFilter();

    private void ModuleSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyModuleFilter();

    private void ApplyPessoasFilter()
    {
        var query = PessoasSearchBox.Text;
        PessoasGrid.ItemsSource = _pessoas
            .Where(x => ContainsSearch(query, x.Nome, x.Documento, x.Roles, x.Telefone, x.Email))
            .ToList();
    }

    private void ApplyImoveisFilter()
    {
        var query = ImoveisSearchBox.Text;
        ImoveisGrid.ItemsSource = _imoveis
            .Where(x => ContainsSearch(query, x.Endereco, x.Bairro, x.Proprietario, x.Finalidade, x.Status))
            .ToList();
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

    private async void SavePessoaButton_Click(object sender, RoutedEventArgs e)
    {
        PessoaErrorText.Text = string.Empty;

        try
        {
            var roles = new List<PessoaRoleTipo>();
            if (PessoaProprietarioBox.IsChecked == true) roles.Add(PessoaRoleTipo.Proprietario);
            if (PessoaLocatarioBox.IsChecked == true) roles.Add(PessoaRoleTipo.Locatario);
            if (PessoaFiadorBox.IsChecked == true) roles.Add(PessoaRoleTipo.Fiador);

            var tipo = PessoaTipoBox.SelectedValue is TipoPessoa selectedTipo ? selectedTipo : TipoPessoa.Fisica;
            await _rentalManagementService.CreatePessoaAsync(new CreatePessoaRequest(
                TipoPessoa: tipo,
                NomeDisplay: PessoaNomeBox.Text,
                Telefone: PessoaTelefoneBox.Text,
                Email: PessoaEmailBox.Text,
                Documento: PessoaDocumentoBox.Text,
                Roles: roles.ToArray(),
                Observacoes: PessoaObservacoesBox.Text,
                Endereco: tipo == TipoPessoa.Fisica ? PessoaEnderecoBox.Text : PessoaJuridicaEnderecoEmpresaBox.Text,
                EstadoCivil: PessoaEstadoCivilBox.Text,
                Nacionalidade: PessoaNacionalidadeBox.Text,
                DataNascimento: ToDateOnly(PessoaDataNascimentoBox.SelectedDate),
                Rg: PessoaRgBox.Text,
                Profissao: PessoaProfissaoBox.Text,
                OndeTrabalha: PessoaOndeTrabalhaBox.Text,
                EnderecoTrabalho: PessoaEnderecoTrabalhoBox.Text,
                NomeEmpresaTrabalho: PessoaNomeEmpresaTrabalhoBox.Text,
                TelefoneEmpresaTrabalho: PessoaTelefoneEmpresaTrabalhoBox.Text,
                DadosBancarios: PessoaDadosBancariosBox.Text,
                ConjugeNome: PessoaConjugeNomeBox.Text,
                ConjugeRg: PessoaConjugeRgBox.Text,
                ConjugeCpf: PessoaConjugeCpfBox.Text,
                ConjugeDataNascimento: ToDateOnly(PessoaConjugeDataNascimentoBox.SelectedDate),
                ConjugeProfissao: PessoaConjugeProfissaoBox.Text,
                ConjugeNacionalidade: PessoaConjugeNacionalidadeBox.Text,
                ConjugeTelefone: PessoaConjugeTelefoneBox.Text,
                ResponsavelNome: PessoaResponsavelNomeBox.Text,
                ResponsavelEndereco: PessoaResponsavelEnderecoBox.Text,
                ResponsavelEstadoCivil: PessoaResponsavelEstadoCivilBox.Text,
                ResponsavelNacionalidade: PessoaResponsavelNacionalidadeBox.Text,
                ResponsavelDataNascimento: ToDateOnly(PessoaResponsavelDataNascimentoBox.SelectedDate),
                ResponsavelTelefone: PessoaResponsavelTelefoneBox.Text,
                ResponsavelEmail: PessoaResponsavelEmailBox.Text,
                ResponsavelRg: PessoaResponsavelRgBox.Text,
                ResponsavelCpf: PessoaResponsavelCpfBox.Text,
                ResponsavelProfissao: PessoaResponsavelProfissaoBox.Text,
                ResponsavelOndeTrabalha: PessoaResponsavelOndeTrabalhaBox.Text,
                ResponsavelEnderecoTrabalho: PessoaResponsavelEnderecoTrabalhoBox.Text,
                ResponsavelNomeEmpresaTrabalho: PessoaResponsavelNomeEmpresaTrabalhoBox.Text,
                ResponsavelTelefoneEmpresaTrabalho: PessoaResponsavelTelefoneEmpresaTrabalhoBox.Text,
                ResponsavelDadosBancarios: PessoaResponsavelDadosBancariosBox.Text));

            ClearPessoaForm();
            await LoadPessoasAsync();
        }
        catch (Exception ex)
        {
            PessoaErrorText.Text = ex.Message;
        }
    }

    private async void SavePessoaDocumentoButton_Click(object sender, RoutedEventArgs e)
    {
        PessoaDocumentoErrorText.Text = string.Empty;

        try
        {
            var pessoaId = PessoaDocumentoPessoaBox.SelectedValue is Guid selectedPessoaId ? selectedPessoaId : Guid.Empty;
            var tipo = PessoaDocumentoTipoBox.SelectedValue as string ?? "outros";

            await _rentalManagementService.CreatePessoaDocumentoAsync(new CreatePessoaDocumentoRequest(
                pessoaId,
                tipo,
                PessoaDocumentoNomeBox.Text,
                PessoaDocumentoArquivoBox.Text,
                null,
                ToDateOnly(PessoaDocumentoValidadeBox.SelectedDate),
                PessoaDocumentoObservacoesBox.Text));

            PessoaDocumentoNomeBox.Clear();
            PessoaDocumentoArquivoBox.Clear();
            PessoaDocumentoValidadeBox.SelectedDate = null;
            PessoaDocumentoObservacoesBox.Clear();
        }
        catch (Exception ex)
        {
            PessoaDocumentoErrorText.Text = ex.Message;
        }
    }

    private void ClearPessoaForm()
    {
        PessoaNomeBox.Clear();
        PessoaDocumentoBox.Clear();
        PessoaTelefoneBox.Clear();
        PessoaEmailBox.Clear();
        PessoaObservacoesBox.Clear();
        PessoaProprietarioBox.IsChecked = false;
        PessoaLocatarioBox.IsChecked = false;
        PessoaFiadorBox.IsChecked = false;
        PessoaEnderecoBox.Clear();
        PessoaRgBox.Clear();
        PessoaEstadoCivilBox.Clear();
        PessoaNacionalidadeBox.Clear();
        PessoaDataNascimentoBox.SelectedDate = null;
        PessoaProfissaoBox.Clear();
        PessoaOndeTrabalhaBox.Clear();
        PessoaEnderecoTrabalhoBox.Clear();
        PessoaNomeEmpresaTrabalhoBox.Clear();
        PessoaTelefoneEmpresaTrabalhoBox.Clear();
        PessoaDadosBancariosBox.Clear();
        PessoaConjugeNomeBox.Clear();
        PessoaConjugeRgBox.Clear();
        PessoaConjugeCpfBox.Clear();
        PessoaConjugeDataNascimentoBox.SelectedDate = null;
        PessoaConjugeProfissaoBox.Clear();
        PessoaConjugeNacionalidadeBox.Clear();
        PessoaConjugeTelefoneBox.Clear();
        PessoaJuridicaEnderecoEmpresaBox.Clear();
        PessoaResponsavelNomeBox.Clear();
        PessoaResponsavelEnderecoBox.Clear();
        PessoaResponsavelEstadoCivilBox.Clear();
        PessoaResponsavelNacionalidadeBox.Clear();
        PessoaResponsavelDataNascimentoBox.SelectedDate = null;
        PessoaResponsavelTelefoneBox.Clear();
        PessoaResponsavelEmailBox.Clear();
        PessoaResponsavelRgBox.Clear();
        PessoaResponsavelCpfBox.Clear();
        PessoaResponsavelProfissaoBox.Clear();
        PessoaResponsavelOndeTrabalhaBox.Clear();
        PessoaResponsavelEnderecoTrabalhoBox.Clear();
        PessoaResponsavelNomeEmpresaTrabalhoBox.Clear();
        PessoaResponsavelTelefoneEmpresaTrabalhoBox.Clear();
        PessoaResponsavelDadosBancariosBox.Clear();
    }

    private async void SaveImovelButton_Click(object sender, RoutedEventArgs e)
    {
        ImovelErrorText.Text = string.Empty;

        try
        {
            var finalidade = ImovelFinalidadeBox.SelectedValue is ImovelFinalidade selectedFinalidade
                ? selectedFinalidade
                : ImovelFinalidade.Locacao;

            decimal? valorAluguel = null;
            if (!string.IsNullOrWhiteSpace(ImovelValorAluguelBox.Text)
                && decimal.TryParse(ImovelValorAluguelBox.Text, NumberStyles.Currency, CultureInfo.GetCultureInfo("pt-BR"), out var parsedValue))
            {
                valorAluguel = parsedValue;
            }

            var valorVenda = ParseNullableDecimal(ImovelValorVendaBox.Text);
            var latitude = ParseNullableDecimal(ImovelLatitudeBox.Text);
            var longitude = ParseNullableDecimal(ImovelLongitudeBox.Text);

            var proprietarioId = ImovelProprietarioBox.SelectedValue is Guid selectedOwnerId ? selectedOwnerId : Guid.Empty;
            await _rentalManagementService.CreateImovelAsync(new CreateImovelRequest(
                ProprietarioId: proprietarioId,
                Rua: ImovelRuaBox.Text,
                Numero: ImovelNumeroBox.Text,
                Bairro: ImovelBairroBox.Text,
                Cidade: "Paranavaí",
                Estado: "PR",
                ValorAluguel: valorAluguel,
                Finalidade: finalidade,
                Observacoes: ImovelObservacoesBox.Text,
                Complemento: ImovelComplementoBox.Text,
                Cep: ImovelCepBox.Text,
                SaneparMatricula: ImovelSaneparBox.Text,
                CopelMatricula: ImovelCopelBox.Text,
                IptuMatricula: ImovelIptuBox.Text,
                TipoImovel: ImovelTipoBox.Text,
                Descricao: ImovelDescricaoBox.Text,
                ValorVenda: valorVenda,
                Latitude: latitude,
                Longitude: longitude));

            ImovelRuaBox.Clear();
            ImovelNumeroBox.Clear();
            ImovelComplementoBox.Clear();
            ImovelBairroBox.Clear();
            ImovelCepBox.Clear();
            ImovelTipoBox.Clear();
            ImovelSaneparBox.Clear();
            ImovelCopelBox.Clear();
            ImovelIptuBox.Clear();
            ImovelValorAluguelBox.Clear();
            ImovelValorVendaBox.Clear();
            ImovelLatitudeBox.Clear();
            ImovelLongitudeBox.Clear();
            ImovelDescricaoBox.Clear();
            ImovelObservacoesBox.Clear();
            await LoadImoveisAsync();
        }
        catch (Exception ex)
        {
            ImovelErrorText.Text = ex.Message;
        }
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
        ModuleSearchBox.Text = string.Empty;
        ApplyModuleFilter();
    }

    private void ModulePrimaryActionButton_Click(object sender, RoutedEventArgs e)
    {
        var message = _activeModulePage switch
        {
            ShellPage.Boletos => "Integração bancária ainda não configurada.",
            ShellPage.NotasFiscais => "Integração automática com NFS-e ainda não configurada. Use o fluxo manual/semi-manual.",
            ShellPage.Dimob => "Exportação oficial DIMOB pendente de confirmação do layout vigente da Receita Federal.",
            ShellPage.Documentos => "Modelos iniciais criados como pendentes de revisão. A redação final deve ser confirmada com o cliente.",
            ShellPage.Configuracoes => "Certificados A1: registrar apenas metadados por enquanto. Não armazene senha ou arquivo do certificado sem armazenamento seguro.",
            _ => "CRUD completo deste módulo será implementado em uma próxima etapa."
        };

        MessageBox.Show(this, message, "Monthoya", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void NewUserButton_Click(object sender, RoutedEventArgs e)
    {
        ClearUserForm();
    }

    private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not UserSummary selected)
        {
            return;
        }

        _editingUserId = selected.Id;
        UserNameBox.Text = selected.DisplayName;
        UserLoginNameBox.Text = selected.LoginName;
        UserEmailBox.Text = selected.Email;
        UserRoleBox.SelectedValue = selected.Role;
        SetAccessCheckboxes(RolePermissions.GetEffectiveAccess(selected.Role, selected.Access));
        UpdateAccessControlState();
        UserPasswordBox.Clear();
        ToggleUserActiveButton.Content = selected.IsActive ? "Desativar usuário" : "Reativar usuário";
    }

    private async void SaveUserButton_Click(object sender, RoutedEventArgs e)
    {
        UserErrorText.Text = string.Empty;

        try
        {
            var selectedRole = UserRoleBox.SelectedValue is UserRole role ? role : UserRole.Usuario;

            if (_editingUserId is null)
            {
                await _userService.CreateUserAsync(
                    new CreateUserRequest(UserNameBox.Text, UserLoginNameBox.Text, UserEmailBox.Text, UserPasswordBox.Password, selectedRole, GetSelectedAccess()));
            }
            else
            {
                await _userService.UpdateUserAsync(
                    new UpdateUserRequest(_editingUserId.Value, UserNameBox.Text, UserLoginNameBox.Text, UserEmailBox.Text, selectedRole, GetSelectedAccess()));
            }

            ClearUserForm();
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            UserErrorText.Text = ex.Message;
        }
    }

    private async void ToggleUserActiveButton_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not UserSummary selected)
        {
            UserErrorText.Text = "Selecione um usuário.";
            return;
        }

        if (selected.Id == _currentUser.Id)
        {
            UserErrorText.Text = "Você não pode desativar o próprio usuário logado.";
            return;
        }

        await _userService.SetUserActiveAsync(selected.Id, !selected.IsActive);
        ClearUserForm();
        await LoadUsersAsync();
    }

    private void ClearUserForm()
    {
        _editingUserId = null;
        UsersGrid.SelectedItem = null;
        UserNameBox.Clear();
        UserLoginNameBox.Clear();
        UserEmailBox.Clear();
        UserPasswordBox.Clear();
        UserRoleBox.SelectedValue = UserRole.Usuario;
        SetAccessCheckboxes(RolePermissions.DefaultUserAccess);
        UpdateAccessControlState();
        UserErrorText.Text = string.Empty;
        ToggleUserActiveButton.Content = "Ativar/Desativar";
    }

    private void UserRoleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateAccessControlState();
    }

    private UserAccess GetSelectedAccess()
    {
        var access = UserAccess.None;

        if (UserManagementAccessBox.IsChecked == true)
        {
            access |= UserAccess.UserManagement;
        }

        return access;
    }

    private void SetAccessCheckboxes(UserAccess access)
    {
        UserManagementAccessBox.IsChecked = access.HasFlag(UserAccess.UserManagement);
    }

    private void UpdateAccessControlState()
    {
        if (UserRoleBox.SelectedValue is not UserRole selectedRole)
        {
            return;
        }

        var isNormalUser = selectedRole == UserRole.Usuario;
        if (!isNormalUser)
        {
            SetAccessCheckboxes(RolePermissions.GetEffectiveAccess(selectedRole, RolePermissions.DefaultUserAccess));
        }

        UserManagementAccessBox.IsEnabled = isNormalUser;
        AccessHelpText.Text = isNormalUser
            ? "Marque se este usuário normal pode abrir o cadastro de usuários para cadastrar, editar, ativar e desativar usuários."
            : "Este perfil tem acesso ao cadastro de usuários por regra do sistema.";
    }

    private void LogoutPrompt_Click(object sender, RoutedEventArgs e)
    {
        RequestLogout();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        RequestLogout();
    }

    private void RequestLogout()
    {
        var result = MessageBox.Show(
            this,
            "Deseja sair do sistema e voltar para a tela de login?",
            "Sair do Monthoya",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        IsLogoutRequested = true;
        Close();
    }

    private static string GetRoleLabel(UserRole role) =>
        role switch
        {
            UserRole.Usuario => "Usuário",
            UserRole.Administrador => "Administrador",
            UserRole.Desenvolvedor => "Desenvolvedor",
            _ => role.ToString()
        };

    private static string GetAccessLabel(UserAccess access) =>
        access.HasFlag(UserAccess.UserManagement)
            ? "Cadastro de usuários"
            : "Básico";

    private static bool IsGenericModulePage(ShellPage page) =>
        page is ShellPage.Locacoes
            or ShellPage.Financeiro
            or ShellPage.Boletos
            or ShellPage.NotasFiscais
            or ShellPage.Documentos
            or ShellPage.Relatorios
            or ShellPage.Dimob
            or ShellPage.Manutencoes
            or ShellPage.Vistorias
            or ShellPage.Configuracoes;

    private static ModuleDefinition GetModuleDefinition(ShellPage page) =>
        page switch
        {
            ShellPage.Locacoes => new("Locações", "Contratos de locação vinculados a imóvel, proprietário, locatário e fiadores.", "Fundação criada. O cadastro completo de locação deve validar imóvel, locatário, proprietário, fiadores, reajuste e taxas antes de ativar.", "Nova locação"),
            ShellPage.Financeiro => new("Financeiro", "Lançamentos financeiros, contas a pagar e contas a receber.", "Fundação criada para aluguel, manutenção, taxas, descontos, multa, juros, administração, boleto, nota fiscal e outros.", "Novo lançamento"),
            ShellPage.Boletos => new("Boletos", "Controle interno de boletos vinculados a locações e lançamentos.", "Integração bancária ainda não configurada. As ações Gerar, Registrar, Cancelar, Baixar PDF e Consultar status ficam preparadas para provider futuro.", "Ações do boleto"),
            ShellPage.NotasFiscais => new("Notas Fiscais", "Fluxo manual/semi-manual de NFS-e para registrar dados emitidos no portal municipal.", "Integração automática com NFS-e ainda não configurada. Use o fluxo manual/semi-manual e registre número, código de verificação, PDF/XML e status.", "Ações de NFS-e"),
            ShellPage.Documentos => new("Documentos", "Modelos e documentos gerados em PDF.", "Modelos iniciais foram criados como pendentes de revisão. Não use redação jurídica como definitiva sem validação do cliente.", "Novo documento"),
            ShellPage.Relatorios => new("Relatórios", "Consultas operacionais de aluguéis, imóveis, locações e contas.", "Relatórios oficiais e exportações finais serão detalhados conforme os dados reais e decisões do cliente.", "Gerar relatório"),
            ShellPage.Dimob => new("DIMOB", "Fundação para conferência anual de dados da DIMOB.", "Exportação TXT oficial pendente de confirmação do layout vigente da Receita Federal/PGD/Receitanet.", "Exportar DIMOB"),
            ShellPage.Manutencoes => new("Manutenções", "Solicitações e execução de manutenção de imóveis.", "Fundação criada com status solicitada, em andamento, concluída e cancelada.", "Nova manutenção"),
            ShellPage.Vistorias => new("Vistorias", "Vistorias de entrada, saída, periódicas e outras.", "Fundação criada. Anexos, fotos e laudos em PDF devem usar o módulo de documentos.", "Nova vistoria"),
            ShellPage.Configuracoes => new("Configurações", "Índices de reajuste, certificado A1 e integrações futuras.", "Certificados digitais: registrar somente metadados agora. TODO: armazenamento criptografado, senha segura, auditoria, alertas e ambiente homologação/produção.", "Abrir configurações"),
            _ => new("Módulo", "Fundação do módulo.", "Sem ações disponíveis.", "Abrir")
        };

    private const string MapHtmlTemplate = """
<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css">
  <style>
    html, body, #map { height: 100%; margin: 0; font-family: Segoe UI, Arial, sans-serif; }
    .empty { position: absolute; z-index: 999; top: 18px; left: 68px; width: min(560px, calc(100% - 92px)); background: white; border: 1px solid #dbe8e2; border-radius: 8px; padding: 12px 14px; color: #66756f; box-shadow: 0 10px 30px rgba(20,37,33,.08); }
  </style>
</head>
<body>
  <div id="map"></div>
  <script src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"></script>
  <script>
    const properties = __PROPERTIES__;
    const map = L.map('map', { zoomControl: true }).setView([-23.0816, -52.4617], 13);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap'
    }).addTo(map);

    if (!properties.length) {
      const empty = document.createElement('div');
      empty.className = 'empty';
      empty.textContent = 'Nenhum imóvel disponível para locação com coordenadas cadastrado ainda.';
      document.body.appendChild(empty);
    }

    const markerGroup = L.featureGroup().addTo(map);
    properties.forEach(property => {
      const marker = L.marker([property.Latitude, property.Longitude]).addTo(markerGroup);
      marker.bindPopup(`<strong>${property.Code}</strong><br>${property.AddressLine}<br>${property.City} - ${property.State}<br>${property.RentalPrice ?? ''}`);
    });

    if (properties.length) {
      map.fitBounds(markerGroup.getBounds().pad(0.18));
    }
  </script>
</body>
</html>
""";

    private enum ShellPage
    {
        Dashboard,
        Users,
        Pessoas,
        Imoveis,
        Locacoes,
        Financeiro,
        Boletos,
        NotasFiscais,
        Documentos,
        Relatorios,
        Dimob,
        Manutencoes,
        Vistorias,
        Configuracoes,
        Diagnostics
    }

    private sealed class ShellTab(Guid id, string title, ShellPage page)
    {
        public Guid Id { get; } = id;

        public string Title { get; set; } = title;

        public ShellPage Page { get; set; } = page;
    }

    private sealed record UserRoleOption(string Label, UserRole Role);
    private sealed record TipoPessoaOption(string Label, TipoPessoa Tipo);
    private sealed record ImovelFinalidadeOption(string Label, ImovelFinalidade Finalidade);
    private sealed record PessoaDocumentoTipoOption(string Label, string Tipo);
    private sealed record ModuleDefinition(string Title, string Subtitle, string Notice, string ActionText);

    private static readonly IReadOnlyList<UserRoleOption> UserRoleOptions =
    [
        new("Usuário", UserRole.Usuario),
        new("Administrador", UserRole.Administrador),
        new("Desenvolvedor", UserRole.Desenvolvedor)
    ];

    private static readonly IReadOnlyList<TipoPessoaOption> TipoPessoaOptions =
    [
        new("Física", TipoPessoa.Fisica),
        new("Jurídica", TipoPessoa.Juridica)
    ];

    private static readonly IReadOnlyList<ImovelFinalidadeOption> ImovelFinalidadeOptions =
    [
        new("Locação", ImovelFinalidade.Locacao),
        new("Venda", ImovelFinalidade.Venda),
        new("Ambos", ImovelFinalidade.Ambos)
    ];

    private static readonly IReadOnlyList<PessoaDocumentoTipoOption> PessoaDocumentoTipoOptions =
    [
        new("CPF", "cpf"),
        new("RG", "rg"),
        new("Comprovante de residência", "comprovante_residencia"),
        new("Comprovante de renda", "comprovante_renda"),
        new("Comprovante de estado civil", "estado_civil"),
        new("Contrato social", "contrato_social"),
        new("Cartão CNPJ", "cartao_cnpj"),
        new("Procuração/autorização", "procuracao"),
        new("Dados bancários", "dados_bancarios"),
        new("Outros", "outros")
    ];
}
