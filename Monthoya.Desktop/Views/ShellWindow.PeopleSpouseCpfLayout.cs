using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool PeopleSpouseCpfLayoutRegistered = RegisterPeopleSpouseCpfLayout();

    private static bool RegisterPeopleSpouseCpfLayout()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForPeopleSpouseCpfLayout));

        return true;
    }

    private static void OnShellWindowLoadedForPeopleSpouseCpfLayout(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyPeopleSpouseCpfLayoutFix, DispatcherPriority.ApplicationIdle);
        }
    }

    private void ApplyPeopleSpouseCpfLayoutFix()
    {
        PessoaConjugeCpfBox.Margin = new Thickness(0, 10, 0, 0);
        PessoaConjugeCpfBox.VerticalAlignment = VerticalAlignment.Top;

        if (PessoaConjugeCpfBox.Parent is not StackPanel cpfPanel)
        {
            return;
        }

        cpfPanel.MinHeight = 72;
        cpfPanel.Margin = new Thickness(
            cpfPanel.Margin.Left,
            cpfPanel.Margin.Top,
            cpfPanel.Margin.Right,
            Math.Max(cpfPanel.Margin.Bottom, 12));

        var label = cpfPanel.Children.OfType<TextBlock>().FirstOrDefault();
        if (label is not null)
        {
            label.Text = "CPF";
            label.Visibility = Visibility.Visible;
            label.Margin = new Thickness(0, 0, 0, 0);
            Panel.SetZIndex(label, 1);
        }

        Panel.SetZIndex(PessoaConjugeCpfBox, 0);
    }
}
