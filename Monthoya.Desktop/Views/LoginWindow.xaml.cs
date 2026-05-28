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

    private void LoginTopDragArea_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        BeginWindowDrag(e);
    }

    private void BeginWindowDrag(MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // Windows may have already ended the mouse operation.
        }
    }

    private void TitleBarMinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void TitleBarCloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

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
