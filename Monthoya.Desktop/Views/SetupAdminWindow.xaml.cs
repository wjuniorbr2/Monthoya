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

        var displayName = NameBox.Text.Trim();
        var loginName = LoginNameBox.Text.Trim();
        var email = EmailBox.Text.Trim();
        var password = PasswordBox.Password;
        var confirmPassword = ConfirmPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(displayName))
        {
            ErrorText.Text = "Informe o nome do administrador.";
            NameBox.Focus();
            return;
        }

        if (loginName.Length < 3)
        {
            ErrorText.Text = "Informe um login com pelo menos 3 caracteres no campo Login.";
            LoginNameBox.Focus();
            return;
        }

        if (loginName.Any(char.IsWhiteSpace))
        {
            ErrorText.Text = "O login não pode conter espaços. Use um apelido curto, por exemplo: junior.";
            LoginNameBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            ErrorText.Text = "Informe um e-mail válido.";
            EmailBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            ErrorText.Text = "A senha deve ter pelo menos 8 caracteres.";
            PasswordBox.Focus();
            return;
        }

        if (password != confirmPassword)
        {
            ErrorText.Text = "As senhas não conferem.";
            ConfirmPasswordBox.Focus();
            return;
        }

        try
        {
            var result = await _authService.CreateFirstAdminAsync(
                new CreateUserRequest(displayName, loginName, email, password, UserRole.Administrador));

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
