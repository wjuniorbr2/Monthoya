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
        _pessoaCnpjEmpresaTrabalhoBox ??= NewTextBox(190);
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
            Width = 180,
            Margin = new Thickness(0, 6, 0, 0),
            ItemsSource = new[] { "Não possui trabalho", "Possui trabalho" },
            SelectedIndex = -1
        };
        _pessoaConjugeNomeEmpresaTrabalhoBox ??= NewTextBox(260);
        _pessoaConjugeCnpjEmpresaTrabalhoBox ??= NewTextBox(190);
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
            ("CNPJ", _pessoaCnpjEmpresaTrabalhoBox!, 190),
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
            ("Trabalho", _pessoaConjugeWorkComboBox!, 180),
            ("Nome", PessoaConjugeNomeBox, 260),
            ("CPF", PessoaConjugeCpfBox, 190),
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
            ("CNPJ", _pessoaConjugeCnpjEmpresaTrabalhoBox!, 190),
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
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "CPF do responsável", PessoaResponsavelCpfBox, 190);
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
            Width = 180,
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
        SetCompact(PessoaDocumentoBox, 190);
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
        SetCompact(PessoaConjugeCpfBox, 190);
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
        SetCompact(PessoaResponsavelCpfBox, 190);
        SetCompact(PessoaResponsavelRgBox, 150);
        SetCompact(PessoaResponsavelTelefoneBox, 160);
        SetCompact(PessoaResponsavelCepBox, 120);
        SetCompact(PessoaResponsavelEstadoBox, 80);
        SetCompact(PessoaResponsavelNumeroBox, 90);
        SetCompact(PessoaResponsavelDataNascimentoBox, 140);
        SetCompact(PessoaTelefoneEmpresaTrabalhoBox, 160);
        SetCompact(PessoaResponsavelTelefoneEmpresaTrabalhoBox, 160);
        EnsureFieldLabel(PessoaConjugeCpfBox, "CPF");
    }

    private static void SetCompact(Control control, double width)
    {
        control.Width = width;
        control.HorizontalAlignment = HorizontalAlignment.Left;
    }

    private static void EnsureFieldLabel(Control control, string labelText)
    {
        if (control.Parent is not StackPanel parent)
        {
            return;
        }

        var label = parent.Children.OfType<TextBlock>().FirstOrDefault();
        if (label is null)
        {
            parent.Children.Insert(0, new TextBlock { Text = labelText, FontWeight = FontWeights.SemiBold });
        }
        else
        {
            label.Text = labelText;
            label.Visibility = Visibility.Visible;
        }

        control.Margin = new Thickness(0, 6, 0, 0);
    }
}




