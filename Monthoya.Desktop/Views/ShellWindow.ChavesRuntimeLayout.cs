using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ChavesRuntimeLayoutRegistered = RegisterChavesRuntimeLayout();
    private bool _chavesRuntimeLayoutApplied;
    private ComboBox? _chavesMovimentoTipoBox;
    private TextBlock? _chavesActionTitleText;
    private StackPanel? _chavesRetiradaFieldsPanel;
    private StackPanel? _chavesDevolucaoFieldsPanel;

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
        RelabelTextBlockImmediatelyBefore(ChavesRetiradoPorRelacaoBox, "Tipo de pessoa");

        ConfigureChavesSingleList();

        var formHost = FindChavesFormHost();
        if (formHost is not null)
        {
            BuildChavesTopUnifiedForm(formHost);
        }

        SetChavesMovimentoMode(isReturn: false, clearMode: true);
        ApplyChavesFilter();
    }

    private void ConfigureChavesSingleList()
    {
        ChavesGrid.MaxHeight = 280;
        ChavesGrid.SelectionMode = DataGridSelectionMode.Single;
        ChavesGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
        ChavesGrid.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

        if (ChavesGrid.Columns.Count > 0)
        {
            ChavesGrid.Columns.Clear();
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Imóvel", Binding = new System.Windows.Data.Binding("Imovel"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Código", Binding = new System.Windows.Data.Binding("ChaveCodigo"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Situação", Binding = new System.Windows.Data.Binding("Status"), Width = new DataGridLength(1.2, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Retirado por", Binding = new System.Windows.Data.Binding("RetiradoPorNome"), Width = new DataGridLength(1.4, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Telefone", Binding = new System.Windows.Data.Binding("RetiradoPorTelefone"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Tipo de pessoa", Binding = new System.Windows.Data.Binding("TipoPessoa"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Motivo", Binding = new System.Windows.Data.Binding("Motivo"), Width = new DataGridLength(1.2, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Retirado em", Binding = new System.Windows.Data.Binding("RetiradoEm") { StringFormat = "dd/MM/yyyy HH:mm" }, Width = new DataGridLength(1.1, DataGridLengthUnitType.Star) });
            ChavesGrid.Columns.Add(new DataGridTextColumn { Header = "Previsão", Binding = new System.Windows.Data.Binding("PrevisaoDevolucaoEm") { StringFormat = "dd/MM/yyyy HH:mm" }, Width = new DataGridLength(1.1, DataGridLengthUnitType.Star) });
        }
    }

    private Border? FindChavesFormHost()
    {
        return FindVisualChildrenForPeopleRuntimeAdjustment<Border>(ChavesPanel)
            .FirstOrDefault(border => FindVisualChildrenForPeopleRuntimeAdjustment<Button>(border)
                .Any(button => ReferenceEquals(button, SaveChaveRetiradaButton)));
    }

    private void BuildChavesTopUnifiedForm(Border retiradaHost)
    {
        var originalPanel = retiradaHost.Child as StackPanel;
        if (originalPanel is null)
        {
            return;
        }

        var devolucaoHost = FindVisualChildrenForPeopleRuntimeAdjustment<Border>(ChavesPanel)
            .FirstOrDefault(border => FindVisualChildrenForPeopleRuntimeAdjustment<Button>(border)
                .Any(button => ReferenceEquals(button, ReturnChaveButton)));

        if (devolucaoHost is not null)
        {
            devolucaoHost.Visibility = Visibility.Collapsed;
        }

        retiradaHost.Margin = new Thickness(0, 0, 0, 12);
        Grid.SetRow(retiradaHost, 2);
        Grid.SetColumn(retiradaHost, 0);
        Grid.SetColumnSpan(retiradaHost, 2);

        var listsHost = FindVisualChildrenForPeopleRuntimeAdjustment<Border>(ChavesPanel)
            .FirstOrDefault(border => border.Child == ChavesGrid);
        if (listsHost is not null)
        {
            Grid.SetRow(listsHost, 3);
            listsHost.Margin = new Thickness(0, 0, 0, 0);
        }

        var mainPanel = new StackPanel();
        var header = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        _chavesActionTitleText = new TextBlock
        {
            Text = "Retirada de chaves",
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

        _chavesRetiradaFieldsPanel = new StackPanel();
        _chavesRetiradaFieldsPanel.Children.Add(ExtractWrapPanel(originalPanel));
        DetachFromParent(SaveChaveRetiradaButton);
        _chavesRetiradaFieldsPanel.Children.Add(SaveChaveRetiradaButton);

        _chavesDevolucaoFieldsPanel = new StackPanel();
        DetachFromParent(ChavesSelectedMovimentoText);
        _chavesDevolucaoFieldsPanel.Children.Add(ChavesSelectedMovimentoText);
        _chavesDevolucaoFieldsPanel.Children.Add(CreateLabeledField("Recebido por", ChavesDevolvidoParaBox, 260));
        _chavesDevolucaoFieldsPanel.Children.Add(CreateLabeledField("Observações da devolução", ChavesDevolucaoObservacoesBox, 420));
        DetachFromParent(ReturnChaveButton);
        _chavesDevolucaoFieldsPanel.Children.Add(ReturnChaveButton);

        mainPanel.Children.Add(header);
        mainPanel.Children.Add(_chavesRetiradaFieldsPanel);
        mainPanel.Children.Add(_chavesDevolucaoFieldsPanel);
        DetachFromParent(ChavesErrorText);
        mainPanel.Children.Add(ChavesErrorText);
        retiradaHost.Child = mainPanel;
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
        field.Margin = new Thickness(0, 6, 0, 0);
        panel.Children.Add(field);
        return panel;
    }

    private static WrapPanel ExtractWrapPanel(StackPanel sourcePanel)
    {
        var wrap = sourcePanel.Children.OfType<WrapPanel>().FirstOrDefault();
        if (wrap is not null)
        {
            sourcePanel.Children.Remove(wrap);
            return wrap;
        }

        return new WrapPanel();
    }

    private void SetChavesMovimentoMode(bool isReturn, bool clearMode = false)
    {
        if (_chavesMovimentoTipoBox is not null && !clearMode)
        {
            _chavesMovimentoTipoBox.SelectedIndex = isReturn ? 2 : 1;
        }

        if (_chavesActionTitleText is not null)
        {
            _chavesActionTitleText.Text = clearMode
                ? "Retirada de chaves"
                : isReturn ? "Devolução de chaves" : "Retirada de chaves";
        }

        if (_chavesRetiradaFieldsPanel is not null)
        {
            _chavesRetiradaFieldsPanel.Visibility = isReturn && !clearMode ? Visibility.Collapsed : Visibility.Visible;
        }

        if (_chavesDevolucaoFieldsPanel is not null)
        {
            _chavesDevolucaoFieldsPanel.Visibility = isReturn && !clearMode ? Visibility.Visible : Visibility.Collapsed;
        }

        SaveChaveRetiradaButton.Visibility = isReturn && !clearMode ? Visibility.Collapsed : Visibility.Visible;
        ReturnChaveButton.Visibility = isReturn && !clearMode ? Visibility.Visible : Visibility.Collapsed;
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
