using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoasLayoutPatched;
    private bool _isFormattingPessoaRgPatch;
    private Button? _topSavePessoaButton;
    private ComboBox? _pessoaEstadoCivilComboBox;
    private ComboBox? _pessoaWorkComboBox;
    private StackPanel? _pessoaRolesCell;
    private StackPanel? _pessoaRolesTopCell;

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
        ArrangePessoaMainFieldsIntoRows(editCard);
        ArrangePessoaAddressFieldsIntoRows();
        ApplyPessoaActionButtonVisibilityRules();
        ApplyPessoaResetRules();
        AttachPessoaRgEightDigitFormatter();
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
        PessoaTipoBox.SelectedIndex = -1;
        if (_pessoaEstadoCivilComboBox is not null)
        {
            _pessoaEstadoCivilComboBox.SelectedIndex = -1;
            _pessoaEstadoCivilComboBox.Text = string.Empty;
        }
        if (_pessoaWorkComboBox is not null)
        {
            _pessoaWorkComboBox.SelectedIndex = -1;
            _pessoaWorkComboBox.Text = string.Empty;
        }
        SetPessoaEditMode(true, isNew: true);
        UpdatePessoaRolesVisibility();
    }

    private void UpdatePessoaRolesVisibility()
    {
        var visibility = PessoasGrid.SelectedItem is null ? Visibility.Collapsed : Visibility.Visible;
        if (_pessoaRolesTopCell is not null)
        {
            _pessoaRolesTopCell.Visibility = visibility;
        }
        if (_pessoaRolesCell is not null)
        {
            _pessoaRolesCell.Visibility = visibility;
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
            Children = { PessoaProprietarioBox, PessoaLocatarioBox, PessoaFiadorBox }
        };

        _pessoaRolesTopCell = AddTopCell(topGrid, 0, rolesLabel ?? new TextBlock { Text = "Funções atuais", FontWeight = FontWeights.SemiBold }, _pessoaRolesCell);
        AddTopCell(topGrid, 1, typeLabel ?? new TextBlock { Text = "Tipo", FontWeight = FontWeights.SemiBold }, PessoaTipoBox, 0);
        AddTopCell(topGrid, 2, new TextBlock { Text = "Estado civil", FontWeight = FontWeights.SemiBold }, _pessoaEstadoCivilComboBox!, 18);
        AddTopCell(topGrid, 3, new TextBlock { Text = "Trabalho", FontWeight = FontWeights.SemiBold }, _pessoaWorkComboBox!, 18);

        formStack.Children.Insert(insertIndex, topGrid);
        UpdatePessoaRolesVisibility();
    }

    private void ArrangePessoaMainFieldsIntoRows(Border editCard)
    {
        if (editCard.Child is not ScrollViewer scrollViewer
            || scrollViewer.Content is not StackPanel formStack
            || formStack.Children.OfType<WrapPanel>().Any(panel => panel.Tag as string == "PessoaMainFieldsRow"))
        {
            return;
        }

        var topInfo = formStack.Children.OfType<Grid>().FirstOrDefault(grid => grid.Tag as string == "PessoaTopInfoGrid");
        var insertIndex = topInfo is null ? 1 : formStack.Children.IndexOf(topInfo) + 1;

        var row = new WrapPanel
        {
            Tag = "PessoaMainFieldsRow",
            Margin = new Thickness(0, 0, 0, 12)
        };

        MoveFieldToWrapPanel(formStack, row, "Nome / Razão social", PessoaNomeBox, 260);
        MoveFieldToWrapPanel(formStack, row, "CPF/CNPJ", PessoaDocumentoBox, 170);
        MoveFieldToWrapPanel(PessoaFisicaFieldsPanel, row, "RG", PessoaRgBox, 150);
        MoveFieldToWrapPanel(formStack, row, "Telefone", PessoaTelefoneBox, 160);
        MoveFieldToWrapPanel(formStack, row, "E-mail", PessoaEmailBox, 260);

        if (row.Children.Count > 0)
        {
            formStack.Children.Insert(insertIndex, row);
        }
    }

    private void ArrangePessoaAddressFieldsIntoRows()
    {
        ArrangeAddressFields(PessoaFisicaFieldsPanel,
            ("Rua", PessoaRuaBox, 300),
            ("Número", PessoaNumeroBox, 90),
            ("Complemento", PessoaComplementoBox, 220),
            ("Bairro", PessoaBairroBox, 220),
            ("Cidade", PessoaCidadeBox, 180),
            ("Estado", PessoaEstadoBox, 80),
            ("CEP", PessoaCepBox, 120));

        ArrangeAddressFields(PessoaJuridicaFieldsPanel,
            ("Rua da empresa", PessoaEmpresaRuaBox, 300),
            ("Número da empresa", PessoaEmpresaNumeroBox, 90),
            ("Complemento da empresa", PessoaEmpresaComplementoBox, 220),
            ("Bairro da empresa", PessoaEmpresaBairroBox, 220),
            ("Cidade da empresa", PessoaEmpresaCidadeBox, 180),
            ("Estado da empresa", PessoaEmpresaEstadoBox, 80),
            ("CEP da empresa", PessoaEmpresaCepBox, 120));
    }

    private static void ArrangeAddressFields(StackPanel parent, params (string Label, Control Control, double Width)[] fields)
    {
        if (parent.Children.OfType<WrapPanel>().Any(panel => panel.Tag as string == "PessoaAddressFieldsRow"))
        {
            return;
        }

        var row = new WrapPanel
        {
            Tag = "PessoaAddressFieldsRow",
            Margin = new Thickness(0, 0, 0, 12)
        };

        foreach (var field in fields)
        {
            MoveFieldToWrapPanel(parent, row, field.Label, field.Control, field.Width);
        }

        if (row.Children.Count == 0)
        {
            return;
        }

        var insertIndex = 0;
        for (var index = 0; index < parent.Children.Count; index++)
        {
            if (parent.Children[index] is TextBlock block && block.Text.Contains("Endereço", StringComparison.OrdinalIgnoreCase))
            {
                insertIndex = index + 1;
                break;
            }
        }

        parent.Children.Insert(insertIndex, row);
    }

    private static void MoveFieldToWrapPanel(StackPanel source, WrapPanel target, string labelText, Control control, double width)
    {
        var label = RemoveTextBlock(source, labelText) ?? new TextBlock { Text = labelText, FontWeight = FontWeights.SemiBold };
        RemoveChild(source, control);
        control.Width = width;
        control.HorizontalAlignment = HorizontalAlignment.Left;
        control.Margin = new Thickness(0, 6, 0, 0);

        var field = new StackPanel
        {
            Width = width,
            Margin = new Thickness(0, 0, 14, 12)
        };
        field.Children.Add(label);
        field.Children.Add(control);
        target.Children.Add(field);
    }

    private static StackPanel AddTopCell(Grid grid, int column, TextBlock label, UIElement control, double leftMargin = 0)
    {
        var stack = new StackPanel { Margin = new Thickness(leftMargin, 0, 0, 0) };
        stack.Children.Add(label);
        stack.Children.Add(control);
        Grid.SetColumn(stack, column);
        grid.Children.Add(stack);
        return stack;
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
            IsEditable = false,
            SelectedIndex = -1,
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
            PessoaEstadoCivilBox.Text = _pessoaEstadoCivilComboBox.SelectedItem as string ?? string.Empty;
        };
        PessoaEstadoCivilBox.TextChanged += (_, _) => SyncPessoaEstadoCivilComboFromTextBox();
        SyncPessoaEstadoCivilComboFromTextBox();
    }

    private void SyncPessoaEstadoCivilComboFromTextBox()
    {
        if (_pessoaEstadoCivilComboBox is null)
        {
            return;
        }

        var value = PessoaEstadoCivilBox.Text;
        _pessoaEstadoCivilComboBox.SelectedItem = string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private void CreatePessoaWorkComboBox()
    {
        _pessoaWorkComboBox ??= new ComboBox
        {
            Width = 150,
            Margin = new Thickness(0, 6, 0, 0),
            ItemsSource = new[] { "Não trabalha", "Trabalha" },
            SelectedIndex = -1
        };
    }

    private void AttachPessoaRgEightDigitFormatter()
    {
        PessoaRgBox.TextChanged += PessoaRgEightDigitFormatter_TextChanged;
        PessoaConjugeRgBox.TextChanged += PessoaRgEightDigitFormatter_TextChanged;
        PessoaResponsavelRgBox.TextChanged += PessoaRgEightDigitFormatter_TextChanged;
    }

    private void PessoaRgEightDigitFormatter_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isFormattingPessoaRgPatch || sender is not TextBox textBox)
        {
            return;
        }

        var digits = new string((textBox.Text ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length != 8)
        {
            return;
        }

        var formatted = $"{digits[..1]}.{digits.Substring(1, 3)}.{digits.Substring(4, 3)}-{digits[7..]}";
        if (textBox.Text == formatted)
        {
            return;
        }

        _isFormattingPessoaRgPatch = true;
        textBox.Text = formatted;
        textBox.CaretIndex = formatted.Length;
        _isFormattingPessoaRgPatch = false;
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
        SetCompact(PessoaNomeBox, 260);
        SetCompact(PessoaDocumentoBox, 170);
        SetCompact(PessoaRgBox, 150);
        SetCompact(PessoaTelefoneBox, 160);
        SetCompact(PessoaEmailBox, 260);
        SetCompact(PessoaRuaBox, 300);
        SetCompact(PessoaComplementoBox, 220);
        SetCompact(PessoaBairroBox, 220);
        SetCompact(PessoaCidadeBox, 180);
        SetCompact(PessoaCepBox, 120);
        SetCompact(PessoaEstadoBox, 80);
        SetCompact(PessoaNumeroBox, 90);
        SetCompact(PessoaDataNascimentoBox, 140);
        SetCompact(PessoaConjugeCpfBox, 170);
        SetCompact(PessoaConjugeRgBox, 150);
        SetCompact(PessoaConjugeTelefoneBox, 160);
        SetCompact(PessoaConjugeDataNascimentoBox, 140);
        SetCompact(PessoaEmpresaRuaBox, 300);
        SetCompact(PessoaEmpresaComplementoBox, 220);
        SetCompact(PessoaEmpresaBairroBox, 220);
        SetCompact(PessoaEmpresaCidadeBox, 180);
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
