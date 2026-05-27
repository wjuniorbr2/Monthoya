using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoasRuntimeAdjustmentsApplied;

    static ShellWindow()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForPeopleRuntimeAdjustments));
    }

    private static void OnShellWindowLoadedForPeopleRuntimeAdjustments(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyPeopleRuntimeAdjustments, DispatcherPriority.ContextIdle);
        }
    }

    private void ApplyPeopleRuntimeAdjustments()
    {
        if (_pessoasRuntimeAdjustmentsApplied)
        {
            return;
        }

        _pessoasRuntimeAdjustmentsApplied = true;

        PessoasGrid.SelectionChanged += (_, _) =>
            Dispatcher.BeginInvoke(UpdatePeopleTopRowSpacingAndRolesVisibility, DispatcherPriority.Background);

        PessoasPanel.IsVisibleChanged += (_, _) =>
            Dispatcher.BeginInvoke(UpdatePeopleTopRowSpacingAndRolesVisibility, DispatcherPriority.Background);

        UpdatePeopleTopRowSpacingAndRolesVisibility();
    }

    private void UpdatePeopleTopRowSpacingAndRolesVisibility()
    {
        var hasSelectedPerson = PessoasGrid.SelectedItem is not null;
        var rolesVisibility = hasSelectedPerson ? Visibility.Visible : Visibility.Collapsed;

        if (_pessoaRolesTopCell is not null)
        {
            _pessoaRolesTopCell.Visibility = rolesVisibility;
        }

        if (_pessoaRolesCell is not null)
        {
            _pessoaRolesCell.Visibility = rolesVisibility;
        }

        var topGrid = FindPessoaTopInfoGridForRuntimeAdjustment();
        if (topGrid is null)
        {
            return;
        }

        foreach (var cell in topGrid.Children.OfType<StackPanel>())
        {
            var label = cell.Children.OfType<TextBlock>().FirstOrDefault()?.Text;
            if (label == "Funções atuais")
            {
                cell.Visibility = rolesVisibility;
            }
            else if (label == "Tipo")
            {
                cell.Margin = hasSelectedPerson ? new Thickness(18, 0, 0, 0) : new Thickness(0);
            }
        }
    }

    private Grid? FindPessoaTopInfoGridForRuntimeAdjustment()
    {
        return FindVisualChildrenForPeopleRuntimeAdjustment<Grid>(PessoasPanel)
            .FirstOrDefault(grid => grid.Tag as string == "PessoaTopInfoGrid");
    }

    private static IEnumerable<T> FindVisualChildrenForPeopleRuntimeAdjustment<T>(DependencyObject parent)
        where T : DependencyObject
    {
        var childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < childCount; index++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, index);
            if (child is T typedChild)
            {
                yield return typedChild;
            }

            foreach (var descendant in FindVisualChildrenForPeopleRuntimeAdjustment<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
