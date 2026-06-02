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

    private void ChavesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSelectedChaveMovement();

    private void ApplyChavesFilter()
    {
        if (ChavesGrid is null)
        {
            return;
        }

        var query = ChavesSearchBox.Text;
        var status = ChavesStatusFilterBox.SelectedValue as string ?? "ativas";
        var filtered = _chaveMovimentos
            .Where(x => status switch
            {
                "ativas" => !x.DevolvidoEm.HasValue,
                "atraso" => string.Equals(x.Status, "Em atraso", StringComparison.OrdinalIgnoreCase),
                "devolvidas" => x.DevolvidoEm.HasValue,
                _ => true
            })
            .Where(x => ContainsSearch(
                query,
                x.Imovel,
                x.ChaveCodigo,
                x.Status,
                x.RetiradoPorNome,
                x.RetiradoPorTelefone,
                x.RetiradoPorDocumento,
                x.RetiradoPorRelacao,
                x.Motivo,
                x.Observacoes))
            .OrderByDescending(x => x.DevolvidoEm is null)
            .ThenByDescending(x => x.RetiradoEm)
            .ToList();

        ChavesGrid.ItemsSource = filtered;
    }

    private async void SaveChaveRetiradaButton_Click(object sender, RoutedEventArgs e)
    {
        ChavesErrorText.Text = string.Empty;
        try
        {
            if (ChavesImovelBox.SelectedValue is not Guid imovelId || imovelId == Guid.Empty)
            {
                ChavesErrorText.Text = "Selecione o imóvel da chave.";
                return;
            }

            var previsao = ChavesPrevisaoBox.SelectedDate;
            if (!previsao.HasValue)
            {
                ChavesErrorText.Text = "Informe a previsão de devolução.";
                return;
            }

            var previsaoDevolucao = new DateTimeOffset(
                previsao.Value.Date.AddHours(18),
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
            await LoadChavesAsync();
            RestoreDataGridSelection(ChavesGrid, movimento.Id);
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
            if (ChavesGrid.SelectedItem is not ImovelChaveMovimentoSummary movimento)
            {
                ChavesErrorText.Text = "Selecione uma retirada ativa na lista.";
                return;
            }

            if (movimento.DevolvidoEm.HasValue)
            {
                ChavesErrorText.Text = "Esta chave já foi devolvida.";
                return;
            }

            var returned = await _rentalManagementService.ReturnImovelChaveMovimentoAsync(
                new ReturnImovelChaveMovimentoRequest(
                    movimento.Id,
                    ChavesDevolvidoParaBox.Text,
                    ChavesDevolucaoObservacoesBox.Text));

            ChavesDevolvidoParaBox.Clear();
            ChavesDevolucaoObservacoesBox.Clear();
            await LoadChavesAsync();
            RestoreDataGridSelection(ChavesGrid, returned.Id);
        }
        catch (Exception ex)
        {
            ChavesErrorText.Text = ex.Message;
        }
    }

    private void UpdateSelectedChaveMovement()
    {
        if (ChavesGrid.SelectedItem is not ImovelChaveMovimentoSummary movimento)
        {
            ChavesSelectedMovimentoText.Text = "Selecione uma retirada ativa na lista.";
            ReturnChaveButton.IsEnabled = false;
            return;
        }

        var retirada = movimento.RetiradoEm?.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR")) ?? "-";
        ChavesSelectedMovimentoText.Text = $"{movimento.Imovel} | {movimento.RetiradoPorNome ?? "-"} | retirada em {retirada}";
        ReturnChaveButton.IsEnabled = !movimento.DevolvidoEm.HasValue;
    }

    private void ClearChavesRetiradaForm()
    {
        ChavesCodigoBox.Clear();
        ChavesRetiradoPorNomeBox.Clear();
        ChavesRetiradoPorTelefoneBox.Clear();
        ChavesRetiradoPorDocumentoBox.Clear();
        ChavesRetiradoPorRelacaoBox.Clear();
        ChavesMotivoBox.Clear();
        ChavesObservacoesBox.Clear();
        ChavesPrevisaoBox.SelectedDate = DateTime.Today;
    }
}
