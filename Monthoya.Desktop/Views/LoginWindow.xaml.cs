using System.Windows;
using System.Windows.Input;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly IAuthService _authService;

    public LoginWindow(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        Loaded += (_, _) =>
        {
            LoginNameBox.Focus();
            Keyboard.Focus(LoginNameBox);
        };
    }

    public AuthenticatedUser? AuthenticatedUser { get; private set; }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;

        var result = await _authService.SignInAsync(LoginNameBox.Text, PasswordBox.Password);
        if (!result.Succeeded || result.User is null)
        {
            ErrorText.Text = result.ErrorMessage ?? "Login ou senha inválidos.";
            return;
        }

        AuthenticatedUser = result.User;
        DialogResult = true;
    }
}
