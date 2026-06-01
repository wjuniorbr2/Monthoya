using System.Windows;

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
        LogoutButton.Click -= LogoutButton_Click;
        LogoutButton.Click += ExitApplicationButton_Click;
    }

    private void ExitApplicationButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            this,
            "Deseja fechar o Monthoya?",
            "Sair do Monthoya",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        IsLogoutRequested = false;
        Application.Current.Shutdown();
    }
}
