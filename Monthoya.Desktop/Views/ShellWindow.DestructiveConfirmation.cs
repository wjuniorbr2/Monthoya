using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private async Task<bool> ConfirmDestructiveActionWithPasswordAsync(
        string title,
        string message,
        string confirmButtonText = "Confirmar remoção")
    {
        var dialog = new Window
        {
            Title = title,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            SizeToContent = SizeToContent.WidthAndHeight,
            MinWidth = 420,
            Background = Brushes.White,
            ShowInTaskbar = false
        };

        var root = new StackPanel
        {
            Margin = new Thickness(22),
            Width = 420
        };

        root.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(20, 35, 60)),
            Margin = new Thickness(0, 0, 0, 8)
        });

        root.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 14)
        });

        root.Children.Add(new TextBlock
        {
            Text = "Digite sua senha para confirmar:",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });

        var passwordBox = new PasswordBox
        {
            Margin = new Thickness(0, 0, 0, 8),
            MinHeight = 34
        };
        root.Children.Add(passwordBox);

        var errorText = new TextBlock
        {
            Foreground = Brushes.Firebrick,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        };
        root.Children.Add(errorText);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var confirmButton = new Button
        {
            Content = confirmButtonText,
            Style = (Style)FindResource("PrimaryButtonSmall"),
            MinWidth = 130,
            Margin = new Thickness(0, 0, 8, 0),
            IsDefault = true
        };

        var cancelButton = new Button
        {
            Content = "Cancelar",
            Style = (Style)FindResource("SecondaryButton"),
            MinWidth = 100,
            IsCancel = true
        };

        buttons.Children.Add(confirmButton);
        buttons.Children.Add(cancelButton);
        root.Children.Add(buttons);

        var confirmed = false;
        confirmButton.Click += async (_, _) =>
        {
            errorText.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(passwordBox.Password))
            {
                errorText.Text = "Digite a senha para confirmar.";
                passwordBox.Focus();
                return;
            }

            confirmButton.IsEnabled = false;
            cancelButton.IsEnabled = false;
            try
            {
                var passwordOk = await _userService.VerifyPasswordAsync(_currentUser.Id, passwordBox.Password);
                if (!passwordOk)
                {
                    errorText.Text = "Senha incorreta. A remoção não foi executada.";
                    passwordBox.Clear();
                    passwordBox.Focus();
                    confirmButton.IsEnabled = true;
                    cancelButton.IsEnabled = true;
                    return;
                }

                confirmed = true;
                dialog.DialogResult = true;
                dialog.Close();
            }
            catch (Exception ex)
            {
                errorText.Text = $"Não foi possível confirmar a senha. {ex.Message}";
                confirmButton.IsEnabled = true;
                cancelButton.IsEnabled = true;
            }
        };

        cancelButton.Click += (_, _) => dialog.Close();
        passwordBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                confirmButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        };

        dialog.Content = root;
        dialog.Loaded += (_, _) => passwordBox.Focus();
        dialog.ShowDialog();
        return confirmed;
    }
}
