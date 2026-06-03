using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ChavesExtraAdjustmentsRegistered = RegisterChavesExtraAdjustments();
    private bool _chavesExtraAdjustmentsApplied;

    private static bool RegisterChavesExtraAdjustments()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForChavesExtraAdjustments));

        return true;
    }

    private static void OnShellWindowLoadedForChavesExtraAdjustments(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyChavesExtraAdjustments, DispatcherPriority.ApplicationIdle);
        }
    }

    private void ApplyChavesExtraAdjustments()
    {
        if (_chavesExtraAdjustmentsApplied)
        {
            return;
        }

        _chavesExtraAdjustmentsApplied = true;

        ChavesStatusFilterBox.Visibility = Visibility.Collapsed;
        CollapseImmediateLabelBefore(ChavesStatusFilterBox);
        CollapseFieldContainer(ChavesCodigoBox);

        ChavesRetiradoPorTelefoneBox.Width = 120;
        ChavesMotivoBox.Width = 230;
        ChavesObservacoesBox.Width = 300;

        ChavesGrid.SelectionChanged += (_, _) => UpdateChavesBoardCodeDisplayFromSelection();
        ChavesPanel.IsVisibleChanged += (_, _) => Dispatcher.BeginInvoke(UpdateChavesBoardCodeDisplayFromSelection, DispatcherPriority.Background);

        if (_chavesRelacaoComboBox is not null)
        {
            _chavesRelacaoComboBox.Width = 125;
        }
    }

    private void UpdateChavesBoardCodeDisplayFromSelection()
    {
        if (_chavesSelectedImovelText is null)
        {
            return;
        }

        if (ChavesGrid.SelectedItem is not ChavesListItem item)
        {
            _chavesSelectedImovelText.Text = string.Empty;
            return;
        }

        var codigo = string.IsNullOrWhiteSpace(item.ChaveCodigo) ? "-" : item.ChaveCodigo;
        _chavesSelectedImovelText.Text = $"Código: {codigo} | Imóvel: {item.Imovel} | Proprietário: {item.Proprietario}";
    }

    private static void CollapseImmediateLabelBefore(FrameworkElement field)
    {
        if (field.Parent is not Panel panel)
        {
            return;
        }

        var index = panel.Children.IndexOf(field);
        if (index > 0 && panel.Children[index - 1] is TextBlock label)
        {
            label.Visibility = Visibility.Collapsed;
        }
    }

    private static void CollapseFieldContainer(FrameworkElement field)
    {
        if (field.Parent is not FrameworkElement parentElement)
        {
            field.Visibility = Visibility.Collapsed;
            return;
        }

        parentElement.Visibility = Visibility.Collapsed;
    }
}
