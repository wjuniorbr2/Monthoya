using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
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
    private IReadOnlyList<PessoaDocumentoSummary> _pessoaDocumentos = [];
    private IReadOnlyList<ImovelSummary> _imoveis = [];
    private IReadOnlyList<object> _moduleItems = [];
    private Guid? _selectedPessoaId;
    private PessoaDetails? _selectedPessoaDetails;
    private bool _isPessoaEditing = true;
    private bool _isFormattingPessoaText;
    private bool _isRestoringTabState;
    private bool _isAutoCompletingText;
    private IReadOnlyList<string> _streetSuggestions = [];
    private Popup? _addressSuggestionPopup;
    private ListBox? _addressSuggestionList;
    private TextBox? _addressSuggestionTarget;
    private const int WmGetMinMaxInfo = 0x0024;
    private const int MonitorDefaultToNearest = 0x00000002;

    public ShellWindow(
        AuthenticatedUser currentUser,
        IUserService userService,
        IDashboardService dashboardService,
        IRentalManagementService rentalManagementService)
    {
        InitializeComponent();
        // Start the main window maximized after login
        WindowState = WindowState.Maximized;
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
        TogglePessoaTypePanels();
        PessoaDocumentoTipoBox.ItemsSource = PessoaDocumentoTipoFisicaOptions;
        PessoaDocumentoTipoBox.SelectedValuePath = "Tipo";
        PessoaDocumentoTipoBox.DisplayMemberPath = "Label";
        PessoaDocumentoTipoBox.SelectedValue = "cpf";
        PessoaDocumentoDonoBox.ItemsSource = PessoaDocumentoDonoFisicaOptions;
        PessoaDocumentoDonoBox.SelectedValuePath = "Tipo";
        PessoaDocumentoDonoBox.DisplayMemberPath = "Label";
        PessoaDocumentoDonoBox.SelectedValue = "pessoa";
        PessoaStatusFilterBox.ItemsSource = PessoaStatusFilterOptions;
        PessoaStatusFilterBox.SelectedValuePath = "Status";
        PessoaStatusFilterBox.DisplayMemberPath = "Label";
        PessoaStatusFilterBox.SelectedValue = "ativo";
        SetPessoaDocumentoSelection(null);
        ConfigurePessoaInputBehavior();
        SetPessoaEditMode(true, isNew: true);
        ImovelFinalidadeBox.ItemsSource = ImovelFinalidadeOptions;
        ImovelFinalidadeBox.SelectedValuePath = "Finalidade";
        ImovelFinalidadeBox.DisplayMemberPath = "Label";
        ImovelFinalidadeBox.SelectedValue = ImovelFinalidade.Locacao;
        SetAccessCheckboxes(RolePermissions.DefaultUserAccess);
        UpdateAccessControlState();
        DiagnosticsText.Text = $"Login: {currentUser.LoginName}{Environment.NewLine}E-mail: {currentUser.Email}{Environment.NewLine}Perfil: {GetRoleLabel(currentUser.Role)}{Environment.NewLine}Acessos: {GetAccessLabel(RolePermissions.GetEffectiveAccess(currentUser.Role, currentUser.Access))}{Environment.NewLine}Banco: configurado via secrets/appsettings";

        AddShellTab(ShellPage.Dashboard, "Tela Inicial");
        SourceInitialized += ShellWindow_SourceInitialized;
        Loaded += async (_, _) => await ShowPageAsync(ShellPage.Dashboard, true);
        UpdateMaximizeButtonIcon();
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


    private void ShellWindow_SourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).Handle;
        HwndSource.FromHwnd(handle)?.AddHook(WindowProc);
    }

    private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmGetMinMaxInfo)
        {
            ApplyMaximizedWorkArea(hwnd, lParam);
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static void ApplyMaximizedWorkArea(IntPtr hwnd, IntPtr lParam)
    {
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor == IntPtr.Zero)
        {
            return;
        }

        var monitorInfo = new MonitorInfo
        {
            Size = Marshal.SizeOf<MonitorInfo>()
        };
        if (!GetMonitorInfo(monitor, ref monitorInfo))
        {
            return;
        }

        var minMaxInfo = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        var workArea = monitorInfo.WorkArea;
        var monitorArea = monitorInfo.Monitor;

        minMaxInfo.MaxPosition.X = Math.Abs(workArea.Left - monitorArea.Left);
        minMaxInfo.MaxPosition.Y = Math.Abs(workArea.Top - monitorArea.Top);
        minMaxInfo.MaxSize.X = Math.Abs(workArea.Right - workArea.Left);
        minMaxInfo.MaxSize.Y = Math.Abs(workArea.Bottom - workArea.Top);

        Marshal.StructureToPtr(minMaxInfo, lParam, true);
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

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public Point Reserved;
        public Point MaxSize;
        public Point MaxPosition;
        public Point MinTrackSize;
        public Point MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect WorkArea;
        public int Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

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



    private sealed record UserRoleOption(string Label, UserRole Role);
    private sealed record TipoPessoaOption(string Label, TipoPessoa Tipo);
    private sealed record ImovelFinalidadeOption(string Label, ImovelFinalidade Finalidade);
    private sealed record PessoaDocumentoTipoOption(string Label, string Tipo);
    private sealed record PessoaDocumentoDonoOption(string Label, string Tipo);
    private sealed record PessoaStatusFilterOption(string Label, string Status);
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

    private static readonly IReadOnlyList<PessoaDocumentoTipoOption> PessoaDocumentoTipoFisicaOptions =
    [
        new("Pessoal", "pessoal"),
        new("Residência", "residencia"),
        new("Trabalho", "trabalho"),
        new("Pessoal do cônjuge", "pessoal_conjuge"),
        new("Trabalho do cônjuge", "trabalho_conjuge"),
        new("Outros", "outros")
    ];

    private static readonly IReadOnlyList<PessoaDocumentoTipoOption> PessoaDocumentoTipoJuridicaOptions =
    [
        new("Documentos da empresa", "documentos_empresa"),
        new("Endereço/residência", "endereco_residencia"),
        new("Identificação pessoal", "identificacao_pessoal"),
        new("Receita/Renda", "receita_renda"),
        new("Outros", "outros")
    ];

    private static readonly IReadOnlyList<PessoaDocumentoDonoOption> PessoaDocumentoDonoFisicaOptions =
    [
        new("Pessoa", "pessoa"),
        new("Cônjuge", "conjuge"),
        new("Empresa onde trabalha", "empresa_trabalho"),
        new("Outros", "outros")
    ];

    private static readonly IReadOnlyList<PessoaDocumentoDonoOption> PessoaDocumentoDonoJuridicaOptions =
    [
        new("Empresa", "empresa"),
        new("Responsável", "responsavel"),
        new("Cônjuge", "conjuge")
    ];

    private static readonly IReadOnlyList<PessoaStatusFilterOption> PessoaStatusFilterOptions =
    [
        new("Ativos", "ativo"),
        new("Inativos", "inativo"),
        new("Todos", "todos")
    ];
}


