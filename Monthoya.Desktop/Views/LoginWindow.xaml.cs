using System.Windows;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly IAuthService _authService;

    public LoginWindow(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
    }

    public AuthenticatedUser? AuthenticatedUser { get; private set; }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;

        var result = await _authService.SignInAsync(EmailBox.Text, PasswordBox.Password);
        if (!result.Succeeded || result.User is null)
        {
            ErrorText.Text = result.ErrorMessage ?? "E-mail ou senha invalidos.";
            return;
        }

        AuthenticatedUser = result.User;
        DialogResult = true;
    }
}
