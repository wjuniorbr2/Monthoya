using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool RefreshFeedbackRegistered = RegisterRefreshFeedback();

    private static bool RegisterRefreshFeedback()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            ButtonBase.ClickEvent,
            new RoutedEventHandler(OnShellWindowRefreshButtonClicked),
            true);

        return true;
    }

    private static void OnShellWindowRefreshButtonClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not ShellWindow window || e.OriginalSource is not Button button)
        {
            return;
        }

        if (button.Content is not string content || !string.Equals(content, "Atualizar", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        window.Dispatcher.BeginInvoke(() => window.ShowRefreshButtonFeedback(button), DispatcherPriority.Background);
    }

    private async void ShowRefreshButtonFeedback(Button button)
    {
        if (button.Tag as string == "RefreshFeedbackRunning")
        {
            return;
        }

        button.Tag = "RefreshFeedbackRunning";
        var originalContent = button.Content;
        var wasEnabled = button.IsEnabled;

        try
        {
            button.Content = "Atualizando...";
            button.IsEnabled = false;
            await Task.Delay(700);
            button.Content = "Atualizado";
            await Task.Delay(700);
        }
        finally
        {
            button.Content = originalContent;
            button.IsEnabled = wasEnabled;
            button.Tag = null;
        }
    }
}
