using System.Windows;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ChavesReturnButtonStateRegistered = RegisterChavesReturnButtonState();
    private bool _chavesReturnButtonStateApplied;

    private static bool RegisterChavesReturnButtonState()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForChavesReturnButtonState));

        return true;
    }

    private static void OnShellWindowLoadedForChavesReturnButtonState(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyChavesReturnButtonState, DispatcherPriority.ApplicationIdle);
        }
    }

    private void ApplyChavesReturnButtonState()
    {
        if (_chavesReturnButtonStateApplied)
        {
            return;
        }

        _chavesReturnButtonStateApplied = true;
        ChavesDevolvidoParaBox.TextChanged += (_, _) => UpdateSelectedChaveMovement();
        UpdateSelectedChaveMovement();
    }
}
