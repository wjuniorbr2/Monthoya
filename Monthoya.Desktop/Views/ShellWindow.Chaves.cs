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
    }

    private void ChavesStatusFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyChavesFilter();
        SaveActiveTabState();
    }

    private void ChavesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
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
                null,
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
            movimento.ChaveCodigo,
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
        ChavesErrorText.Text = string.Empty;
        try
        {
            if (ChavesImovelBox.SelectedValue is not Guid imovelId || imovelId == Guid.Empty)
            {
                ChavesErrorText.Text = "Selecione o imóvel na lista.";
                return;
            }

            var previsao = ChavesPrevisaoBox.SelectedDate;
            if (!previsao.HasValue)
            {
                ChavesErrorText.Text = "Informe a previsão de devolução.";
                return;
            }

            var previsaoHora = GetChavesPrevisaoHorario();
            var previsaoDevolucao = new DateTimeOffset(
                previsao.Value.Date.Add(previsaoHora),
                TimeZoneInfo.Local.GetUtcOffset(previsao.Value.Date));

            var movimento = await _rentalManagementService.CreateImovelChaveMovimentoAsync(
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
                    ChavesObservacoesBox.Text));

            ClearChavesRetiradaForm();
            SetChavesMovimentoMode(isReturn: true);
            await LoadChavesAsync();
            RestoreChavesListSelection(movimento.Id);
        }
        catch (Exception ex)
        {
            ChavesErrorText.Text = ex.Message;
        }
    }

    private async void ReturnChaveButton_Click(object sender, RoutedEventArgs e)
    {
        ChavesErrorText.Text = string.Empty;
        try
        {
            if (ChavesGrid.SelectedItem is not ChavesListItem item || !item.MovimentoId.HasValue)
            {
                ChavesErrorText.Text = "Selecione uma chave retirada na lista.";
                return;
            }

            var returned = await _rentalManagementService.ReturnImovelChaveMovimentoAsync(
                new ReturnImovelChaveMovimentoRequest(
                    item.MovimentoId.Value,
                    ChavesDevolvidoParaBox.Text,
                    ChavesDevolucaoObservacoesBox.Text));

            ChavesDevolvidoParaBox.Clear();
            ChavesDevolucaoObservacoesBox.Clear();
            await LoadChavesAsync();
            RestoreChavesListSelection(returned.Id);
        }
        catch (Exception ex)
        {
            ChavesErrorText.Text = ex.Message;
        }
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
        if (ChavesGrid.SelectedItem is not ChavesListItem item || !item.MovimentoId.HasValue)
        {
            ChavesSelectedMovimentoText.Text = "Selecione uma chave retirada na lista.";
            ReturnChaveButton.IsEnabled = false;
            return;
        }

        var retirada = item.RetiradoEm?.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR")) ?? "-";
        ChavesSelectedMovimentoText.Text = $"{item.Imovel} | {item.RetiradoPorNome ?? "-"} | retirada em {retirada}";
        ReturnChaveButton.IsEnabled = true;
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
