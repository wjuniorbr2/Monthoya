using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ImoveisFastOpenRegistered = RegisterImoveisFastOpen();
    private bool _imoveisFastOpenApplied;
    private bool _isOpeningImoveisFast;

    private static bool RegisterImoveisFastOpen()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForImoveisFastOpen));

        return true;
    }

    private static void OnShellWindowLoadedForImoveisFastOpen(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyImoveisFastOpen, DispatcherPriority.ContextIdle);
        }
    }

    private void ApplyImoveisFastOpen()
    {
        if (_imoveisFastOpenApplied)
        {
            return;
        }

        _imoveisFastOpenApplied = true;
        ImoveisNavButton.PreviewMouseLeftButtonDown += ImoveisNavButton_PreviewMouseLeftButtonDownForFastOpen;
        ImoveisNavButton.PreviewKeyDown += ImoveisNavButton_PreviewKeyDownForFastOpen;
    }

    private async void ImoveisNavButton_PreviewMouseLeftButtonDownForFastOpen(object sender, MouseButtonEventArgs e)
    {
        if (_isOpeningImoveisFast)
        {
            return;
        }

        e.Handled = true;
        await OpenImoveisFastAsync();
    }

    private async void ImoveisNavButton_PreviewKeyDownForFastOpen(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Space) || _isOpeningImoveisFast)
        {
            return;
        }

        e.Handled = true;
        await OpenImoveisFastAsync();
    }

    private async Task OpenImoveisFastAsync()
    {
        if (_isOpeningImoveisFast)
        {
            return;
        }

        try
        {
            _isOpeningImoveisFast = true;
            await UpdateActiveTabAsync(ShellPage.Imoveis, "Imóveis", loadData: false);
            await LoadImoveisAsync();

            // Do not call RestoreActiveTabStateAsync again here. The list is visible after load,
            // and a delayed restore can clear a house clicked immediately after opening.
        }
        finally
        {
            _isOpeningImoveisFast = false;
        }
    }
}
