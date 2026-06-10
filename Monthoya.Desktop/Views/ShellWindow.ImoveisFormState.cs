using System.Globalization;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private ImoveisPageState CaptureImoveisPageState() =>
        new()
        {
            SearchText = ImoveisSearchBox.Text,
            SelectedImovelId = TryGetItemId(ImoveisGrid.SelectedItem),
            ProprietarioId = ImovelProprietarioBox.SelectedValue as Guid?,
            Finalidade = ImovelFinalidadeBox.SelectedValue is ImovelFinalidade finalidade ? finalidade : ImovelFinalidade.Locacao,
            Rua = ImovelRuaBox.Text,
            Numero = ImovelNumeroBox.Text,
            Complemento = ImovelComplementoBox.Text,
            Bairro = ImovelBairroBox.Text,
            Cidade = ImovelCidadeBox.Text,
            Estado = ImovelEstadoBox.Text,
            Cep = ImovelCepBox.Text,
            TipoImovel = ImovelTipoBox.Text,
            Sanepar = ImovelSaneparBox.Text,
            Copel = ImovelCopelBox.Text,
            IptuInscricaoImobiliaria = ImovelIptuInscricaoBox.Text,
            IptuCadastroImovel = ImovelIptuCadastroBox.Text,
            ColetaLixo = ImovelColetaLixoBox.Text,
            ValorAluguel = ImovelValorAluguelBox.Text,
            ValorVenda = ImovelValorVendaBox.Text,
            ValorCondominio = ImovelValorCondominioBox.Text,
            ValorIptu = ImovelValorIptuBox.Text,
            Latitude = ImovelLatitudeBox.Text,
            Longitude = ImovelLongitudeBox.Text,
            Status = ImovelStatusBox.SelectedValue is ImovelStatus status ? status : ImovelStatus.Disponivel,
            Quartos = ImovelQuartosBox.Text,
            Suites = ImovelSuitesBox.Text,
            Banheiros = ImovelBanheirosBox.Text,
            Vagas = ImovelVagasBox.Text,
            AreaConstruida = ImovelAreaConstruidaBox.Text,
            AreaTerreno = ImovelAreaTerrenoBox.Text,
            Mobiliado = ImovelMobiliadoBox.IsChecked,
            AceitaPets = ImovelAceitaPetsBox.IsChecked,
            Descricao = ImovelDescricaoBox.Text,
            DescricaoPublica = ImovelDescricaoPublicaBox.Text,
            Observacoes = ImovelObservacoesBox.Text,
            PublicarSite = ImovelPublicarSiteBox.IsChecked == true,
            PublicarApp = ImovelPublicarAppBox.IsChecked == true,
            Destaque = ImovelDestaqueBox.IsChecked == true,
            MostrarEnderecoCompleto = ImovelMostrarEnderecoCompletoBox.IsChecked == true,
            ModoEnderecoPublico = ImovelEnderecoPublicoModoBox.SelectedValue is ImovelEnderecoPublicoModo modo ? modo : ImovelEnderecoPublicoModo.BairroCidade,
            ChavePosse = ImovelChavePosseBox.SelectedValue is ImovelChavePosse posse ? posse : ImovelChavePosse.NaoCadastrada,
            ChaveCodigo = ImovelChaveCodigoBox.Text,
            ChaveQuemTem = ImovelChaveQuemTemBox.Text,
            ChaveTelefone = ImovelChaveTelefoneBox.Text,
            ChaveContatoNome = ImovelChaveContatoNomeBox.Text,
            ChaveContatoDocumento = ImovelChaveContatoDocumentoBox.Text,
            ChaveLocal = ImovelChaveLocalBox.Text,
            ChaveHorario = ImovelChaveHorarioBox.Text,
            ChaveAutorizacao = ImovelChaveAutorizacaoBox.IsChecked == true,
            ChaveObservacoes = ImovelChaveObservacoesBox.Text
        };
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
        ImovelCidadeBox.Text = state.Cidade;
        ImovelEstadoBox.Text = state.Estado;
        ImovelCepBox.Text = state.Cep;
        ImovelTipoBox.Text = state.TipoImovel;
        ImovelSaneparBox.Text = state.Sanepar;
        ImovelCopelBox.Text = state.Copel;
        ImovelIptuInscricaoBox.Text = state.IptuInscricaoImobiliaria;
        ImovelIptuCadastroBox.Text = state.IptuCadastroImovel;
        ImovelColetaLixoBox.Text = state.ColetaLixo;
        ImovelValorAluguelBox.Text = state.ValorAluguel;
        ImovelValorVendaBox.Text = state.ValorVenda;
        ImovelValorCondominioBox.Text = state.ValorCondominio;
        ImovelValorIptuBox.Text = state.ValorIptu;
        ImovelLatitudeBox.Text = state.Latitude;
        ImovelLongitudeBox.Text = state.Longitude;
        ImovelStatusBox.SelectedValue = state.Status;
        ImovelQuartosBox.Text = state.Quartos;
        ImovelSuitesBox.Text = state.Suites;
        ImovelBanheirosBox.Text = state.Banheiros;
        ImovelVagasBox.Text = state.Vagas;
        ImovelAreaConstruidaBox.Text = state.AreaConstruida;
        ImovelAreaTerrenoBox.Text = state.AreaTerreno;
        ImovelMobiliadoBox.IsChecked = state.Mobiliado;
        ImovelAceitaPetsBox.IsChecked = state.AceitaPets;
        ImovelDescricaoBox.Text = state.Descricao;
        ImovelDescricaoPublicaBox.Text = state.DescricaoPublica;
        ImovelObservacoesBox.Text = state.Observacoes;
        ImovelPublicarSiteBox.IsChecked = state.PublicarSite;
        ImovelPublicarAppBox.IsChecked = state.PublicarApp;
        ImovelDestaqueBox.IsChecked = state.Destaque;
        ImovelMostrarEnderecoCompletoBox.IsChecked = state.MostrarEnderecoCompleto;
        ImovelEnderecoPublicoModoBox.SelectedValue = state.ModoEnderecoPublico;
        ImovelChavePosseBox.SelectedValue = state.ChavePosse;
        ImovelChaveCodigoBox.Text = state.ChaveCodigo;
        ImovelChaveQuemTemBox.Text = state.ChaveQuemTem;
        ImovelChaveTelefoneBox.Text = state.ChaveTelefone;
        ImovelChaveContatoNomeBox.Text = state.ChaveContatoNome;
        ImovelChaveContatoDocumentoBox.Text = state.ChaveContatoDocumento;
        ImovelChaveLocalBox.Text = state.ChaveLocal;
        ImovelChaveHorarioBox.Text = state.ChaveHorario;
        ImovelChaveAutorizacaoBox.IsChecked = state.ChaveAutorizacao;
        ImovelChaveObservacoesBox.Text = state.ChaveObservacoes;
        return Task.CompletedTask;
    }

    private void SetImovelForm(CreateImovelRequest dados)
    {
        ImovelProprietarioBox.SelectedValue = dados.ProprietarioId;
        ImovelFinalidadeBox.SelectedValue = dados.Finalidade;
        ImovelRuaBox.Text = dados.Rua;
        ImovelNumeroBox.Text = dados.Numero ?? string.Empty;
        ImovelComplementoBox.Text = dados.Complemento ?? string.Empty;
        ImovelBairroBox.Text = dados.Bairro ?? string.Empty;
        ImovelCidadeBox.Text = dados.Cidade;
        ImovelEstadoBox.Text = dados.Estado;
        ImovelCepBox.Text = dados.Cep ?? string.Empty;
        ImovelTipoBox.Text = dados.TipoImovel ?? string.Empty;
        ImovelSaneparBox.Text = dados.SaneparMatricula ?? string.Empty;
        ImovelCopelBox.Text = dados.CopelMatricula ?? string.Empty;
        ImovelIptuInscricaoBox.Text = dados.IptuInscricaoImobiliaria ?? string.Empty;
        ImovelIptuCadastroBox.Text = dados.IptuCadastroImovel ?? string.Empty;
        ImovelColetaLixoBox.Text = dados.ColetaLixo ?? string.Empty;
        ImovelValorAluguelBox.Text = FormatNullableDecimal(dados.ValorAluguel);
        ImovelValorVendaBox.Text = FormatNullableDecimal(dados.ValorVenda);
        ImovelValorCondominioBox.Text = FormatNullableDecimal(dados.ValorCondominio);
        ImovelValorIptuBox.Text = FormatNullableDecimal(dados.ValorIptu);
        ImovelLatitudeBox.Text = FormatNullableDecimal(dados.Latitude);
        ImovelLongitudeBox.Text = FormatNullableDecimal(dados.Longitude);
        ImovelStatusBox.SelectedValue = dados.Status;
        ImovelQuartosBox.Text = dados.Quartos?.ToString(CultureInfo.GetCultureInfo("pt-BR")) ?? string.Empty;
        ImovelSuitesBox.Text = dados.Suites?.ToString(CultureInfo.GetCultureInfo("pt-BR")) ?? string.Empty;
        ImovelBanheirosBox.Text = dados.Banheiros?.ToString(CultureInfo.GetCultureInfo("pt-BR")) ?? string.Empty;
        ImovelVagasBox.Text = dados.VagasGaragem?.ToString(CultureInfo.GetCultureInfo("pt-BR")) ?? string.Empty;
        ImovelAreaConstruidaBox.Text = FormatNullableDecimal(dados.AreaConstruida);
        ImovelAreaTerrenoBox.Text = FormatNullableDecimal(dados.AreaTerreno);
        ImovelMobiliadoBox.IsChecked = dados.Mobiliado;
        ImovelAceitaPetsBox.IsChecked = dados.AceitaPets;
        ImovelDescricaoBox.Text = dados.DescricaoInterna ?? dados.Descricao ?? string.Empty;
        ImovelDescricaoPublicaBox.Text = dados.DescricaoPublica ?? string.Empty;
        ImovelObservacoesBox.Text = dados.Observacoes ?? string.Empty;
        ImovelPublicarSiteBox.IsChecked = dados.PublicarNoSite;
        ImovelPublicarAppBox.IsChecked = dados.PublicarNoApp;
        ImovelDestaqueBox.IsChecked = dados.Destaque;
        ImovelMostrarEnderecoCompletoBox.IsChecked = dados.MostrarEnderecoCompletoPublicamente;
        ImovelEnderecoPublicoModoBox.SelectedValue = dados.ModoExibicaoEnderecoPublico;
        ImovelChavePosseBox.SelectedValue = dados.ChavePosse;
        ImovelChaveCodigoBox.Text = dados.ChaveCodigo ?? string.Empty;
        ImovelChaveQuemTemBox.Text = dados.ChaveQuemTem ?? string.Empty;
        ImovelChaveTelefoneBox.Text = dados.ChaveTelefone ?? string.Empty;
        ImovelChaveContatoNomeBox.Text = dados.ChaveContatoNome ?? string.Empty;
        ImovelChaveContatoDocumentoBox.Text = dados.ChaveContatoDocumento ?? string.Empty;
        ImovelChaveLocalBox.Text = dados.ChaveLocalRetirada ?? string.Empty;
        ImovelChaveHorarioBox.Text = dados.ChaveMelhorHorario ?? string.Empty;
        ImovelChaveAutorizacaoBox.IsChecked = dados.ChaveAutorizacaoNecessaria;
        ImovelChaveObservacoesBox.Text = dados.ChaveObservacoes ?? string.Empty;
        UpdateImovelChaveFieldsVisibility();
    }

    private void SetImovelEditMode(bool isEditing, bool isNew)
    {
        _isImovelEditing = isEditing;
        ImovelFormTitleText.Text = isNew ? "Criar novo" : ImovelFormTitleText.Text;
        if (isNew)
        {
            SetActiveImovelTabLabel("Criar novo");
        }
        SaveImovelButton.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
        CancelImovelEditButton.Visibility = isEditing && !isNew ? Visibility.Visible : Visibility.Collapsed;
        ImovelEditButton.IsEnabled = !isEditing && _selectedImovelId.HasValue;
        ImovelDeactivateButton.IsEnabled = !isEditing && _selectedImovelId.HasValue;
        ImovelDeactivateButton.Content = _selectedImovelDetails?.Dados.Status == ImovelStatus.Inativo ? "Reativar" : "Remover";

        foreach (var textBox in GetImovelTextBoxes())
        {
            textBox.IsReadOnly = !isEditing;
        }

        foreach (var comboBox in GetImovelComboBoxes())
        {
            comboBox.IsEnabled = isEditing;
        }

        foreach (var checkBox in GetImovelCheckBoxes())
        {
            checkBox.IsEnabled = isEditing;
        }

        ImovelImagemArquivoBox.IsReadOnly = true;
        ImovelVistoriaDataBox.IsEnabled = isEditing;
        BrowseImovelImagemButton.IsEnabled = isEditing;
        BrowseImovelImagensBulkButton.IsEnabled = isEditing;
        SaveImovelImagemButton.IsEnabled = isEditing;
        SaveImovelVistoriaButton.IsEnabled = isEditing;
        ImovelImagemErrorText.Text = isEditing ? ImovelImagemErrorText.Text : string.Empty;
        UpdateImovelChaveFieldsVisibility();
    }

    private IEnumerable<TextBox> GetImovelTextBoxes()
    {
        yield return ImovelRuaBox;
        yield return ImovelNumeroBox;
        yield return ImovelComplementoBox;
        yield return ImovelBairroBox;
        yield return ImovelCidadeBox;
        yield return ImovelEstadoBox;
        yield return ImovelCepBox;
        yield return ImovelLatitudeBox;
        yield return ImovelLongitudeBox;
        yield return ImovelSaneparBox;
        yield return ImovelCopelBox;
        yield return ImovelIptuInscricaoBox;
        yield return ImovelIptuCadastroBox;
        yield return ImovelColetaLixoBox;
        yield return ImovelValorAluguelBox;
        yield return ImovelValorVendaBox;
        yield return ImovelValorCondominioBox;
        yield return ImovelValorIptuBox;
        yield return ImovelQuartosBox;
        yield return ImovelSuitesBox;
        yield return ImovelBanheirosBox;
        yield return ImovelVagasBox;
        yield return ImovelAreaConstruidaBox;
        yield return ImovelAreaTerrenoBox;
        yield return ImovelDescricaoBox;
        yield return ImovelDescricaoPublicaBox;
        yield return ImovelObservacoesBox;
        yield return ImovelChaveCodigoBox;
        yield return ImovelChaveQuemTemBox;
        yield return ImovelChaveTelefoneBox;
        yield return ImovelChaveContatoNomeBox;
        yield return ImovelChaveContatoDocumentoBox;
        yield return ImovelChaveLocalBox;
        yield return ImovelChaveHorarioBox;
        yield return ImovelChaveObservacoesBox;
        yield return ImovelImagemArquivoBox;
        yield return ImovelImagemLegendaBox;
        yield return ImovelImagemOrdemBox;
        yield return ImovelVistoriaResponsavelBox;
        yield return ImovelVistoriaDescricaoBox;
        yield return ImovelVistoriaObservacoesBox;
    }

    private IEnumerable<ComboBox> GetImovelComboBoxes()
    {
        yield return ImovelProprietarioBox;
        yield return ImovelTipoBox;
        yield return ImovelFinalidadeBox;
        yield return ImovelStatusBox;
        yield return ImovelChavePosseBox;
        yield return ImovelEnderecoPublicoModoBox;
        yield return ImovelMediaCategoryBox;
        yield return ImovelVistoriaTipoBox;
        yield return ImovelVistoriaStatusBox;
    }

    private IEnumerable<CheckBox> GetImovelCheckBoxes()
    {
        yield return ImovelMobiliadoBox;
        yield return ImovelAceitaPetsBox;
        yield return ImovelPublicarSiteBox;
        yield return ImovelPublicarAppBox;
        yield return ImovelDestaqueBox;
        yield return ImovelMostrarEnderecoCompletoBox;
        yield return ImovelChaveAutorizacaoBox;
        yield return ImovelImagemPublicaBox;
        yield return ImovelImagemCapaBox;
    }

    private void ClearImovelForm()
    {
        ImovelFormTitleText.Text = "Criar novo";
        SetActiveImovelTabLabel("Criar novo");
        ImovelProprietarioBox.SelectedIndex = -1;
        ImovelProprietarioBox.SelectedValue = null;
        ImovelProprietarioBox.Text = string.Empty;
        ImovelRuaBox.Clear();
        ImovelNumeroBox.Clear();
        ImovelComplementoBox.Clear();
        ImovelBairroBox.Clear();
        ImovelCidadeBox.Text = "Paranavaí";
        ImovelEstadoBox.Text = "PR";
        ImovelCepBox.Clear();
        ImovelTipoBox.SelectedIndex = -1;
        ImovelTipoBox.Text = string.Empty;
        ImovelSaneparBox.Clear();
        ImovelCopelBox.Clear();
        ImovelIptuInscricaoBox.Clear();
        ImovelIptuCadastroBox.Clear();
        ImovelColetaLixoBox.Clear();
        ImovelValorAluguelBox.Clear();
        ImovelValorVendaBox.Clear();
        ImovelValorCondominioBox.Clear();
        ImovelValorIptuBox.Clear();
        ImovelLatitudeBox.Clear();
        ImovelLongitudeBox.Clear();
        ImovelQuartosBox.Clear();
        ImovelSuitesBox.Clear();
        ImovelBanheirosBox.Clear();
        ImovelVagasBox.Clear();
        ImovelAreaConstruidaBox.Clear();
        ImovelAreaTerrenoBox.Clear();
        ImovelMobiliadoBox.IsChecked = false;
        ImovelAceitaPetsBox.IsChecked = false;
        ImovelDescricaoBox.Clear();
        ImovelDescricaoPublicaBox.Clear();
        ImovelObservacoesBox.Clear();
        ImovelPublicarSiteBox.IsChecked = false;
        ImovelPublicarAppBox.IsChecked = false;
        ImovelDestaqueBox.IsChecked = false;
        ImovelMostrarEnderecoCompletoBox.IsChecked = false;
        ImovelStatusBox.SelectedIndex = -1;
        ImovelStatusBox.Text = string.Empty;
        ImovelFinalidadeBox.SelectedIndex = -1;
        ImovelFinalidadeBox.Text = string.Empty;
        ImovelChavePosseBox.SelectedValue = ImovelChavePosse.NaoCadastrada;
        ImovelEnderecoPublicoModoBox.SelectedValue = ImovelEnderecoPublicoModo.BairroCidade;
        ImovelChaveCodigoBox.Clear();
        ImovelChaveQuemTemBox.Clear();
        ImovelChaveTelefoneBox.Clear();
        ImovelChaveContatoNomeBox.Clear();
        ImovelChaveContatoDocumentoBox.Clear();
        ImovelChaveLocalBox.Clear();
        ImovelChaveHorarioBox.Clear();
        ImovelChaveAutorizacaoBox.IsChecked = false;
        ImovelChaveObservacoesBox.Clear();
        UpdateImovelChaveFieldsVisibility();
        ClearImovelImagemForm();
        ClearImovelVistoriaForm();
        _pendingImovelMedia.Clear();
        _imovelImagens = [];
        RefreshImovelMediaGrid();
        _imovelVistorias = [];
        ImovelVistoriasGrid.ItemsSource = _imovelVistorias;
    }
}
