using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _sidebarExitButtonBehaviorApplied;
    private static readonly bool SidebarExitButtonBehaviorRegistered = RegisterSidebarExitButtonBehavior();

    private static bool RegisterSidebarExitButtonBehavior()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler((sender, _) => ((ShellWindow)sender).ApplySidebarExitButtonBehavior()));

        return true;
    }

    private void ApplySidebarExitButtonBehavior()
    {
        _ = SidebarExitButtonBehaviorRegistered;

        if (_sidebarExitButtonBehaviorApplied)
        {
            return;
        }

        _sidebarExitButtonBehaviorApplied = true;
        var exitButton = FindSidebarExitButton(this);
        if (exitButton is null)
        {
            return;
        }

        exitButton.PreviewMouseLeftButtonDown += ExitApplicationButton_PreviewMouseLeftButtonDown;
    }

    private void ExitApplicationButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        var result = MessageBox.Show(
            this,
            "Deseja fechar o Monthoya?",
            "Fechar Monthoya",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        IsLogoutRequested = false;
        Application.Current.Shutdown();
    }

    private static Button? FindSidebarExitButton(DependencyObject parent)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is Button button && ButtonContainsText(button, "Sair"))
            {
                return button;
            }

            var descendant = FindSidebarExitButton(child);
            if (descendant is not null)
            {
                return descendant;
            }
        }

        return null;
    }

    private static bool ButtonContainsText(Button button, string text)
    {
        if (button.Content is TextBlock textBlock)
        {
            return string.Equals(textBlock.Text?.Trim(), text, StringComparison.OrdinalIgnoreCase);
        }

        if (button.Content is DependencyObject contentObject)
        {
            return FindVisualChildren<TextBlock>(contentObject)
                .Any(block => string.Equals(block.Text?.Trim(), text, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }
}
