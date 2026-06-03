using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private async Task LoadChavesAsync()
    {
        _imoveis = await _rentalManagementService.GetImoveisAsync();
        ChavesImovelBox.ItemsSource = _imoveis
            .Where(x => !string.Equals(x.Status, "Inativo", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Endereco)
            .ToList();

        _chaveMovimentos = await _rentalManagementService.GetImovelChaveMovimentosAsync();
        ApplyChavesFilter();
        await RefreshChavesDualListsAsync();
        RefreshChavesHistoryFromCurrentData();
        UpdateSelectedChaveMovement();
        if (!ChavesPrevisaoBox.SelectedDate.HasValue)
        {
            ChavesPrevisaoBox.SelectedDate = DateTime.Today;
        }
    }

    private async void ReloadChavesButton_Click(object sender, RoutedEventArgs e) => await LoadChavesAsync();

    private void ChavesSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyChavesFilter();
        SaveActiveTabState();
        _ = RefreshChavesDualListsAsync();
    }

    private void ChavesStatusFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyChavesFilter();
        SaveActiveTabState();
    }

    private void ChavesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_chavesDualListsApplied)
        {
            UpdateChavesWithdrawalButtonState();
            UpdateChavesBoardCodeDisplayFromSelection();
            return;
        }

        if (ChavesGrid.SelectedItem is ChavesListItem item)
        {
            SelectChavesListItem(item);
        }

        UpdateSelectedChaveMovement();
    }

    private void SelectChavesListItem(ChavesListItem item)
    {
        ChavesImovelBox.SelectedValue = item.ImovelId;
        ChavesCodigoBox.Text = item.ChaveCodigo ?? string.Empty;
        UpdateChavesSelectedImovelSummary(item.Imovel, item.Proprietario);

        if (item.MovimentoId.HasValue)
        {
            SetChavesMovimentoMode(isReturn: true);
            SetChavesReturnDateTimeToNow();
        }
        else
        {
            SetChavesMovimentoMode(isReturn: false);
        }
    }

    private void ApplyChavesFilter()
    {
        if (ChavesGrid is null)
        {
            return;
        }

        if (_chavesDualListsApplied)
        {
            _ = RefreshChavesDualListsAsync();
            return;
        }

        var query = ChavesSearchBox.Text;
        var mode = GetChavesSelectedMode();
        var activeMovementsByImovelId = _chaveMovimentos
            .Where(x => !x.DevolvidoEm.HasValue)
            .GroupBy(x => x.ImovelId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(x => x.RetiradoEm).First());

        IEnumerable<ChavesListItem> rows = _imoveis
            .Where(x => !string.Equals(x.Status, "Inativo", StringComparison.OrdinalIgnoreCase))
            .Select(imovel =>
            {
                activeMovementsByImovelId.TryGetValue(imovel.Id, out var movimento);
                return CreateChavesListItem(imovel, movimento);
            });

        rows = mode switch
        {
            "Retirada" => rows.Where(x => !x.MovimentoId.HasValue),
            "Devolução" => rows.Where(x => x.MovimentoId.HasValue),
            _ => rows
        };

        rows = rows
            .Where(x => ContainsSearch(
                query,
                x.Imovel,
                x.Proprietario,
                x.ChaveCodigo,
                x.Status,
                x.RetiradoPorNome,
                x.RetiradoPorTelefone,
                x.RetiradoPorDocumento,
                x.Relacao,
                x.Motivo,
                x.Observacoes))
            .OrderByDescending(x => x.MovimentoId.HasValue)
            .ThenBy(x => x.Imovel)
            .ToList();

        ChavesGrid.ItemsSource = rows;
    }

    private static ChavesListItem CreateChavesListItem(ImovelSummary imovel, ImovelChaveMovimentoSummary? movimento)
    {
        if (movimento is null)
        {
            return new ChavesListItem(
                imovel.Id,
                null,
                imovel.Endereco,
                imovel.Proprietario,
                imovel.ChaveCodigo,
                imovel.Chaves,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);
        }

        return new ChavesListItem(
            imovel.Id,
            movimento.Id,
            imovel.Endereco,
            imovel.Proprietario,
            movimento.ChaveCodigo ?? imovel.ChaveCodigo,
            movimento.Status,
            movimento.RetiradoPorNome,
            movimento.RetiradoPorTelefone,
            movimento.RetiradoPorDocumento,
            movimento.RetiradoPorRelacao,
            movimento.Motivo,
            movimento.RetiradoEm,
            movimento.PrevisaoDevolucaoEm,
            movimento.Observacoes);
    }

    private async void SaveChaveRetiradaButton_Click(object sender, RoutedEventArgs e)
    {
        ShowChavesStatusMessage("Salvando retirada...");
        SaveChaveRetiradaButton.IsEnabled = false;
        Guid selectedImovelId = Guid.Empty;

        try
        {
            if (ChavesImovelBox.SelectedValue is not Guid imovelId || imovelId == Guid.Empty)
            {
                ShowChavesDialog("Selecione o imóvel na lista.", "Chaves", MessageBoxImage.Warning);
                return;
            }

            selectedImovelId = imovelId;

            var previsao = ChavesPrevisaoBox.SelectedDate;
            if (!previsao.HasValue)
            {
                ShowChavesDialog("Informe a previsão de devolução.", "Chaves", MessageBoxImage.Warning);
                return;
            }

            var previsaoHora = GetChavesPrevisaoHorario();
            var previsaoLocalDateTime = previsao.Value.Date.Add(previsaoHora);
            var previsaoDevolucao = new DateTimeOffset(
                previsaoLocalDateTime,
                TimeZoneInfo.Local.GetUtcOffset(previsaoLocalDateTime));

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await _rentalManagementService.CreateImovelChaveMovimentoAsync(
                new CreateImovelChaveMovimentoRequest(
                    imovelId,
                    ChavesCodigoBox.Text,
                    ImovelChaveMovimentoTipo.Retirada,
                    ChavesRetiradoPorNomeBox.Text,
                    ChavesRetiradoPorTelefoneBox.Text,
                    ChavesRetiradoPorDocumentoBox.Text,
                    ChavesRetiradoPorRelacaoBox.Text,
                    ChavesMotivoBox.Text,
                    DateTimeOffset.Now,
                    previsaoDevolucao,
                    ChavesObservacoesBox.Text),
                timeout.Token);

            await LoadChavesAsync();
            ShowChavesDialogAndReset("Retirada registrada com sucesso.", "Chaves", MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            await ReloadChavesAfterTimedSaveAsync(selectedImovelId, "Retirada");
        }
        catch (Exception ex)
        {
            ShowChavesDialog(GetChavesExceptionMessage(ex), "Chaves", MessageBoxImage.Warning);
        }
        finally
        {
            UpdateChavesWithdrawalButtonState();
        }
    }

    private async Task ReloadChavesAfterTimedSaveAsync(Guid imovelId, string actionName)
    {
        try
        {
            ShowChavesStatusMessage("Atualizando listas...");
            await LoadChavesAsync();
            await RefreshChavesDualListsAsync();

            var saved = imovelId != Guid.Empty
                && _chaveMovimentos.Any(x => x.ImovelId == imovelId && !x.DevolvidoEm.HasValue);

            if (saved)
            {
                ShowChavesDialogAndReset($"{actionName} registrada com sucesso.", "Chaves", MessageBoxImage.Information);
            }
            else
            {
                ShowChavesDialog($"{actionName} demorou mais que o esperado. Clique em Atualizar para conferir se foi registrada.", "Chaves", MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            ShowChavesDialog(GetChavesExceptionMessage(ex), "Chaves", MessageBoxImage.Warning);
        }
    }

    private async void ReturnChaveButton_Click(object sender, RoutedEventArgs e)
    {
        ShowChavesStatusMessage("Salvando devolução...");
        ReturnChaveButton.IsEnabled = false;
        try
        {
            var item = GetSelectedChavesReturnItem();
            if (item is null || !item.MovimentoId.HasValue)
            {
                ShowChavesDialog("Selecione uma chave retirada na lista da direita.", "Chaves", MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ChavesDevolvidoParaBox.Text))
            {
                ShowChavesDialog("Informe quem recebeu a chave.", "Chaves", MessageBoxImage.Warning);
                return;
            }

            var devolucao = GetChavesReturnDateTime();
            if (item.RetiradoEm.HasValue && devolucao < item.RetiradoEm.Value)
            {
                ShowChavesDialog("A data/hora da devolução não pode ser anterior à retirada da chave.", "Chaves", MessageBoxImage.Warning);
                return;
            }

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await _rentalManagementService.ReturnImovelChaveMovimentoAsync(
                new ReturnImovelChaveMovimentoRequest(
                    item.MovimentoId.Value,
                    ChavesDevolvidoParaBox.Text,
                    ChavesDevolucaoObservacoesBox.Text,
                    devolucao),
                timeout.Token);

            await LoadChavesAsync();
            ShowChavesDialogAndReset("Devolução registrada com sucesso.", "Chaves", MessageBoxImage.Information);
        }
        catch (OperationCanceledException)
        {
            ShowChavesDialog("A devolução demorou mais que o esperado. Clique em Atualizar para conferir se foi registrada.", "Chaves", MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            ShowChavesDialog(GetChavesExceptionMessage(ex), "Chaves", MessageBoxImage.Warning);
        }
        finally
        {
            UpdateSelectedChaveMovement();
        }
    }

    private static string GetChavesExceptionMessage(Exception ex)
    {
        var baseException = ex.GetBaseException();
        return baseException is not null && !string.Equals(baseException.Message, ex.Message, StringComparison.Ordinal)
            ? $"{ex.Message} Detalhe: {baseException.Message}"
            : ex.Message;
    }

    private void RestoreChavesListSelection(Guid movimentoId)
    {
        foreach (var item in ChavesGrid.ItemsSource?.Cast<object>() ?? [])
        {
            if (item is ChavesListItem chaveItem && chaveItem.MovimentoId == movimentoId)
            {
                ChavesGrid.SelectedItem = item;
                ChavesGrid.ScrollIntoView(item);
                return;
            }
        }
    }

    private void UpdateSelectedChaveMovement()
    {
        var item = GetSelectedChavesReturnItem();
        var hasReceiver = !string.IsNullOrWhiteSpace(ChavesDevolvidoParaBox.Text);
        ReturnChaveButton.IsEnabled = item is not null && item.MovimentoId.HasValue && hasReceiver;
    }

    private void ShowChavesDialog(string message, string caption, MessageBoxImage icon)
    {
        MessageBox.Show(this, message, caption, MessageBoxButton.OK, icon);
    }

    private void ShowChavesDialogAndReset(string message, string caption, MessageBoxImage icon)
    {
        MessageBox.Show(this, message, caption, MessageBoxButton.OK, icon);
        ResetChavesCardState();
    }

    private void ResetChavesCardState()
    {
        ChavesGrid.SelectedItem = null;
        if (_chavesTakenGrid is not null)
        {
            _chavesTakenGrid.SelectedItem = null;
        }

        _selectedChavesTakenItem = null;
        ChavesImovelBox.SelectedValue = null;
        ChavesStatusFilterBox.SelectedIndex = -1;
        ClearChavesRetiradaForm();
        ChavesDevolvidoParaBox.Clear();
        ChavesDevolucaoObservacoesBox.Clear();
        SetChavesReturnDateTimeToNow();
        ChavesErrorText.Text = string.Empty;
        ReturnChaveButton.IsEnabled = false;
        UpdateChavesWithdrawalButtonState();
    }

    private void ClearChavesRetiradaForm()
    {
        ChavesImovelBox.SelectedValue = null;
        ChavesCodigoBox.Clear();
        ChavesRetiradoPorNomeBox.Clear();
        ChavesRetiradoPorTelefoneBox.Clear();
        ChavesRetiradoPorDocumentoBox.Clear();
        ChavesRetiradoPorRelacaoBox.Clear();
        ChavesMotivoBox.Clear();
        ChavesObservacoesBox.Clear();
        ChavesPrevisaoBox.SelectedDate = DateTime.Today;
        ClearChavesSelectedImovelSummary();
        ResetChavesRelacaoDropdown();
    }

    private sealed record ChavesListItem(
        Guid ImovelId,
        Guid? MovimentoId,
        string Imovel,
        string Proprietario,
        string? ChaveCodigo,
        string Status,
        string? RetiradoPorNome,
        string? RetiradoPorTelefone,
        string? RetiradoPorDocumento,
        string? Relacao,
        string? Motivo,
        DateTimeOffset? RetiradoEm,
        DateTimeOffset? PrevisaoDevolucaoEm,
        string? Observacoes);
}
