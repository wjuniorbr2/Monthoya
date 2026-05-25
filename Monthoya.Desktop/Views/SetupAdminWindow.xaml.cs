using System.Windows;
using System.Windows.Input;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class SetupAdminWindow : Window
{
    private readonly IAuthService _authService;

    public SetupAdminWindow(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        Loaded += (_, _) =>
        {
            NameBox.Focus();
            Keyboard.Focus(NameBox);
        };
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;

        if (PasswordBox.Password != ConfirmPasswordBox.Password)
        {
            ErrorText.Text = "As senhas não conferem.";
            return;
        }

        try
        {
            var result = await _authService.CreateFirstAdminAsync(
                new CreateUserRequest(NameBox.Text, LoginNameBox.Text, EmailBox.Text, PasswordBox.Password, UserRole.Administrador));

            if (!result.Succeeded)
            {
                ErrorText.Text = result.ErrorMessage ?? "Não foi possível criar o administrador.";
                return;
            }

            DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
        }
    }
}
