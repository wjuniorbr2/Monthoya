using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ChavesStableLayoutRegistered = RegisterChavesStableLayout();
    private bool _chavesStableLayoutApplied;
    private bool _chavesStableLayoutEventsAttached;

    private static bool RegisterChavesStableLayout()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForChavesStableLayout));

        return true;
    }

    private static void OnShellWindowLoadedForChavesStableLayout(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyChavesStableLayout, DispatcherPriority.SystemIdle);
        }
    }

    private void ApplyChavesStableLayout()
    {
        if (_chavesStableLayoutApplied)
        {
            _ = RefreshChavesStableDualListsAsync();
            return;
        }

        var formHost = FindChavesFormHost();
        var returnHost = FindChavesReturnHost();
        var listHost = FindChavesListHost();
        var searchHost = ChavesSearchBox.Parent as UIElement;

        if (formHost is null || listHost is null || searchHost is null)
        {
            Dispatcher.BeginInvoke(ApplyChavesStableLayout, DispatcherPriority.SystemIdle);
            return;
        }

        _chavesStableLayoutApplied = true;

        if (returnHost is not null)
        {
            returnHost.Visibility = Visibility.Collapsed;
        }

        if (formHost.Child is StackPanel)
        {
            BuildChavesTopUnifiedForm(formHost);
        }

        ChavesImovelBox.Visibility = Visibility.Collapsed;
        HideLabelImmediatelyBefore(ChavesImovelBox);
        ChavesStatusFilterBox.Visibility = Visibility.Collapsed;
        CollapseImmediateLabelBefore(ChavesStatusFilterBox);
        CollapseFieldContainer(ChavesCodigoBox);

        PrepareChavesAvailableGrid();
        PrepareChavesTakenGrid();

        DetachFromParent(formHost);
        DetachFromParent(searchHost);
        DetachFromParent(listHost);
        if (_chavesTakenGrid is not null)
        {
            DetachFromParent(_chavesTakenGrid);
        }

        ChavesPanel.Children.Clear();
        ChavesPanel.RowDefinitions.Clear();
        ChavesPanel.ColumnDefinitions.Clear();
        ChavesPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        ChavesPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        formHost.Margin = new Thickness(0, 0, 0, 12);
        Grid.SetRow(formHost, 0);
        ChavesPanel.Children.Add(formHost);

        var dualGrid = BuildChavesStableDualGrid(listHost, searchHost);
        Grid.SetRow(dualGrid, 1);
        ChavesPanel.Children.Add(dualGrid);

        AttachChavesStableLayoutEvents();
        _ = RefreshChavesStableDualListsAsync();
    }

    private void PrepareChavesAvailableGrid()
    {
        ChavesGrid.AutoGenerateColumns = false;
        ChavesGrid.IsReadOnly = true;
        ChavesGrid.SelectionMode = DataGridSelectionMode.Single;
        ChavesGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
        ChavesGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
        ChavesGrid.VerticalAlignment = VerticalAlignment.Stretch;
        ChavesGrid.MinHeight = 0;
        ChavesGrid.MaxHeight = double.PositiveInfinity;
        ChavesGrid.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        ChavesGrid.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        ConfigureAvailableKeysGridColumns();
    }

    private void PrepareChavesTakenGrid()
    {
        _chavesTakenGrid ??= new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            SelectionMode = DataGridSelectionMode.Single,
            SelectionUnit = DataGridSelectionUnit.FullRow
        };

        _chavesTakenGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
        _chavesTakenGrid.VerticalAlignment = VerticalAlignment.Stretch;
        _chavesTakenGrid.MinHeight = 0;
        _chavesTakenGrid.MaxHeight = double.PositiveInfinity;
        _chavesTakenGrid.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _chavesTakenGrid.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        ConfigureTakenKeysGridColumns();
    }

    private Grid BuildChavesStableDualGrid(Border originalListHost, UIElement originalSearchHost)
    {
        ChavesSearchBox.Width = double.NaN;
        ChavesSearchBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        ChavesSearchBox.Margin = new Thickness(0);

        _chavesTakenSearchBox ??= new TextBox();
        _chavesTakenSearchBox.Width = double.NaN;
        _chavesTakenSearchBox.Height = ChavesSearchBox.Height;
        _chavesTakenSearchBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        _chavesTakenSearchBox.Margin = new Thickness(0);
        DetachFromParent(_chavesTakenSearchBox);

        originalListHost.Margin = new Thickness(0);
        originalListHost.Padding = new Thickness(8);
        originalListHost.HorizontalAlignment = HorizontalAlignment.Stretch;
        originalListHost.VerticalAlignment = VerticalAlignment.Stretch;
        originalListHost.Child = ChavesGrid;

        var rightListHost = new Border
        {
            Background = originalListHost.Background,
            BorderBrush = originalListHost.BorderBrush,
            BorderThickness = originalListHost.BorderThickness,
            CornerRadius = originalListHost.CornerRadius,
            Padding = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = _chavesTakenGrid
        };

        var leftPanel = BuildChavesStableListPanel("Pesquisar imóveis disponíveis", originalSearchHost, originalListHost);
        var rightPanel = BuildChavesStableListPanel("Pesquisar chaves retiradas", _chavesTakenSearchBox, rightListHost);

        var dualGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        dualGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        dualGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        dualGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        Grid.SetColumn(leftPanel, 0);
        Grid.SetColumn(rightPanel, 2);
        dualGrid.Children.Add(leftPanel);
        dualGrid.Children.Add(rightPanel);
        return dualGrid;
    }

    private static Grid BuildChavesStableListPanel(string title, UIElement searchHost, Border listHost)
    {
        var panel = new Grid();
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var titleText = new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        };

        Grid.SetRow(titleText, 0);
        Grid.SetRow(searchHost, 1);
        Grid.SetRow(listHost, 2);
        listHost.Margin = new Thickness(0, 8, 0, 0);

        panel.Children.Add(titleText);
        panel.Children.Add(searchHost);
        panel.Children.Add(listHost);
        return panel;
    }

    private void AttachChavesStableLayoutEvents()
    {
        if (_chavesStableLayoutEventsAttached)
        {
            return;
        }

        _chavesStableLayoutEventsAttached = true;

        ChavesSearchBox.TextChanged += async (_, _) => await RefreshChavesStableDualListsAsync();

        if (_chavesTakenSearchBox is not null)
        {
            _chavesTakenSearchBox.TextChanged += async (_, _) => await RefreshChavesStableDualListsAsync();
        }

        ChavesPanel.IsVisibleChanged += (_, _) =>
        {
            if (ChavesPanel.IsVisible)
            {
                Dispatcher.BeginInvoke(async () => await RefreshChavesStableDualListsAsync(), DispatcherPriority.SystemIdle);
            }
        };
    }

    private async Task RefreshChavesStableDualListsAsync()
    {
        if (_chavesTakenGrid is null)
        {
            return;
        }

        await LoadMissingChavesBoardCodesForAllImoveisAsync();

        var activeMovementsByImovelId = _chaveMovimentos
            .Where(x => !x.DevolvidoEm.HasValue)
            .GroupBy(x => x.ImovelId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(x => x.RetiradoEm).First());

        var allItems = _imoveis
            .Where(x => !string.Equals(x.Status, "Inativo", StringComparison.OrdinalIgnoreCase))
            .Select(imovel =>
            {
                activeMovementsByImovelId.TryGetValue(imovel.Id, out var movimento);
                var item = CreateChavesListItem(imovel, movimento);
                var boardCode = GetChavesBoardCodeForImovel(item.ImovelId, item.ChaveCodigo);
                return item with { ChaveCodigo = boardCode };
            })
            .ToList();

        _lastAllChavesItems = allItems;

        var availableQuery = ChavesSearchBox.Text;
        var takenQuery = _chavesTakenSearchBox?.Text ?? string.Empty;

        var available = allItems
            .Where(item => !item.MovimentoId.HasValue)
            .Where(item => ContainsSearch(availableQuery, item.ChaveCodigo, item.Imovel, item.Proprietario, item.Status))
            .OrderBy(item => item.Imovel)
            .ToList();

        var taken = allItems
            .Where(item => item.MovimentoId.HasValue)
            .Where(item => ContainsSearch(takenQuery, item.ChaveCodigo, item.Imovel, item.Proprietario, item.RetiradoPorNome, item.RetiradoPorTelefone, item.Relacao, item.Motivo))
            .OrderByDescending(item => item.RetiradoEm)
            .ToList();

        ChavesGrid.ItemsSource = available;
        _chavesTakenGrid.ItemsSource = taken;
        ConfigureAvailableKeysGridColumns();
        ConfigureTakenKeysGridColumns();
        UpdateChavesBoardCodeDisplayFromSelection();
        UpdateChavesWithdrawalButtonState();
    }

    private async Task LoadMissingChavesBoardCodesForAllImoveisAsync()
    {
        foreach (var imovel in _imoveis)
        {
            if (_chavesBoardCodeByImovelId.ContainsKey(imovel.Id))
            {
                continue;
            }

            var details = await _rentalManagementService.GetImovelAsync(imovel.Id);
            _chavesBoardCodeByImovelId[imovel.Id] = details?.Dados.ChaveCodigo;
        }
    }

    private string? GetChavesBoardCodeForImovel(Guid imovelId, string? fallback)
    {
        if (_chavesBoardCodeByImovelId.TryGetValue(imovelId, out var boardCode)
            && !string.IsNullOrWhiteSpace(boardCode))
        {
            return boardCode;
        }

        return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
    }
}
