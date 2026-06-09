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
        MoveFieldToWrapPanel(formStack, row, "CPF/CNPJ", PessoaDocumentoBox, 170);
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

    private void AddMissingPessoaDetailedFields()
    {
        if (PessoaFisicaFieldsPanel.Children.OfType<StackPanel>().Any(panel => panel.Tag as string == "PessoaDetailedFields"))
        {
            return;
        }

        CreateDynamicPessoaControls();
        AddPessoaFisicaDetailedFields();
        AddPessoaJuridicaDetailedFields();
        ConfigureDynamicPessoaInputBehavior();
        ConfigurePessoaBankInputBehavior();
        _pessoaEstadoCivilComboBox!.SelectionChanged += (_, _) => UpdatePessoaConditionalSections();
        _pessoaWorkComboBox!.SelectionChanged += (_, _) => UpdatePessoaConditionalSections();
        _pessoaConjugeWorkComboBox!.SelectionChanged += (_, _) => UpdatePessoaConditionalSections();
    }

    private void CreateDynamicPessoaControls()
    {
        _pessoaCnpjEmpresaTrabalhoBox ??= NewTextBox(170);
        _pessoaEmailEmpresaTrabalhoBox ??= NewTextBox(260);
        _pessoaCargoTrabalhoBox ??= NewTextBox(160);
        _pessoaRendaTrabalhoBox ??= NewTextBox(120);
        _pessoaTempoEmpregoBox ??= NewTextBox(140);
        _pessoaTipoComprovanteRendaBox ??= NewTextBox(210);
        _pessoaOutrasInformacoesBox ??= NewMultilineBox(360, 64);
        _pessoaTrabalhoOutrasInformacoesBox ??= NewMultilineBox(360, 64);
        _pessoaTrabalhoRuaBox ??= NewTextBox(300);
        _pessoaTrabalhoNumeroBox ??= NewTextBox(90);
        _pessoaTrabalhoComplementoBox ??= NewTextBox(220);
        _pessoaTrabalhoBairroBox ??= NewTextBox(180);
        _pessoaTrabalhoEstadoBox ??= NewTextBox(80);
        _pessoaTrabalhoCidadeBox ??= NewTextBox(180);
        _pessoaTrabalhoCepBox ??= NewTextBox(120);
        CreatePessoaBankControls();

        _pessoaConjugeEmailBox ??= NewTextBox(260);
        _pessoaConjugeDadosBancariosBox ??= NewMultilineBox(64);
        _pessoaConjugeObservacoesBox ??= NewMultilineBox(64);
        _pessoaConjugeOutrasInformacoesBox ??= NewMultilineBox(360, 64);
        _pessoaConjugeWorkComboBox ??= new ComboBox
        {
            Width = 150,
            Margin = new Thickness(0, 6, 0, 0),
            ItemsSource = new[] { "Não possui trabalho", "Possui trabalho" },
            SelectedIndex = -1
        };
        _pessoaConjugeNomeEmpresaTrabalhoBox ??= NewTextBox(260);
        _pessoaConjugeCnpjEmpresaTrabalhoBox ??= NewTextBox(170);
        _pessoaConjugeTelefoneEmpresaTrabalhoBox ??= NewTextBox(160);
        _pessoaConjugeEmailEmpresaTrabalhoBox ??= NewTextBox(260);
        _pessoaConjugeCargoTrabalhoBox ??= NewTextBox(160);
        _pessoaConjugeRendaTrabalhoBox ??= NewTextBox(120);
        _pessoaConjugeTempoEmpregoBox ??= NewTextBox(140);
        _pessoaConjugeTipoComprovanteRendaBox ??= NewTextBox(210);
        _pessoaConjugeTrabalhoOutrasInformacoesBox ??= NewMultilineBox(360, 64);
        _pessoaConjugeEmpresaRuaBox ??= NewTextBox(300);
        _pessoaConjugeEmpresaNumeroBox ??= NewTextBox(90);
        _pessoaConjugeEmpresaComplementoBox ??= NewTextBox(220);
        _pessoaConjugeEmpresaBairroBox ??= NewTextBox(180);
        _pessoaConjugeEmpresaEstadoBox ??= NewTextBox(80);
        _pessoaConjugeEmpresaCidadeBox ??= NewTextBox(180);
        _pessoaConjugeEmpresaCepBox ??= NewTextBox(120);

        _pessoaNomeFantasiaBox ??= NewTextBox(260);
        _pessoaAtividadeBox ??= NewTextBox(220);
        _pessoaReceitaMensalBox ??= NewTextBox(140);
        _pessoaInscricaoEstadualBox ??= NewTextBox(160);
        _pessoaInscricaoMunicipalBox ??= NewTextBox(160);
        _pessoaDataAberturaBox ??= NewDatePicker();
        _pessoaResponsavelCargoBox ??= NewTextBox(160);
        CreatePessoaResponsavelBankControls();
        _pessoaResponsavelObservacoesBox ??= NewMultilineBox(64);
    }

    private void CreatePessoaBankControls()
    {
        _pessoaBancoBox ??= NewBankComboBox();
        _pessoaBancoCodigoBox ??= NewTextBox(90);
        _pessoaBancoNomeBox ??= NewTextBox(180);
        _pessoaAgenciaNumeroBox ??= NewTextBox(100);
        _pessoaAgenciaDigitoBox ??= NewTextBox(60);
        _pessoaContaNumeroBox ??= NewTextBox(130);
        _pessoaContaDigitoBox ??= NewTextBox(60);
        _pessoaContaTipoBox ??= NewContaTipoComboBox();
        _pessoaTitularNomeBox ??= NewTextBox(220);
        _pessoaTitularDocumentoBox ??= NewTextBox(170);
        _pessoaPixTipoBox ??= NewPixTipoComboBox();
        _pessoaPixChaveBox ??= NewTextBox(220);
        _pessoaRepassePreferencialBox ??= NewRepassePreferencialComboBox();
        _pessoaUsarDadosPessoaBancoButton ??= NewSmallBankActionButton("Usar dados da pessoa", (_, _) => FillPessoaBankHolderFromPessoa());
        _pessoaUsarPixButton ??= NewSmallBankActionButton("Usar como PIX", (_, _) => FillPessoaPixFromSelectedType());
        if (!_pessoaBankSelectionConfigured)
        {
            _pessoaBankSelectionConfigured = true;
            AttachBankSelection(_pessoaBancoBox, _pessoaBancoCodigoBox, _pessoaBancoNomeBox);
        }
    }

    private void CreatePessoaResponsavelBankControls()
    {
        _pessoaResponsavelBancoBox ??= NewBankComboBox();
        _pessoaResponsavelBancoCodigoBox ??= NewTextBox(90);
        _pessoaResponsavelBancoNomeBox ??= NewTextBox(180);
        _pessoaResponsavelAgenciaNumeroBox ??= NewTextBox(100);
        _pessoaResponsavelAgenciaDigitoBox ??= NewTextBox(60);
        _pessoaResponsavelContaNumeroBox ??= NewTextBox(130);
        _pessoaResponsavelContaDigitoBox ??= NewTextBox(60);
        _pessoaResponsavelContaTipoBox ??= NewContaTipoComboBox();
        _pessoaResponsavelTitularNomeBox ??= NewTextBox(220);
        _pessoaResponsavelTitularDocumentoBox ??= NewTextBox(170);
        _pessoaResponsavelPixTipoBox ??= NewPixTipoComboBox();
        _pessoaResponsavelPixChaveBox ??= NewTextBox(220);
        _pessoaResponsavelRepassePreferencialBox ??= NewRepassePreferencialComboBox();
        _pessoaUsarDadosResponsavelBancoButton ??= NewSmallBankActionButton("Usar dados do responsável", (_, _) => FillResponsavelBankHolderFromResponsavel());
        _pessoaResponsavelUsarPixButton ??= NewSmallBankActionButton("Usar como PIX", (_, _) => FillResponsavelPixFromSelectedType());
        if (!_pessoaResponsavelBankSelectionConfigured)
        {
            _pessoaResponsavelBankSelectionConfigured = true;
            AttachBankSelection(_pessoaResponsavelBancoBox, _pessoaResponsavelBancoCodigoBox, _pessoaResponsavelBancoNomeBox);
        }
    }

    private void AddPessoaFisicaDetailedFields()
    {
        var personalRow = new WrapPanel { Tag = "PessoaDetailedFields", Margin = new Thickness(0, 0, 0, 12) };
        MoveFieldToWrapPanel(PessoaFisicaFieldsPanel, personalRow, "Data de nascimento", PessoaDataNascimentoBox, 140);
        MoveFieldToWrapPanel(PessoaFisicaFieldsPanel, personalRow, "Profissão", PessoaProfissaoBox, 170);
        MoveFieldToWrapPanel(PessoaFisicaFieldsPanel, personalRow, "Nacionalidade", PessoaNacionalidadeBox, 150);
        personalRow.Children.Add(FieldStack("Outras informações", _pessoaOutrasInformacoesBox!, 360));
        PessoaFisicaFieldsPanel.Children.Insert(0, personalRow);
        PessoaFisicaFieldsPanel.Children.Insert(1, CreateBankSection(
            PessoaDadosBancariosBox,
            _pessoaBancoBox!, _pessoaAgenciaNumeroBox!, _pessoaAgenciaDigitoBox!,
            _pessoaContaNumeroBox!, _pessoaContaDigitoBox!, _pessoaContaTipoBox!, _pessoaTitularNomeBox!,
            _pessoaTitularDocumentoBox!, _pessoaPixTipoBox!, _pessoaPixChaveBox!, _pessoaRepassePreferencialBox!,
            _pessoaUsarDadosPessoaBancoButton!, _pessoaUsarPixButton!));
        RemoveTextBlock(PessoaFisicaFieldsPanel, "CPF");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "CPF/CNPJ");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Dados bancários");

        RenameSection(PessoaFisicaFieldsPanel, "Endereço:", "ENDEREÇO DE RESIDÊNCIA:");
        ReplaceSectionHeaderWithCep(PessoaFisicaFieldsPanel, "ENDEREÇO DE RESIDÊNCIA:", PessoaCepBox);

        _pessoaFisicaWorkSection = new StackPanel { Tag = "PessoaDetailedFields", Margin = new Thickness(0, 10, 0, 12) };
        _pessoaFisicaWorkSection.Children.Add(SectionHeader("DADOS DO TRABALHO:"));
        _pessoaFisicaWorkSection.Children.Add(WrapFields(
            ("Nome da empresa", PessoaNomeEmpresaTrabalhoBox, 260),
            ("CNPJ", _pessoaCnpjEmpresaTrabalhoBox!, 170),
            ("Telefone", PessoaTelefoneEmpresaTrabalhoBox, 160),
            ("E-mail", _pessoaEmailEmpresaTrabalhoBox!, 260),
            ("Cargo", _pessoaCargoTrabalhoBox!, 160),
            ("Renda", _pessoaRendaTrabalhoBox!, 120),
            ("Tempo no emprego", _pessoaTempoEmpregoBox!, 140),
            ("Tipo de comprovante de renda", _pessoaTipoComprovanteRendaBox!, 210),
            ("Outras informações", _pessoaTrabalhoOutrasInformacoesBox!, 360)));
        _pessoaFisicaWorkSection.Children.Add(AddressHeader("ENDEREÇO DA EMPRESA:", _pessoaTrabalhoCepBox!));
        _pessoaFisicaWorkSection.Children.Add(WrapFields(
            ("Rua", _pessoaTrabalhoRuaBox!, 300),
            ("Número", _pessoaTrabalhoNumeroBox!, 90),
            ("Complemento", _pessoaTrabalhoComplementoBox!, 220),
            ("Bairro", _pessoaTrabalhoBairroBox!, 180),
            ("Estado", _pessoaTrabalhoEstadoBox!, 80),
            ("Cidade", _pessoaTrabalhoCidadeBox!, 180)));
        PessoaFisicaFieldsPanel.Children.Add(_pessoaFisicaWorkSection);
        RemoveLegacyPessoaFisicaWorkFields();

        _pessoaConjugeSection = new StackPanel { Tag = "PessoaDetailedFields", Margin = new Thickness(0, 10, 0, 12) };
        _pessoaConjugeSection.Children.Add(SectionHeader("DADOS DO CÔNJUGE:"));
        _pessoaConjugeSection.Children.Add(WrapFields(
            ("Trabalho", _pessoaConjugeWorkComboBox!, 150),
            ("Nome", PessoaConjugeNomeBox, 260),
            ("CPF", PessoaConjugeCpfBox, 170),
            ("RG", PessoaConjugeRgBox, 150),
            ("Telefone", PessoaConjugeTelefoneBox, 160),
            ("E-mail", _pessoaConjugeEmailBox!, 260),
            ("Data de nascimento", PessoaConjugeDataNascimentoBox, 140),
            ("Profissão", PessoaConjugeProfissaoBox, 170),
            ("Nacionalidade", PessoaConjugeNacionalidadeBox, 150),
            ("Dados bancários", _pessoaConjugeDadosBancariosBox!, 360),
            ("Outras informações", _pessoaConjugeOutrasInformacoesBox!, 360)));
        PessoaFisicaFieldsPanel.Children.Add(_pessoaConjugeSection);
        RemoveLegacyPessoaConjugeLabels();

        _pessoaConjugeWorkSection = new StackPanel { Tag = "PessoaDetailedFields", Margin = new Thickness(0, 10, 0, 12) };
        _pessoaConjugeWorkSection.Children.Add(SectionHeader("DADOS DO TRABALHO (CÔNJUGE):"));
        _pessoaConjugeWorkSection.Children.Add(WrapFields(
            ("Nome da empresa", _pessoaConjugeNomeEmpresaTrabalhoBox!, 260),
            ("CNPJ", _pessoaConjugeCnpjEmpresaTrabalhoBox!, 170),
            ("Telefone", _pessoaConjugeTelefoneEmpresaTrabalhoBox!, 160),
            ("E-mail", _pessoaConjugeEmailEmpresaTrabalhoBox!, 260),
            ("Cargo", _pessoaConjugeCargoTrabalhoBox!, 160),
            ("Renda", _pessoaConjugeRendaTrabalhoBox!, 120),
            ("Tempo no emprego", _pessoaConjugeTempoEmpregoBox!, 140),
            ("Tipo de comprovante de renda", _pessoaConjugeTipoComprovanteRendaBox!, 210),
            ("Outras informações", _pessoaConjugeTrabalhoOutrasInformacoesBox!, 360)));
        _pessoaConjugeWorkSection.Children.Add(AddressHeader("ENDEREÇO DA EMPRESA:", _pessoaConjugeEmpresaCepBox!));
        _pessoaConjugeWorkSection.Children.Add(WrapFields(
            ("Rua", _pessoaConjugeEmpresaRuaBox!, 300),
            ("Número", _pessoaConjugeEmpresaNumeroBox!, 90),
            ("Complemento", _pessoaConjugeEmpresaComplementoBox!, 220),
            ("Bairro", _pessoaConjugeEmpresaBairroBox!, 180),
            ("Estado", _pessoaConjugeEmpresaEstadoBox!, 80),
            ("Cidade", _pessoaConjugeEmpresaCidadeBox!, 180)));
        PessoaFisicaFieldsPanel.Children.Add(_pessoaConjugeWorkSection);
    }

    private void AddPessoaJuridicaDetailedFields()
    {
        RenameSection(PessoaJuridicaFieldsPanel, "Endereço da empresa:", "ENDEREÇO DA EMPRESA:");
        ReplaceSectionHeaderWithCep(PessoaJuridicaFieldsPanel, "ENDEREÇO DA EMPRESA:", PessoaEmpresaCepBox);
        var companyRow = new WrapPanel { Tag = "PessoaDetailedFields", Margin = new Thickness(0, 0, 0, 12) };
        companyRow.Children.Insert(0, FieldStack("Nome fantasia", _pessoaNomeFantasiaBox!, 260));
        companyRow.Children.Add(FieldStack("Receita mensal", _pessoaReceitaMensalBox!, 140));
        companyRow.Children.Add(FieldStack("Atividade", _pessoaAtividadeBox!, 220));
        companyRow.Children.Add(FieldStack("Inscrição estadual", _pessoaInscricaoEstadualBox!, 160));
        companyRow.Children.Add(FieldStack("Inscrição municipal", _pessoaInscricaoMunicipalBox!, 160));
        companyRow.Children.Add(FieldStack("Data de abertura", _pessoaDataAberturaBox!, 140));
        PessoaJuridicaFieldsPanel.Children.Insert(0, companyRow);

        InsertSectionHeaderBefore(PessoaJuridicaFieldsPanel, "Nome do responsável", "DADOS DO REPRESENTANTE LEGAL:");
        var responsibleRow = new WrapPanel { Tag = "PessoaDetailedFields", Margin = new Thickness(0, 0, 0, 12) };
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "Nome do responsável", PessoaResponsavelNomeBox, 260);
        responsibleRow.Children.Insert(0, FieldStack("Cargo", _pessoaResponsavelCargoBox!, 160));
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "CPF do responsável", PessoaResponsavelCpfBox, 170);
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "RG do responsável", PessoaResponsavelRgBox, 150);
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "Telefone do responsável", PessoaResponsavelTelefoneBox, 160);
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "E-mail do responsável", PessoaResponsavelEmailBox, 260);
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "Nascimento do responsável", PessoaResponsavelDataNascimentoBox, 140);
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "Profissão do responsável", PessoaResponsavelProfissaoBox, 170);
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "Nacionalidade do responsável", PessoaResponsavelNacionalidadeBox, 150);
        responsibleRow.Children.Add(FieldStack("Outras informações", _pessoaResponsavelObservacoesBox!, 360));
        PessoaJuridicaFieldsPanel.Children.Add(responsibleRow);
        PessoaJuridicaFieldsPanel.Children.Add(CreateBankSection(
            PessoaResponsavelDadosBancariosBox,
            _pessoaResponsavelBancoBox!, _pessoaResponsavelAgenciaNumeroBox!, _pessoaResponsavelAgenciaDigitoBox!,
            _pessoaResponsavelContaNumeroBox!, _pessoaResponsavelContaDigitoBox!, _pessoaResponsavelContaTipoBox!, _pessoaResponsavelTitularNomeBox!,
            _pessoaResponsavelTitularDocumentoBox!, _pessoaResponsavelPixTipoBox!, _pessoaResponsavelPixChaveBox!, _pessoaResponsavelRepassePreferencialBox!,
            _pessoaUsarDadosResponsavelBancoButton!, _pessoaResponsavelUsarPixButton!));
        RemoveTextBlock(PessoaJuridicaFieldsPanel, "Dados bancários");
        RemoveLegacyPessoaResponsavelWorkFields();

        RenameSection(PessoaJuridicaFieldsPanel, "Endereço do responsável:", "ENDEREÇO DE RESIDÊNCIA:");
        ReplaceSectionHeaderWithCep(PessoaJuridicaFieldsPanel, "ENDEREÇO DE RESIDÊNCIA:", PessoaResponsavelCepBox);
    }

    private void RemoveLegacyPessoaFisicaWorkFields()
    {
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Nome da empresa");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Telefone da empresa");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Onde trabalha");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Endereço do trabalho");
        RemoveChild(PessoaFisicaFieldsPanel, PessoaOndeTrabalhaBox);
        RemoveChild(PessoaFisicaFieldsPanel, PessoaEnderecoTrabalhoBox);
    }

    private void RemoveLegacyPessoaConjugeLabels()
    {
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Cônjuge");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "RG do cônjuge");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "CPF do cônjuge");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Nascimento do cônjuge");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Profissão do cônjuge");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Nacionalidade do cônjuge");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Telefone do cônjuge");
    }

    private void RemoveLegacyPessoaResponsavelWorkFields()
    {
        RemoveTextBlock(PessoaJuridicaFieldsPanel, "Estado civil do responsável");
        RemoveTextBlock(PessoaJuridicaFieldsPanel, "Onde trabalha");
        RemoveTextBlock(PessoaJuridicaFieldsPanel, "Endereço do trabalho");
        RemoveTextBlock(PessoaJuridicaFieldsPanel, "Empresa onde trabalha");
        RemoveTextBlock(PessoaJuridicaFieldsPanel, "Telefone da empresa");
        RemoveChild(PessoaJuridicaFieldsPanel, PessoaResponsavelEstadoCivilBox);
        RemoveChild(PessoaJuridicaFieldsPanel, PessoaResponsavelOndeTrabalhaBox);
        RemoveChild(PessoaJuridicaFieldsPanel, PessoaResponsavelEnderecoTrabalhoBox);
        RemoveChild(PessoaJuridicaFieldsPanel, PessoaResponsavelNomeEmpresaTrabalhoBox);
        RemoveChild(PessoaJuridicaFieldsPanel, PessoaResponsavelTelefoneEmpresaTrabalhoBox);
    }

    private void FillPessoaBankHolderFromPessoa()
    {
        SetText(_pessoaTitularNomeBox, PessoaNomeBox.Text);
        SetText(_pessoaTitularDocumentoBox, PessoaDocumentoBox.Text);
    }

    private void FillResponsavelBankHolderFromResponsavel()
    {
        SetText(_pessoaResponsavelTitularNomeBox, PessoaResponsavelNomeBox.Text);
        SetText(_pessoaResponsavelTitularDocumentoBox, PessoaResponsavelCpfBox.Text);
    }

    private void FillPessoaPixFromSelectedType() =>
        FillPixFromSelectedType(_pessoaPixTipoBox, _pessoaPixChaveBox, PessoaDocumentoBox.Text, null, PessoaEmailBox.Text, PessoaTelefoneBox.Text);

    private void FillResponsavelPixFromSelectedType() =>
        FillPixFromSelectedType(_pessoaResponsavelPixTipoBox, _pessoaResponsavelPixChaveBox, PessoaResponsavelCpfBox.Text, null, PessoaResponsavelEmailBox.Text, PessoaResponsavelTelefoneBox.Text);

    private static void FillPixFromSelectedType(ComboBox? tipoBox, TextBox? chaveBox, string? cpf, string? cnpj, string? email, string? telefone)
    {
        if (tipoBox?.SelectedValue is not Monthoya.Core.Entities.PixChaveTipo pixTipo || chaveBox is null)
        {
            return;
        }

        var source = pixTipo switch
        {
            Monthoya.Core.Entities.PixChaveTipo.Cpf => cpf,
            Monthoya.Core.Entities.PixChaveTipo.Cnpj => cnpj,
            Monthoya.Core.Entities.PixChaveTipo.Email => email,
            Monthoya.Core.Entities.PixChaveTipo.Telefone => telefone,
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(source))
        {
            chaveBox.Text = source.Trim();
        }
    }

    private void ConfigurePessoaBankInputBehavior()
    {
        if (_pessoaBankInputBehaviorConfigured)
        {
            return;
        }

        _pessoaBankInputBehaviorConfigured = true;
        RegisterDigitsOnly(_pessoaAgenciaNumeroBox, 6);
        RegisterDigitsOnly(_pessoaContaNumeroBox, 14);
        RegisterDigitOrX(_pessoaAgenciaDigitoBox);
        RegisterDigitOrX(_pessoaContaDigitoBox);
        RegisterCpfCnpjFormatter(_pessoaTitularDocumentoBox);
        RegisterPixFormatter(_pessoaPixTipoBox, _pessoaPixChaveBox);

        RegisterDigitsOnly(_pessoaResponsavelAgenciaNumeroBox, 6);
        RegisterDigitsOnly(_pessoaResponsavelContaNumeroBox, 14);
        RegisterDigitOrX(_pessoaResponsavelAgenciaDigitoBox);
        RegisterDigitOrX(_pessoaResponsavelContaDigitoBox);
        RegisterCpfCnpjFormatter(_pessoaResponsavelTitularDocumentoBox);
        RegisterPixFormatter(_pessoaResponsavelPixTipoBox, _pessoaResponsavelPixChaveBox);
    }

    private void RegisterDigitsOnly(TextBox? textBox, int maxDigits)
    {
        if (textBox is null)
        {
            return;
        }

        textBox.PreviewTextInput += (_, e) => e.Handled = e.Text.Any(ch => !char.IsDigit(ch));
        textBox.TextChanged += (_, _) => FormatPessoaBankTextBox(textBox, OnlyDigits(textBox.Text, maxDigits));
        DataObject.AddPastingHandler(textBox, (_, e) => ReplacePastedText(e, value => OnlyDigits(value, maxDigits)));
    }

    private void RegisterDigitOrX(TextBox? textBox)
    {
        if (textBox is null)
        {
            return;
        }

        textBox.PreviewTextInput += (_, e) => e.Handled = e.Text.Any(ch => !char.IsDigit(ch) && char.ToUpperInvariant(ch) != 'X');
        textBox.TextChanged += (_, _) => FormatPessoaBankTextBox(textBox, NormalizeDigitOrX(textBox.Text));
        DataObject.AddPastingHandler(textBox, (_, e) => ReplacePastedText(e, NormalizeDigitOrX));
    }

    private void RegisterCpfCnpjFormatter(TextBox? textBox)
    {
        if (textBox is null)
        {
            return;
        }

        textBox.PreviewTextInput += (_, e) => e.Handled = e.Text.Any(ch => !char.IsDigit(ch));
        textBox.TextChanged += (_, _) => FormatPessoaBankTextBox(textBox, FormatCpfCnpjDocument(textBox.Text));
        DataObject.AddPastingHandler(textBox, (_, e) => ReplacePastedText(e, FormatCpfCnpjDocument));
    }

    private void RegisterPixFormatter(ComboBox? pixTipoBox, TextBox? pixChaveBox)
    {
        if (pixTipoBox is null || pixChaveBox is null)
        {
            return;
        }

        pixChaveBox.PreviewTextInput += (_, e) =>
        {
            if (GetPessoaPixType(pixTipoBox) is Monthoya.Core.Entities.PixChaveTipo.Cpf
                or Monthoya.Core.Entities.PixChaveTipo.Cnpj
                or Monthoya.Core.Entities.PixChaveTipo.Telefone)
            {
                e.Handled = e.Text.Any(ch => !char.IsDigit(ch));
            }
        };
        pixChaveBox.TextChanged += (_, _) => FormatPessoaBankTextBox(pixChaveBox, FormatPixByType(pixChaveBox.Text, GetPessoaPixType(pixTipoBox)));
        pixTipoBox.SelectionChanged += (_, _) => FormatPessoaBankTextBox(pixChaveBox, FormatPixByType(pixChaveBox.Text, GetPessoaPixType(pixTipoBox)));
        DataObject.AddPastingHandler(pixChaveBox, (_, e) => ReplacePastedText(e, value => FormatPixByType(value, GetPessoaPixType(pixTipoBox))));
    }

    private void FormatPessoaBankTextBox(TextBox textBox, string formatted)
    {
        if (_isFormattingPessoaBankFields || textBox.Text == formatted)
        {
            return;
        }

        _isFormattingPessoaBankFields = true;
        textBox.Text = formatted;
        textBox.CaretIndex = textBox.Text.Length;
        _isFormattingPessoaBankFields = false;
    }

    private static void ReplacePastedText(DataObjectPastingEventArgs e, Func<string, string> formatter)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        e.DataObject = new DataObject(DataFormats.Text, formatter(text));
    }

    private static Monthoya.Core.Entities.PixChaveTipo? GetPessoaPixType(ComboBox comboBox) =>
        comboBox.SelectedValue is Monthoya.Core.Entities.PixChaveTipo value ? value : null;

    private static string OnlyDigits(string? value, int maxDigits)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).Take(maxDigits).ToArray());
        return digits;
    }

    private static string NormalizeDigitOrX(string? value)
    {
        var first = (value ?? string.Empty)
            .Select(char.ToUpperInvariant)
            .FirstOrDefault(ch => char.IsDigit(ch) || ch == 'X');
        return first == default ? string.Empty : first.ToString();
    }

    private static string FormatCpfCnpjDocument(string? value)
    {
        var digits = OnlyDigits(value, 14);
        return digits.Length <= 11 ? FormatCpfDigits(digits) : FormatCnpjDigits(digits);
    }

    private static string FormatPixByType(string? value, Monthoya.Core.Entities.PixChaveTipo? pixType) =>
        pixType switch
        {
            Monthoya.Core.Entities.PixChaveTipo.Cpf => FormatCpfDigits(OnlyDigits(value, 11)),
            Monthoya.Core.Entities.PixChaveTipo.Cnpj => FormatCnpjDigits(OnlyDigits(value, 14)),
            Monthoya.Core.Entities.PixChaveTipo.Telefone => FormatPhoneDigits(OnlyDigits(value, 11)),
            Monthoya.Core.Entities.PixChaveTipo.Email => (value ?? string.Empty).Trim(),
            _ => value ?? string.Empty
        };

    private static string FormatCpfDigits(string digits) =>
        digits.Length switch
        {
            <= 3 => digits,
            <= 6 => $"{digits[..3]}.{digits[3..]}",
            <= 9 => $"{digits[..3]}.{digits.Substring(3, 3)}.{digits[6..]}",
            _ => $"{digits[..3]}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits[9..]}"
        };

    private static string FormatCnpjDigits(string digits) =>
        digits.Length switch
        {
            <= 2 => digits,
            <= 5 => $"{digits[..2]}.{digits[2..]}",
            <= 8 => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits[5..]}",
            <= 12 => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}/{digits[8..]}",
            _ => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}/{digits.Substring(8, 4)}-{digits[12..]}"
        };

    private static string FormatPhoneDigits(string digits)
    {
        if (digits.Length <= 2)
        {
            return digits;
        }

        var ddd = digits[..2];
        var number = digits[2..];
        if (number.Length <= 4)
        {
            return $"({ddd}) {number}";
        }

        return number.Length <= 8
            ? $"({ddd}) {number[..4]}-{number[4..]}"
            : $"({ddd}) {number[..5]}-{number[5..]}";
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

    private static void SetFieldLabel(Control control, string labelText)
    {
        if (control.Parent is StackPanel parent
            && parent.Children.OfType<TextBlock>().FirstOrDefault() is TextBlock label)
        {
            label.Text = labelText;
        }
    }

    private static TextBox NewTextBox(double width) => new()
    {
        Width = width,
        HorizontalAlignment = HorizontalAlignment.Left,
        Margin = new Thickness(0, 6, 0, 0)
    };

    private static TextBox NewMultilineBox(double height) => new()
    {
        Width = 360,
        Height = height,
        AcceptsReturn = true,
        TextWrapping = TextWrapping.Wrap,
        HorizontalAlignment = HorizontalAlignment.Left,
        Margin = new Thickness(0, 6, 0, 0)
    };

    private static TextBox NewMultilineBox(double width, double height)
    {
        var textBox = NewMultilineBox(height);
        textBox.Width = width;
        return textBox;
    }

    private static ComboBox NewBankComboBox()
    {
        var comboBox = new ComboBox
        {
            Width = 290,
            Margin = new Thickness(0, 6, 0, 0),
            IsEditable = true,
            IsTextSearchEnabled = false,
            StaysOpenOnEdit = true,
            ItemsSource = BrazilianBankCatalog
        };
        TextSearch.SetTextPath(comboBox, nameof(BankOption.SearchText));
        return comboBox;
    }

    private static Button NewSmallBankActionButton(string content, RoutedEventHandler clickHandler)
    {
        var button = new Button
        {
            Content = content,
            Style = (Style)Application.Current.FindResource("SecondaryButton"),
            Margin = new Thickness(0, 6, 8, 0),
            Padding = new Thickness(10, 4, 10, 4),
            MinHeight = 28
        };
        button.Click += clickHandler;
        return button;
    }

    private static void AttachBankSelection(ComboBox bankBox, TextBox codigoBox, TextBox nomeBox)
    {
        bankBox.DropDownOpened += (_, _) =>
        {
            if (GetEditableComboTextBox(bankBox) is TextBox { Text.Length: 0 })
            {
                bankBox.ItemsSource = BrazilianBankCatalog;
            }
        };

        bankBox.Loaded += (_, _) =>
        {
            bankBox.ApplyTemplate();
            if (GetEditableComboTextBox(bankBox) is not TextBox textBox)
            {
                return;
            }

            textBox.TextChanged += (_, _) =>
            {
                if (bankBox.SelectedItem is BankOption selectedBank
                    && textBox.Text.Equals(selectedBank.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (bankBox.IsKeyboardFocusWithin)
                {
                    var query = textBox.Text;
                    ApplyBankSearch(bankBox, query);
                    bankBox.Dispatcher.BeginInvoke(() =>
                    {
                        if (GetEditableComboTextBox(bankBox) is not { } editableTextBox
                            || editableTextBox.Text == query)
                        {
                            return;
                        }

                        editableTextBox.Text = query;
                        editableTextBox.CaretIndex = editableTextBox.Text.Length;
                    }, DispatcherPriority.Background);
                }
            };
        };

        bankBox.SelectionChanged += (_, _) =>
        {
            if (bankBox.SelectedItem is BankOption bank)
            {
                codigoBox.Text = bank.Code;
                nomeBox.Text = bank.Name;
            }
        };
    }

    private static TextBox? GetEditableComboTextBox(ComboBox comboBox) =>
        comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;

    private static void ApplyBankSearch(ComboBox bankBox, string searchText)
    {
        var query = searchText.Trim();
        if (query.Length == 0)
        {
            bankBox.ItemsSource = BrazilianBankCatalog;
            return;
        }

        var normalizedQuery = NormalizeBankSearchText(query);
        var matches = BrazilianBankCatalog
            .Where(bank => bank.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
                           || NormalizeBankSearchText(bank.Name).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                           || NormalizeBankSearchText(bank.ToString()).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                           || NormalizeBankSearchText(bank.SearchText).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            .ToArray();
        bankBox.ItemsSource = matches;

        bankBox.IsDropDownOpen = true;
    }

    private static string NormalizeBankSearchText(string value)
    {
        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var builder = new System.Text.StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    private static ComboBox NewContaTipoComboBox() => new()
    {
        Width = 130,
        Margin = new Thickness(0, 6, 0, 0),
        ItemsSource = new EnumOption<Monthoya.Core.Entities.ContaBancariaTipo>[]
        {
            new(Monthoya.Core.Entities.ContaBancariaTipo.Corrente, "Corrente"),
            new(Monthoya.Core.Entities.ContaBancariaTipo.Poupanca, "Poupança"),
            new(Monthoya.Core.Entities.ContaBancariaTipo.Pagamento, "Pagamento"),
            new(Monthoya.Core.Entities.ContaBancariaTipo.Outro, "Outro")
        },
        SelectedValuePath = "Value",
        DisplayMemberPath = "Label",
        SelectedIndex = -1
    };

    private static ComboBox NewPixTipoComboBox() => new()
    {
        Width = 130,
        Margin = new Thickness(0, 6, 0, 0),
        ItemsSource = new EnumOption<Monthoya.Core.Entities.PixChaveTipo>[]
        {
            new(Monthoya.Core.Entities.PixChaveTipo.Cpf, "CPF"),
            new(Monthoya.Core.Entities.PixChaveTipo.Cnpj, "CNPJ"),
            new(Monthoya.Core.Entities.PixChaveTipo.Email, "E-mail"),
            new(Monthoya.Core.Entities.PixChaveTipo.Telefone, "Telefone"),
            new(Monthoya.Core.Entities.PixChaveTipo.Aleatoria, "Aleatória"),
            new(Monthoya.Core.Entities.PixChaveTipo.Outro, "Outro")
        },
        SelectedValuePath = "Value",
        DisplayMemberPath = "Label",
        SelectedIndex = -1
    };

    private static ComboBox NewRepassePreferencialComboBox() => new()
    {
        Width = 160,
        Margin = new Thickness(0, 6, 0, 0),
        ItemsSource = new EnumOption<Monthoya.Core.Entities.MetodoRepassePreferencial>[]
        {
            new(Monthoya.Core.Entities.MetodoRepassePreferencial.Pix, "PIX"),
            new(Monthoya.Core.Entities.MetodoRepassePreferencial.TransferenciaBancaria, "Transferência"),
            new(Monthoya.Core.Entities.MetodoRepassePreferencial.Manual, "Manual")
        },
        SelectedValuePath = "Value",
        DisplayMemberPath = "Label",
        SelectedIndex = -1
    };

    private static StackPanel CreateBankSection(
        TextBox observacoes,
        ComboBox banco,
        TextBox agenciaNumero,
        TextBox agenciaDigito,
        TextBox contaNumero,
        TextBox contaDigito,
        ComboBox contaTipo,
        TextBox titularNome,
        TextBox titularDocumento,
        ComboBox pixTipo,
        TextBox pixChave,
        ComboBox repassePreferencial,
        Button usarDadosButton,
        Button usarPixButton)
    {
        var actionRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 6),
            Children = { usarDadosButton, usarPixButton }
        };

        return new StackPanel
        {
            Tag = "PessoaBankFields",
            Margin = new Thickness(0, 10, 0, 12),
            Children =
            {
                SectionHeader("DADOS BANCÁRIOS / PIX:"),
                actionRow,
                WrapFields(
                    ("Banco", banco, 290),
                    ("Agência", agenciaNumero, 100),
                    ("Dígito ag.", agenciaDigito, 70),
                    ("Conta", contaNumero, 130),
                    ("Dígito conta", contaDigito, 80),
                    ("Tipo de conta", contaTipo, 130),
                    ("Titular", titularNome, 220),
                    ("CPF/CNPJ titular", titularDocumento, 170),
                    ("Tipo PIX", pixTipo, 130),
                    ("Chave PIX", pixChave, 220),
                    ("Repasse preferencial", repassePreferencial, 160),
                    ("Observações bancárias", observacoes, 360))
            }
        };
    }

    private static DatePicker NewDatePicker() => new()
    {
        Width = 140,
        HorizontalAlignment = HorizontalAlignment.Left,
        Margin = new Thickness(0, 6, 0, 0),
        Language = System.Windows.Markup.XmlLanguage.GetLanguage("pt-BR"),
        SelectedDateFormat = DatePickerFormat.Short
    };

    private static TextBlock SectionHeader(string text) => new()
    {
        Text = text,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 8, 0, 8)
    };

    private static DockPanel AddressHeader(string text, Control cepControl)
    {
        RemoveFromCurrentParent(cepControl);
        cepControl.Width = 120;
        cepControl.HorizontalAlignment = HorizontalAlignment.Left;
        cepControl.Margin = new Thickness(8, 0, 0, 0);

        var header = new DockPanel
        {
            Tag = text,
            LastChildFill = false,
            Margin = new Thickness(0, 8, 0, 8)
        };
        header.Children.Add(new TextBlock
        {
            Text = text,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        });
        header.Children.Add(new TextBlock
        {
            Text = "CEP",
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(18, 0, 0, 0)
        });
        header.Children.Add(cepControl);
        return header;
    }

    private static void ReplaceSectionHeaderWithCep(StackPanel parent, string sectionText, Control cepControl)
    {
        RemoveTextBlock(parent, "CEP");
        RemoveTextBlock(parent, "CEP da empresa");
        RemoveTextBlock(parent, "CEP do responsável");
        var index = parent.Children
            .OfType<TextBlock>()
            .Select(block => new { Block = block, Index = parent.Children.IndexOf(block) })
            .FirstOrDefault(x => x.Block.Text == sectionText)?.Index;
        if (index is null)
        {
            return;
        }

        parent.Children.RemoveAt(index.Value);
        parent.Children.Insert(index.Value, AddressHeader(sectionText, cepControl));
    }

    private static WrapPanel WrapFields(params (string Label, Control Control, double Width)[] fields)
    {
        var row = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        foreach (var field in fields)
        {
            row.Children.Add(FieldStack(field.Label, field.Control, field.Width));
        }

        return row;
    }

    private static StackPanel FieldStack(string labelText, Control control, double width)
    {
        RemoveFromCurrentParent(control);
        control.Width = width;
        control.HorizontalAlignment = HorizontalAlignment.Left;
        control.Margin = new Thickness(0, 6, 0, 0);
        return new StackPanel
        {
            Width = width,
            Margin = new Thickness(0, 0, 14, 12),
            Children =
            {
                new TextBlock { Text = labelText, FontWeight = FontWeights.SemiBold },
                control
            }
        };
    }

    private sealed record EnumOption<T>(T Value, string Label);
    private sealed record BankOption(string Code, string Name)
    {
        public string SearchText => $"{Code} {Name}";
        public override string ToString() => $"{Code} - {Name}";
    }

    private static readonly BankOption[] BrazilianBankCatalog =
    [
        new("001", "Banco do Brasil"),
        new("003", "Banco da Amazônia"),
        new("004", "Banco do Nordeste"),
        new("021", "Banestes"),
        new("033", "Santander"),
        new("041", "Banrisul"),
        new("070", "BRB"),
        new("077", "Banco Inter"),
        new("104", "Caixa Econômica Federal"),
        new("121", "Banco Agibank"),
        new("136", "Unicred"),
        new("197", "Stone"),
        new("208", "BTG Pactual"),
        new("212", "Banco Original"),
        new("237", "Bradesco"),
        new("260", "Nubank"),
        new("290", "PagBank"),
        new("318", "Banco BMG"),
        new("320", "China Construction Bank Brasil"),
        new("323", "Mercado Pago"),
        new("336", "Banco C6"),
        new("341", "Itaú Unibanco"),
        new("422", "Banco Safra"),
        new("623", "Banco PAN"),
        new("633", "Banco Rendimento"),
        new("655", "Banco Votorantim"),
        new("707", "Banco Daycoval"),
        new("735", "Banco Neon"),
        new("739", "Banco Cetelem"),
        new("748", "Sicredi"),
        new("756", "Sicoob")
    ];

    private static void RemoveFromCurrentParent(UIElement element)
    {
        if (element is FrameworkElement { Parent: Panel parentPanel })
        {
            parentPanel.Children.Remove(element);
            return;
        }

        if (element is FrameworkElement { Parent: ContentControl contentControl }
            && ReferenceEquals(contentControl.Content, element))
        {
            contentControl.Content = null;
            return;
        }

        if (element is FrameworkElement { Parent: Decorator decorator }
            && ReferenceEquals(decorator.Child, element))
        {
            decorator.Child = null;
        }
    }

    private static void InsertSectionHeader(StackPanel parent, string text, int index)
    {
        parent.Children.Insert(Math.Min(index, parent.Children.Count), SectionHeader(text));
    }

    private static void InsertSectionHeaderBefore(StackPanel parent, string beforeLabel, string sectionText)
    {
        var index = parent.Children
            .OfType<TextBlock>()
            .Select(block => new { Block = block, Index = parent.Children.IndexOf(block) })
            .FirstOrDefault(x => x.Block.Text == beforeLabel)?.Index ?? parent.Children.Count;
        parent.Children.Insert(index, SectionHeader(sectionText));
    }

    private static void RenameSection(StackPanel parent, string oldText, string newText)
    {
        var label = parent.Children.OfType<TextBlock>().FirstOrDefault(block => block.Text == oldText);
        if (label is not null)
        {
            label.Text = newText;
        }
    }

    private void ArrangePessoaAddressFieldsIntoRows()
    {
        ArrangeAddressFields(PessoaFisicaFieldsPanel, "PessoaResidencialAddressFieldsRow", "ENDEREÇO DE RESIDÊNCIA:",
            ("Rua", PessoaRuaBox, 300),
            ("Número", PessoaNumeroBox, 90),
            ("Complemento", PessoaComplementoBox, 220),
            ("Bairro", PessoaBairroBox, 220),
            ("Estado", PessoaEstadoBox, 80),
            ("Cidade", PessoaCidadeBox, 180));

        ArrangeAddressFields(PessoaJuridicaFieldsPanel, "PessoaEmpresaAddressFieldsRow", "ENDEREÇO DA EMPRESA:",
            ("Rua da empresa", PessoaEmpresaRuaBox, 300),
            ("Número da empresa", PessoaEmpresaNumeroBox, 90),
            ("Complemento da empresa", PessoaEmpresaComplementoBox, 220),
            ("Bairro da empresa", PessoaEmpresaBairroBox, 220),
            ("Estado da empresa", PessoaEmpresaEstadoBox, 80),
            ("Cidade da empresa", PessoaEmpresaCidadeBox, 180));

        ArrangeAddressFields(PessoaJuridicaFieldsPanel, "PessoaResponsavelAddressFieldsRow", "ENDEREÇO DE RESIDÊNCIA:",
            ("Rua do responsável", PessoaResponsavelRuaBox, 300),
            ("Número do responsável", PessoaResponsavelNumeroBox, 90),
            ("Complemento do responsável", PessoaResponsavelComplementoBox, 220),
            ("Bairro do responsável", PessoaResponsavelBairroBox, 220),
            ("Estado do responsável", PessoaResponsavelEstadoBox, 80),
            ("Cidade do responsável", PessoaResponsavelCidadeBox, 180));
    }

    private static void ArrangeAddressFields(StackPanel parent, string rowTag, string sectionText, params (string Label, Control Control, double Width)[] fields)
    {
        if (parent.Children.OfType<WrapPanel>().Any(panel => panel.Tag as string == rowTag))
        {
            return;
        }

        var row = new WrapPanel
        {
            Tag = rowTag,
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
            if ((parent.Children[index] is TextBlock block
                    && block.Text.Equals(sectionText, StringComparison.OrdinalIgnoreCase))
                || (parent.Children[index] is FrameworkElement element
                    && element.Tag as string == sectionText))
            {
                insertIndex = index + 1;
                break;
            }
        }

        parent.Children.Insert(insertIndex, row);
    }

    private static StackPanel MoveFieldToWrapPanel(StackPanel source, WrapPanel target, string labelText, Control control, double width)
    {
        var label = RemoveTextBlock(source, labelText) ?? new TextBlock { Text = labelText, FontWeight = FontWeights.SemiBold };
        RemoveFromCurrentParent(label);
        RemoveFromCurrentParent(control);
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
        return field;
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
            ItemsSource = new[] { "Não possui trabalho", "Possui trabalho" },
            SelectedIndex = -1
        };
    }

    private void CreatePessoaPetControls()
    {
        if (_pessoaPetComboBox is not null)
        {
            return;
        }

        _pessoaPetQualBox = new TextBox
        {
            Width = 150,
            Margin = new Thickness(0, 6, 0, 0),
            Visibility = Visibility.Collapsed
        };
        _pessoaPetComboBox = new ComboBox
        {
            Width = 100,
            Margin = new Thickness(0, 6, 0, 0),
            ItemsSource = new[] { "Não", "Sim" },
            SelectedIndex = -1
        };
        _pessoaPetComboBox.SelectionChanged += (_, _) =>
        {
            UpdatePessoaPetQualVisibility();
        };
    }

    private void UpdatePessoaPetQualVisibility()
    {
        var isJuridica = PessoaTipoBox.SelectedValue is Monthoya.Core.Entities.TipoPessoa.Juridica;
        var hasPet = string.Equals(_pessoaPetComboBox?.SelectedItem as string, "Sim", StringComparison.OrdinalIgnoreCase);
        var visibility = !isJuridica && hasPet ? Visibility.Visible : Visibility.Collapsed;

        if (_pessoaPetQualTopCell is not null)
        {
            _pessoaPetQualTopCell.Visibility = visibility;
        }

        if (_pessoaPetQualBox is not null)
        {
            _pessoaPetQualBox.Visibility = visibility;
        }
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
            if (formStack.Children[index] is TextBlock label
                && (label.Text == "Documento digitalizado" || label.Text == "Documentos anexos:"))
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

