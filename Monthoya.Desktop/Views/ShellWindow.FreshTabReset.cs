using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private void RegisterFreshTabReset(ButtonBase navButton, ShellPage page, Action resetSharedState)
    {
        navButton.PreviewMouseLeftButtonDown += (_, _) => ResetPageIfActiveTabHasNoState(page, resetSharedState);
        navButton.PreviewKeyDown += (_, e) =>
        {
            if (e.Key is Key.Enter or Key.Space)
            {
                ResetPageIfActiveTabHasNoState(page, resetSharedState);
            }
        };
    }

    private void ResetPageIfActiveTabHasNoState(ShellPage page, Action resetSharedState)
    {
        if (_activeTab is null)
        {
            return;
        }

        // If the active tab is already on that page, or already has saved state for that page,
        // keep its own state. Otherwise, clear shared ShellWindow fields before the page loads.
        if (_activeTab.Page == page || _activeTab.PageStates.ContainsKey(page))
        {
            return;
        }

        resetSharedState();
    }
}
