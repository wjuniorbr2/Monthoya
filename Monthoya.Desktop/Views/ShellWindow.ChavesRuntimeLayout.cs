using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ChavesRuntimeLayoutRegistered = RegisterChavesRuntimeLayout();
    private bool _chavesRuntimeLayoutApplied;
    private ComboBox? _chavesMovimentoTipoBox;
    private TextBlock? _chavesActionTitleText;
    private TextBlock? _chavesSelectedImovelText;
    private StackPanel? _chavesRetiradaFieldsPanel;
    private StackPanel? _chavesDevolucaoFieldsPanel;
    private ComboBox? _chavesRelacaoComboBox;
    private TextBox? _chavesPrevisaoHoraBox;

    private static bool RegisterChavesRuntimeLayout()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForChavesRuntimeLayout));

        return true;
    }

    private static void OnShellWindowLoadedForChavesRuntimeLayout(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyChavesRuntimeLayout, DispatcherPriority.ContextIdle);
        }
    }

    private void ApplyChavesRuntimeLayout()
    {
        if (_chavesRuntimeLayoutApplied)
        {
            return;
        }

        _chavesRuntimeLayoutApplied = true;

        ChavesImovelBox.Visibility = Visibility.Collapsed;
        HideLabelImmediatelyBefore(ChavesImovelBox);
        RelabelTextBlockImmediatelyBefore(ChavesRetiradoPorRelacaoBox, "Relação");

        ConfigureChavesSingleList();
        RebuildChavesPageLayout();

        SetChavesMovimentoMode(isReturn: false, clearMode: true);
        ApplyChavesFilter();
    }

    private void ConfigureChavesSingleList()
    {
        ChavesGrid.MaxHeight = double.PositiveInfinity;
        ChavesGrid.SelectionMode = DataGridSelectionMode.Single;
        ChavesGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
        ChavesGrid.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        ChavesGrid.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;

        if (ChavesGrid.Columns.Count > 0)
        {
            ChavesGrid.Columns.Clear();
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Código", Binding = new System.Windows.Data.Binding("ChaveCodigo"), Width = new DataGridLength(0.7, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Endereço", Binding = new System.Windows.Data.Binding("Imovel"), Width = new DataGridLength(2.1, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Proprietário", Binding = new System.Windows.Data.Binding("Proprietario"), Width = new DataGridLength(1.5, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Situação", Binding = new System.Windows.Data.Binding("Status"), Width = new DataGridLength(1.2, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Retirado por", Binding = new System.Windows.Data.Binding("RetiradoPorNome"), Width = new DataGridLength(1.3, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Telefone", Binding = new System.Windows.Data.Binding("RetiradoPorTelefone"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Relação", Binding = new System.Windows.Data.Binding("Relacao"), Width = new DataGridLength(0.9, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Motivo", Binding = new System.Windows.Data.Binding("Motivo"), Width = new DataGridLength(1.2, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Retirado em", Binding = new System.Windows.Data.Binding("RetiradoEm") { StringFormat = "dd/MM/yyyy HH:mm" }, Width = new DataGridLength(1.1, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Previsão", Binding = new System.Windows.Data.Binding("PrevisaoDevolucaoEm") { StringFormat = "dd/MM/yyyy HH:mm" }, Width = new DataGridLength(1.1, DataGridLengthUnitType.Star) });
        }
    }

    private void RebuildChavesPageLayout()
    {
        var formHost = FindChavesFormHost();
        var returnHost = FindChavesReturnHost();
        var listHost = FindChavesListHost();
        var searchHost = ChavesSearchBox.Parent as UIElement;

        if (formHost is null || listHost is null || searchHost is null)
        {
            return;
        }

        if (returnHost is not null)
        {
            returnHost.Visibility = Visibility.Collapsed;
        }

        BuildChavesTopUnifiedForm(formHost);

        DetachFromParent(formHost);
        DetachFromParent(searchHost);
        DetachFromParent(listHost);

        var searchSection = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        searchSection.Children.Add(new TextBlock
        {
            Text = "Pesquisar imóveis",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        });
        searchSection.Children.Add(searchHost);

        var contentGrid = new Grid { Margin = new Thickness(0, 16, 0, 0) };
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        formHost.Margin = new Thickness(0, 0, 0, 12);
        Grid.SetRow(formHost, 0);
        contentGrid.Children.Add(formHost);

        Grid.SetRow(searchSection, 1);
        contentGrid.Children.Add(searchSection);

        listHost.Margin = new Thickness(0);
        Grid.SetRow(listHost, 2);
        contentGrid.Children.Add(listHost);

        Grid.SetRow(contentGrid, 1);
        Grid.SetRowSpan(contentGrid, 3);
        ChavesPanel.Children.Add(contentGrid);
    }

    private Border? FindChavesFormHost()
    {
        return FindVisualChildrenForPeopleRuntimeAdjustment<Border>(ChavesPanel)
            .FirstOrDefault(border => FindVisualChildrenForPeopleRuntimeAdjustment<Button>(border)
                .Any(button => ReferenceEquals(button, SaveChaveRetiradaButton)));
    }

    private Border? FindChavesReturnHost()
    {
        return FindVisualChildrenForPeopleRuntimeAdjustment<Border>(ChavesPanel)
            .FirstOrDefault(border => FindVisualChildrenForPeopleRuntimeAdjustment<Button>(border)
                .Any(button => ReferenceEquals(button, ReturnChaveButton)));
    }

    private Border? FindChavesListHost()
    {
        return FindVisualChildrenForPeopleRuntimeAdjustment<Border>(ChavesPanel)
            .FirstOrDefault(border => border.Child == ChavesGrid);
    }

    private void BuildChavesTopUnifiedForm(Border retiradaHost)
    {
        if (retiradaHost.Child is not StackPanel)
        {
            return;
        }

        DetachChavesOriginalFields();

        var mainPanel = new StackPanel();
        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        _chavesActionTitleText = new TextBlock
        {
            Text = "Chaves dos imóveis disponíveis",
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        _chavesMovimentoTipoBox = new ComboBox
        {
            Width = 170,
            Margin = new Thickness(14, 0, 0, 0),
            ItemsSource = new[] { "", "Retirada", "Devolução" },
            SelectedIndex = 0,
            ToolTip = "Escolha Retirada ou Devolução para filtrar a lista"
        };
        _chavesMovimentoTipoBox.SelectionChanged += (_, _) =>
        {
            SetChavesMovimentoMode(_chavesMovimentoTipoBox.SelectedItem as string == "Devolução", clearMode: _chavesMovimentoTipoBox.SelectedIndex == 0);
            ApplyChavesFilter();
        };
        header.Children.Add(_chavesActionTitleText);
        header.Children.Add(_chavesMovimentoTipoBox);

        _chavesSelectedImovelText = new TextBlock
        {
            Foreground = Brushes.DimGray,
            Margin = new Thickness(0, 0, 0, 10),
            TextWrapping = TextWrapping.Wrap
        };

        _chavesRetiradaFieldsPanel = BuildChavesRetiradaPanel();
        _chavesDevolucaoFieldsPanel = BuildChavesDevolucaoPanel();

        mainPanel.Children.Add(header);
        mainPanel.Children.Add(_chavesSelectedImovelText);
        mainPanel.Children.Add(_chavesRetiradaFieldsPanel);
        mainPanel.Children.Add(_chavesDevolucaoFieldsPanel);
        mainPanel.Children.Add(ChavesErrorText);
        retiradaHost.Child = mainPanel;
    }

    private StackPanel BuildChavesRetiradaPanel()
    {
        var panel = new StackPanel();
        var fields = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Left };

        fields.Children.Add(CreateLabeledField("Código", ChavesCodigoBox, 90));
        fields.Children.Add(CreateLabeledField("Retirado por", ChavesRetiradoPorNomeBox, 210));
        fields.Children.Add(CreateLabeledField("Telefone", ChavesRetiradoPorTelefoneBox, 135));
        fields.Children.Add(CreateLabeledField("Documento", ChavesRetiradoPorDocumentoBox, 145));
        fields.Children.Add(CreateRelacaoField());
        fields.Children.Add(CreateLabeledField("Motivo", ChavesMotivoBox, 230));
        fields.Children.Add(CreatePrevisaoField());
        fields.Children.Add(CreateLabeledField("Observações", ChavesObservacoesBox, 300));

        SaveChaveRetiradaButton.HorizontalAlignment = HorizontalAlignment.Left;
        SaveChaveRetiradaButton.MinWidth = 0;
        SaveChaveRetiradaButton.Width = double.NaN;
        SaveChaveRetiradaButton.Padding = new Thickness(18, 8, 18, 8);
        SaveChaveRetiradaButton.Background = new SolidColorBrush(Color.FromRgb(0, 109, 176));
        SaveChaveRetiradaButton.Foreground = Brushes.White;

        panel.Children.Add(fields);
        panel.Children.Add(SaveChaveRetiradaButton);
        return panel;
    }

    private StackPanel BuildChavesDevolucaoPanel()
    {
        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Left };
        var fields = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Left };
        fields.Children.Add(CreateLabeledField("Recebido por", ChavesDevolvidoParaBox, 260));
        fields.Children.Add(CreateLabeledField("Observações da devolução", ChavesDevolucaoObservacoesBox, 420));

        ReturnChaveButton.HorizontalAlignment = HorizontalAlignment.Left;
        ReturnChaveButton.MinWidth = 0;
        ReturnChaveButton.Width = double.NaN;
        ReturnChaveButton.Padding = new Thickness(18, 8, 18, 8);
        ReturnChaveButton.Background = new SolidColorBrush(Color.FromRgb(0, 109, 176));
        ReturnChaveButton.Foreground = Brushes.White;

        panel.Children.Add(ChavesSelectedMovimentoText);
        panel.Children.Add(fields);
        panel.Children.Add(ReturnChaveButton);
        return panel;
    }

    private UIElement CreateRelacaoField()
    {
        _chavesRelacaoComboBox = new ComboBox
        {
            Width = 145,
            Margin = new Thickness(0, 6, 0, 0),
            IsEditable = true,
            ItemsSource = new[]
            {
                "Interessado",
                "Cliente",
                "Locatário",
                "Proprietário",
                "Corretor",
                "Prestador",
                "Terceiro",
                "Outro"
            }
        };
        _chavesRelacaoComboBox.SelectionChanged += (_, _) =>
            ChavesRetiradoPorRelacaoBox.Text = _chavesRelacaoComboBox.Text;
        _chavesRelacaoComboBox.LostFocus += (_, _) =>
            ChavesRetiradoPorRelacaoBox.Text = _chavesRelacaoComboBox.Text;

        var panel = new StackPanel { Width = 145, Margin = new Thickness(0, 0, 14, 12) };
        panel.Children.Add(new TextBlock { Text = "Relação", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(_chavesRelacaoComboBox);
        return panel;
    }

    private UIElement CreatePrevisaoField()
    {
        ChavesPrevisaoBox.Width = 125;
        ChavesPrevisaoBox.Margin = new Thickness(0, 6, 6, 0);
        _chavesPrevisaoHoraBox = new TextBox
        {
            Width = 70,
            Margin = new Thickness(0, 6, 0, 0),
            Text = "18:00",
            ToolTip = "Horário previsto de devolução"
        };

        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.Children.Add(ChavesPrevisaoBox);
        row.Children.Add(_chavesPrevisaoHoraBox);

        var panel = new StackPanel { Width = 210, Margin = new Thickness(0, 0, 14, 12) };
        panel.Children.Add(new TextBlock { Text = "Previsão de devolução", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(row);
        return panel;
    }

    private void DetachChavesOriginalFields()
    {
        DetachFromParent(ChavesCodigoBox);
        DetachFromParent(ChavesRetiradoPorNomeBox);
        DetachFromParent(ChavesRetiradoPorTelefoneBox);
        DetachFromParent(ChavesRetiradoPorDocumentoBox);
        DetachFromParent(ChavesRetiradoPorRelacaoBox);
        DetachFromParent(ChavesPrevisaoBox);
        DetachFromParent(ChavesMotivoBox);
        DetachFromParent(ChavesObservacoesBox);
        DetachFromParent(SaveChaveRetiradaButton);
        DetachFromParent(ChavesSelectedMovimentoText);
        DetachFromParent(ChavesDevolvidoParaBox);
        DetachFromParent(ChavesDevolucaoObservacoesBox);
        DetachFromParent(ReturnChaveButton);
        DetachFromParent(ChavesErrorText);
    }

    private string? GetChavesSelectedMode()
    {
        return _chavesMovimentoTipoBox?.SelectedItem as string;
    }

    private static UIElement CreateLabeledField(string label, FrameworkElement field, double width)
    {
        DetachFromParent(field);

        var panel = new StackPanel { Width = width, Margin = new Thickness(0, 0, 14, 12) };
        panel.Children.Add(new TextBlock { Text = label, FontWeight = FontWeights.SemiBold });
        field.Width = width;
        field.Margin = new Thickness(0, 6, 0, 0);
        panel.Children.Add(field);
        return panel;
    }

    private void SetChavesMovimentoMode(bool isReturn, bool clearMode = false)
    {
        if (_chavesMovimentoTipoBox is not null && !clearMode)
        {
            _chavesMovimentoTipoBox.SelectedIndex = isReturn ? 2 : 1;
        }

        if (_chavesActionTitleText is not null)
        {
            _chavesActionTitleText.Text = "Chaves dos imóveis disponíveis";
        }

        var showFields = !clearMode;
        if (_chavesSelectedImovelText is not null)
        {
            _chavesSelectedImovelText.Visibility = showFields ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_chavesRetiradaFieldsPanel is not null)
        {
            _chavesRetiradaFieldsPanel.Visibility = showFields && !isReturn ? Visibility.Visible : Visibility.Collapsed;
        }

        if (_chavesDevolucaoFieldsPanel is not null)
        {
            _chavesDevolucaoFieldsPanel.Visibility = showFields && isReturn ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void UpdateChavesSelectedImovelSummary(string? imovel, string? proprietario)
    {
        if (_chavesSelectedImovelText is null)
        {
            return;
        }

        _chavesSelectedImovelText.Text = string.IsNullOrWhiteSpace(imovel)
            ? string.Empty
            : $"Imóvel: {imovel} | Proprietário: {proprietario ?? "-"}";
    }

    private void ClearChavesSelectedImovelSummary()
    {
        if (_chavesSelectedImovelText is not null)
        {
            _chavesSelectedImovelText.Text = string.Empty;
        }
    }

    private void ResetChavesRelacaoDropdown()
    {
        if (_chavesRelacaoComboBox is not null)
        {
            _chavesRelacaoComboBox.SelectedIndex = -1;
            _chavesRelacaoComboBox.Text = string.Empty;
        }
    }

    private TimeSpan GetChavesPrevisaoHorario()
    {
        if (_chavesPrevisaoHoraBox is not null
            && TimeSpan.TryParse(_chavesPrevisaoHoraBox.Text, CultureInfo.GetCultureInfo("pt-BR"), out var parsed))
        {
            return parsed;
        }

        return TimeSpan.FromHours(18);
    }

    private static void HideLabelImmediatelyBefore(FrameworkElement field)
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

    private static void RelabelTextBlockImmediatelyBefore(FrameworkElement field, string newLabel)
    {
        if (field.Parent is not Panel panel)
        {
            return;
        }

        var index = panel.Children.IndexOf(field);
        if (index > 0 && panel.Children[index - 1] is TextBlock label)
        {
            label.Text = newLabel;
        }
    }

    private static void DetachFromParent(UIElement element)
    {
        var parent = element is FrameworkElement frameworkElement
            ? frameworkElement.Parent
            : null;

        if (parent is Panel panel)
        {
            panel.Children.Remove(element);
        }
        else if (parent is Decorator decorator)
        {
            decorator.Child = null;
        }
        else if (parent is ContentControl contentControl)
        {
            contentControl.Content = null;
        }
    }
}
