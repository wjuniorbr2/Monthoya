using System.Globalization;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Entities;
using Monthoya.Core.Security;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow : Window
{
    private readonly AuthenticatedUser _currentUser;
    private readonly IUserService _userService;
    private readonly IDashboardService _dashboardService;
    private Guid? _editingUserId;

    public ShellWindow(
        AuthenticatedUser currentUser,
        IUserService userService,
        IDashboardService dashboardService)
    {
        InitializeComponent();
        _currentUser = currentUser;
        _userService = userService;
        _dashboardService = dashboardService;

        CurrentUserText.Text = currentUser.DisplayName;
        CurrentRoleText.Text = currentUser.Role.ToString();
        UsersNavButton.Visibility = RolePermissions.CanManageUsers(currentUser.Role) ? Visibility.Visible : Visibility.Collapsed;
        DiagnosticsNavButton.Visibility = RolePermissions.CanAccessDiagnostics(currentUser.Role) ? Visibility.Visible : Visibility.Collapsed;
        UserRoleBox.ItemsSource = Enum.GetValues<UserRole>();
        UserRoleBox.SelectedItem = UserRole.Usuario;
        SetAccessCheckboxes(RolePermissions.DefaultUserAccess);
        UpdateAccessControlState();
        DiagnosticsText.Text = $"Login: {currentUser.LoginName}{Environment.NewLine}E-mail: {currentUser.Email}{Environment.NewLine}Perfil: {currentUser.Role}{Environment.NewLine}Acessos: {RolePermissions.GetEffectiveAccess(currentUser.Role, currentUser.Access)}{Environment.NewLine}Banco: configurado via secrets/appsettings";

        Loaded += async (_, _) => await LoadDashboardAsync();
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
                ? "Nenhum imovel disponivel para locacao com coordenadas cadastrado ainda."
                : $"{summary.AvailableRentalProperties.Count} imovel(is) disponivel(is) com localizacao.";

            await LoadMapAsync(summary.AvailableRentalProperties);
        }
        catch (Exception ex)
        {
            DashboardStatusText.Text = $"Nao foi possivel carregar o dashboard: {ex.Message}";
            ShowMapFallback("Mapa indisponivel no momento.");
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
            ShowMapFallback("Nao foi possivel iniciar o WebView2. Instale o runtime do Microsoft Edge WebView2 para ver o mapa.");
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
        ShowDashboard();
        await LoadDashboardAsync();
    }

    private async void UsersNavButton_Click(object sender, RoutedEventArgs e)
    {
        if (!RolePermissions.CanManageUsers(_currentUser.Role))
        {
            return;
        }

        DashboardPanel.Visibility = Visibility.Collapsed;
        UsersPanel.Visibility = Visibility.Visible;
        DiagnosticsPanel.Visibility = Visibility.Collapsed;
        SetActiveNavigation(UsersNavButton);
        await LoadUsersAsync();
    }

    private void DiagnosticsNavButton_Click(object sender, RoutedEventArgs e)
    {
        if (!RolePermissions.CanAccessDiagnostics(_currentUser.Role))
        {
            return;
        }

        DashboardPanel.Visibility = Visibility.Collapsed;
        UsersPanel.Visibility = Visibility.Collapsed;
        DiagnosticsPanel.Visibility = Visibility.Visible;
        SetActiveNavigation(DiagnosticsNavButton);
    }

    private void SetActiveNavigation(Button activeButton)
    {
        DashboardNavButton.Style = (Style)FindResource("NavButton");
        UsersNavButton.Style = (Style)FindResource("NavButton");
        DiagnosticsNavButton.Style = (Style)FindResource("NavButton");
        activeButton.Style = (Style)FindResource("SelectedNavButton");
    }

    private async void RefreshDashboardButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadDashboardAsync();
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
        UserRoleBox.SelectedItem = selected.Role;
        SetAccessCheckboxes(RolePermissions.GetEffectiveAccess(selected.Role, selected.Access));
        UpdateAccessControlState();
        UserPasswordBox.Clear();
        ToggleUserActiveButton.Content = selected.IsActive ? "Desativar usuario" : "Reativar usuario";
    }

    private async void SaveUserButton_Click(object sender, RoutedEventArgs e)
    {
        UserErrorText.Text = string.Empty;

        try
        {
            var selectedRole = UserRoleBox.SelectedItem is UserRole role ? role : UserRole.Usuario;

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
            UserErrorText.Text = "Selecione um usuario.";
            return;
        }

        if (selected.Id == _currentUser.Id)
        {
            UserErrorText.Text = "Voce nao pode desativar o proprio usuario logado.";
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
        UserRoleBox.SelectedItem = UserRole.Usuario;
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

        if (DashboardAccessBox.IsChecked == true)
        {
            access |= UserAccess.Dashboard;
        }

        if (PropertiesAccessBox.IsChecked == true)
        {
            access |= UserAccess.Properties;
        }

        if (ContractsAccessBox.IsChecked == true)
        {
            access |= UserAccess.Contracts;
        }

        if (FinancialAccessBox.IsChecked == true)
        {
            access |= UserAccess.Financial;
        }

        if (DocumentsAccessBox.IsChecked == true)
        {
            access |= UserAccess.Documents;
        }

        return access;
    }

    private void SetAccessCheckboxes(UserAccess access)
    {
        DashboardAccessBox.IsChecked = access.HasFlag(UserAccess.Dashboard);
        PropertiesAccessBox.IsChecked = access.HasFlag(UserAccess.Properties);
        ContractsAccessBox.IsChecked = access.HasFlag(UserAccess.Contracts);
        FinancialAccessBox.IsChecked = access.HasFlag(UserAccess.Financial);
        DocumentsAccessBox.IsChecked = access.HasFlag(UserAccess.Documents);
    }

    private void UpdateAccessControlState()
    {
        if (UserRoleBox.SelectedItem is not UserRole selectedRole)
        {
            return;
        }

        var isNormalUser = selectedRole == UserRole.Usuario;
        if (!isNormalUser)
        {
            SetAccessCheckboxes(RolePermissions.GetEffectiveAccess(selectedRole, RolePermissions.DefaultUserAccess));
        }

        DashboardAccessBox.IsEnabled = isNormalUser;
        PropertiesAccessBox.IsEnabled = isNormalUser;
        ContractsAccessBox.IsEnabled = isNormalUser;
        FinancialAccessBox.IsEnabled = isNormalUser;
        DocumentsAccessBox.IsEnabled = isNormalUser;
        AccessHelpText.Text = isNormalUser
            ? "Marque apenas as areas que este usuario pode acessar."
            : "Este perfil tem acesso completo por regra do sistema.";
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private const string MapHtmlTemplate = """
<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css">
  <style>
    html, body, #map { height: 100%; margin: 0; font-family: Segoe UI, Arial, sans-serif; }
    .empty { position: absolute; z-index: 999; top: 18px; left: 18px; right: 18px; background: white; border: 1px solid #dbe8e2; border-radius: 8px; padding: 14px 16px; color: #66756f; box-shadow: 0 10px 30px rgba(20,37,33,.08); }
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
      empty.textContent = 'Nenhum imovel disponivel para locacao com coordenadas cadastrado ainda.';
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
}
