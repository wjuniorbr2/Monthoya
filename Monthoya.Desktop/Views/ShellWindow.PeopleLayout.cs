using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoasLayoutPatched;
    private Button? _topSavePessoaButton;
    private ComboBox? _pessoaEstadoCivilComboBox;
    private ComboBox? _pessoaWorkComboBox;
    private StackPanel? _pessoaRolesCell;

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
        ApplyTopPessoaInfoLayout(editCard);
        ApplyPessoaActionButtonVisibilityRules();
        ApplyPessoaResetRules();
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
            Margin = new Thickness(0, 0, 8, 0)
        };
        _topSavePessoaButton.SetBinding(VisibilityProperty, new Binding(nameof(Visibility)) { Source = SavePessoaButton });
        _topSavePessoaButton.Click += SavePessoaButton_Click;
        actionButtons.Children.Insert(0, _topSavePessoaButton);
    }

    private void ApplyPessoaActionButtonVisibilityRules()
    {
        CollapseWhenDisabled(PessoaEditButton);
        CollapseWhenDisabled(PessoaDeactivateButton);
    }

    private static void CollapseWhenDisabled(Button button)
    {
        button.Visibility = button.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
        button.IsEnabledChanged += (_, _) =>
        {
            button.Visibility = button.IsEnabled ? Visibility.Visible : Visibility.Collapsed;
        };
    }

    private void ApplyPessoaResetRules()
    {
        PessoasPanel.IsVisibleChanged += (_, _) =>
        {
            if (PessoasPanel.IsVisible)
            {
                Dispatcher.BeginInvoke(ResetPessoaFormForPageOpen, DispatcherPriority.Background);
            }
        };

        PessoasNavButton.Click += (_, _) => Dispatcher.BeginInvoke(ResetPessoaFormForPageOpen, DispatcherPriority.Background);
        PessoasGrid.SelectionChanged += (_, _) => Dispatcher.BeginInvoke(UpdatePessoaRolesVisibility, DispatcherPriority.Background);
    }

    private void ResetPessoaFormForPageOpen()
    {
        PessoasGrid.SelectedItem = null;
        _selectedPessoaId = null;
        _selectedPessoaDetails = null;
        SetPessoaDocumentoSelection(null);
        ClearPessoaForm();
        SetPessoaEditMode(true, isNew: true);
        SyncPessoaEstadoCivilComboFromTextBox();
        UpdatePessoaRolesVisibility();
    }

    private void UpdatePessoaRolesVisibility()
    {
        if (_pessoaRolesCell is not null)
        {
            _pessoaRolesCell.Visibility = PessoasGrid.SelectedItem is null ? Visibility.Collapsed : Visibility.Visible;
        }
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

    private void ApplyTopPessoaInfoLayout(Border editCard)
    {
        if (editCard.Child is not ScrollViewer scrollViewer
            || scrollViewer.Content is not StackPanel formStack
            || formStack.Children.OfType<Grid>().Any(grid => grid.Tag as string == "PessoaTopInfoGrid"))
        {
            return;
        }

        var headerIndex = formStack.Children.OfType<DockPanel>().Select(panel => formStack.Children.IndexOf(panel)).FirstOrDefault();
        var insertIndex = headerIndex + 1;

        var rolesLabel = RemoveTextBlock(formStack, "Funções atuais");
        RemoveChild(formStack, PessoaProprietarioBox);
        RemoveChild(formStack, PessoaLocatarioBox);
        RemoveChild(formStack, PessoaFiadorBox);
        var typeLabel = RemoveTextBlock(formStack, "Tipo");
        RemoveChild(formStack, PessoaTipoBox);

        HideOriginalEstadoCivilField();
        CreatePessoaEstadoCivilComboBox();
        CreatePessoaWorkComboBox();

        var topGrid = new Grid
        {
            Tag = "PessoaTopInfoGrid",
            Margin = new Thickness(0, 0, 0, 12)
        };
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _pessoaRolesCell = new StackPanel
        {
            Visibility = PessoasGrid.SelectedItem is null ? Visibility.Collapsed : Visibility.Visible,
            Children = { PessoaProprietarioBox, PessoaLocatarioBox, PessoaFiadorBox }
        };

        AddTopCell(topGrid, 0, rolesLabel ?? new TextBlock { Text = "Funções atuais", FontWeight = FontWeights.SemiBold }, _pessoaRolesCell);
        AddTopCell(topGrid, 1, typeLabel ?? new TextBlock { Text = "Tipo", FontWeight = FontWeights.SemiBold }, PessoaTipoBox, 18);
        AddTopCell(topGrid, 2, new TextBlock { Text = "Estado civil", FontWeight = FontWeights.SemiBold }, _pessoaEstadoCivilComboBox!, 18);
        AddTopCell(topGrid, 3, new TextBlock { Text = "Work", FontWeight = FontWeights.SemiBold }, _pessoaWorkComboBox!, 18);

        formStack.Children.Insert(insertIndex, topGrid);
        UpdatePessoaRolesVisibility();
    }

    private static void AddTopCell(Grid grid, int column, TextBlock label, UIElement control, double leftMargin = 0)
    {
        var stack = new StackPanel { Margin = new Thickness(leftMargin, 0, 0, 0) };
        stack.Children.Add(label);
        stack.Children.Add(control);
        Grid.SetColumn(stack, column);
        grid.Children.Add(stack);
    }

    private void CreatePessoaEstadoCivilComboBox()
    {
        if (_pessoaEstadoCivilComboBox is not null)
        {
            return;
        }

        _pessoaEstadoCivilComboBox = new ComboBox
        {
            Width = 170,
            Margin = new Thickness(0, 6, 0, 0),
            IsEditable = true,
            ItemsSource = new[]
            {
                string.Empty,
                "Solteiro(a)",
                "Casado(a)",
                "União estável",
                "Divorciado(a)",
                "Separado(a)",
                "Viúvo(a)"
            }
        };

        _pessoaEstadoCivilComboBox.SelectionChanged += (_, _) =>
        {
            if (_pessoaEstadoCivilComboBox.SelectedItem is string selected)
            {
                PessoaEstadoCivilBox.Text = selected;
            }
        };
        _pessoaEstadoCivilComboBox.LostKeyboardFocus += (_, _) => PessoaEstadoCivilBox.Text = _pessoaEstadoCivilComboBox.Text;
        PessoaEstadoCivilBox.TextChanged += (_, _) => SyncPessoaEstadoCivilComboFromTextBox();
        SyncPessoaEstadoCivilComboFromTextBox();
    }

    private void SyncPessoaEstadoCivilComboFromTextBox()
    {
        if (_pessoaEstadoCivilComboBox is null)
        {
            return;
        }

        _pessoaEstadoCivilComboBox.Text = PessoaEstadoCivilBox.Text;
    }

    private void CreatePessoaWorkComboBox()
    {
        _pessoaWorkComboBox ??= new ComboBox
        {
            Width = 150,
            Margin = new Thickness(0, 6, 0, 0),
            ItemsSource = new[] { "Not working", "Working" },
            SelectedIndex = 0
        };
    }

    private void HideOriginalEstadoCivilField()
    {
        PessoaEstadoCivilBox.Visibility = Visibility.Collapsed;
        PessoaEstadoCivilBox.Width = 0;

        if (PessoaEstadoCivilBox.Parent is Panel parent)
        {
            var index = parent.Children.IndexOf(PessoaEstadoCivilBox);
            if (index > 0 && parent.Children[index - 1] is TextBlock label && label.Text == "Estado civil")
            {
                label.Visibility = Visibility.Collapsed;
            }
        }
    }

    private static bool RemoveChild(Panel parent, UIElement child)
    {
        if (parent.Children.Contains(child))
        {
            parent.Children.Remove(child);
            return true;
        }

        return false;
    }

    private static TextBlock? RemoveTextBlock(Panel parent, string text)
    {
        var match = parent.Children.OfType<TextBlock>().FirstOrDefault(block => block.Text == text);
        if (match is not null)
        {
            parent.Children.Remove(match);
        }

        return match;
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
