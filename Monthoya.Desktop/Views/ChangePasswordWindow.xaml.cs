using System.Windows;
using System.Windows.Input;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly IUserPasswordService _service;
    private readonly AuthenticatedUser _user;

    public ChangePasswordWindow(IUserPasswordService service, AuthenticatedUser user)
    {
        InitializeComponent();
        _service = service;
        _user = user;
        Loaded += (_, _) =>
        {
            CurrentBox.Focus();
            Keyboard.Focus(CurrentBox);
        };
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Text = string.Empty;

        try
        {
            var request = new ChangeUserPasswordRequest(
                _user.Id,
                CurrentBox.Password,
                NewBox.Password,
                ConfirmBox.Password);

            await _service.ChangePasswordAsync(request);
            MessageBox.Show(this, "Senha alterada com sucesso.", "Alterar senha", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ErrorText.Text = ex.Message;
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
