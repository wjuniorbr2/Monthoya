using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ChavesDualListsRegistered = RegisterChavesDualLists();
    private bool _chavesDualListsApplied;
    private DataGrid? _chavesTakenGrid;
    private TextBox? _chavesTakenSearchBox;
    private ChavesListItem? _selectedChavesTakenItem;
    private List<ChavesListItem> _lastAllChavesItems = [];

    private static bool RegisterChavesDualLists()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForChavesDualLists));

        return true;
    }

    private static void OnShellWindowLoadedForChavesDualLists(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyChavesDualLists, DispatcherPriority.ApplicationIdle);
        }
    }

    private void ApplyChavesDualLists()
    {
        if (_chavesDualListsApplied)
        {
            return;
        }

        var listHost = FindVisualChildrenForPeopleRuntimeAdjustment<Border>(ChavesPanel)
            .FirstOrDefault(border => border.Child == ChavesGrid);
        var searchHost = ChavesSearchBox.Parent as UIElement;
        if (listHost is null || searchHost is null)
        {
            Dispatcher.BeginInvoke(ApplyChavesDualLists, DispatcherPriority.ApplicationIdle);
            return;
        }

        _chavesDualListsApplied = true;
        BuildChavesDualListLayout(listHost, searchHost);

        ChavesGrid.SelectionChanged += (_, _) =>
        {
            if (ChavesGrid.SelectedItem is ChavesListItem item)
            {
                _selectedChavesTakenItem = null;
                if (_chavesTakenGrid is not null)
                {
                    _chavesTakenGrid.SelectedItem = null;
                }

                SetChavesMovimentoMode(isReturn: false);
                ChavesImovelBox.SelectedValue = item.ImovelId;
                ChavesCodigoBox.Text = item.ChaveCodigo ?? string.Empty;
                UpdateChavesSelectedImovelSummary(item.Imovel, item.Proprietario);
                UpdateChavesWithdrawalButtonState();
            }
        };

        _ = RefreshChavesDualListsAsync();
    }

    private void BuildChavesDualListLayout(Border originalListHost, UIElement originalSearchHost)
    {
        DetachFromParent(originalSearchHost);
        DetachFromParent(originalListHost);

        ChavesSearchBox.Width = double.NaN;
        ChavesSearchBox.HorizontalAlignment = HorizontalAlignment.Stretch;

        var leftSearchSection = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        leftSearchSection.Children.Add(new TextBlock
        {
            Text = "Pesquisar imóveis disponíveis",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        });
        leftSearchSection.Children.Add(originalSearchHost);

        ConfigureAvailableKeysGridColumns();
        ChavesGrid.MinHeight = 0;
        ChavesGrid.MaxHeight = double.PositiveInfinity;
        ChavesGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
        ChavesGrid.VerticalAlignment = VerticalAlignment.Stretch;

        var leftPanel = new Grid();
        leftPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(leftSearchSection, 0);
        Grid.SetRow(originalListHost, 1);
        originalListHost.Margin = new Thickness(0);
        originalListHost.Padding = new Thickness(8);
        leftPanel.Children.Add(leftSearchSection);
        leftPanel.Children.Add(originalListHost);

        _chavesTakenSearchBox = new TextBox
        {
            Height = ChavesSearchBox.Height,
            Margin = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _chavesTakenSearchBox.TextChanged += async (_, _) => await RefreshChavesDualListsAsync();

        var rightSearchSection = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        rightSearchSection.Children.Add(new TextBlock
        {
            Text = "Pesquisar chaves retiradas",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        });
        rightSearchSection.Children.Add(_chavesTakenSearchBox);

        _chavesTakenGrid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            SelectionMode = DataGridSelectionMode.Single,
            SelectionUnit = DataGridSelectionUnit.FullRow,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            MinHeight = 0,
            MaxHeight = double.PositiveInfinity
        };
        ConfigureTakenKeysGridColumns();
        _chavesTakenGrid.SelectionChanged += (_, _) =>
        {
            if (_chavesTakenGrid.SelectedItem is ChavesListItem item)
            {
                _selectedChavesTakenItem = item;
                ChavesGrid.SelectedItem = null;
                SetChavesMovimentoMode(isReturn: true);
                ChavesImovelBox.SelectedValue = item.ImovelId;
                ChavesCodigoBox.Text = item.ChaveCodigo ?? string.Empty;
                UpdateChavesSelectedImovelSummary(item.Imovel, item.Proprietario);
                UpdateSelectedChaveMovement();
            }
        };

        var rightListHost = new Border
        {
            Background = originalListHost.Background,
            BorderBrush = originalListHost.BorderBrush,
            BorderThickness = originalListHost.BorderThickness,
            CornerRadius = originalListHost.CornerRadius,
            Padding = new Thickness(8),
            Child = _chavesTakenGrid
        };

        var rightPanel = new Grid();
        rightPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rightPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(rightSearchSection, 0);
        Grid.SetRow(rightListHost, 1);
        rightPanel.Children.Add(rightSearchSection);
        rightPanel.Children.Add(rightListHost);

        var dualGrid = new Grid { Margin = new Thickness(0, 14, 0, 10) };
        dualGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        dualGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        dualGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(leftPanel, 0);
        Grid.SetColumn(rightPanel, 2);
        dualGrid.Children.Add(leftPanel);
        dualGrid.Children.Add(rightPanel);

        Grid.SetRow(dualGrid, 3);
        Grid.SetColumnSpan(dualGrid, 2);
        ChavesPanel.Children.Add(dualGrid);
    }

    private async Task RefreshChavesDualListsAsync()
    {
        if (_chavesTakenGrid is null)
        {
            return;
        }

        var items = ChavesGrid.ItemsSource?.Cast<object>().OfType<ChavesListItem>().ToList() ?? [];
        if (items.Count > 0)
        {
            _lastAllChavesItems = MergeChavesItems(_lastAllChavesItems, items);
        }

        if (_lastAllChavesItems.Count == 0)
        {
            return;
        }

        await RefreshChavesBoardCodesFromImoveisAsync();

        var availableQuery = ChavesSearchBox.Text;
        var takenQuery = _chavesTakenSearchBox?.Text ?? string.Empty;

        var available = _lastAllChavesItems
            .Where(item => !item.MovimentoId.HasValue)
            .Where(item => ContainsSearch(availableQuery, item.ChaveCodigo, item.Imovel, item.Proprietario, item.Status))
            .OrderBy(item => item.Imovel)
            .ToList();

        var taken = _lastAllChavesItems
            .Where(item => item.MovimentoId.HasValue)
            .Where(item => ContainsSearch(takenQuery, item.ChaveCodigo, item.Imovel, item.Proprietario, item.Status, item.RetiradoPorNome, item.RetiradoPorTelefone, item.Relacao, item.Motivo))
            .OrderByDescending(item => item.RetiradoEm)
            .ToList();

        ChavesGrid.ItemsSource = available;
        _chavesTakenGrid.ItemsSource = taken;
        ConfigureAvailableKeysGridColumns();
        ConfigureTakenKeysGridColumns();
    }

    private static List<ChavesListItem> MergeChavesItems(List<ChavesListItem> previous, List<ChavesListItem> current)
    {
        var byKey = previous.ToDictionary(item => item.MovimentoId ?? item.ImovelId, item => item);
        foreach (var item in current)
        {
            byKey[item.MovimentoId ?? item.ImovelId] = item;
        }

        return byKey.Values.ToList();
    }

    private void ConfigureAvailableKeysGridColumns()
    {
        ChavesGrid.Columns.Clear();
        AddGridColumn(ChavesGrid, "Código", "ChaveCodigo", 0.55);
        AddGridColumn(ChavesGrid, "Endereço", "Imovel", 1.7);
        AddGridColumn(ChavesGrid, "Proprietário", "Proprietario", 1.2);
        AddGridColumn(ChavesGrid, "Situação", "Status", 0.8);
    }

    private void ConfigureTakenKeysGridColumns()
    {
        if (_chavesTakenGrid is null)
        {
            return;
        }

        _chavesTakenGrid.Columns.Clear();
        AddGridColumn(_chavesTakenGrid, "Código", "ChaveCodigo", 0.45);
        AddGridColumn(_chavesTakenGrid, "Endereço", "Imovel", 1.4);
        AddGridColumn(_chavesTakenGrid, "Retirado por", "RetiradoPorNome", 1.0);
        AddGridColumn(_chavesTakenGrid, "Telefone", "RetiradoPorTelefone", 0.8);
        AddGridColumn(_chavesTakenGrid, "Previsão", "PrevisaoDevolucaoEm", 0.9, "dd/MM HH:mm");
    }

    private static void AddGridColumn(DataGrid grid, string header, string binding, double width, string? stringFormat = null)
    {
        var columnBinding = new System.Windows.Data.Binding(binding);
        if (!string.IsNullOrWhiteSpace(stringFormat))
        {
            columnBinding.StringFormat = stringFormat;
        }

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = columnBinding,
            Width = new DataGridLength(width, DataGridLengthUnitType.Star)
        });
    }

    private ChavesListItem? GetSelectedChavesReturnItem()
    {
        return _selectedChavesTakenItem
            ?? _chavesTakenGrid?.SelectedItem as ChavesListItem
            ?? ChavesGrid.SelectedItem as ChavesListItem;
    }

    private void ShowChavesStatusMessage(string message)
    {
        ChavesErrorText.Foreground = Brushes.DimGray;
        ChavesErrorText.Text = message;
    }

    private void ShowChavesSuccessMessage(string message)
    {
        ChavesErrorText.Foreground = Brushes.ForestGreen;
        ChavesErrorText.Text = message;
    }

    private void ShowChavesErrorMessage(string message)
    {
        ChavesErrorText.Foreground = Brushes.Red;
        ChavesErrorText.Text = message;
    }
}
