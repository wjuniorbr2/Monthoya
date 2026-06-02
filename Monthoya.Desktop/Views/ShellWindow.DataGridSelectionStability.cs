using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool DataGridSelectionStabilityRegistered = RegisterDataGridSelectionStability();
    private bool _dataGridSelectionStabilityApplied;

    private static bool RegisterDataGridSelectionStability()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForDataGridSelectionStability));

        return true;
    }

    private static void OnShellWindowLoadedForDataGridSelectionStability(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyDataGridSelectionStability, DispatcherPriority.ContextIdle);
        }
    }

    private void ApplyDataGridSelectionStability()
    {
        if (_dataGridSelectionStabilityApplied)
        {
            return;
        }

        _dataGridSelectionStabilityApplied = true;
        StabilizeMainSelectionGrid(PessoasGrid);
        StabilizeMainSelectionGrid(ImoveisGrid);
    }

    private static void StabilizeMainSelectionGrid(DataGrid grid)
    {
        grid.SelectionMode = DataGridSelectionMode.Single;
        grid.SelectionUnit = DataGridSelectionUnit.FullRow;

        // WPF normally paints a selected row gray when the DataGrid loses keyboard focus.
        // For these master lists, that makes users think the item was deselected.
        // Keep the selected row visually active even when focus moves to the detail form.
        grid.Resources[SystemColors.InactiveSelectionHighlightBrushKey] = SystemColors.HighlightBrush;
        grid.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey] = SystemColors.HighlightTextBrush;
        grid.Resources[SystemColors.ControlBrushKey] = SystemColors.HighlightBrush;
    }
}
