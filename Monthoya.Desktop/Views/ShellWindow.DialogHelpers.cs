using System.Windows;
using System.Windows.Controls;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
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
}
