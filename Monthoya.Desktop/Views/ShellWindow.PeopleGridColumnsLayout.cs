using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool PeopleGridColumnsLayoutRegistered = RegisterPeopleGridColumnsLayout();

    private static bool RegisterPeopleGridColumnsLayout()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForPeopleGridColumnsLayout));

        return true;
    }

    private static void OnShellWindowLoadedForPeopleGridColumnsLayout(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyPeopleGridColumnsLayoutFix, DispatcherPriority.ApplicationIdle);
        }
    }

    private void ApplyPeopleGridColumnsLayoutFix()
    {
        foreach (var column in PessoasGrid.Columns)
        {
            switch (column.Header?.ToString())
            {
                case "Nome":
                    column.Width = new DataGridLength(2.0, DataGridLengthUnitType.Star);
                    break;
                case "Tipo":
                    column.Width = new DataGridLength(0.85, DataGridLengthUnitType.Star);
                    break;
                case "Funções":
                    column.Width = new DataGridLength(1.35, DataGridLengthUnitType.Star);
                    break;
                case "CPF/CNPJ":
                    column.Width = new DataGridLength(1.45, DataGridLengthUnitType.Star);
                    break;
                case "Telefone":
                    column.Width = new DataGridLength(1.05, DataGridLengthUnitType.Star);
                    break;
                case "E-mail":
                    column.Width = new DataGridLength(2.1, DataGridLengthUnitType.Star);
                    break;
                case "Status":
                    column.Width = new DataGridLength(0.75, DataGridLengthUnitType.Star);
                    break;
            }
        }
    }
}
