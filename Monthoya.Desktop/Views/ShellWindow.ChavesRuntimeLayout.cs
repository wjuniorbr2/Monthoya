using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ChavesRuntimeLayoutRegistered = RegisterChavesRuntimeLayout();
    private bool _chavesRuntimeLayoutApplied;
    private DataGrid? _chavesAvailableImoveisGrid;
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

        var listsHost = FindChavesListsHost();
        if (listsHost is not null)
        {
            BuildChavesTwoListArea(listsHost);
        }

        var formHost = FindChavesFormHost();
        if (formHost is not null)
        {
            BuildChavesUnifiedForm(formHost);
        }

        SetChavesMovimentoMode(isReturn: false);
        ApplyChavesFilter();
    }

    private Border? FindChavesListsHost()
    {
        return FindVisualChildrenForPeopleRuntimeAdjustment<Border>(ChavesPanel)
            .FirstOrDefault(border => border.Child == ChavesGrid);
    }

    private Border? FindChavesFormHost()
    {
        return FindVisualChildrenForPeopleRuntimeAdjustment<Border>(ChavesPanel)
            .FirstOrDefault(border => FindVisualChildrenForPeopleRuntimeAdjustment<Button>(border)
                .Any(button => ReferenceEquals(button, SaveChaveRetiradaButton)));
    }

    private void BuildChavesTwoListArea(Border host)
    {
        host.Margin = new Thickness(0, 12, 0, 12);
        host.MinHeight = 160;
        host.MaxHeight = 260;

        _chavesAvailableImoveisGrid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            BorderThickness = new Thickness(0),
            MaxHeight = 220,
            SelectionMode = DataGridSelectionMode.Single,
            SelectionUnit = DataGridSelectionUnit.FullRow,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        _chavesAvailableImoveisGrid.Columns.Add(new DataGridTextColumn { Header = "Imóvel disponível", Binding = new System.Windows.Data.Binding("Endereco"), Width = new DataGridLength(2, DataGridLengthUnitType.Star) });
        _chavesAvailableImoveisGrid.Columns.Add(new DataGridTextColumn { Header = "Código", Binding = new System.Windows.Data.Binding("Chaves"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        _chavesAvailableImoveisGrid.Columns.Add(new DataGridTextColumn { Header = "Status", Binding = new System.Windows.Data.Binding("Status"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        _chavesAvailableImoveisGrid.SelectionChanged += (_, _) =>
        {
            if (_chavesAvailableImoveisGrid.SelectedItem is ImovelSummary imovel)
            {
                SelectChavesAvailableImovel(imovel);
            }
        };

        ChavesGrid.MaxHeight = 220;
        ChavesGrid.SelectionMode = DataGridSelectionMode.Single;
        ChavesGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
        ChavesGrid.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

        var leftPanel = new DockPanel();
        leftPanel.Children.Add(new TextBlock
        {
            Text = "Imóveis disponíveis",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });
        DockPanel.SetDock(leftPanel.Children[0], Dock.Top);
        leftPanel.Children.Add(_chavesAvailableImoveisGrid);

        var rightPanel = new DockPanel();
        rightPanel.Children.Add(new TextBlock
        {
            Text = "Chaves retiradas",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        });
        DockPanel.SetDock(rightPanel.Children[0], Dock.Top);
        rightPanel.Children.Add(ChavesGrid);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });

        Grid.SetColumn(leftPanel, 0);
        Grid.SetColumn(rightPanel, 1);
        leftPanel.Margin = new Thickness(0, 0, 7, 0);
        rightPanel.Margin = new Thickness(7, 0, 0, 0);
        grid.Children.Add(leftPanel);
        grid.Children.Add(rightPanel);

        host.Child = grid;
    }

    private void BuildChavesUnifiedForm(Border retiradaHost)
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

        retiradaHost.Margin = new Thickness(0);
        Grid.SetColumnSpan(retiradaHost, 2);

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
            Width = 160,
            Margin = new Thickness(14, 0, 0, 0),
            ItemsSource = new[] { "Retirada", "Devolução" },
            SelectedIndex = 0
        };
        _chavesMovimentoTipoBox.SelectionChanged += (_, _) => SetChavesMovimentoMode(_chavesMovimentoTipoBox.SelectedIndex == 1);
        header.Children.Add(_chavesActionTitleText);
        header.Children.Add(_chavesMovimentoTipoBox);

        _chavesRetiradaFieldsPanel = new StackPanel();
        _chavesRetiradaFieldsPanel.Children.Add(ExtractWrapPanel(originalPanel));
        _chavesRetiradaFieldsPanel.Children.Add(SaveChaveRetiradaButton);

        _chavesDevolucaoFieldsPanel = new StackPanel();
        _chavesDevolucaoFieldsPanel.Children.Add(ChavesSelectedMovimentoText);
        _chavesDevolucaoFieldsPanel.Children.Add(CreateLabeledField("Recebido por", ChavesDevolvidoParaBox, 260));
        _chavesDevolucaoFieldsPanel.Children.Add(CreateLabeledField("Observações da devolução", ChavesDevolucaoObservacoesBox, 420));
        _chavesDevolucaoFieldsPanel.Children.Add(ReturnChaveButton);

        mainPanel.Children.Add(header);
        mainPanel.Children.Add(_chavesRetiradaFieldsPanel);
        mainPanel.Children.Add(_chavesDevolucaoFieldsPanel);
        mainPanel.Children.Add(ChavesErrorText);
        retiradaHost.Child = mainPanel;
    }

    private static UIElement CreateLabeledField(string label, FrameworkElement field, double width)
    {
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

    private void SetChavesMovimentoMode(bool isReturn)
    {
        if (_chavesMovimentoTipoBox is not null)
        {
            _chavesMovimentoTipoBox.SelectedIndex = isReturn ? 1 : 0;
        }

        if (_chavesActionTitleText is not null)
        {
            _chavesActionTitleText.Text = isReturn ? "Devolução de chaves" : "Retirada de chaves";
        }

        if (_chavesRetiradaFieldsPanel is not null)
        {
            _chavesRetiradaFieldsPanel.Visibility = isReturn ? Visibility.Collapsed : Visibility.Visible;
        }

        if (_chavesDevolucaoFieldsPanel is not null)
        {
            _chavesDevolucaoFieldsPanel.Visibility = isReturn ? Visibility.Visible : Visibility.Collapsed;
        }

        SaveChaveRetiradaButton.Visibility = isReturn ? Visibility.Collapsed : Visibility.Visible;
        ReturnChaveButton.Visibility = isReturn ? Visibility.Visible : Visibility.Collapsed;
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
}
