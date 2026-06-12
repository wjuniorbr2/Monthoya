using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private void ShowLocacaoSelectionDetails()
    {
        if (ModuleGrid.SelectedItem is LocacaoSummary locacao)
        {
            ShowLocacaoDetails(locacao);
            return;
        }

        if (_activeModulePage == ShellPage.Locacoes)
        {
            ModuleDetailsBorder.Visibility = Visibility.Visible;
            ModuleDetailsHost.Content = new TextBlock
            {
                Text = "Selecione uma locação na lista ou clique em Nova locação.",
                Foreground = System.Windows.Media.Brushes.DimGray,
                TextWrapping = TextWrapping.Wrap
            };
        }
    }
}
