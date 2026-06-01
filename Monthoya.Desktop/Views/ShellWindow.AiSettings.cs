using System.Windows;
using System.Windows.Controls;
using Monthoya.Desktop.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private void ShowAiSettingsDialog()
    {
        var current = LocalAiSettingsStore.Load();

        var window = new Window
        {
            Title = "Configurações de IA",
            Owner = this,
            Width = 560,
            Height = 430,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize
        };

        var root = new StackPanel
        {
            Margin = new Thickness(22)
        };

        root.Children.Add(new TextBlock
        {
            Text = "OCR inteligente / Gemini",
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        root.Children.Add(new TextBlock
        {
            Text = "Informe a chave da API Gemini usada para ler documentos com IA. A chave fica salva somente neste computador, fora do GitHub.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = TryFindResource("MutedBrush") as System.Windows.Media.Brush,
            Margin = new Thickness(0, 0, 0, 18)
        });

        root.Children.Add(new TextBlock
        {
            Text = "Chave da API Gemini",
            FontWeight = FontWeights.SemiBold
        });

        var keyBox = new PasswordBox
        {
            Margin = new Thickness(0, 6, 0, 12),
            Password = current.GeminiApiKey ?? string.Empty
        };
        root.Children.Add(keyBox);

        root.Children.Add(new TextBlock
        {
            Text = "Modelo Gemini",
            FontWeight = FontWeights.SemiBold
        });

        var modelBox = new TextBox
        {
            Margin = new Thickness(0, 6, 0, 12),
            Text = string.IsNullOrWhiteSpace(current.GeminiModel) ? "gemini-2.0-flash" : current.GeminiModel
        };
        root.Children.Add(modelBox);

        var statusText = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            Foreground = TryFindResource("MutedBrush") as System.Windows.Media.Brush,
            Margin = new Thickness(0, 0, 0, 16),
            Text = LocalAiSettingsStore.HasGeminiApiKey()
                ? "Status: chave configurada."
                : "Status: chave não configurada."
        };
        root.Children.Add(statusText);

        root.Children.Add(new TextBlock
        {
            Text = "Atenção: documentos enviados por OCR inteligente são processados pelo provedor configurado. Em produção, use uma chave da própria empresa/cliente.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = TryFindResource("MutedBrush") as System.Windows.Media.Brush,
            Margin = new Thickness(0, 0, 0, 18)
        });

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var saveButton = new Button
        {
            Content = "Salvar",
            MinWidth = 110,
            Margin = new Thickness(0, 0, 8, 0),
            Style = TryFindResource("PrimaryButton") as Style
        };
        saveButton.Click += (_, _) =>
        {
            LocalAiSettingsStore.Save(new LocalAiSettings(keyBox.Password, modelBox.Text));
            statusText.Text = "Status: chave salva neste computador.";
            MessageBox.Show(window, "Configurações de IA salvas.", "Monthoya", MessageBoxButton.OK, MessageBoxImage.Information);
            window.DialogResult = true;
            window.Close();
        };

        var testButton = new Button
        {
            Content = "Testar configuração",
            MinWidth = 150,
            Margin = new Thickness(0, 0, 8, 0),
            Style = TryFindResource("SecondaryButton") as Style
        };
        testButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(keyBox.Password))
            {
                MessageBox.Show(window, "Informe a chave da API Gemini primeiro.", "Monthoya", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            statusText.Text = "Status: chave informada. O teste real será feito ao usar dados de um documento.";
        };

        var cancelButton = new Button
        {
            Content = "Cancelar",
            MinWidth = 100,
            Style = TryFindResource("SecondaryButton") as Style
        };
        cancelButton.Click += (_, _) => window.Close();

        buttons.Children.Add(saveButton);
        buttons.Children.Add(testButton);
        buttons.Children.Add(cancelButton);
        root.Children.Add(buttons);

        window.Content = root;
        window.ShowDialog();
    }
}
