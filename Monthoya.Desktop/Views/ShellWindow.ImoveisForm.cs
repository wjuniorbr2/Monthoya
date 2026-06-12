using System;
using System.Globalization;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private CreateImovelRequest BuildImovelRequestFromForm()
    {
        var finalidade = ImovelFinalidadeBox.SelectedValue is ImovelFinalidade selectedFinalidade
            ? selectedFinalidade
            : ImovelFinalidade.Locacao;
        var status = ImovelStatusBox.SelectedValue is ImovelStatus selectedStatus
            ? selectedStatus
            : ImovelStatus.Disponivel;
        var chavePosse = ImovelChavePosseBox.SelectedValue is ImovelChavePosse selectedChavePosse
            ? selectedChavePosse
            : ImovelChavePosse.NaoCadastrada;
        var enderecoPublicoModo = ImovelEnderecoPublicoModoBox.SelectedValue is ImovelEnderecoPublicoModo selectedEnderecoPublicoModo
            ? selectedEnderecoPublicoModo
            : ImovelEnderecoPublicoModo.BairroCidade;

        decimal? valorAluguel = null;
        if (!string.IsNullOrWhiteSpace(ImovelValorAluguelBox.Text)
            && decimal.TryParse(ImovelValorAluguelBox.Text, NumberStyles.Currency, CultureInfo.GetCultureInfo("pt-BR"), out var parsedValue))
        {
            valorAluguel = parsedValue;
        }

        return new CreateImovelRequest(
            ProprietarioId: ResolveImovelProprietarioId(),
            Rua: ImovelRuaBox.Text,
            Numero: ImovelNumeroBox.Text,
            Bairro: ImovelBairroBox.Text,
            Cidade: string.IsNullOrWhiteSpace(ImovelCidadeBox.Text) ? "ParanavaÃ­" : ImovelCidadeBox.Text,
            Estado: string.IsNullOrWhiteSpace(ImovelEstadoBox.Text) ? "PR" : ImovelEstadoBox.Text,
            ValorAluguel: valorAluguel,
            Finalidade: finalidade,
            Observacoes: ImovelObservacoesBox.Text,
            Complemento: ImovelComplementoBox.Text,
            Cep: ImovelCepBox.Text,
            SaneparMatricula: ImovelSaneparBox.Text,
            CopelMatricula: ImovelCopelBox.Text,
            IptuInscricaoImobiliaria: ImovelIptuInscricaoBox.Text,
            IptuCadastroImovel: ImovelIptuCadastroBox.Text,
            ColetaLixo: ImovelColetaLixoBox.Text,
            TipoImovel: ImovelTipoBox.Text,
            Descricao: ImovelDescricaoBox.Text,
            ValorVenda: ParseNullableDecimal(ImovelValorVendaBox.Text),
            Latitude: ParseNullableDecimal(ImovelLatitudeBox.Text),
            Longitude: ParseNullableDecimal(ImovelLongitudeBox.Text),
            Status: status,
            ValorCondominio: ParseNullableDecimal(ImovelValorCondominioBox.Text),
            ValorIptu: ParseNullableDecimal(ImovelValorIptuBox.Text),
            Quartos: ParseNullableInt(ImovelQuartosBox.Text),
            Suites: ParseNullableInt(ImovelSuitesBox.Text),
            Banheiros: ParseNullableInt(ImovelBanheirosBox.Text),
            VagasGaragem: ParseNullableInt(ImovelVagasBox.Text),
            Lavabos: ParseNullableInt(ImovelLavabosBox.Text),
            Salas: ParseNullableInt(ImovelSalasBox.Text),
            Cozinhas: ParseNullableInt(ImovelCozinhasBox.Text),
            Copas: ParseNullableInt(ImovelCopasBox.Text),
            Despensas: ParseNullableInt(ImovelDespensasBox.Text),
            Lavanderias: ParseNullableInt(ImovelLavanderiasBox.Text),
            AreasServico: ParseNullableInt(ImovelAreasServicoBox.Text),
            Sacadas: ParseNullableInt(ImovelSacadasBox.Text),
            Churrasqueiras: ParseNullableInt(ImovelChurrasqueirasBox.Text),
            Piscinas: ParseNullableInt(ImovelPiscinasBox.Text),
            Quintais: ParseNullableInt(ImovelQuintaisBox.Text),
            HallsEntrada: ParseNullableInt(ImovelHallsEntradaBox.Text),
            Estendais: ParseNullableInt(ImovelEstendaisBox.Text),
            AreaConstruida: ParseNullableDecimal(ImovelAreaConstruidaBox.Text),
            AreaTerreno: ParseNullableDecimal(ImovelAreaTerrenoBox.Text),
            Mobiliado: ImovelMobiliadoBox.IsChecked,
            AceitaPets: ImovelAceitaPetsBox.IsChecked,
            DescricaoInterna: ImovelDescricaoBox.Text,
            DescricaoPublica: ImovelDescricaoPublicaBox.Text,
            PublicarNoSite: ImovelPublicarSiteBox.IsChecked == true,
            PublicarNoApp: ImovelPublicarAppBox.IsChecked == true,
            Destaque: ImovelDestaqueBox.IsChecked == true,
            MostrarEnderecoCompletoPublicamente: ImovelMostrarEnderecoCompletoBox.IsChecked == true,
            ModoExibicaoEnderecoPublico: enderecoPublicoModo,
            ChavePosse: chavePosse,
            ChaveCodigo: ImovelChaveCodigoBox.Text,
            ChaveQuemTem: ImovelChaveQuemTemBox.Text,
            ChaveTelefone: ImovelChaveTelefoneBox.Text,
            ChaveContatoNome: ImovelChaveContatoNomeBox.Text,
            ChaveContatoDocumento: ImovelChaveContatoDocumentoBox.Text,
            ChaveLocalRetirada: ImovelChaveLocalBox.Text,
            ChaveMelhorHorario: ImovelChaveHorarioBox.Text,
            ChaveAutorizacaoNecessaria: ImovelChaveAutorizacaoBox.IsChecked == true,
            ChaveObservacoes: ImovelChaveObservacoesBox.Text);
    }
}


