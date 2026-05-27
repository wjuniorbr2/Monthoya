using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoasLayoutPatched;

    private void ApplyPessoasPanelLayoutPatch()
    {
        if (_pessoasLayoutPatched)
        {
            return;
        }

        _pessoasLayoutPatched = true;

        if (PessoasGrid.Parent is not Border resultsCard
            || resultsCard.Parent is not Grid leftColumnGrid
            || leftColumnGrid.Parent is not Grid pessoasWorkspace
            || PessoaDocumentosGrid.Parent is not Grid documentsGrid
            || documentsGrid.Parent is not Border documentsCard)
        {
            return;
        }

        var editCard = pessoasWorkspace.Children
            .OfType<Border>()
            .FirstOrDefault(border => !ReferenceEquals(border, resultsCard)
                                      && !ReferenceEquals(border, documentsCard));

        if (editCard is null)
        {
            return;
        }

        // The current XAML has an inner left column grid containing results + documents,
        // and the person form on the right. Rebuild the workspace so the results card
        // takes the whole first row, then the form and documents sit below it.
        leftColumnGrid.Children.Remove(resultsCard);
        leftColumnGrid.Children.Remove(documentsCard);
        pessoasWorkspace.Children.Remove(leftColumnGrid);

        pessoasWorkspace.RowDefinitions.Clear();
        pessoasWorkspace.RowDefinitions.Add(new RowDefinition { Height = new GridLength(220) });
        pessoasWorkspace.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        pessoasWorkspace.ColumnDefinitions.Clear();
        pessoasWorkspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4, GridUnitType.Star) });
        pessoasWorkspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        if (!pessoasWorkspace.Children.Contains(resultsCard))
        {
            pessoasWorkspace.Children.Add(resultsCard);
        }

        if (!pessoasWorkspace.Children.Contains(documentsCard))
        {
            pessoasWorkspace.Children.Add(documentsCard);
        }

        Grid.SetRow(resultsCard, 0);
        Grid.SetColumn(resultsCard, 0);
        Grid.SetColumnSpan(resultsCard, 2);
        resultsCard.Margin = new Thickness(0);
        resultsCard.VerticalAlignment = VerticalAlignment.Stretch;
        resultsCard.HorizontalAlignment = HorizontalAlignment.Stretch;
        PessoasGrid.MaxHeight = 220;

        Grid.SetRow(editCard, 1);
        Grid.SetColumn(editCard, 0);
        Grid.SetColumnSpan(editCard, 1);
        editCard.Margin = new Thickness(0, 18, 18, 0);
        editCard.HorizontalAlignment = HorizontalAlignment.Stretch;
        editCard.VerticalAlignment = VerticalAlignment.Stretch;

        Grid.SetRow(documentsCard, 1);
        Grid.SetColumn(documentsCard, 1);
        Grid.SetColumnSpan(documentsCard, 1);
        documentsCard.Margin = new Thickness(0, 18, 0, 0);
        documentsCard.HorizontalAlignment = HorizontalAlignment.Stretch;
        documentsCard.VerticalAlignment = VerticalAlignment.Stretch;
    }
}
