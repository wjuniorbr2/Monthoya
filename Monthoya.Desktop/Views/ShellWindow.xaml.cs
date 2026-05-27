using System.Globalization;
using System.Text.RegularExpressions;
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
    private IReadOnlyList<PessoaDocumentoSummary> _pessoaDocumentos = [];
    private IReadOnlyList<ImovelSummary> _imoveis = [];
    private IReadOnlyList<object> _moduleItems = [];
    private Guid? _selectedPessoaId;
    private PessoaDetails? _selectedPessoaDetails;
    private bool _isPessoaEditing = true;
    private bool _isFormattingPessoaText;

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
        TogglePessoaTypePanels();
        PessoaDocumentoTipoBox.ItemsSource = PessoaDocumentoTipoOptions;
        PessoaDocumentoTipoBox.SelectedValuePath = "Tipo";
        PessoaDocumentoTipoBox.DisplayMemberPath = "Label";
        PessoaDocumentoTipoBox.SelectedValue = "cpf";
        PessoaDocumentoDonoBox.ItemsSource = PessoaDocumentoDonoOptions;
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

    private static DateTime? ToDateTime(DateOnly? value) =>
        value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : null;

    private void ConfigurePessoaInputBehavior()
    {
        foreach (var textBox in new[]
        {
            PessoaDocumentoBox, PessoaConjugeCpfBox, PessoaResponsavelCpfBox,
            PessoaTelefoneBox, PessoaConjugeTelefoneBox, PessoaResponsavelTelefoneBox, PessoaTelefoneEmpresaTrabalhoBox, PessoaResponsavelTelefoneEmpresaTrabalhoBox,
            PessoaCepBox, PessoaEmpresaCepBox, PessoaResponsavelCepBox,
            PessoaRgBox, PessoaConjugeRgBox, PessoaResponsavelRgBox
        })
        {
            textBox.PreviewTextInput += NumericMaskedTextBox_PreviewTextInput;
            DataObject.AddPastingHandler(textBox, NumericMaskedTextBox_OnPaste);
        }

        PessoaDocumentoBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaDocumentoBox, FormatCpfOrCnpj);
        PessoaConjugeCpfBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaConjugeCpfBox, FormatCpf);
        PessoaResponsavelCpfBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaResponsavelCpfBox, FormatCpf);
        PessoaTelefoneBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaTelefoneBox, FormatBrazilPhone);
        PessoaConjugeTelefoneBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaConjugeTelefoneBox, FormatBrazilPhone);
        PessoaResponsavelTelefoneBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaResponsavelTelefoneBox, FormatBrazilPhone);
        PessoaTelefoneEmpresaTrabalhoBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaTelefoneEmpresaTrabalhoBox, FormatBrazilPhone);
        PessoaResponsavelTelefoneEmpresaTrabalhoBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaResponsavelTelefoneEmpresaTrabalhoBox, FormatBrazilPhone);
        PessoaCepBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaCepBox, FormatCep);
        PessoaEmpresaCepBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaEmpresaCepBox, FormatCep);
        PessoaResponsavelCepBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaResponsavelCepBox, FormatCep);
        PessoaRgBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaRgBox, FormatRg);
        PessoaConjugeRgBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaConjugeRgBox, FormatRg);
        PessoaResponsavelRgBox.TextChanged += (_, _) => FormatMaskedTextBox(PessoaResponsavelRgBox, FormatRg);

        foreach (var datePicker in new[] { PessoaDataNascimentoBox, PessoaConjugeDataNascimentoBox, PessoaResponsavelDataNascimentoBox, PessoaDocumentoValidadeBox })
        {
            datePicker.Language = System.Windows.Markup.XmlLanguage.GetLanguage("pt-BR");
            datePicker.SelectedDateFormat = DatePickerFormat.Short;
            datePicker.CalendarOpened += (_, _) =>
            {
                if (datePicker.SelectedDate is null)
                {
                    datePicker.DisplayDate = new DateTime(2000, 1, 1);
                }
            };
            datePicker.KeyDown += PessoaDatePicker_KeyDown;
            datePicker.LostKeyboardFocus += PessoaDatePicker_LostKeyboardFocus;
        }
    }

    private void NumericMaskedTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (Regex.IsMatch(e.Text, @"^\d+$"))
        {
            return;
        }

        e.Handled = true;
        PessoaErrorText.Text = "Digite apenas números. Pontos, traços, parênteses e espaços são preenchidos automaticamente pelo sistema.";
    }

    private void NumericMaskedTextBox_OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            return;
        }

        var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        if (Regex.IsMatch(text, @"^\d+$"))
        {
            return;
        }

        e.CancelCommand();
        PessoaErrorText.Text = "Cole apenas números. Pontos, traços, parênteses e espaços são preenchidos automaticamente pelo sistema.";
    }

    private void FormatMaskedTextBox(TextBox textBox, Func<string, string> formatter)
    {
        if (_isFormattingPessoaText)
        {
            return;
        }

        _isFormattingPessoaText = true;
        var formatted = formatter(OnlyDigits(textBox.Text));
        textBox.Text = formatted;
        textBox.CaretIndex = formatted.Length;
        _isFormattingPessoaText = false;
    }

    private void PessoaDatePicker_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not DatePicker datePicker)
        {
            return;
        }

        e.Handled = true;
        TryApplyBrazilianDate(datePicker);
    }

    private void PessoaDatePicker_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is DatePicker datePicker)
        {
            TryApplyBrazilianDate(datePicker);
        }
    }

    private bool TryApplyBrazilianDate(DatePicker datePicker)
    {
        var text = datePicker.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        var digits = OnlyDigits(text);
        if (digits.Length == 6)
        {
            PessoaErrorText.Text = "Use o ano com quatro números. Exemplo: 25/04/1998.";
            return false;
        }

        if (digits.Length != 8)
        {
            PessoaErrorText.Text = "Data inválida. Use dia/mês/ano no formato brasileiro. Exemplo: 25/04/1998.";
            return false;
        }

        var normalized = $"{digits[..2]}/{digits.Substring(2, 2)}/{digits.Substring(4, 4)}";
        if (!DateTime.TryParseExact(normalized, "dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var parsed))
        {
            PessoaErrorText.Text = "Data inválida. Use dia/mês/ano no formato brasileiro. Exemplo: 25/04/1998.";
            return false;
        }

        datePicker.SelectedDate = parsed;
        datePicker.Text = parsed.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("pt-BR"));
        PessoaErrorText.Text = string.Empty;
        return true;
    }

    private static string OnlyDigits(string? value) =>
        Regex.Replace(value ?? string.Empty, @"\D", string.Empty);

    private static string FormatCpfOrCnpj(string digits) =>
        digits.Length > 11 ? FormatCnpj(digits) : FormatCpf(digits);

    private static string FormatCpf(string digits)
    {
        digits = digits.Length > 11 ? digits[..11] : digits;
        return digits.Length switch
        {
            <= 3 => digits,
            <= 6 => $"{digits[..3]}.{digits[3..]}",
            <= 9 => $"{digits[..3]}.{digits.Substring(3, 3)}.{digits[6..]}",
            _ => $"{digits[..3]}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits[9..]}"
        };
    }

    private static string FormatCnpj(string digits)
    {
        digits = digits.Length > 14 ? digits[..14] : digits;
        return digits.Length switch
        {
            <= 2 => digits,
            <= 5 => $"{digits[..2]}.{digits[2..]}",
            <= 8 => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits[5..]}",
            <= 12 => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}/{digits[8..]}",
            _ => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}/{digits.Substring(8, 4)}-{digits[12..]}"
        };
    }

    private static string FormatBrazilPhone(string digits)
    {
        digits = digits.Length > 11 ? digits[..11] : digits;
        if (digits.Length <= 2) return digits.Length == 0 ? string.Empty : $"({digits}";
        var ddd = digits[..2];
        var number = digits[2..];
        if (number.Length <= 4) return $"({ddd}) {number}";
        return number.Length <= 8
            ? $"({ddd}) {number[..4]}-{number[4..]}"
            : $"({ddd}) {number[..5]}-{number[5..]}";
    }

    private static string FormatCep(string digits)
    {
        digits = digits.Length > 8 ? digits[..8] : digits;
        return digits.Length <= 5 ? digits : $"{digits[..5]}-{digits[5..]}";
    }

    private static string FormatRg(string digits)
    {
        digits = digits.Length > 9 ? digits[..9] : digits;
        return digits.Length switch
        {
            <= 2 => digits,
            <= 5 => $"{digits[..2]}.{digits[2..]}",
            <= 8 => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits[5..]}",
            _ => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}-{digits[8..]}"
        };
    }

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

        ImovelProprietarioBox.ItemsSource = _pessoas.Where(x => x.Status == "Ativo").ToList();
        await LoadPessoaDocumentosAsync(_selectedPessoaId);
    }

    private async Task LoadImoveisAsync()
    {
        _imoveis = await _rentalManagementService.GetImoveisAsync();
        ApplyImoveisFilter();

        _pessoas = await _rentalManagementService.GetPessoasAsync();
        ImovelProprietarioBox.ItemsSource = _pessoas.Where(x => x.Status == "Ativo").ToList();
    }

    private async void ReloadPessoasButton_Click(object sender, RoutedEventArgs e) => await LoadPessoasAsync();

    private async void ReloadImoveisButton_Click(object sender, RoutedEventArgs e) => await LoadImoveisAsync();

    private void PessoasSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyPessoasFilter();

    private void PessoaStatusFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyPessoasFilter();

    private void ImoveisSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyImoveisFilter();

    private void ModuleSearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyModuleFilter();

    private async void PessoasGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PessoasGrid.SelectedItem is not PessoaSummary pessoa)
        {
            SetPessoaDocumentoSelection(null);
            await LoadPessoaDocumentosAsync(null);
            return;
        }

        SetPessoaDocumentoSelection(pessoa);
        _selectedPessoaDetails = await _rentalManagementService.GetPessoaAsync(pessoa.Id);
        if (_selectedPessoaDetails is not null)
        {
            PopulatePessoaForm(_selectedPessoaDetails);
            SetPessoaEditMode(false, isNew: false);
        }

        await LoadPessoaDocumentosAsync(pessoa.Id);
    }

    private void PessoaTipoBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        TogglePessoaTypePanels();
    }

    private void TogglePessoaTypePanels()
    {
        if (PessoaFisicaFieldsPanel is null || PessoaJuridicaFieldsPanel is null)
        {
            return;
        }

        var tipo = PessoaTipoBox.SelectedValue is TipoPessoa selectedTipo ? selectedTipo : TipoPessoa.Fisica;
        PessoaFisicaFieldsPanel.Visibility = tipo == TipoPessoa.Fisica ? Visibility.Visible : Visibility.Collapsed;
        PessoaJuridicaFieldsPanel.Visibility = tipo == TipoPessoa.Juridica ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task LoadPessoaDocumentosAsync(Guid? pessoaId)
    {
        if (!pessoaId.HasValue)
        {
            _pessoaDocumentos = [];
            PessoaDocumentosGrid.ItemsSource = _pessoaDocumentos;
            PessoaDocumentosTitleText.Text = "Documentos da pessoa selecionada";
            return;
        }

        _pessoaDocumentos = await _rentalManagementService.GetPessoaDocumentosAsync(pessoaId.Value);
        PessoaDocumentosGrid.ItemsSource = _pessoaDocumentos;
        PessoaDocumentosTitleText.Text = _pessoaDocumentos.Count == 0
            ? "Nenhum documento cadastrado para esta pessoa"
            : "Documentos da pessoa selecionada";
    }

    private void SetPessoaDocumentoSelection(PessoaSummary? pessoa)
    {
        _selectedPessoaId = pessoa?.Id;
        PessoaDocumentoPessoaText.Text = pessoa is null ? "Nenhuma pessoa selecionada" : pessoa.Nome;
        SavePessoaDocumentoButton.IsEnabled = pessoa is not null;
        PessoaProprietarioBox.IsChecked = pessoa?.IsProprietario == true;
        PessoaLocatarioBox.IsChecked = pessoa?.IsLocatario == true;
        PessoaFiadorBox.IsChecked = pessoa?.IsFiador == true;
    }

    private CreatePessoaRequest BuildPessoaRequest()
    {
        var tipo = PessoaTipoBox.SelectedValue is TipoPessoa selectedTipo ? selectedTipo : TipoPessoa.Fisica;
        return new CreatePessoaRequest(
            TipoPessoa: tipo,
            NomeDisplay: PessoaNomeBox.Text,
            Telefone: PessoaTelefoneBox.Text,
            Email: PessoaEmailBox.Text,
            Documento: PessoaDocumentoBox.Text,
            Observacoes: PessoaObservacoesBox.Text,
            Endereco: null,
            Rua: tipo == TipoPessoa.Fisica ? PessoaRuaBox.Text : PessoaEmpresaRuaBox.Text,
            Numero: tipo == TipoPessoa.Fisica ? PessoaNumeroBox.Text : PessoaEmpresaNumeroBox.Text,
            Complemento: tipo == TipoPessoa.Fisica ? PessoaComplementoBox.Text : PessoaEmpresaComplementoBox.Text,
            Bairro: tipo == TipoPessoa.Fisica ? PessoaBairroBox.Text : PessoaEmpresaBairroBox.Text,
            Cidade: tipo == TipoPessoa.Fisica ? PessoaCidadeBox.Text : PessoaEmpresaCidadeBox.Text,
            Estado: tipo == TipoPessoa.Fisica ? PessoaEstadoBox.Text : PessoaEmpresaEstadoBox.Text,
            Cep: tipo == TipoPessoa.Fisica ? PessoaCepBox.Text : PessoaEmpresaCepBox.Text,
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
            ResponsavelEndereco: null,
            ResponsavelRua: PessoaResponsavelRuaBox.Text,
            ResponsavelNumero: PessoaResponsavelNumeroBox.Text,
            ResponsavelComplemento: PessoaResponsavelComplementoBox.Text,
            ResponsavelBairro: PessoaResponsavelBairroBox.Text,
            ResponsavelCidade: PessoaResponsavelCidadeBox.Text,
            ResponsavelEstado: PessoaResponsavelEstadoBox.Text,
            ResponsavelCep: PessoaResponsavelCepBox.Text,
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
            ResponsavelDadosBancarios: PessoaResponsavelDadosBancariosBox.Text);
    }

    private bool ValidatePessoaForm()
    {
        if (!ValidateEmail(PessoaEmailBox.Text, "E-mail"))
        {
            return false;
        }

        if (!ValidateEmail(PessoaResponsavelEmailBox.Text, "E-mail do responsável"))
        {
            return false;
        }

        return TryApplyBrazilianDate(PessoaDataNascimentoBox)
            && TryApplyBrazilianDate(PessoaConjugeDataNascimentoBox)
            && TryApplyBrazilianDate(PessoaResponsavelDataNascimentoBox);
    }

    private bool ValidateEmail(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (Regex.IsMatch(value.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.CultureInvariant))
        {
            return true;
        }

        PessoaErrorText.Text = $"{fieldName} não está no formato correto. Exemplo: cliente@email.com";
        return false;
    }

    private void ApplyPessoasFilter()
    {
        var query = PessoasSearchBox.Text;
        var statusFilter = PessoaStatusFilterBox.SelectedValue as string ?? "ativo";
        PessoasGrid.ItemsSource = _pessoas
            .Where(x => statusFilter switch
            {
                "inativo" => x.Status == "Inativo",
                "todos" => true,
                _ => x.Status == "Ativo"
            })
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
            if (!ValidatePessoaForm())
            {
                return;
            }

            var request = BuildPessoaRequest();
            if (_selectedPessoaId.HasValue && _selectedPessoaDetails is not null)
            {
                await _rentalManagementService.UpdatePessoaAsync(new UpdatePessoaRequest(_selectedPessoaId.Value, request));
                SetPessoaEditMode(false, isNew: false);
            }
            else
            {
                await _rentalManagementService.CreatePessoaAsync(request);
                ClearPessoaForm();
                SetPessoaEditMode(true, isNew: true);
            }

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
            var pessoaId = _selectedPessoaId ?? Guid.Empty;
            var tipo = PessoaDocumentoTipoBox.SelectedValue as string ?? "outros";
            var documentoDe = PessoaDocumentoDonoBox.SelectedValue as string ?? "pessoa";

            await _rentalManagementService.CreatePessoaDocumentoAsync(new CreatePessoaDocumentoRequest(
                pessoaId,
                tipo,
                PessoaDocumentoNomeBox.Text,
                PessoaDocumentoArquivoBox.Text,
                null,
                ToDateOnly(PessoaDocumentoValidadeBox.SelectedDate),
                PessoaDocumentoObservacoesBox.Text,
                documentoDe));

            PessoaDocumentoNomeBox.Clear();
            PessoaDocumentoArquivoBox.Clear();
            PessoaDocumentoValidadeBox.SelectedDate = null;
            PessoaDocumentoObservacoesBox.Clear();
            await LoadPessoasAsync();
            await LoadPessoaDocumentosAsync(_selectedPessoaId);
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
        PessoaRuaBox.Clear();
        PessoaNumeroBox.Clear();
        PessoaComplementoBox.Clear();
        PessoaBairroBox.Clear();
        PessoaCidadeBox.Clear();
        PessoaEstadoBox.Clear();
        PessoaCepBox.Clear();
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
        PessoaEmpresaRuaBox.Clear();
        PessoaEmpresaNumeroBox.Clear();
        PessoaEmpresaComplementoBox.Clear();
        PessoaEmpresaBairroBox.Clear();
        PessoaEmpresaCidadeBox.Clear();
        PessoaEmpresaEstadoBox.Clear();
        PessoaEmpresaCepBox.Clear();
        PessoaResponsavelNomeBox.Clear();
        PessoaResponsavelRuaBox.Clear();
        PessoaResponsavelNumeroBox.Clear();
        PessoaResponsavelComplementoBox.Clear();
        PessoaResponsavelBairroBox.Clear();
        PessoaResponsavelCidadeBox.Clear();
        PessoaResponsavelEstadoBox.Clear();
        PessoaResponsavelCepBox.Clear();
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

    private void PopulatePessoaForm(PessoaDetails details)
    {
        var dados = details.Dados;
        PessoaFormTitleText.Text = details.Summary.Nome;
        PessoaTipoBox.SelectedValue = dados.TipoPessoa;
        PessoaNomeBox.Text = dados.NomeDisplay;
        PessoaDocumentoBox.Text = dados.Documento ?? string.Empty;
        PessoaTelefoneBox.Text = dados.Telefone ?? string.Empty;
        PessoaEmailBox.Text = dados.Email ?? string.Empty;
        PessoaObservacoesBox.Text = dados.Observacoes ?? string.Empty;
        PessoaRuaBox.Text = dados.Rua ?? string.Empty;
        PessoaNumeroBox.Text = dados.Numero ?? string.Empty;
        PessoaComplementoBox.Text = dados.Complemento ?? string.Empty;
        PessoaBairroBox.Text = dados.Bairro ?? string.Empty;
        PessoaCidadeBox.Text = dados.Cidade ?? string.Empty;
        PessoaEstadoBox.Text = dados.Estado ?? string.Empty;
        PessoaCepBox.Text = dados.Cep ?? string.Empty;
        PessoaRgBox.Text = dados.Rg ?? string.Empty;
        PessoaEstadoCivilBox.Text = dados.EstadoCivil ?? string.Empty;
        PessoaNacionalidadeBox.Text = dados.Nacionalidade ?? string.Empty;
        PessoaDataNascimentoBox.SelectedDate = ToDateTime(dados.DataNascimento);
        PessoaProfissaoBox.Text = dados.Profissao ?? string.Empty;
        PessoaOndeTrabalhaBox.Text = dados.OndeTrabalha ?? string.Empty;
        PessoaEnderecoTrabalhoBox.Text = dados.EnderecoTrabalho ?? string.Empty;
        PessoaNomeEmpresaTrabalhoBox.Text = dados.NomeEmpresaTrabalho ?? string.Empty;
        PessoaTelefoneEmpresaTrabalhoBox.Text = dados.TelefoneEmpresaTrabalho ?? string.Empty;
        PessoaDadosBancariosBox.Text = dados.DadosBancarios ?? string.Empty;
        PessoaConjugeNomeBox.Text = dados.ConjugeNome ?? string.Empty;
        PessoaConjugeRgBox.Text = dados.ConjugeRg ?? string.Empty;
        PessoaConjugeCpfBox.Text = dados.ConjugeCpf ?? string.Empty;
        PessoaConjugeDataNascimentoBox.SelectedDate = ToDateTime(dados.ConjugeDataNascimento);
        PessoaConjugeProfissaoBox.Text = dados.ConjugeProfissao ?? string.Empty;
        PessoaConjugeNacionalidadeBox.Text = dados.ConjugeNacionalidade ?? string.Empty;
        PessoaConjugeTelefoneBox.Text = dados.ConjugeTelefone ?? string.Empty;
        PessoaEmpresaRuaBox.Text = dados.Rua ?? string.Empty;
        PessoaEmpresaNumeroBox.Text = dados.Numero ?? string.Empty;
        PessoaEmpresaComplementoBox.Text = dados.Complemento ?? string.Empty;
        PessoaEmpresaBairroBox.Text = dados.Bairro ?? string.Empty;
        PessoaEmpresaCidadeBox.Text = dados.Cidade ?? string.Empty;
        PessoaEmpresaEstadoBox.Text = dados.Estado ?? string.Empty;
        PessoaEmpresaCepBox.Text = dados.Cep ?? string.Empty;
        PessoaResponsavelNomeBox.Text = dados.ResponsavelNome ?? string.Empty;
        PessoaResponsavelRuaBox.Text = dados.ResponsavelRua ?? string.Empty;
        PessoaResponsavelNumeroBox.Text = dados.ResponsavelNumero ?? string.Empty;
        PessoaResponsavelComplementoBox.Text = dados.ResponsavelComplemento ?? string.Empty;
        PessoaResponsavelBairroBox.Text = dados.ResponsavelBairro ?? string.Empty;
        PessoaResponsavelCidadeBox.Text = dados.ResponsavelCidade ?? string.Empty;
        PessoaResponsavelEstadoBox.Text = dados.ResponsavelEstado ?? string.Empty;
        PessoaResponsavelCepBox.Text = dados.ResponsavelCep ?? string.Empty;
        PessoaResponsavelEstadoCivilBox.Text = dados.ResponsavelEstadoCivil ?? string.Empty;
        PessoaResponsavelNacionalidadeBox.Text = dados.ResponsavelNacionalidade ?? string.Empty;
        PessoaResponsavelDataNascimentoBox.SelectedDate = ToDateTime(dados.ResponsavelDataNascimento);
        PessoaResponsavelTelefoneBox.Text = dados.ResponsavelTelefone ?? string.Empty;
        PessoaResponsavelEmailBox.Text = dados.ResponsavelEmail ?? string.Empty;
        PessoaResponsavelRgBox.Text = dados.ResponsavelRg ?? string.Empty;
        PessoaResponsavelCpfBox.Text = dados.ResponsavelCpf ?? string.Empty;
        PessoaResponsavelProfissaoBox.Text = dados.ResponsavelProfissao ?? string.Empty;
        PessoaResponsavelOndeTrabalhaBox.Text = dados.ResponsavelOndeTrabalha ?? string.Empty;
        PessoaResponsavelEnderecoTrabalhoBox.Text = dados.ResponsavelEnderecoTrabalho ?? string.Empty;
        PessoaResponsavelNomeEmpresaTrabalhoBox.Text = dados.ResponsavelNomeEmpresaTrabalho ?? string.Empty;
        PessoaResponsavelTelefoneEmpresaTrabalhoBox.Text = dados.ResponsavelTelefoneEmpresaTrabalho ?? string.Empty;
        PessoaResponsavelDadosBancariosBox.Text = dados.ResponsavelDadosBancarios ?? string.Empty;
        PessoaErrorText.Text = string.Empty;
        TogglePessoaTypePanels();
    }

    private void SetPessoaEditMode(bool isEditing, bool isNew)
    {
        _isPessoaEditing = isEditing;
        PessoaFormTitleText.Text = isNew ? "Nova pessoa" : PessoaFormTitleText.Text;
        SavePessoaButton.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
        CancelPessoaEditButton.Visibility = isEditing && !isNew ? Visibility.Visible : Visibility.Collapsed;
        PessoaEditButton.IsEnabled = !isEditing && _selectedPessoaId.HasValue;
        PessoaDeactivateButton.IsEnabled = !isEditing && _selectedPessoaId.HasValue;

        foreach (var textBox in GetPessoaTextBoxes())
        {
            textBox.IsReadOnly = !isEditing;
        }

        foreach (var datePicker in new[] { PessoaDataNascimentoBox, PessoaConjugeDataNascimentoBox, PessoaResponsavelDataNascimentoBox })
        {
            datePicker.IsEnabled = isEditing;
        }

        PessoaTipoBox.IsEnabled = isEditing;
    }

    private IEnumerable<TextBox> GetPessoaTextBoxes()
    {
        yield return PessoaNomeBox;
        yield return PessoaDocumentoBox;
        yield return PessoaTelefoneBox;
        yield return PessoaEmailBox;
        yield return PessoaRuaBox;
        yield return PessoaNumeroBox;
        yield return PessoaComplementoBox;
        yield return PessoaBairroBox;
        yield return PessoaCidadeBox;
        yield return PessoaEstadoBox;
        yield return PessoaCepBox;
        yield return PessoaRgBox;
        yield return PessoaEstadoCivilBox;
        yield return PessoaNacionalidadeBox;
        yield return PessoaProfissaoBox;
        yield return PessoaOndeTrabalhaBox;
        yield return PessoaEnderecoTrabalhoBox;
        yield return PessoaNomeEmpresaTrabalhoBox;
        yield return PessoaTelefoneEmpresaTrabalhoBox;
        yield return PessoaDadosBancariosBox;
        yield return PessoaConjugeNomeBox;
        yield return PessoaConjugeRgBox;
        yield return PessoaConjugeCpfBox;
        yield return PessoaConjugeProfissaoBox;
        yield return PessoaConjugeNacionalidadeBox;
        yield return PessoaConjugeTelefoneBox;
        yield return PessoaEmpresaRuaBox;
        yield return PessoaEmpresaNumeroBox;
        yield return PessoaEmpresaComplementoBox;
        yield return PessoaEmpresaBairroBox;
        yield return PessoaEmpresaCidadeBox;
        yield return PessoaEmpresaEstadoBox;
        yield return PessoaEmpresaCepBox;
        yield return PessoaResponsavelNomeBox;
        yield return PessoaResponsavelRuaBox;
        yield return PessoaResponsavelNumeroBox;
        yield return PessoaResponsavelComplementoBox;
        yield return PessoaResponsavelBairroBox;
        yield return PessoaResponsavelCidadeBox;
        yield return PessoaResponsavelEstadoBox;
        yield return PessoaResponsavelCepBox;
        yield return PessoaResponsavelEstadoCivilBox;
        yield return PessoaResponsavelNacionalidadeBox;
        yield return PessoaResponsavelTelefoneBox;
        yield return PessoaResponsavelEmailBox;
        yield return PessoaResponsavelRgBox;
        yield return PessoaResponsavelCpfBox;
        yield return PessoaResponsavelProfissaoBox;
        yield return PessoaResponsavelOndeTrabalhaBox;
        yield return PessoaResponsavelEnderecoTrabalhoBox;
        yield return PessoaResponsavelNomeEmpresaTrabalhoBox;
        yield return PessoaResponsavelTelefoneEmpresaTrabalhoBox;
        yield return PessoaResponsavelDadosBancariosBox;
        yield return PessoaObservacoesBox;
    }

    private void NewPessoaButton_Click(object sender, RoutedEventArgs e)
    {
        PessoasGrid.SelectedItem = null;
        _selectedPessoaId = null;
        _selectedPessoaDetails = null;
        SetPessoaDocumentoSelection(null);
        ClearPessoaForm();
        SetPessoaEditMode(true, isNew: true);
    }

    private void EditPessoaButton_Click(object sender, RoutedEventArgs e) =>
        SetPessoaEditMode(true, isNew: false);

    private void CancelPessoaEditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPessoaDetails is not null)
        {
            PopulatePessoaForm(_selectedPessoaDetails);
            SetPessoaEditMode(false, isNew: false);
        }
    }

    private async void DeactivatePessoaButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedPessoaId.HasValue)
        {
            return;
        }

        var confirm = MessageBox.Show(
            "Remover esta pessoa apenas altera o status para inativo. Deseja continuar?",
            "Confirmar remoção",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var password = PromptPassword("Digite sua senha para confirmar a remoção.");
        if (password is null)
        {
            return;
        }

        if (!await _userService.VerifyPasswordAsync(_currentUser.Id, password))
        {
            PessoaErrorText.Text = "Senha incorreta. A pessoa não foi removida.";
            return;
        }

        await _rentalManagementService.SetPessoaActiveAsync(_selectedPessoaId.Value, false);
        _selectedPessoaId = null;
        _selectedPessoaDetails = null;
        ClearPessoaForm();
        SetPessoaDocumentoSelection(null);
        SetPessoaEditMode(true, isNew: true);
        await LoadPessoasAsync();
    }

    private static string? PromptPassword(string message)
    {
        var window = new Window
        {
            Title = "Confirmação",
            Width = 360,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize
        };
        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) });
        var passwordBox = new PasswordBox { Margin = new Thickness(0, 0, 0, 14) };
        panel.Children.Add(passwordBox);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var ok = new Button { Content = "Confirmar", Width = 92, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new Button { Content = "Cancelar", Width = 82, IsCancel = true };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        ok.Click += (_, _) => window.DialogResult = true;
        window.Content = panel;
        return window.ShowDialog() == true ? passwordBox.Password : null;
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

    private static readonly IReadOnlyList<PessoaDocumentoDonoOption> PessoaDocumentoDonoOptions =
    [
        new("Pessoa", "pessoa"),
        new("Cônjuge", "conjuge"),
        new("Empresa onde trabalha", "empresa_trabalho")
    ];

    private static readonly IReadOnlyList<PessoaStatusFilterOption> PessoaStatusFilterOptions =
    [
        new("Ativos", "ativo"),
        new("Inativos", "inativo"),
        new("Todos", "todos")
    ];
}
