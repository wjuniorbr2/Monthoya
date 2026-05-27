using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoasLayoutPatched;
    private Button? _topSavePessoaButton;

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

        AddTopPessoaSaveButton(editCard);
        MovePessoaDocumentEditorToSideCard(editCard, documentsGrid);
        ApplyCompactPessoaFieldSizing();
    }

    private void AddTopPessoaSaveButton(Border editCard)
    {
        if (_topSavePessoaButton is not null
            || editCard.Child is not ScrollViewer scrollViewer
            || scrollViewer.Content is not StackPanel formStack
            || formStack.Children.OfType<DockPanel>().FirstOrDefault() is not DockPanel header
            || header.Children.OfType<StackPanel>().FirstOrDefault() is not StackPanel actionButtons)
        {
            return;
        }

        _topSavePessoaButton = new Button
        {
            Content = "Salvar pessoa",
            Style = (Style)FindResource("PrimaryButton"),
            Margin = new Thickness(0, 0, 8, 0),
            Visibility = SavePessoaButton.Visibility
        };
        _topSavePessoaButton.Click += SavePessoaButton_Click;
        actionButtons.Children.Insert(0, _topSavePessoaButton);
    }

    private void MovePessoaDocumentEditorToSideCard(Border editCard, Grid documentsGrid)
    {
        if (editCard.Child is not ScrollViewer scrollViewer
            || scrollViewer.Content is not StackPanel formStack
            || documentsGrid.Children.OfType<ScrollViewer>().Any(viewer => viewer.Tag as string == "PessoaDocumentEditor"))
        {
            return;
        }

        var documentStartIndex = FindDocumentEditorStart(formStack);
        if (documentStartIndex < 0)
        {
            return;
        }

        var moveStartIndex = documentStartIndex;
        if (documentStartIndex > 0 && formStack.Children[documentStartIndex - 1] is Border)
        {
            moveStartIndex = documentStartIndex - 1;
        }

        var documentEditorStack = new StackPanel { Margin = new Thickness(14, 12, 14, 14) };
        while (formStack.Children.Count > moveStartIndex)
        {
            var child = formStack.Children[moveStartIndex];
            formStack.Children.RemoveAt(moveStartIndex);

            if (child is Border)
            {
                continue;
            }

            documentEditorStack.Children.Add(child);
        }

        documentsGrid.RowDefinitions.Clear();
        documentsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        documentsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(210) });
        documentsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        PessoaDocumentosGrid.MaxHeight = 210;
        PessoaDocumentosGrid.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        Grid.SetRow(PessoaDocumentosGrid, 1);

        var editorScrollViewer = new ScrollViewer
        {
            Tag = "PessoaDocumentEditor",
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = documentEditorStack
        };

        Grid.SetRow(editorScrollViewer, 2);
        documentsGrid.Children.Add(editorScrollViewer);
    }

    private static int FindDocumentEditorStart(StackPanel formStack)
    {
        for (var index = 0; index < formStack.Children.Count; index++)
        {
            if (formStack.Children[index] is TextBlock { Text: "Documento digitalizado" })
            {
                return index;
            }
        }

        return -1;
    }

    private void ApplyCompactPessoaFieldSizing()
    {
        SetCompact(PessoaTipoBox, 140);
        SetCompact(PessoaDocumentoBox, 170);
        SetCompact(PessoaRgBox, 150);
        SetCompact(PessoaTelefoneBox, 160);
        SetCompact(PessoaCepBox, 120);
        SetCompact(PessoaEstadoBox, 80);
        SetCompact(PessoaNumeroBox, 90);
        SetCompact(PessoaDataNascimentoBox, 140);
        SetCompact(PessoaConjugeCpfBox, 170);
        SetCompact(PessoaConjugeRgBox, 150);
        SetCompact(PessoaConjugeTelefoneBox, 160);
        SetCompact(PessoaConjugeDataNascimentoBox, 140);
        SetCompact(PessoaEmpresaCepBox, 120);
        SetCompact(PessoaEmpresaEstadoBox, 80);
        SetCompact(PessoaEmpresaNumeroBox, 90);
        SetCompact(PessoaResponsavelCpfBox, 170);
        SetCompact(PessoaResponsavelRgBox, 150);
        SetCompact(PessoaResponsavelTelefoneBox, 160);
        SetCompact(PessoaResponsavelCepBox, 120);
        SetCompact(PessoaResponsavelEstadoBox, 80);
        SetCompact(PessoaResponsavelNumeroBox, 90);
        SetCompact(PessoaResponsavelDataNascimentoBox, 140);
        SetCompact(PessoaTelefoneEmpresaTrabalhoBox, 160);
        SetCompact(PessoaResponsavelTelefoneEmpresaTrabalhoBox, 160);
    }

    private static void SetCompact(Control control, double width)
    {
        control.Width = width;
        control.HorizontalAlignment = HorizontalAlignment.Left;
    }
}
