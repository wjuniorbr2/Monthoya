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
    private readonly INotificationService _notificationService;
    private readonly INotificationEmailSettingsService _notificationEmailSettingsService;
    private Guid? _editingUserId;
    private ShellTab? _activeTab;
    private ShellPage _activeModulePage;
    private IReadOnlyList<PessoaSummary> _pessoas = [];
    private IReadOnlyList<PessoaDocumentoSummary> _pessoaDocumentos = [];
    private IReadOnlyList<ImovelSummary> _imoveis = [];
    private IReadOnlyList<ImovelImagemSummary> _imovelImagens = [];
    private readonly List<PendingImovelMedia> _pendingImovelMedia = [];
    private IReadOnlyList<VistoriaSummary> _imovelVistorias = [];
    private IReadOnlyList<ImovelChaveMovimentoSummary> _chaveMovimentos = [];
    private IReadOnlyList<NotificationSummary> _notifications = [];
    private IReadOnlyList<NotificationSummary> _notificationHistory = [];
    private IReadOnlyList<UserSummary> _notificationUsers = [];
    private IReadOnlyList<object> _moduleItems = [];
    private Guid? _selectedPessoaId;
    private PessoaDetails? _selectedPessoaDetails;
    private Guid? _selectedImovelId;
    private ImovelDetails? _selectedImovelDetails;
    private bool _isPessoaEditing = true;
    private bool _isImovelEditing = true;
    private bool _isFormattingPessoaText;
    private bool _isRestoringTabState;
    private bool _isAutoCompletingText;
    private bool _isPageTransitionRunning;
    private bool _isLoadingNotifications;
    private bool _isRefreshingNotifications;
    private bool _isLoadingNotificationDetails;
    private bool _isAcknowledgingNotification;
    private bool _isChangingNotificationSelection;
    private bool _isNotificationHistoryVisible;
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
        IRentalManagementService rentalManagementService,
        INotificationService notificationService,
        INotificationEmailSettingsService notificationEmailSettingsService)
    {
        InitializeComponent();
        // Start the main window maximized after login
        WindowState = WindowState.Maximized;
        _currentUser = currentUser;
        _userService = userService;
        _dashboardService = dashboardService;
        _rentalManagementService = rentalManagementService;
        _notificationService = notificationService;
        _notificationEmailSettingsService = notificationEmailSettingsService;

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
        PessoaDocumentoDonoBox.SelectedValue = "";
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
        ImovelFinalidadeBox.SelectedIndex = -1;
        ImoveisFinalidadeFilterBox.ItemsSource = ImoveisFinalidadeFilterOptions;
        ImoveisFinalidadeFilterBox.SelectedValuePath = "Finalidade";
        ImoveisFinalidadeFilterBox.DisplayMemberPath = "Label";
        ImoveisFinalidadeFilterBox.SelectedValue = null;
        ImoveisStatusFilterBox.ItemsSource = ImoveisStatusFilterOptions;
        ImoveisStatusFilterBox.SelectedValuePath = "Status";
        ImoveisStatusFilterBox.DisplayMemberPath = "Label";
        ImoveisStatusFilterBox.SelectedValue = "ativos";
        ImoveisPublicacaoFilterBox.ItemsSource = ImoveisPublicacaoFilterOptions;
        ImoveisPublicacaoFilterBox.SelectedValuePath = "Publicacao";
        ImoveisPublicacaoFilterBox.DisplayMemberPath = "Label";
        ImoveisPublicacaoFilterBox.SelectedValue = "todos";
        ImovelTipoBox.ItemsSource = ImovelTipoOptions;
        ImovelStatusBox.ItemsSource = ImovelStatusOptions;
        ImovelStatusBox.SelectedValuePath = "Status";
        ImovelStatusBox.DisplayMemberPath = "Label";
        ImovelStatusBox.SelectedIndex = -1;
        ImovelChavePosseBox.ItemsSource = ImovelChavePosseOptions;
        ImovelChavePosseBox.SelectedValuePath = "Posse";
        ImovelChavePosseBox.DisplayMemberPath = "Label";
        ImovelChavePosseBox.SelectedValue = ImovelChavePosse.NaoCadastrada;
        ImovelChavePosseBox.SelectionChanged += (_, _) => UpdateImovelChaveFieldsVisibility();
        UpdateImovelChaveFieldsVisibility();
        ImovelEnderecoPublicoModoBox.ItemsSource = ImovelEnderecoPublicoModoOptions;
        ImovelEnderecoPublicoModoBox.SelectedValuePath = "Modo";
        ImovelEnderecoPublicoModoBox.DisplayMemberPath = "Label";
        ImovelEnderecoPublicoModoBox.SelectedValue = ImovelEnderecoPublicoModo.BairroCidade;
        ImovelMediaCategoryBox.ItemsSource = ImovelMediaCategoryOptions;
        ImovelMediaCategoryBox.SelectedValuePath = "Category";
        ImovelMediaCategoryBox.DisplayMemberPath = "Label";
        ImovelMediaCategoryBox.SelectedValue = ImovelMediaCategory.PropertyPhoto;
        ImovelVistoriaTipoBox.ItemsSource = ImovelVistoriaTipoOptions;
        ImovelVistoriaTipoBox.SelectedValuePath = "Tipo";
        ImovelVistoriaTipoBox.DisplayMemberPath = "Label";
        ImovelVistoriaTipoBox.SelectedValue = VistoriaTipo.InicialProprietario;
        ImovelVistoriaStatusBox.ItemsSource = VistoriaStatusOptions;
        ImovelVistoriaStatusBox.SelectedValuePath = "Status";
        ImovelVistoriaStatusBox.DisplayMemberPath = "Label";
        ImovelVistoriaStatusBox.SelectedValue = VistoriaStatus.Draft;
        ChavesStatusFilterBox.ItemsSource = ChavesStatusFilterOptions;
        ChavesStatusFilterBox.SelectedValuePath = "Status";
        ChavesStatusFilterBox.DisplayMemberPath = "Label";
        ChavesStatusFilterBox.SelectedValue = "ativas";
        ChavesImovelBox.DisplayMemberPath = "Endereco";
        ChavesImovelBox.SelectedValuePath = "Id";
        NotificationsCategoryFilterBox.ItemsSource = NotificationCategoryFilterOptions;
        NotificationsCategoryFilterBox.SelectedValuePath = "Category";
        NotificationsCategoryFilterBox.DisplayMemberPath = "Label";
        NotificationsCategoryFilterBox.SelectedValue = null;
        NotificationsPriorityFilterBox.ItemsSource = NotificationPriorityFilterOptions;
        NotificationsPriorityFilterBox.SelectedValuePath = "Priority";
        NotificationsPriorityFilterBox.DisplayMemberPath = "Label";
        NotificationsPriorityFilterBox.SelectedValue = null;
        NewNotificationPriorityBox.ItemsSource = NotificationPriorityOptions;
        NewNotificationPriorityBox.SelectedValuePath = "Priority";
        NewNotificationPriorityBox.DisplayMemberPath = "Label";
        NewNotificationPriorityBox.SelectedValue = NotificationPriority.Normal;
        NewNotificationCategoryBox.ItemsSource = NotificationCategoryOptions;
        NewNotificationCategoryBox.SelectedValuePath = "Category";
        NewNotificationCategoryBox.DisplayMemberPath = "Label";
        NewNotificationCategoryBox.SelectedValue = NotificationCategory.ManualMessage;
        SetAccessCheckboxes(RolePermissions.DefaultUserAccess);
        UpdateAccessControlState();
        DiagnosticsText.Text = $"Login: {currentUser.LoginName}{Environment.NewLine}E-mail: {currentUser.Email}{Environment.NewLine}Perfil: {GetRoleLabel(currentUser.Role)}{Environment.NewLine}Acessos: {GetAccessLabel(RolePermissions.GetEffectiveAccess(currentUser.Role, currentUser.Access))}{Environment.NewLine}Banco: configurado via secrets/appsettings";

        AddShellTab(ShellPage.Dashboard, "Tela Inicial");
        SourceInitialized += ShellWindow_SourceInitialized;
        Loaded += async (_, _) =>
        {
            await _notificationService.ProcessDueScheduledNotificationsAsync();
            await _notificationService.CheckAndCreateKeyOverdueNotificationsAsync();
            await ShowPageAsync(ShellPage.Dashboard, true);
            await RefreshNotificationBellAsync();
            await ShowRequiredNotificationsAsync();
        };
        UpdateMaximizeButtonIcon();
    }

    // Allow dragging the window from the top stripe (where tabs live).
    private void TopDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            e.Handled = true;
            ToggleWindowMaximized();
            return;
        }

        BeginWindowDrag(e);
    }

    private void BeginWindowDrag(MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        if (WindowState == WindowState.Maximized)
        {
            RestoreWindowForDrag(e);
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // DragMove can throw if Windows has already ended the mouse operation.
        }
    }

    private void RestoreWindowForDrag(MouseButtonEventArgs e)
    {
        var mouseInWindow = e.GetPosition(this);
        var mouseOnScreen = PointToScreen(mouseInWindow);
        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is not null)
        {
            mouseOnScreen = source.CompositionTarget.TransformFromDevice.Transform(mouseOnScreen);
        }

        var horizontalRatio = ActualWidth > 0 ? mouseInWindow.X / ActualWidth : 0.5;
        var restoredWidth = RestoreBounds.Width > 0 ? RestoreBounds.Width : Width;
        var restoredHeight = RestoreBounds.Height > 0 ? RestoreBounds.Height : Height;

        WindowState = WindowState.Normal;
        Width = restoredWidth;
        Height = restoredHeight;
        Left = mouseOnScreen.X - restoredWidth * horizontalRatio;
        Top = Math.Max(0, mouseOnScreen.Y - 14);
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
            BeginWindowDrag(e);
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

    // New pessoa action handled by ResetPessoaFormForPageOpen in the Pessoas layout partial.

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
        if (FindName("TitleBarMaximizeButton") is Button originalMax)
        {
            originalMax.Content = new TextBlock
            {
                Text = "□",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center
            };
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
        ChavesPanel.Visibility = Visibility.Collapsed;
        NotificacoesPanel.Visibility = Visibility.Collapsed;
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
        ChavesNavButton.Style = (Style)FindResource("NavButton");
        NotificacoesNavButton.Style = (Style)FindResource("NavButton");
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

    private async void ChavesNavButton_Click(object sender, RoutedEventArgs e) =>
        await UpdateActiveTabAsync(ShellPage.Chaves, "Chaves", true);

    private async void NotificacoesNavButton_Click(object sender, RoutedEventArgs e) =>
        await UpdateActiveTabAsync(ShellPage.Notificacoes, "Notificações", true);

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

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect NativeMonitor;
        public Rect NativeWorkArea;
        public int Flags;

        public NativeRect Monitor => new(NativeMonitor.Left, NativeMonitor.Top, NativeMonitor.Right, NativeMonitor.Bottom);
        public NativeRect WorkArea => new(NativeWorkArea.Left, NativeWorkArea.Top, NativeWorkArea.Right, NativeWorkArea.Bottom);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public NativeRect(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
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
    private struct MinMaxInfo
    {
        public NativePoint Reserved;
        public NativePoint MaxSize;
        public NativePoint MaxPosition;
        public NativePoint MinTrackSize;
        public NativePoint MaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}