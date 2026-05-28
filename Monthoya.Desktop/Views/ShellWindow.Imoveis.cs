using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{

    private async Task LoadImoveisAsync()
    {
        _imoveis = await _rentalManagementService.GetImoveisAsync();
        ApplyImoveisFilter();

        _pessoas = await _rentalManagementService.GetPessoasAsync();
        ImovelProprietarioBox.ItemsSource = _pessoas.Where(x => x.Status == "Ativo").ToList();
    }

    private async void ReloadImoveisButton_Click(object sender, RoutedEventArgs e) => await LoadImoveisAsync();

    private void ImoveisSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyImoveisFilter();
        SaveActiveTabState();
    }

    private void ApplyImoveisFilter()
    {
        var query = ImoveisSearchBox.Text;
        ImoveisGrid.ItemsSource = _imoveis
            .Where(x => ContainsSearch(query, x.Endereco, x.Bairro, x.Proprietario, x.Finalidade, x.Status))
            .ToList();
    }

    private async void SaveImovelButton_Click(object sender, RoutedEventArgs e)
    {
        ImovelErrorText.Text = string.Empty;

        try
        {
            var finalidade = ImovelFinalidadeBox.SelectedValue is ImovelFinalidade selectedFinalidade
                ? selectedFinalidade
                : ImovelFinalidade.Locacao;

            decimal? valorAluguel = null;
            if (!string.IsNullOrWhiteSpace(ImovelValorAluguelBox.Text)
                && decimal.TryParse(ImovelValorAluguelBox.Text, NumberStyles.Currency, CultureInfo.GetCultureInfo("pt-BR"), out var parsedValue))
            {
                valorAluguel = parsedValue;
            }

            var valorVenda = ParseNullableDecimal(ImovelValorVendaBox.Text);
            var latitude = ParseNullableDecimal(ImovelLatitudeBox.Text);
            var longitude = ParseNullableDecimal(ImovelLongitudeBox.Text);

            var proprietarioId = ImovelProprietarioBox.SelectedValue is Guid selectedOwnerId ? selectedOwnerId : Guid.Empty;
            await _rentalManagementService.CreateImovelAsync(new CreateImovelRequest(
                ProprietarioId: proprietarioId,
                Rua: ImovelRuaBox.Text,
                Numero: ImovelNumeroBox.Text,
                Bairro: ImovelBairroBox.Text,
                Cidade: "Paranavaí",
                Estado: "PR",
                ValorAluguel: valorAluguel,
                Finalidade: finalidade,
                Observacoes: ImovelObservacoesBox.Text,
                Complemento: ImovelComplementoBox.Text,
                Cep: ImovelCepBox.Text,
                SaneparMatricula: ImovelSaneparBox.Text,
                CopelMatricula: ImovelCopelBox.Text,
                IptuMatricula: ImovelIptuBox.Text,
                TipoImovel: ImovelTipoBox.Text,
                Descricao: ImovelDescricaoBox.Text,
                ValorVenda: valorVenda,
                Latitude: latitude,
                Longitude: longitude));

            ImovelRuaBox.Clear();
            ImovelNumeroBox.Clear();
            ImovelComplementoBox.Clear();
            ImovelBairroBox.Clear();
            ImovelCepBox.Clear();
            ImovelTipoBox.Clear();
            ImovelSaneparBox.Clear();
            ImovelCopelBox.Clear();
            ImovelIptuBox.Clear();
            ImovelValorAluguelBox.Clear();
            ImovelValorVendaBox.Clear();
            ImovelLatitudeBox.Clear();
            ImovelLongitudeBox.Clear();
            ImovelDescricaoBox.Clear();
            ImovelObservacoesBox.Clear();
            await LoadImoveisAsync();
        }
        catch (Exception ex)
        {
            ImovelErrorText.Text = ex.Message;
        }
    }

    private ImoveisPageState CaptureImoveisPageState() =>
        new(
            ImoveisSearchBox.Text,
            TryGetItemId(ImoveisGrid.SelectedItem),
            ImovelProprietarioBox.SelectedValue as Guid?,
            ImovelFinalidadeBox.SelectedValue is ImovelFinalidade finalidade ? finalidade : ImovelFinalidade.Locacao,
            ImovelRuaBox.Text,
            ImovelNumeroBox.Text,
            ImovelComplementoBox.Text,
            ImovelBairroBox.Text,
            ImovelCepBox.Text,
            ImovelTipoBox.Text,
            ImovelSaneparBox.Text,
            ImovelCopelBox.Text,
            ImovelIptuBox.Text,
            ImovelValorAluguelBox.Text,
            ImovelValorVendaBox.Text,
            ImovelLatitudeBox.Text,
            ImovelLongitudeBox.Text,
            ImovelDescricaoBox.Text,
            ImovelObservacoesBox.Text);

    private Task RestoreImoveisPageStateAsync(ImoveisPageState state)
    {
        ImoveisSearchBox.Text = state.SearchText;
        ApplyImoveisFilter();
        RestoreDataGridSelection(ImoveisGrid, state.SelectedImovelId);
        ImovelProprietarioBox.SelectedValue = state.ProprietarioId;
        ImovelFinalidadeBox.SelectedValue = state.Finalidade;
        ImovelRuaBox.Text = state.Rua;
        ImovelNumeroBox.Text = state.Numero;
        ImovelComplementoBox.Text = state.Complemento;
        ImovelBairroBox.Text = state.Bairro;
        ImovelCepBox.Text = state.Cep;
        ImovelTipoBox.Text = state.TipoImovel;
        ImovelSaneparBox.Text = state.Sanepar;
        ImovelCopelBox.Text = state.Copel;
        ImovelIptuBox.Text = state.Iptu;
        ImovelValorAluguelBox.Text = state.ValorAluguel;
        ImovelValorVendaBox.Text = state.ValorVenda;
        ImovelLatitudeBox.Text = state.Latitude;
        ImovelLongitudeBox.Text = state.Longitude;
        ImovelDescricaoBox.Text = state.Descricao;
        ImovelObservacoesBox.Text = state.Observacoes;
        return Task.CompletedTask;
    }

    private sealed record ImoveisPageState(
        string SearchText,
        Guid? SelectedImovelId,
        Guid? ProprietarioId,
        ImovelFinalidade Finalidade,
        string Rua,
        string Numero,
        string Complemento,
        string Bairro,
        string Cep,
        string TipoImovel,
        string Sanepar,
        string Copel,
        string Iptu,
        string ValorAluguel,
        string ValorVenda,
        string Latitude,
        string Longitude,
        string Descricao,
        string Observacoes) : IShellPageState
    {
        public static ImoveisPageState Default { get; } = new(
            "",
            null,
            null,
            ImovelFinalidade.Locacao,
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "");
    }
}


