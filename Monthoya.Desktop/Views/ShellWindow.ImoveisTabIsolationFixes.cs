using System.Windows;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ImoveisTabIsolationRegistered = RegisterImoveisTabIsolation();
    private bool _imoveisTabIsolationApplied;

    private static bool RegisterImoveisTabIsolation()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForImoveisTabIsolation));

        return true;
    }

    private static void OnShellWindowLoadedForImoveisTabIsolation(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyImoveisTabIsolation, DispatcherPriority.ContextIdle);
        }
    }

    private void ApplyImoveisTabIsolation()
    {
        if (_imoveisTabIsolationApplied)
        {
            return;
        }

        _imoveisTabIsolationApplied = true;
        RegisterFreshTabReset(ImoveisNavButton, ShellPage.Imoveis, ResetImoveisSharedStateForFreshTab);
    }

    private void ResetImoveisSharedStateForFreshTab()
    {
        _selectedImovelId = null;
        _selectedImovelDetails = null;
        _pendingImovelMedia.Clear();
        _imovelImagens = [];
        _imovelVistorias = [];

        ImoveisGrid.SelectedItem = null;
        ImovelDetailsTabControl.SelectedIndex = 0;
        ClearImovelForm();
        SetImovelEditMode(true, isNew: true);
        ImovelDetailsTabControl.SelectedIndex = 0;
        ImovelVistoriasGrid.ItemsSource = _imovelVistorias;
        RefreshImovelMediaGrid();
    }
}
