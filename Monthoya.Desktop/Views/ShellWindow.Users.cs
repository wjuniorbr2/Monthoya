using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Entities;
using Monthoya.Core.Security;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{

    private async Task LoadUsersAsync()
    {
        UsersGrid.ItemsSource = await _userService.GetUsersAsync();
    }

    private void NewUserButton_Click(object sender, RoutedEventArgs e)
    {
        ClearUserForm();
        SaveActiveTabState();
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
        SaveActiveTabState();
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
        SaveActiveTabState();
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

    private UsersPageState CaptureUsersPageState() =>
        new(
            TryGetItemId(UsersGrid.SelectedItem),
            _editingUserId,
            UserNameBox.Text,
            UserLoginNameBox.Text,
            UserEmailBox.Text,
            UserRoleBox.SelectedValue is UserRole role ? role : UserRole.Usuario,
            UserManagementAccessBox.IsChecked == true,
            UserErrorText.Text,
            ToggleUserActiveButton.Content?.ToString() ?? "Ativar/Desativar");

    private Task RestoreUsersPageStateAsync(UsersPageState state)
    {
        RestoreDataGridSelection(UsersGrid, state.SelectedUserId);
        _editingUserId = state.EditingUserId;
        UserNameBox.Text = state.DisplayName;
        UserLoginNameBox.Text = state.LoginName;
        UserEmailBox.Text = state.Email;
        UserPasswordBox.Clear();
        UserRoleBox.SelectedValue = state.Role;
        UserManagementAccessBox.IsChecked = state.CanManageUsers;
        UpdateAccessControlState();
        UserErrorText.Text = state.ErrorText;
        ToggleUserActiveButton.Content = state.ToggleButtonText;
        return Task.CompletedTask;
    }

    private sealed record UsersPageState(
        Guid? SelectedUserId,
        Guid? EditingUserId,
        string DisplayName,
        string LoginName,
        string Email,
        UserRole Role,
        bool CanManageUsers,
        string ErrorText,
        string ToggleButtonText) : IShellPageState
    {
        public static UsersPageState Default { get; } = new(
            null,
            null,
            "",
            "",
            "",
            UserRole.Usuario,
            false,
            "",
            "Ativar/Desativar");
    }
}


