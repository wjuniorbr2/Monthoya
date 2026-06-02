using System.Windows;
using System.Windows.Input;
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

        // This runs before the normal Imóveis navigation Click handler. It prevents a fresh tab
        // from inheriting the previously selected house through the shared ShellWindow fields.
        ImoveisNavButton.PreviewMouseLeftButtonDown += (_, _) => ResetImoveisIfActiveTabHasNoImoveisState();
        ImoveisNavButton.PreviewKeyDown += (_, e) =>
        {
            if (e.Key is Key.Enter or Key.Space)
            {
                ResetImoveisIfActiveTabHasNoImoveisState();
            }
        };
    }

    private void ResetImoveisIfActiveTabHasNoImoveisState()
    {
        if (_activeTab is null)
        {
            return;
        }

        // Keep the selected house when this tab already has its own Imóveis state.
        if (_activeTab.Page == ShellPage.Imoveis || _activeTab.PageStates.ContainsKey(ShellPage.Imoveis))
        {
            return;
        }

        ResetImoveisSharedStateForFreshTab();
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
