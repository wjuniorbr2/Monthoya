using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static readonly bool ChavesHistoryRegistered = RegisterChavesHistory();
    private bool _chavesHistoryApplied;
    private DataGrid? _chavesHistoryGrid;
    private TextBox? _chavesHistorySearchBox;

    private static bool RegisterChavesHistory()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler(OnShellWindowLoadedForChavesHistory));

        return true;
    }

    private static void OnShellWindowLoadedForChavesHistory(object sender, RoutedEventArgs e)
    {
        if (sender is ShellWindow window)
        {
            window.Dispatcher.BeginInvoke(window.ApplyChavesHistoryPanel, DispatcherPriority.ContextIdle);
        }
    }

    private void ApplyChavesHistoryPanel()
    {
        if (_chavesHistoryApplied)
        {
            return;
        }

        if (!_chavesDualListsApplied || ChavesPanel.RowDefinitions.Count < 2)
        {
            Dispatcher.BeginInvoke(ApplyChavesHistoryPanel, DispatcherPriority.ContextIdle);
            return;
        }

        _chavesHistoryApplied = true;

        ChavesPanel.RowDefinitions[1].Height = new GridLength(0.56, GridUnitType.Star);
        ChavesPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.44, GridUnitType.Star) });

        _chavesHistorySearchBox = new TextBox
        {
            Height = ChavesSearchBox.Height,
            Margin = new Thickness(0, 0, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        _chavesHistorySearchBox.TextChanged += (_, _) => RefreshChavesHistoryFromCurrentData();

        _chavesHistoryGrid = new DataGrid
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
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        ConfigureChavesHistoryGridColumns();
        ConfigureChavesHistoryContextMenu();

        var listHost = FindVisualChildrenForPeopleRuntimeAdjustment<Border>(ChavesPanel)
            .FirstOrDefault(border => border.Child == ChavesGrid);

        var historyHost = new Border
        {
            Background = listHost?.Background ?? Brushes.White,
            BorderBrush = listHost?.BorderBrush ?? Brushes.LightGray,
            BorderThickness = listHost?.BorderThickness ?? new Thickness(1),
            CornerRadius = listHost?.CornerRadius ?? new CornerRadius(8),
            Padding = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Child = _chavesHistoryGrid
        };

        var panel = new Grid { Margin = new Thickness(0, 8, 0, 10) };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var title = new TextBlock
        {
            Text = "Histórico de chaves",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        };

        Grid.SetRow(title, 0);
        Grid.SetRow(_chavesHistorySearchBox, 1);
        Grid.SetRow(historyHost, 2);
        panel.Children.Add(title);
        panel.Children.Add(_chavesHistorySearchBox);
        panel.Children.Add(historyHost);

        Grid.SetRow(panel, 2);
        ChavesPanel.Children.Add(panel);
        RefreshChavesHistoryFromCurrentData();
    }

    private void ConfigureChavesHistoryGridColumns()
    {
        if (_chavesHistoryGrid is null || _chavesHistoryGrid.Columns.Count > 0)
        {
            return;
        }

        AddGridColumn(_chavesHistoryGrid, "Código", "ChaveCodigo", 0.45);
        AddGridColumn(_chavesHistoryGrid, "Endereço", "Imovel", 1.4);
        AddGridColumn(_chavesHistoryGrid, "Proprietário", "Proprietario", 1.1);
        AddGridColumn(_chavesHistoryGrid, "Situação", "Situacao", 0.75);
        AddGridColumn(_chavesHistoryGrid, "Retirado por", "RetiradoPorNome", 0.9);
        AddGridColumn(_chavesHistoryGrid, "Telefone", "RetiradoPorTelefone", 0.75);
        AddGridColumn(_chavesHistoryGrid, "Retirado em", "RetiradoEm", 0.8, "dd/MM HH:mm");
        AddGridColumn(_chavesHistoryGrid, "Previsão", "PrevisaoDevolucaoEm", 0.8, "dd/MM HH:mm");
        AddGridColumn(_chavesHistoryGrid, "Devolvido em", "DevolvidoEm", 0.8, "dd/MM HH:mm");
        AddGridColumn(_chavesHistoryGrid, "Relação", "Relacao", 0.7);
        AddGridColumn(_chavesHistoryGrid, "Documento", "Documento", 0.8);
        AddGridColumn(_chavesHistoryGrid, "Motivo", "Motivo", 1.0);
        AddGridColumn(_chavesHistoryGrid, "Observações", "Observacoes", 1.2);
    }

    private void ConfigureChavesHistoryContextMenu()
    {
        if (_chavesHistoryGrid is null)
        {
            return;
        }

        var removeItem = new MenuItem { Header = "Remover entrada" };
        removeItem.Click += async (_, _) => await RemoveSelectedChavesHistoryEntryAsync();
        _chavesHistoryGrid.ContextMenu = new ContextMenu
        {
            Items = { removeItem }
        };
    }

    private void RefreshChavesHistoryFromCurrentData()
    {
        if (_chavesHistoryGrid is null)
        {
            return;
        }

        var query = _chavesHistorySearchBox?.Text ?? string.Empty;
        var imoveisById = _imoveis.ToDictionary(x => x.Id);

        var rows = _chaveMovimentos
            .Select(movimento =>
            {
                imoveisById.TryGetValue(movimento.ImovelId, out var imovel);
                var chaveCodigo = movimento.ChaveCodigo;
                if (string.IsNullOrWhiteSpace(chaveCodigo) && imovel is not null)
                {
                    chaveCodigo = imovel.ChaveCodigo;
                }

                var situacao = movimento.DevolvidoEm.HasValue
                    ? "Devolvida"
                    : movimento.Status;

                return new ChavesHistoryItem(
                    movimento.Id,
                    chaveCodigo,
                    imovel?.Endereco ?? movimento.Imovel,
                    imovel?.Proprietario ?? "-",
                    situacao,
                    movimento.RetiradoPorNome,
                    movimento.RetiradoPorTelefone,
                    movimento.RetiradoEm,
                    movimento.PrevisaoDevolucaoEm,
                    movimento.DevolvidoEm,
                    movimento.RetiradoPorRelacao,
                    movimento.RetiradoPorDocumento,
                    movimento.Motivo,
                    movimento.Observacoes);
            })
            .Where(item => ContainsSearch(
                query,
                item.ChaveCodigo,
                item.Imovel,
                item.Proprietario,
                item.Situacao,
                item.RetiradoPorNome,
                item.RetiradoPorTelefone,
                item.Relacao,
                item.Documento,
                item.Motivo,
                item.Observacoes))
            .OrderByDescending(item => item.RetiradoEm)
            .ToList();

        _chavesHistoryGrid.ItemsSource = rows;
    }

    private async Task RemoveSelectedChavesHistoryEntryAsync()
    {
        if (_chavesHistoryGrid?.SelectedItem is not ChavesHistoryItem item)
        {
            MessageBox.Show(this, "Selecione uma entrada do histórico para remover.", "Histórico de chaves", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            "Remover esta entrada do histórico de chaves?",
            "Histórico de chaves",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        var deleteMethod = _rentalManagementService.GetType().GetMethod("DeleteImovelChaveMovimentoAsync");
        if (deleteMethod is null)
        {
            MessageBox.Show(this, "A remoção do histórico ainda não está disponível neste serviço.", "Histórico de chaves", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = deleteMethod.Invoke(_rentalManagementService, [item.MovimentoId, CancellationToken.None]);
        if (result is Task task)
        {
            await task;
        }

        await LoadChavesAsync();
        RefreshChavesHistoryFromCurrentData();
        MessageBox.Show(this, "Entrada removida do histórico.", "Histórico de chaves", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private sealed record ChavesHistoryItem(
        Guid MovimentoId,
        string? ChaveCodigo,
        string Imovel,
        string Proprietario,
        string Situacao,
        string? RetiradoPorNome,
        string? RetiradoPorTelefone,
        DateTimeOffset? RetiradoEm,
        DateTimeOffset? PrevisaoDevolucaoEm,
        DateTimeOffset? DevolvidoEm,
        string? Relacao,
        string? Documento,
        string? Motivo,
        string? Observacoes);
}
