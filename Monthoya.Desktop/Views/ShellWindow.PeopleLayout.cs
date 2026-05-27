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
            || resultsCard.Parent is not Grid pessoasWorkspace)
        {
            return;
        }

        var editCard = pessoasWorkspace.Children
            .OfType<Border>()
            .FirstOrDefault(border => !ReferenceEquals(border, resultsCard));

        if (editCard is null)
        {
            return;
        }

        pessoasWorkspace.RowDefinitions.Clear();
        pessoasWorkspace.RowDefinitions.Add(new RowDefinition { Height = new GridLength(220) });
        pessoasWorkspace.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        pessoasWorkspace.ColumnDefinitions.Clear();
        pessoasWorkspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pessoasWorkspace.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(390) });

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

        var documentsCard = BuildPessoaDocumentsCard(editCard);
        if (documentsCard is null)
        {
            return;
        }

        Grid.SetRow(documentsCard, 1);
        Grid.SetColumn(documentsCard, 1);
        documentsCard.Margin = new Thickness(0, 18, 0, 0);
        documentsCard.HorizontalAlignment = HorizontalAlignment.Stretch;
        documentsCard.VerticalAlignment = VerticalAlignment.Stretch;
        pessoasWorkspace.Children.Add(documentsCard);
    }

    private Border? BuildPessoaDocumentsCard(Border editCard)
    {
        if (editCard.Child is not ScrollViewer editScrollViewer
            || editScrollViewer.Content is not StackPanel editStackPanel)
        {
            return null;
        }

        var documentStartIndex = FindDocumentSectionStart(editStackPanel);
        if (documentStartIndex < 0)
        {
            return null;
        }

        // Remove the divider immediately before the document section, because the
        // document controls will now live in their own side card.
        var moveStartIndex = documentStartIndex;
        if (documentStartIndex > 0 && editStackPanel.Children[documentStartIndex - 1] is Border)
        {
            moveStartIndex = documentStartIndex - 1;
        }

        var documentStackPanel = new StackPanel();
        while (editStackPanel.Children.Count > moveStartIndex)
        {
            var child = editStackPanel.Children[moveStartIndex];
            editStackPanel.Children.RemoveAt(moveStartIndex);

            if (child is Border divider)
            {
                divider.Visibility = Visibility.Collapsed;
                continue;
            }

            documentStackPanel.Children.Add(child);
        }

        return new Border
        {
            Style = (Style)FindResource("CardBorder"),
            Child = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = documentStackPanel
            }
        };
    }

    private static int FindDocumentSectionStart(StackPanel stackPanel)
    {
        for (var index = 0; index < stackPanel.Children.Count; index++)
        {
            if (stackPanel.Children[index] is TextBlock { Text: "Documento digitalizado" })
            {
                return index;
            }
        }

        return -1;
    }
}
