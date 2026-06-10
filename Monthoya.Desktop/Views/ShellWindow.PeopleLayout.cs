using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoasLayoutPatched;
    private bool _isFormattingPessoaRgPatch;
    private bool _isFormattingPessoaBankFields;
    private bool _pessoaBankInputBehaviorConfigured;
    private bool _pessoaBankSelectionConfigured;
    private bool _pessoaResponsavelBankSelectionConfigured;
    private Button? _topSavePessoaButton;
    private ComboBox? _pessoaEstadoCivilComboBox;
    private ComboBox? _pessoaWorkComboBox;
    private ComboBox? _pessoaPetComboBox;
    private TextBox? _pessoaPetQualBox;
    private TextBox? _pessoaCnpjEmpresaTrabalhoBox;
    private TextBox? _pessoaEmailEmpresaTrabalhoBox;
    private TextBox? _pessoaCargoTrabalhoBox;
    private TextBox? _pessoaRendaTrabalhoBox;
    private TextBox? _pessoaTempoEmpregoBox;
    private TextBox? _pessoaTipoComprovanteRendaBox;
    private TextBox? _pessoaOutrasInformacoesBox;
    private TextBox? _pessoaTrabalhoOutrasInformacoesBox;
    private TextBox? _pessoaTrabalhoRuaBox;
    private TextBox? _pessoaTrabalhoNumeroBox;
    private TextBox? _pessoaTrabalhoComplementoBox;
    private TextBox? _pessoaTrabalhoBairroBox;
    private TextBox? _pessoaTrabalhoEstadoBox;
    private TextBox? _pessoaTrabalhoCidadeBox;
    private TextBox? _pessoaTrabalhoCepBox;
    private ComboBox? _pessoaBancoBox;
    private TextBox? _pessoaBancoCodigoBox;
    private TextBox? _pessoaBancoNomeBox;
    private TextBox? _pessoaAgenciaNumeroBox;
    private TextBox? _pessoaAgenciaDigitoBox;
    private TextBox? _pessoaContaNumeroBox;
    private TextBox? _pessoaContaDigitoBox;
    private ComboBox? _pessoaContaTipoBox;
    private TextBox? _pessoaTitularNomeBox;
    private TextBox? _pessoaTitularDocumentoBox;
    private ComboBox? _pessoaPixTipoBox;
    private TextBox? _pessoaPixChaveBox;
    private ComboBox? _pessoaRepassePreferencialBox;
    private Button? _pessoaUsarDadosPessoaBancoButton;
    private Button? _pessoaUsarPixButton;
    private TextBox? _pessoaConjugeEmailBox;
    private TextBox? _pessoaConjugeDadosBancariosBox;
    private TextBox? _pessoaConjugeObservacoesBox;
    private TextBox? _pessoaConjugeOutrasInformacoesBox;
    private ComboBox? _pessoaConjugeWorkComboBox;
    private TextBox? _pessoaConjugeNomeEmpresaTrabalhoBox;
    private TextBox? _pessoaConjugeCnpjEmpresaTrabalhoBox;
    private TextBox? _pessoaConjugeTelefoneEmpresaTrabalhoBox;
    private TextBox? _pessoaConjugeEmailEmpresaTrabalhoBox;
    private TextBox? _pessoaConjugeCargoTrabalhoBox;
    private TextBox? _pessoaConjugeRendaTrabalhoBox;
    private TextBox? _pessoaConjugeTempoEmpregoBox;
    private TextBox? _pessoaConjugeTipoComprovanteRendaBox;
    private TextBox? _pessoaConjugeTrabalhoOutrasInformacoesBox;
    private TextBox? _pessoaConjugeEmpresaRuaBox;
    private TextBox? _pessoaConjugeEmpresaNumeroBox;
    private TextBox? _pessoaConjugeEmpresaComplementoBox;
    private TextBox? _pessoaConjugeEmpresaBairroBox;
    private TextBox? _pessoaConjugeEmpresaEstadoBox;
    private TextBox? _pessoaConjugeEmpresaCidadeBox;
    private TextBox? _pessoaConjugeEmpresaCepBox;
    private TextBox? _pessoaNomeFantasiaBox;
    private TextBox? _pessoaAtividadeBox;
    private TextBox? _pessoaReceitaMensalBox;
    private TextBox? _pessoaInscricaoEstadualBox;
    private TextBox? _pessoaInscricaoMunicipalBox;
    private DatePicker? _pessoaDataAberturaBox;
    private TextBox? _pessoaResponsavelCargoBox;
    private ComboBox? _pessoaResponsavelBancoBox;
    private TextBox? _pessoaResponsavelBancoCodigoBox;
    private TextBox? _pessoaResponsavelBancoNomeBox;
    private TextBox? _pessoaResponsavelAgenciaNumeroBox;
    private TextBox? _pessoaResponsavelAgenciaDigitoBox;
    private TextBox? _pessoaResponsavelContaNumeroBox;
    private TextBox? _pessoaResponsavelContaDigitoBox;
    private ComboBox? _pessoaResponsavelContaTipoBox;
    private TextBox? _pessoaResponsavelTitularNomeBox;
    private TextBox? _pessoaResponsavelTitularDocumentoBox;
    private ComboBox? _pessoaResponsavelPixTipoBox;
    private TextBox? _pessoaResponsavelPixChaveBox;
    private ComboBox? _pessoaResponsavelRepassePreferencialBox;
    private Button? _pessoaUsarDadosResponsavelBancoButton;
    private Button? _pessoaResponsavelUsarPixButton;
    private TextBox? _pessoaResponsavelObservacoesBox;
    private StackPanel? _pessoaFisicaWorkSection;
    private StackPanel? _pessoaConjugeSection;
    private StackPanel? _pessoaConjugeWorkSection;
    private StackPanel? _pessoaRolesCell;
    private StackPanel? _pessoaRolesTopCell;
    private StackPanel? _pessoaEstadoCivilTopCell;
    private StackPanel? _pessoaWorkTopCell;
    private StackPanel? _pessoaPetTopCell;
    private StackPanel? _pessoaPetQualTopCell;
    private TextBlock? _pessoaPrimarySectionHeader;
    private StackPanel? _pessoaMainRgField;

    private void ApplyPessoasPanelLayoutPatch()
    {
        if (_pessoasLayoutPatched)
        {
            return;
        }

        if (PessoasGrid.Parent is not Border resultsCard
            || resultsCard.Parent is not Grid leftColumnGrid
            || leftColumnGrid.Parent is not Grid pessoasWorkspace
            || PessoaDocumentosGrid.Parent is not Grid documentsGrid
            || documentsGrid.Parent is not Border documentsCard)
        {
            Dispatcher.BeginInvoke(ApplyPessoasPanelLayoutPatch, DispatcherPriority.Loaded);
            return;
        }

        var editCard = pessoasWorkspace.Children
            .OfType<Border>()
            .FirstOrDefault(border => !ReferenceEquals(border, resultsCard)
                                      && !ReferenceEquals(border, documentsCard));

        if (editCard is null)
        {
            Dispatcher.BeginInvoke(ApplyPessoasPanelLayoutPatch, DispatcherPriority.Loaded);
            return;
        }

        _pessoasLayoutPatched = true;

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
        Grid.SetColumnSpan(resultsCard, 1);
        resultsCard.Margin = new Thickness(0, 0, 18, 0);
        resultsCard.VerticalAlignment = VerticalAlignment.Stretch;
        resultsCard.HorizontalAlignment = HorizontalAlignment.Stretch;
        PessoasGrid.MaxHeight = 220;

        Grid.SetRow(editCard, 1);
        Grid.SetColumn(editCard, 0);
        Grid.SetColumnSpan(editCard, 1);
        editCard.Margin = new Thickness(0, 18, 18, 0);
        editCard.HorizontalAlignment = HorizontalAlignment.Stretch;
        editCard.VerticalAlignment = VerticalAlignment.Stretch;

        Grid.SetRow(documentsCard, 0);
        Grid.SetColumn(documentsCard, 1);
        Grid.SetColumnSpan(documentsCard, 1);
        Grid.SetRowSpan(documentsCard, 2);
        documentsCard.Margin = new Thickness(0, 0, 0, 0);
        documentsCard.HorizontalAlignment = HorizontalAlignment.Stretch;
        documentsCard.VerticalAlignment = VerticalAlignment.Stretch;

        MovePessoaDocumentEditorToSideCard(editCard, documentsGrid);
        ApplyTopPessoaInfoLayout(editCard);
        AddMissingPessoaDetailedFields();
        ArrangePessoaMainFieldsIntoRows(editCard);
        ArrangePessoaAddressFieldsIntoRows();
        ApplyPessoaActionButtonVisibilityRules();
        ApplyPessoaResetRules();
        AttachPessoaRgEightDigitFormatter();
        ApplyCompactPessoaFieldSizing();
        UpdatePessoaConditionalSections();
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
            Style = (Style)FindResource("PrimaryButtonSmall"),
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
        PessoasGrid.SelectionChanged += (_, _) => Dispatcher.BeginInvoke(UpdatePessoaRolesVisibility, DispatcherPriority.Background);
    }

    private void ResetPessoaFormForPageOpen()
    {
        PessoasGrid.SelectedItem = null;
        _selectedPessoaId = null;
        _selectedPessoaDetails = null;
        _pendingPessoaDocumentos.Clear();
        SetPessoaDocumentoSelection(null);
        ClearPessoaDocumentoInputs();
        ClearPessoaForm();
        _pessoaDocumentos = [];
        PessoaDocumentosGrid.ItemsSource = _pessoaDocumentos;
        PessoaDocumentosTitleText.Text = "Documentos anexos";
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
        if (_pessoaPetComboBox is not null)
        {
            _pessoaPetComboBox.SelectedIndex = -1;
        }
        if (_pessoaPetQualBox is not null)
        {
            _pessoaPetQualBox.Clear();
        }
        SetPessoaEditMode(true, isNew: true);
        UpdatePessoaRolesVisibility();
        // When resetting the form for a new person, update the active Pessoas tab label to PT-BR "Criar Novo"
        if (_activeTab is not null && _activeTab.Page == ShellPage.Pessoas)
        {
            _activeTab.SelectedPessoaName = "Criar Novo";
            RenderTabs();
            SaveActiveTabState();
        }
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
        documentsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(160) });
        documentsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        documentsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        PessoaDocumentosGrid.MaxHeight = 160;
        PessoaDocumentosGrid.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        Grid.SetRow(PessoaDocumentosGrid, 1);
        Grid.SetRow(PessoaDocumentosListErrorText, 2);

        var editorScrollViewer = new ScrollViewer
        {
            Tag = "PessoaDocumentEditor",
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = documentEditorStack
        };

        Grid.SetRow(editorScrollViewer, 3);
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
        CreatePessoaPetControls();

        var topGrid = new Grid
        {
            Tag = "PessoaTopInfoGrid",
            Margin = new Thickness(0, 0, 0, 12)
        };
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
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
        _pessoaEstadoCivilTopCell = AddTopCell(topGrid, 2, new TextBlock { Text = "Estado civil", FontWeight = FontWeights.SemiBold }, _pessoaEstadoCivilComboBox!, 18);
        _pessoaWorkTopCell = AddTopCell(topGrid, 3, new TextBlock { Text = "Trabalho", FontWeight = FontWeights.SemiBold }, _pessoaWorkComboBox!, 18);
        _pessoaPetTopCell = AddTopCell(topGrid, 4, new TextBlock { Text = "Possui pet?", FontWeight = FontWeights.SemiBold }, _pessoaPetComboBox!, 18);
        _pessoaPetQualTopCell = AddTopCell(topGrid, 5, new TextBlock { Text = "Qual?", FontWeight = FontWeights.SemiBold }, _pessoaPetQualBox!, 18);

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

        _pessoaPrimarySectionHeader = SectionHeader("DADOS PESSOAIS:");
        MoveFieldToWrapPanel(formStack, row, "Nome / Razão social", PessoaNomeBox, 260);
        MoveFieldToWrapPanel(formStack, row, "CPF/CNPJ", PessoaDocumentoBox, 190);
        _pessoaMainRgField = MoveFieldToWrapPanel(PessoaFisicaFieldsPanel, row, "RG", PessoaRgBox, 150);
        MoveFieldToWrapPanel(formStack, row, "Telefone", PessoaTelefoneBox, 160);
        MoveFieldToWrapPanel(formStack, row, "E-mail", PessoaEmailBox, 260);

        if (row.Children.Count > 0)
        {
            formStack.Children.Insert(insertIndex, _pessoaPrimarySectionHeader);
            insertIndex++;
            formStack.Children.Insert(insertIndex, row);
        }

        UpdatePessoaTypeLabelsAndSections();
    }


    private void UpdatePessoaConditionalSections()
    {
        UpdatePessoaTypeLabelsAndSections();

        if (_pessoaFisicaWorkSection is not null)
        {
            _pessoaFisicaWorkSection.Visibility = GetPessoaPossuiTrabalho() == true ? Visibility.Visible : Visibility.Collapsed;
        }

        var estadoCivil = _pessoaEstadoCivilComboBox?.SelectedItem as string ?? PessoaEstadoCivilBox.Text;
        var showConjuge = estadoCivil.Contains("Casado", StringComparison.OrdinalIgnoreCase)
            || estadoCivil.Contains("União", StringComparison.OrdinalIgnoreCase);
        if (_pessoaConjugeSection is not null)
        {
            EnsureFieldLabel(PessoaConjugeCpfBox, "CPF");
            _pessoaConjugeSection.Visibility = showConjuge ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_pessoaConjugeWorkSection is not null)
        {
            _pessoaConjugeWorkSection.Visibility = showConjuge && GetPessoaConjugePossuiTrabalho() == true
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private void UpdatePessoaTypeLabelsAndSections()
    {
        var isJuridica = PessoaTipoBox.SelectedValue is Monthoya.Core.Entities.TipoPessoa.Juridica;
        if (_pessoaPrimarySectionHeader is not null)
        {
            _pessoaPrimarySectionHeader.Text = isJuridica ? "DADOS DA EMPRESA:" : "DADOS PESSOAIS:";
        }

        SetFieldLabel(PessoaNomeBox, isJuridica ? "Razão social" : "Nome");
        SetFieldLabel(PessoaDocumentoBox, isJuridica ? "CNPJ" : "CPF");
        if (_pessoaMainRgField is not null)
        {
            _pessoaMainRgField.Visibility = isJuridica ? Visibility.Collapsed : Visibility.Visible;
        }

        var fisicaOnlyVisibility = isJuridica ? Visibility.Collapsed : Visibility.Visible;
        if (_pessoaEstadoCivilTopCell is not null)
        {
            _pessoaEstadoCivilTopCell.Visibility = fisicaOnlyVisibility;
        }
        if (_pessoaWorkTopCell is not null)
        {
            _pessoaWorkTopCell.Visibility = fisicaOnlyVisibility;
        }
        if (_pessoaPetTopCell is not null)
        {
            _pessoaPetTopCell.Visibility = fisicaOnlyVisibility;
        }
        if (_pessoaPetQualTopCell is not null)
        {
            UpdatePessoaPetQualVisibility();
        }
    }

}




