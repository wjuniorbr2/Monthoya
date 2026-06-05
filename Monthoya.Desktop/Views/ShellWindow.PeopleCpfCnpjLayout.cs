using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool PeopleCpfCnpjLayoutRegistered = RegisterPeopleCpfCnpjLayout();

    private static bool RegisterPeopleCpfCnpjLayout()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForPeopleCpfCnpjLayout));

        return true;
    }

    private static void OnShellWindowLoadedForPeopleCpfCnpjLayout(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyPeopleCpfCnpjLayoutFix, DispatcherPriority.ApplicationIdle);
        }
    }

    private void ApplyPeopleCpfCnpjLayoutFix()
    {
        // Make the top "Funções atuais" block a little narrower and use that space
        // to widen CPF/CNPJ so a formatted CNPJ fits without cutting the final digits.
        if (_pessoaRolesTopCell is not null)
        {
            _pessoaRolesTopCell.Width = 145;
            _pessoaRolesTopCell.MinWidth = 145;
            _pessoaRolesTopCell.Margin = new Thickness(
                _pessoaRolesTopCell.Margin.Left,
                _pessoaRolesTopCell.Margin.Top,
                10,
                _pessoaRolesTopCell.Margin.Bottom);
        }

        if (_pessoaRolesCell is not null)
        {
            _pessoaRolesCell.Width = 145;
            _pessoaRolesCell.MinWidth = 145;
        }

        if (PessoaDocumentoBox.Parent is StackPanel documentoPanel)
        {
            documentoPanel.Width = 205;
            documentoPanel.MinWidth = 205;
        }

        PessoaDocumentoBox.Width = 205;
        PessoaDocumentoBox.MinWidth = 205;
        PessoaDocumentoBox.HorizontalAlignment = HorizontalAlignment.Left;
    }
}
