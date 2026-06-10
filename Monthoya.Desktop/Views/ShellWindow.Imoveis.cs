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

    private async Task LoadImoveisAsync()
    {
        _imoveis = await _rentalManagementService.GetImoveisAsync();
        ApplyImoveisFilter();

        _pessoas = await _rentalManagementService.GetPessoasAsync();
        RefreshImovelProprietarioOptions();
        await UpdateImovelOverdueKeysIndicatorAsync();
        if (_selectedImovelId.HasValue)
        {
            await LoadImovelImagensAsync(_selectedImovelId.Value);
            await LoadImovelVistoriasAsync(_selectedImovelId.Value);
        }
    }

    private async Task UpdateImovelOverdueKeysIndicatorAsync()
    {
        var movimentos = await _rentalManagementService.GetImovelChaveMovimentosAsync();
        var overdueCount = movimentos.Count(x => string.Equals(x.Status, "Em atraso", StringComparison.OrdinalIgnoreCase));
        ImovelOverdueKeysText.Text = overdueCount == 0
            ? string.Empty
            : overdueCount == 1
                ? "1 chave em atraso"
                : $"{overdueCount} chaves em atraso";
    }

    private async void ReloadImoveisButton_Click(object sender, RoutedEventArgs e) => await LoadImoveisAsync();

    private void ImovelProprietarioBox_KeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Up or Key.Down or Key.Left or Key.Right or Key.Enter or Key.Escape or Key.Tab)
        {
            return;
        }

        RefreshImovelProprietarioOptions(ImovelProprietarioBox.Text, openDropDown: true);
    }

    private void RefreshImovelProprietarioOptions(string? query = null, bool openDropDown = false)
    {
        var text = query ?? string.Empty;
        var owners = _pessoas
            .Where(x => x.Status == "Ativo")
            .Where(x => string.IsNullOrWhiteSpace(text) || ContainsSearch(text, x.Nome, x.Documento, x.Telefone))
            .OrderBy(x => x.Nome)
            .Take(50)
            .ToList();

        ImovelProprietarioBox.ItemsSource = owners;

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        ImovelProprietarioBox.Text = text;
        ImovelProprietarioBox.IsDropDownOpen = openDropDown && owners.Count > 0;
        Dispatcher.BeginInvoke(() =>
        {
            if (ImovelProprietarioBox.Template.FindName("PART_EditableTextBox", ImovelProprietarioBox) is TextBox textBox)
            {
                textBox.SelectionStart = textBox.Text.Length;
                textBox.SelectionLength = 0;
            }
        });
    }

    private void ImoveisSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ImoveisFilter_Changed(sender, e);
    }

    private void ImoveisFilter_Changed(object sender, EventArgs e)
    {
        ApplyImoveisFilter();
        SaveActiveTabState();
    }

    private void ApplyImoveisFilter()
    {
        var query = ImoveisSearchBox.Text;
        var statusFilter = ImoveisStatusFilterBox.SelectedValue as string ?? "ativos";
        var finalidadeFilter = ImoveisFinalidadeFilterBox.SelectedValue as ImovelFinalidade?;
        var publicacaoFilter = ImoveisPublicacaoFilterBox.SelectedValue as string ?? "todos";
        ImoveisGrid.ItemsSource = _imoveis
            .Where(x => ContainsSearch(query, x.Endereco, x.Bairro, x.Proprietario, x.TipoImovel, x.Finalidade, x.Status))
            .Where(x => statusFilter switch
            {
                "ativos" => !string.Equals(x.Status, "Inativo", StringComparison.OrdinalIgnoreCase),
                "todos" => true,
                _ => NormalizeSearch(x.Status).Contains(NormalizeSearch(statusFilter), StringComparison.OrdinalIgnoreCase)
            })
            .Where(x => !finalidadeFilter.HasValue || string.Equals(x.Finalidade, GetImovelFinalidadeLabel(finalidadeFilter.Value), StringComparison.OrdinalIgnoreCase))
            .Where(x => publicacaoFilter switch
            {
                "privado" => string.Equals(x.Publicacao, "Privado", StringComparison.OrdinalIgnoreCase),
                "site" => x.Publicacao.Contains("Site", StringComparison.OrdinalIgnoreCase),
                "app" => x.Publicacao.Contains("App", StringComparison.OrdinalIgnoreCase),
                "destaque" => x.Publicacao.Contains("destaque", StringComparison.OrdinalIgnoreCase),
                _ => true
            })
            .ToList();
    }

    private void NewImovelButton_Click(object sender, RoutedEventArgs e)
    {
        ImoveisGrid.SelectedItem = null;
        _selectedImovelId = null;
        _selectedImovelDetails = null;
        ClearImovelForm();
        SetImovelEditMode(true, isNew: true);
    }

    private async void SaveImovelButton_Click(object sender, RoutedEventArgs e)
    {
        ImovelErrorText.Text = string.Empty;

        try
        {
            var request = BuildImovelRequestFromForm();
            ImovelSummary saved;
            if (_selectedImovelId.HasValue)
            {
                saved = await _rentalManagementService.UpdateImovelAsync(new UpdateImovelRequest(_selectedImovelId.Value, request));
            }
            else
            {
                saved = await _rentalManagementService.CreateImovelAsync(request);
            }

            var savedImovelId = saved.Id;
            await SavePendingImovelMediaAsync(savedImovelId);
            ClearImovelForm();
            await LoadImoveisAsync();
            RestoreDataGridSelection(ImoveisGrid, savedImovelId);
        }
        catch (Exception ex)
        {
            ImovelErrorText.Text = GetImovelExceptionMessage(ex);
        }
    }

    private async void ImoveisGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRestoringTabState)
        {
            return;
        }

        if (TryGetItemId(ImoveisGrid.SelectedItem) is not Guid imovelId)
        {
            _selectedImovelId = null;
            _selectedImovelDetails = null;
            _pendingImovelMedia.Clear();
            _imovelImagens = [];
            RefreshImovelMediaGrid();
            _imovelVistorias = [];
            ImovelVistoriasGrid.ItemsSource = _imovelVistorias;
            SetImovelEditMode(true, isNew: true);
            return;
        }

        await LoadSelectedImovelAsync(imovelId);
        SaveActiveTabState();
    }

    private async Task LoadSelectedImovelAsync(Guid imovelId)
    {
        ImovelErrorText.Text = string.Empty;
        var details = await _rentalManagementService.GetImovelAsync(imovelId);
        if (details is null)
        {
            _selectedImovelId = null;
            _selectedImovelDetails = null;
            SetImovelEditMode(true, isNew: true);
            return;
        }

        _selectedImovelId = imovelId;
        _selectedImovelDetails = details;
        _pendingImovelMedia.Clear();
        SetImovelForm(details.Dados);
        ImovelFormTitleText.Text = details.Summary.Endereco;
        SetActiveImovelTabLabel(details.Summary.Endereco);
        await LoadImovelImagensAsync(imovelId);
        await LoadImovelVistoriasAsync(imovelId);
        SetImovelEditMode(false, isNew: false);
    }

    private void EditImovelButton_Click(object sender, RoutedEventArgs e) => SetImovelEditMode(true, isNew: false);

    private void CancelImovelEditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedImovelDetails is not null)
        {
            SetImovelForm(_selectedImovelDetails.Dados);
            ImovelFormTitleText.Text = _selectedImovelDetails.Summary.Endereco;
            SetActiveImovelTabLabel(_selectedImovelDetails.Summary.Endereco);
            SetImovelEditMode(false, isNew: false);
            return;
        }

        ClearImovelForm();
        SetImovelEditMode(true, isNew: true);
    }

    private async void DeactivateImovelButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedImovelId.HasValue || _selectedImovelDetails is null)
        {
            return;
        }

        var confirm = MessageBox.Show(
            this,
            "Remover este imóvel apenas altera o status para inativo. Deseja continuar?",
            "Remover imóvel",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var isCurrentlyInactive = _selectedImovelDetails.Dados.Status == ImovelStatus.Inativo;
            await _rentalManagementService.SetImovelActiveAsync(_selectedImovelId.Value, isCurrentlyInactive);
            var selectedId = _selectedImovelId.Value;
            await LoadImoveisAsync();
            RestoreDataGridSelection(ImoveisGrid, selectedId);
        }
        catch (Exception ex)
        {
            ImovelErrorText.Text = ex.Message;
        }
    }

    private async Task LoadImovelImagensAsync(Guid imovelId)
    {
        _imovelImagens = await _rentalManagementService.GetImovelImagensAsync(imovelId);
        RefreshImovelMediaGrid();
    }

    private async Task LoadImovelVistoriasAsync(Guid imovelId)
    {
        _imovelVistorias = await _rentalManagementService.GetVistoriasAsync(imovelId);
        ImovelVistoriasGrid.ItemsSource = _imovelVistorias;
    }

    private void BrowseImovelImagemButton_Click(object sender, RoutedEventArgs e)
    {
        ImovelImagemErrorText.Text = string.Empty;

        var dialog = new OpenFileDialog
        {
            Title = "Selecionar foto ou arquivo do imóvel",
            Filter = "Fotos e arquivos|*.png;*.jpg;*.jpeg;*.bmp;*.webp;*.pdf;*.txt|Imagens|*.png;*.jpg;*.jpeg;*.bmp;*.webp|Documentos|*.pdf;*.txt|Todos os arquivos|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        ImovelImagemArquivoBox.Text = dialog.FileName;
        if (string.IsNullOrWhiteSpace(ImovelImagemLegendaBox.Text))
        {
            ImovelImagemLegendaBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
        }
    }

    private async void BrowseImovelImagensBulkButton_Click(object sender, RoutedEventArgs e)
    {
        ImovelImagemErrorText.Text = string.Empty;
        var dialog = new OpenFileDialog
        {
            Title = "Selecionar fotos em lote",
            Filter = "Imagens|*.png;*.jpg;*.jpeg;*.bmp;*.webp|Todos os arquivos|*.*",
            CheckFileExists = true,
            Multiselect = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var category = GetSelectedImovelMediaCategory();
        var order = ParseNullableInt(ImovelImagemOrdemBox.Text) ?? _pendingImovelMedia.Count + _imovelImagens.Count;
        var markFirstAsCover = ImovelImagemCapaBox.IsChecked == true;

        if (_selectedImovelId.HasValue)
        {
            for (var index = 0; index < dialog.FileNames.Length; index++)
            {
                await CreateImovelMediaAsync(
                    _selectedImovelId.Value,
                    dialog.FileNames[index],
                    Path.GetFileNameWithoutExtension(dialog.FileNames[index]),
                    order + index,
                    category,
                    markFirstAsCover && index == 0,
                    ImovelImagemPublicaBox.IsChecked == true);
            }

            await LoadImovelImagensAsync(_selectedImovelId.Value);
            ClearImovelImagemForm();
            return;
        }

        for (var index = 0; index < dialog.FileNames.Length; index++)
        {
            _pendingImovelMedia.Add(new PendingImovelMedia(
                dialog.FileNames[index],
                Path.GetFileNameWithoutExtension(dialog.FileNames[index]),
                order + index,
                category,
                markFirstAsCover && index == 0,
                ImovelImagemPublicaBox.IsChecked == true));
        }

        RefreshImovelMediaGrid();
        ClearImovelImagemForm();
        ImovelImagemErrorText.Text = "Fotos adicionadas à fila. Elas serão salvas quando o imóvel for salvo.";
    }

    private async void SaveImovelImagemButton_Click(object sender, RoutedEventArgs e)
    {
        ImovelImagemErrorText.Text = string.Empty;
        if (string.IsNullOrWhiteSpace(ImovelImagemArquivoBox.Text))
        {
            ImovelImagemErrorText.Text = "Selecione o arquivo antes de adicionar mídia.";
            return;
        }

        try
        {
            var category = GetSelectedImovelMediaCategory();
            var order = ParseNullableInt(ImovelImagemOrdemBox.Text) ?? _pendingImovelMedia.Count + _imovelImagens.Count;

            if (_selectedImovelId.HasValue)
            {
                await CreateImovelMediaAsync(
                    _selectedImovelId.Value,
                    ImovelImagemArquivoBox.Text,
                    ImovelImagemLegendaBox.Text,
                    order,
                    category,
                    ImovelImagemCapaBox.IsChecked == true,
                    ImovelImagemPublicaBox.IsChecked == true);

                ClearImovelImagemForm();
                await LoadImovelImagensAsync(_selectedImovelId.Value);
            }
            else
            {
                _pendingImovelMedia.Add(new PendingImovelMedia(
                    ImovelImagemArquivoBox.Text,
                    ImovelImagemLegendaBox.Text,
                    order,
                    category,
                    ImovelImagemCapaBox.IsChecked == true,
                    ImovelImagemPublicaBox.IsChecked == true));

                RefreshImovelMediaGrid();
                ClearImovelImagemForm();
                ImovelImagemErrorText.Text = "Mídia adicionada à fila. Ela será salva quando o imóvel for salvo.";
            }
        }
        catch (Exception ex)
        {
            ImovelImagemErrorText.Text = ex.Message;
        }
    }

    private static string GetImovelExceptionMessage(Exception ex)
    {
        var baseException = ex.GetBaseException();
        return baseException is not null && !string.Equals(baseException.Message, ex.Message, StringComparison.Ordinal)
            ? $"{ex.Message} Detalhe: {baseException.Message}"
            : ex.Message;
    }

    private async Task SavePendingImovelMediaAsync(Guid imovelId)
    {
        foreach (var media in _pendingImovelMedia)
        {
            await CreateImovelMediaAsync(
                imovelId,
                media.StoragePath,
                media.Caption,
                media.DisplayOrder,
                media.MediaCategory,
                media.IsCover,
                media.IsPublic);
        }
    }

    private Task CreateImovelMediaAsync(
        Guid imovelId,
        string storagePath,
        string? caption,
        int displayOrder,
        ImovelMediaCategory category,
        bool isCover,
        bool isPublic) =>
        _rentalManagementService.CreateImovelImagemAsync(new CreateImovelImagemRequest(
            ImovelId: imovelId,
            FileName: Path.GetFileName(storagePath),
            StoragePath: storagePath,
            ContentType: GuessImovelMediaContentType(storagePath),
            DisplayOrder: displayOrder,
            Caption: caption,
            IsCover: isCover,
            IsPublic: isPublic,
            MediaCategory: category,
            Source: ImovelMediaSource.Windows));

    private void RefreshImovelMediaGrid()
    {
        var savedRows = _imovelImagens.Select(x => new ImovelMediaListItem(
            x.FileName,
            x.MediaCategory,
            x.Caption,
            x.IsCover,
            x.IsPublic,
            x.Source,
            x.Status,
            GetImagePreviewPath(x.StoragePath),
            GetFileKindLabel(x.FileName)));
        var pendingRows = _pendingImovelMedia.Select(x => new ImovelMediaListItem(
            Path.GetFileName(x.StoragePath),
            GetImovelMediaCategoryLabel(x.MediaCategory),
            x.Caption,
            x.IsCover,
            x.IsPublic,
            "Windows",
            "Pendente",
            GetImagePreviewPath(x.StoragePath),
            GetFileKindLabel(x.StoragePath)));

        ImovelImagensGrid.ItemsSource = savedRows.Concat(pendingRows).ToList();
    }

    private async void SaveImovelVistoriaButton_Click(object sender, RoutedEventArgs e)
    {
        ImovelVistoriaErrorText.Text = string.Empty;
        if (!_selectedImovelId.HasValue)
        {
            ImovelVistoriaErrorText.Text = "Selecione ou salve o imóvel antes de adicionar vistorias.";
            return;
        }

        try
        {
            var tipo = ImovelVistoriaTipoBox.SelectedValue is VistoriaTipo selectedTipo
                ? selectedTipo
                : VistoriaTipo.InicialProprietario;
            var status = ImovelVistoriaStatusBox.SelectedValue is VistoriaStatus selectedStatus
                ? selectedStatus
                : VistoriaStatus.Draft;

            await _rentalManagementService.CreateVistoriaAsync(new CreateVistoriaRequest(
                _selectedImovelId.Value,
                null,
                tipo,
                ToDateOnly(ImovelVistoriaDataBox.SelectedDate) ?? DateOnly.FromDateTime(DateTime.Today),
                ImovelVistoriaResponsavelBox.Text,
                status,
                ImovelVistoriaDescricaoBox.Text,
                ImovelVistoriaObservacoesBox.Text));

            ClearImovelVistoriaForm();
            await LoadImovelVistoriasAsync(_selectedImovelId.Value);
        }
        catch (Exception ex)
        {
            ImovelVistoriaErrorText.Text = ex.Message;
        }
    }

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
            Cidade: string.IsNullOrWhiteSpace(ImovelCidadeBox.Text) ? "Paranavaí" : ImovelCidadeBox.Text,
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
            ImovelCidadeBox.Text,
            ImovelEstadoBox.Text,
            ImovelCepBox.Text,
            ImovelTipoBox.Text,
            ImovelSaneparBox.Text,
            ImovelCopelBox.Text,
            ImovelIptuInscricaoBox.Text,
            ImovelIptuCadastroBox.Text,
            ImovelColetaLixoBox.Text,
            ImovelValorAluguelBox.Text,
            ImovelValorVendaBox.Text,
            ImovelValorCondominioBox.Text,
            ImovelValorIptuBox.Text,
            ImovelLatitudeBox.Text,
            ImovelLongitudeBox.Text,
            ImovelStatusBox.SelectedValue is ImovelStatus status ? status : ImovelStatus.Disponivel,
            ImovelQuartosBox.Text,
            ImovelSuitesBox.Text,
            ImovelBanheirosBox.Text,
            ImovelVagasBox.Text,
            ImovelAreaConstruidaBox.Text,
            ImovelAreaTerrenoBox.Text,
            ImovelMobiliadoBox.IsChecked,
            ImovelAceitaPetsBox.IsChecked,
            ImovelDescricaoBox.Text,
            ImovelDescricaoPublicaBox.Text,
            ImovelObservacoesBox.Text,
            ImovelPublicarSiteBox.IsChecked == true,
            ImovelPublicarAppBox.IsChecked == true,
            ImovelDestaqueBox.IsChecked == true,
            ImovelMostrarEnderecoCompletoBox.IsChecked == true,
            ImovelEnderecoPublicoModoBox.SelectedValue is ImovelEnderecoPublicoModo modo ? modo : ImovelEnderecoPublicoModo.BairroCidade,
            ImovelChavePosseBox.SelectedValue is ImovelChavePosse posse ? posse : ImovelChavePosse.NaoCadastrada,
            ImovelChaveCodigoBox.Text,
            ImovelChaveQuemTemBox.Text,
            ImovelChaveTelefoneBox.Text,
            ImovelChaveContatoNomeBox.Text,
            ImovelChaveContatoDocumentoBox.Text,
            ImovelChaveLocalBox.Text,
            ImovelChaveHorarioBox.Text,
            ImovelChaveAutorizacaoBox.IsChecked == true,
            ImovelChaveObservacoesBox.Text);

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

    private void SetActiveImovelTabLabel(string label)
    {
        if (_activeTab?.Page != ShellPage.Imoveis)
        {
            return;
        }

        _activeTab.SelectedImovelName = label;
        RenderTabs();
    }

    private void ClearImovelImagemForm()
    {
        ImovelImagemArquivoBox.Clear();
        ImovelImagemLegendaBox.Clear();
        ImovelImagemOrdemBox.Clear();
        ImovelImagemPublicaBox.IsChecked = false;
        ImovelImagemCapaBox.IsChecked = false;
        ImovelMediaCategoryBox.SelectedValue = ImovelMediaCategory.PropertyPhoto;
        ImovelImagemErrorText.Text = string.Empty;
    }

    private ImovelMediaCategory GetSelectedImovelMediaCategory() =>
        ImovelMediaCategoryBox.SelectedValue is ImovelMediaCategory selectedCategory
            ? selectedCategory
            : ImovelMediaCategory.PropertyPhoto;

    private static string GetImovelMediaCategoryLabel(ImovelMediaCategory category) =>
        category switch
        {
            ImovelMediaCategory.PropertyPhoto => "Foto pública do imóvel",
            ImovelMediaCategory.Document => "Documento",
            ImovelMediaCategory.InspectionPhoto => "Foto de vistoria",
            ImovelMediaCategory.MaintenancePhoto => "Foto de manutenção",
            ImovelMediaCategory.Other => "Foto privada",
            _ => category.ToString()
        };

    private static string? GetImagePreviewPath(string? path) =>
        !string.IsNullOrWhiteSpace(path)
        && Path.IsPathRooted(path)
        && File.Exists(path)
        && IsImageFile(path)
            ? path
            : null;

    private static string GetFileKindLabel(string fileName) =>
        IsImageFile(fileName) ? string.Empty : Path.GetExtension(fileName).Trim('.').ToUpperInvariant();

    private static bool IsImageFile(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".webp";

    private void UpdateImovelChaveFieldsVisibility()
    {
        var posse = ImovelChavePosseBox.SelectedValue is ImovelChavePosse selected
            ? selected
            : ImovelChavePosse.NaoCadastrada;

        var hasExternalHolder = posse is ImovelChavePosse.Proprietario
            or ImovelChavePosse.Locatario
            or ImovelChavePosse.Terceiro
            or ImovelChavePosse.Outro;
        var hasRealEstateKey = posse == ImovelChavePosse.Imobiliaria;

        ImovelChaveCodigoPanel.Visibility = hasRealEstateKey ? Visibility.Visible : Visibility.Collapsed;
        ImovelChaveAutorizacaoBox.Visibility = posse == ImovelChavePosse.NaoCadastrada ? Visibility.Collapsed : Visibility.Visible;
        ImovelChaveContatoPanel.Visibility = hasExternalHolder ? Visibility.Visible : Visibility.Collapsed;
        ImovelChaveRetiradaPanel.Visibility = hasExternalHolder ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ClearImovelVistoriaForm()
    {
        ImovelVistoriaTipoBox.SelectedValue = VistoriaTipo.InicialProprietario;
        ImovelVistoriaDataBox.SelectedDate = DateTime.Today;
        ImovelVistoriaResponsavelBox.Clear();
        ImovelVistoriaStatusBox.SelectedValue = VistoriaStatus.Draft;
        ImovelVistoriaDescricaoBox.Clear();
        ImovelVistoriaObservacoesBox.Clear();
        ImovelVistoriaErrorText.Text = string.Empty;
    }

    private Guid ResolveImovelProprietarioId()
    {
        if (ImovelProprietarioBox.SelectedValue is Guid selectedOwnerId)
        {
            return selectedOwnerId;
        }

        var typed = ImovelProprietarioBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(typed))
        {
            return Guid.Empty;
        }

        var owners = _pessoas.Where(x => x.Status == "Ativo");
        var normalizedTyped = NormalizeSearch(typed);
        var exact = owners.FirstOrDefault(owner =>
            string.Equals(NormalizeSearch(owner.Nome), normalizedTyped, StringComparison.OrdinalIgnoreCase));

        if (exact is not null)
        {
            ImovelProprietarioBox.SelectedValue = exact.Id;
            return exact.Id;
        }

        var matches = owners
            .Where(owner => ContainsSearch(typed, owner.Nome, owner.Documento, owner.Telefone))
            .Take(2)
            .ToList();

        if (matches.Count == 1)
        {
            ImovelProprietarioBox.SelectedValue = matches[0].Id;
            return matches[0].Id;
        }

        return Guid.Empty;
    }

    private static int? ParseNullableInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.GetCultureInfo("pt-BR"), out var parsed) ? parsed : null;

    private static string GuessImovelMediaContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

    private static string GetImovelFinalidadeLabel(ImovelFinalidade finalidade) =>
        finalidade switch
        {
            ImovelFinalidade.Locacao => "Locação",
            ImovelFinalidade.Venda => "Venda",
            ImovelFinalidade.Ambos => "Ambos",
            _ => finalidade.ToString()
        };

    private sealed record PendingImovelMedia(
        string StoragePath,
        string? Caption,
        int DisplayOrder,
        ImovelMediaCategory MediaCategory,
        bool IsCover,
        bool IsPublic);

    private sealed record ImovelMediaListItem(
        string FileName,
        string MediaCategory,
        string? Caption,
        bool IsCover,
        bool IsPublic,
        string Source,
        string Status,
        string? PreviewPath,
        string FileKind);

    private sealed record ImoveisPageState(
        string SearchText,
        Guid? SelectedImovelId,
        Guid? ProprietarioId,
        ImovelFinalidade Finalidade,
        string Rua,
        string Numero,
        string Complemento,
        string Bairro,
        string Cidade,
        string Estado,
        string Cep,
        string TipoImovel,
        string Sanepar,
        string Copel,
    string IptuInscricaoImobiliaria,
    string IptuCadastroImovel,
        string ColetaLixo,
        string ValorAluguel,
        string ValorVenda,
        string ValorCondominio,
        string ValorIptu,
        string Latitude,
        string Longitude,
        ImovelStatus Status,
        string Quartos,
        string Suites,
        string Banheiros,
        string Vagas,
        string AreaConstruida,
        string AreaTerreno,
        bool? Mobiliado,
        bool? AceitaPets,
        string Descricao,
        string DescricaoPublica,
        string Observacoes,
        bool PublicarSite,
        bool PublicarApp,
        bool Destaque,
        bool MostrarEnderecoCompleto,
        ImovelEnderecoPublicoModo ModoEnderecoPublico,
        ImovelChavePosse ChavePosse,
        string ChaveCodigo,
        string ChaveQuemTem,
        string ChaveTelefone,
        string ChaveContatoNome,
        string ChaveContatoDocumento,
        string ChaveLocal,
        string ChaveHorario,
        bool ChaveAutorizacao,
        string ChaveObservacoes) : IShellPageState
    {
        public static ImoveisPageState Default { get; } = new(
            SearchText: "",
            SelectedImovelId: null,
            ProprietarioId: null,
            Finalidade: ImovelFinalidade.Locacao,
            Rua: "",
            Numero: "",
            Complemento: "",
            Bairro: "",
            Cidade: "ParanavaÃ­",
            Estado: "PR",
            Cep: "",
            TipoImovel: "",
            Sanepar: "",
            Copel: "",
            IptuInscricaoImobiliaria: "",
            IptuCadastroImovel: "",
            ColetaLixo: "",
            ValorAluguel: "",
            ValorVenda: "",
            ValorCondominio: "",
            ValorIptu: "",
            Latitude: "",
            Longitude: "",
            Status: ImovelStatus.Disponivel,
            Quartos: "",
            Suites: "",
            Banheiros: "",
            Vagas: "",
            AreaConstruida: "",
            AreaTerreno: "",
            Mobiliado: false,
            AceitaPets: false,
            Descricao: "",
            DescricaoPublica: "",
            Observacoes: "",
            PublicarSite: false,
            PublicarApp: false,
            Destaque: false,
            MostrarEnderecoCompleto: false,
            ModoEnderecoPublico: ImovelEnderecoPublicoModo.BairroCidade,
            ChavePosse: ImovelChavePosse.NaoCadastrada,
            ChaveCodigo: "",
            ChaveQuemTem: "",
            ChaveTelefone: "",
            ChaveContatoNome: "",
            ChaveContatoDocumento: "",
            ChaveLocal: "",
            ChaveHorario: "",
            ChaveAutorizacao: false,
            ChaveObservacoes: ""
        );
    }
}


