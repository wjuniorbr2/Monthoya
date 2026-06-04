using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private async void ShowEmailSettingsDialog()
    {
        using var scope = CreateSettingsServiceScope("E-mail de envio");
        if (scope is null)
        {
            return;
        }

        var service = scope.ServiceProvider.GetRequiredService<INotificationEmailSettingsService>();
        var current = await service.GetAsync();

        var window = new Window
        {
            Title = "E-mail de envio",
            Owner = this,
            Width = 680,
            Height = 680,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize
        };

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var root = new StackPanel { Margin = new Thickness(22) };
        scroll.Content = root;

        root.Children.Add(new TextBlock
        {
            Text = "E-mail de envio",
            FontSize = 22,
            FontWeight = FontWeights.SemiBold
        });
        root.Children.Add(new TextBlock
        {
            Text = "Use uma senha de aplicativo quando o provedor exigir. A senha salva não é exibida de volta nesta tela.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = TryFindResource("MutedBrush") as System.Windows.Media.Brush,
            Margin = new Thickness(0, 6, 0, 16)
        });

        var enabledBox = new CheckBox { Content = "Habilitar envio de e-mail", IsChecked = current.IsEnabled, Margin = new Thickness(0, 0, 0, 12) };
        root.Children.Add(enabledBox);

        var senderNameBox = AddLabeledTextBox(root, "Nome do remetente", current.SenderDisplayName);
        var senderEmailBox = AddLabeledTextBox(root, "E-mail do remetente", current.SenderEmail);
        var hostBox = AddLabeledTextBox(root, "Servidor SMTP", current.SmtpHost);
        var portBox = AddLabeledTextBox(root, "Porta SMTP", current.SmtpPort.ToString());
        var tlsBox = new CheckBox { Content = "Usar SSL/TLS", IsChecked = current.UseSslTls, Margin = new Thickness(0, 0, 0, 12) };
        root.Children.Add(tlsBox);
        var usernameBox = AddLabeledTextBox(root, "Usuário SMTP", current.SmtpUsername);

        root.Children.Add(new TextBlock { Text = "Senha/app password SMTP", FontWeight = FontWeights.SemiBold });
        var passwordBox = new PasswordBox { Margin = new Thickness(0, 6, 0, 4) };
        root.Children.Add(passwordBox);
        root.Children.Add(new TextBlock
        {
            Text = current.HasPassword ? "Senha configurada. Digite uma nova apenas se quiser substituir." : "Nenhuma senha configurada.",
            Foreground = TryFindResource("MutedBrush") as System.Windows.Media.Brush,
            Margin = new Thickness(0, 0, 0, 12)
        });

        var replyToBox = AddLabeledTextBox(root, "Reply-to opcional", current.ReplyToEmail);
        var testEmailBox = AddLabeledTextBox(root, "E-mail para teste", current.SenderEmail);

        root.Children.Add(new TextBlock
        {
            Text = "Gmail: smtp.gmail.com, porta 587, TLS ativo, use senha de aplicativo. Outlook/Microsoft 365: smtp.office365.com, porta 587, TLS ativo; SMTP AUTH pode precisar estar habilitado.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = TryFindResource("MutedBrush") as System.Windows.Media.Brush,
            Margin = new Thickness(0, 0, 0, 18)
        });

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var saveButton = new Button { Content = "Salvar", MinWidth = 110, Style = TryFindResource("PrimaryButton") as Style, Margin = new Thickness(0, 0, 8, 0) };
        var testButton = new Button { Content = "Testar envio", MinWidth = 120, Style = TryFindResource("SecondaryButton") as Style, Margin = new Thickness(0, 0, 8, 0) };
        var cancelButton = new Button { Content = "Cancelar", MinWidth = 100, Style = TryFindResource("SecondaryButton") as Style };

        saveButton.Click += async (_, _) =>
        {
            try
            {
                await SaveEmailSettingsAsync(service, enabledBox, senderNameBox, senderEmailBox, hostBox, portBox, tlsBox, usernameBox, passwordBox, replyToBox);
                MessageBox.Show(window, "Configurações de e-mail salvas.", "E-mail de envio", MessageBoxButton.OK, MessageBoxImage.Information);
                window.DialogResult = true;
                window.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(window, ex.GetBaseException().Message, "E-mail de envio", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };

        testButton.Click += async (_, _) =>
        {
            try
            {
                await SaveEmailSettingsAsync(service, enabledBox, senderNameBox, senderEmailBox, hostBox, portBox, tlsBox, usernameBox, passwordBox, replyToBox);
                var result = await service.SendTestAsync(testEmailBox.Text);
                var message = result.Sent ? "E-mail de teste enviado." : result.ErrorMessage ?? "Não foi possível enviar o teste.";
                MessageBox.Show(window, message, "Teste de e-mail", MessageBoxButton.OK, result.Sent ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(window, ex.GetBaseException().Message, "Teste de e-mail", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };

        cancelButton.Click += (_, _) => window.Close();
        buttons.Children.Add(saveButton);
        buttons.Children.Add(testButton);
        buttons.Children.Add(cancelButton);
        root.Children.Add(buttons);
        window.Content = scroll;
        window.ShowDialog();
    }

    private static async Task SaveEmailSettingsAsync(
        INotificationEmailSettingsService service,
        CheckBox enabledBox,
        TextBox senderNameBox,
        TextBox senderEmailBox,
        TextBox hostBox,
        TextBox portBox,
        CheckBox tlsBox,
        TextBox usernameBox,
        PasswordBox passwordBox,
        TextBox replyToBox)
    {
        var port = int.TryParse(portBox.Text, out var parsedPort) ? parsedPort : 587;
        await service.SaveAsync(new SaveNotificationEmailSettingsRequest(
            enabledBox.IsChecked == true,
            senderNameBox.Text,
            senderEmailBox.Text,
            hostBox.Text,
            port,
            tlsBox.IsChecked == true,
            usernameBox.Text,
            passwordBox.Password,
            replyToBox.Text));
    }

    private static TextBox AddLabeledTextBox(StackPanel root, string label, string? value)
    {
        root.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold });
        var textBox = new TextBox
        {
            Text = value ?? string.Empty,
            Margin = new Thickness(0, 6, 0, 12)
        };
        root.Children.Add(textBox);
        return textBox;
    }
}
