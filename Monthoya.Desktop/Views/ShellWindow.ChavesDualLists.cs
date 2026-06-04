using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private const double ChavesListPanelMaxWidth = 800;

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
        var formHost = FindChavesFormHost();

        HideOldChavesSearchSection(originalSearchHost);
        DetachFromParent(originalSearchHost);
        DetachFromParent(ChavesSearchBox);
        DetachFromParent(originalListHost);
        if (formHost is not null)
        {
            DetachFromParent(formHost);
        }

        ChavesSearchBox.Width = double.NaN;
        ChavesSearchBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        ChavesSearchBox.Margin = new Thickness(0);

        ConfigureAvailableKeysGridColumns();
        ChavesGrid.MinHeight = 0;
        ChavesGrid.MaxHeight = double.PositiveInfinity;
        ChavesGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
        ChavesGrid.VerticalAlignment = VerticalAlignment.Stretch;
        ChavesGrid.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

        originalListHost.Margin = new Thickness(0, 6, 0, 0);
        originalListHost.Padding = new Thickness(8);
        originalListHost.HorizontalAlignment = HorizontalAlignment.Stretch;
        originalListHost.VerticalAlignment = VerticalAlignment.Stretch;
        originalListHost.Child = ChavesGrid;

        var leftPanel = CreateChavesDualListPanel(
            "Pesquisar imóveis disponíveis",
            ChavesSearchBox,
            originalListHost,
            HorizontalAlignment.Stretch);

        _chavesTakenSearchBox = new TextBox
        {
            Height = ChavesSearchBox.Height,
            Margin = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _chavesTakenSearchBox.TextChanged += (_, _) => RefreshChavesDualListsFromCache();

        _chavesTakenGrid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            SelectionMode = DataGridSelectionMode.Single,
            SelectionUnit = DataGridSelectionUnit.FullRow,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            MinHeight = 0,
            MaxHeight = double.PositiveInfinity,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        ConfigureTakenKeysGridColumns();
        _chavesTakenGrid.SelectionChanged += (_, _) =>
        {
            if (_chavesTakenGrid.SelectedItem is ChavesListItem item)
            {
                _selectedChavesTakenItem = item;
                ChavesGrid.SelectedItem = null;
                SetChavesMovimentoMode(isReturn: true);
                SetChavesReturnDateTimeToNow();
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
            Margin = new Thickness(0, 6, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = _chavesTakenGrid
        };

        var rightPanel = CreateChavesDualListPanel(
            "Pesquisar chaves retiradas",
            _chavesTakenSearchBox,
            rightListHost,
            HorizontalAlignment.Stretch);

        var dualGrid = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        dualGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.03, GridUnitType.Star) });
        dualGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
        dualGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(leftPanel, 0);
        Grid.SetColumn(rightPanel, 2);
        dualGrid.Children.Add(leftPanel);
        dualGrid.Children.Add(rightPanel);

        if (formHost is null)
        {
            Grid.SetRow(dualGrid, 3);
            Grid.SetColumnSpan(dualGrid, 2);
            ChavesPanel.Children.Add(dualGrid);
            return;
        }

        ChavesPanel.Children.Clear();
        ChavesPanel.RowDefinitions.Clear();
        ChavesPanel.ColumnDefinitions.Clear();
        ChavesPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        ChavesPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        formHost.Margin = new Thickness(0, 0, 0, 6);
        Grid.SetRow(formHost, 0);
        Grid.SetRow(dualGrid, 1);
        ChavesPanel.Children.Add(formHost);
        ChavesPanel.Children.Add(dualGrid);
    }

    private static Grid CreateChavesDualListPanel(string title, UIElement searchHost, Border listHost, HorizontalAlignment alignment)
    {
        var panel = new Grid
        {
            MaxWidth = ChavesListPanelMaxWidth,
            HorizontalAlignment = alignment
        };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var titleBlock = new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        };

        Grid.SetRow(titleBlock, 0);
        Grid.SetRow(searchHost, 1);
        Grid.SetRow(listHost, 2);
        panel.Children.Add(titleBlock);
        panel.Children.Add(searchHost);
        panel.Children.Add(listHost);
        return panel;
    }

    private void HideOldChavesSearchSection(UIElement searchHost)
    {
        if (searchHost is not FrameworkElement searchElement)
        {
            return;
        }

        if (searchElement.Parent is StackPanel oldSearchSection)
        {
            foreach (var textBlock in oldSearchSection.Children.OfType<TextBlock>())
            {
                textBlock.Visibility = Visibility.Collapsed;
            }

            oldSearchSection.Margin = new Thickness(0);
        }
    }

    private async Task RefreshChavesDualListsAsync()
    {
        if (_chavesTakenGrid is null)
        {
            return;
        }

        _lastAllChavesItems = BuildAllChavesItemsFromCurrentData();
        await LoadMissingChavesBoardCodesForCachedItemsAsync();
        ApplyLoadedBoardCodesToCachedItems();
        RefreshChavesDualListsFromCache();
    }

    private async Task LoadMissingChavesBoardCodesForCachedItemsAsync()
    {
        foreach (var item in _lastAllChavesItems)
        {
            if (_chavesBoardCodeByImovelId.TryGetValue(item.ImovelId, out var code) && !string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            var details = await _rentalManagementService.GetImovelAsync(item.ImovelId);
            _chavesBoardCodeByImovelId[item.ImovelId] = details?.Dados.ChaveCodigo;
        }
    }

    private void RefreshChavesDualListsFromCache()
    {
        if (_chavesTakenGrid is null)
        {
            return;
        }

        if (_lastAllChavesItems.Count == 0)
        {
            ChavesGrid.ItemsSource = Array.Empty<ChavesListItem>();
            _chavesTakenGrid.ItemsSource = Array.Empty<ChavesListItem>();
            return;
        }

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
        UpdateChavesBoardCodeDisplayFromSelection();
        UpdateChavesWithdrawalButtonState();
    }

    private List<ChavesListItem> BuildAllChavesItemsFromCurrentData()
    {
        var activeMovementsByImovelId = _chaveMovimentos
            .Where(x => !x.DevolvidoEm.HasValue)
            .GroupBy(x => x.ImovelId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(x => x.RetiradoEm).First());

        return _imoveis
            .Where(x => !string.Equals(x.Status, "Inativo", StringComparison.OrdinalIgnoreCase))
            .Select(imovel =>
            {
                activeMovementsByImovelId.TryGetValue(imovel.Id, out var movimento);
                return CreateChavesListItem(imovel, movimento);
            })
            .ToList();
    }

    private void ApplyLoadedBoardCodesToCachedItems()
    {
        _lastAllChavesItems = _lastAllChavesItems
            .Select(item => _chavesBoardCodeByImovelId.TryGetValue(item.ImovelId, out var code) && !string.IsNullOrWhiteSpace(code)
                ? item with { ChaveCodigo = code }
                : item)
            .ToList();
    }

    private void ConfigureAvailableKeysGridColumns()
    {
        if (ChavesGrid.Columns.Count == 4)
        {
            return;
        }

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

        if (_chavesTakenGrid.Columns.Count == 6)
        {
            return;
        }

        _chavesTakenGrid.Columns.Clear();
        AddGridColumn(_chavesTakenGrid, "Código", "ChaveCodigo", 0.38);
        AddGridColumn(_chavesTakenGrid, "Endereço", "Imovel", 1.22);
        AddGridColumn(_chavesTakenGrid, "Retirado por", "RetiradoPorNome", 0.85);
        AddGridColumn(_chavesTakenGrid, "Telefone", "RetiradoPorTelefone", 0.7);
        AddGridColumn(_chavesTakenGrid, "Retirado em", "RetiradoEm", 0.78, "dd/MM HH:mm");
        AddGridColumn(_chavesTakenGrid, "Previsão", "PrevisaoDevolucaoEm", 0.74, "dd/MM HH:mm");
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
}
