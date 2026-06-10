using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{

    private async Task LoadPessoasAsync()
    {
        _pessoas = await _rentalManagementService.GetPessoasAsync();
        _ = LoadPessoasStreetSuggestionsInBackgroundAsync();
        ApplyPessoasFilter();

        ImovelProprietarioBox.ItemsSource = _pessoas.Where(x => x.Status == "Ativo").ToList();
    }


    private async Task LoadPessoasStreetSuggestionsInBackgroundAsync()
    {
        try
        {
            _streetSuggestions = await _rentalManagementService.GetStreetSuggestionsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Street suggestions failed: {ex.Message}");
        }
    }
    private async void ReloadPessoasButton_Click(object sender, RoutedEventArgs e) => await LoadPessoasAsync();

    private void PessoasSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyPessoasFilter();
        SaveActiveTabState();
    }

    private void PessoaStatusFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyPessoasFilter();
        SaveActiveTabState();
    }

    private async void PessoasGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isRestoringTabState)
        {
            return;
        }

        if (PessoasGrid.SelectedItem is not PessoaSummary pessoa)
        {
            SetPessoaDocumentoSelection(null);
            await LoadPessoaDocumentosAsync(null);
            SaveActiveTabState();
            return;
        }

        SetPessoaDocumentoSelection(pessoa);
        _selectedPessoaDetails = await _rentalManagementService.GetPessoaAsync(pessoa.Id);
        if (_selectedPessoaDetails is not null)
        {
            PopulatePessoaForm(_selectedPessoaDetails);
            SetPessoaEditMode(false, isNew: false);
        }

        await LoadPessoaDocumentosAsync(pessoa.Id);
        SaveActiveTabState();
    }

    private void PessoaTipoBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        TogglePessoaTypePanels();
        UpdatePessoaConditionalSections();
    }

    private void TogglePessoaTypePanels()
    {
        if (PessoaFisicaFieldsPanel is null || PessoaJuridicaFieldsPanel is null)
        {
            return;
        }

        var tipo = PessoaTipoBox.SelectedValue is TipoPessoa selectedTipo ? selectedTipo : TipoPessoa.Fisica;
        PessoaFisicaFieldsPanel.Visibility = tipo == TipoPessoa.Fisica ? Visibility.Visible : Visibility.Collapsed;
        PessoaJuridicaFieldsPanel.Visibility = tipo == TipoPessoa.Juridica ? Visibility.Visible : Visibility.Collapsed;
        PessoaDocumentoDonoBox.ItemsSource = tipo == TipoPessoa.Fisica ? PessoaDocumentoDonoFisicaOptions : PessoaDocumentoDonoJuridicaOptions;
        PessoaDocumentoTipoBox.ItemsSource = tipo == TipoPessoa.Fisica ? PessoaDocumentoTipoFisicaOptions : PessoaDocumentoTipoJuridicaOptions;
        if (PessoaDocumentoDonoBox.SelectedValue is null)
        {
            PessoaDocumentoDonoBox.SelectedIndex = 0;
        }

        if (PessoaDocumentoTipoBox.SelectedValue is null)
        {
            PessoaDocumentoTipoBox.SelectedIndex = 0;
        }
    }

    private async Task LoadPessoaDocumentosAsync(Guid? pessoaId)
    {
        PessoaDocumentosListErrorText.Text = string.Empty;

        if (!pessoaId.HasValue)
        {
            _pessoaDocumentos = [];
            PessoaDocumentosGrid.ItemsSource = _pessoaDocumentos;
            PessoaDocumentosTitleText.Text = "Documentos anexos";
            return;
        }

        _pessoaDocumentos = await _rentalManagementService.GetPessoaDocumentosAsync(pessoaId.Value);
        PessoaDocumentosGrid.ItemsSource = _pessoaDocumentos;
        PessoaDocumentosTitleText.Text = "Documentos anexos";
    }

    private void SetPessoaDocumentoSelection(PessoaSummary? pessoa)
    {
        PessoaDocumentosListErrorText.Text = string.Empty;
        PessoaDocumentoErrorText.Text = string.Empty;
        _selectedPessoaId = pessoa?.Id;
        PessoaDocumentoPessoaText.Text = pessoa is null ? "Nenhuma pessoa selecionada" : pessoa.Nome;
        PessoaProprietarioBox.IsChecked = pessoa?.IsProprietario == true;
        PessoaLocatarioBox.IsChecked = pessoa?.IsLocatario == true;
        PessoaFiadorBox.IsChecked = pessoa?.IsFiador == true;
        UpdatePessoaDocumentoEditorAvailability();
    }

    private CreatePessoaRequest BuildPessoaRequest()
    {
        var tipo = PessoaTipoBox.SelectedValue is TipoPessoa selectedTipo ? selectedTipo : TipoPessoa.Fisica;
        return new CreatePessoaRequest(
            TipoPessoa: tipo,
            NomeDisplay: PessoaNomeBox.Text,
            Telefone: PessoaTelefoneBox.Text,
            Email: PessoaEmailBox.Text,
            Documento: PessoaDocumentoBox.Text,
            Observacoes: PessoaObservacoesBox.Text,
            Endereco: null,
            Rua: tipo == TipoPessoa.Fisica ? PessoaRuaBox.Text : PessoaEmpresaRuaBox.Text,
            Numero: tipo == TipoPessoa.Fisica ? PessoaNumeroBox.Text : PessoaEmpresaNumeroBox.Text,
            Complemento: tipo == TipoPessoa.Fisica ? PessoaComplementoBox.Text : PessoaEmpresaComplementoBox.Text,
            Bairro: tipo == TipoPessoa.Fisica ? PessoaBairroBox.Text : PessoaEmpresaBairroBox.Text,
            Cidade: tipo == TipoPessoa.Fisica ? PessoaCidadeBox.Text : PessoaEmpresaCidadeBox.Text,
            Estado: tipo == TipoPessoa.Fisica ? PessoaEstadoBox.Text : PessoaEmpresaEstadoBox.Text,
            Cep: tipo == TipoPessoa.Fisica ? PessoaCepBox.Text : PessoaEmpresaCepBox.Text,
            EstadoCivil: PessoaEstadoCivilBox.Text,
            PossuiTrabalho: GetPessoaPossuiTrabalho(),
            PossuiPet: GetPessoaPossuiPet(),
            PetQual: GetPessoaPetQual(),
            Nacionalidade: PessoaNacionalidadeBox.Text,
            DataNascimento: ToDateOnly(PessoaDataNascimentoBox.SelectedDate),
            Rg: PessoaRgBox.Text,
            Profissao: PessoaProfissaoBox.Text,
            OndeTrabalha: PessoaOndeTrabalhaBox.Text,
            EnderecoTrabalho: PessoaEnderecoTrabalhoBox.Text,
            NomeEmpresaTrabalho: PessoaNomeEmpresaTrabalhoBox.Text,
            CnpjEmpresaTrabalho: _pessoaCnpjEmpresaTrabalhoBox?.Text,
            TelefoneEmpresaTrabalho: PessoaTelefoneEmpresaTrabalhoBox.Text,
            EmailEmpresaTrabalho: _pessoaEmailEmpresaTrabalhoBox?.Text,
            CargoTrabalho: _pessoaCargoTrabalhoBox?.Text,
            RendaTrabalho: ParseNullableDecimal(_pessoaRendaTrabalhoBox?.Text),
            TempoEmprego: _pessoaTempoEmpregoBox?.Text,
            TipoComprovanteRenda: _pessoaTipoComprovanteRendaBox?.Text,
            OutrasInformacoes: _pessoaOutrasInformacoesBox?.Text,
            TrabalhoOutrasInformacoes: _pessoaTrabalhoOutrasInformacoesBox?.Text,
            EmpresaRua: _pessoaTrabalhoRuaBox?.Text,
            EmpresaNumero: _pessoaTrabalhoNumeroBox?.Text,
            EmpresaComplemento: _pessoaTrabalhoComplementoBox?.Text,
            EmpresaBairro: _pessoaTrabalhoBairroBox?.Text,
            EmpresaCidade: _pessoaTrabalhoCidadeBox?.Text,
            EmpresaEstado: _pessoaTrabalhoEstadoBox?.Text,
            EmpresaCep: _pessoaTrabalhoCepBox?.Text,
            DadosBancarios: PessoaDadosBancariosBox.Text,
            BancoCodigo: GetBankCode(_pessoaBancoBox, _pessoaBancoCodigoBox),
            BancoNome: GetBankName(_pessoaBancoBox, _pessoaBancoNomeBox),
            AgenciaNumero: _pessoaAgenciaNumeroBox?.Text,
            AgenciaDigito: _pessoaAgenciaDigitoBox?.Text,
            ContaNumero: _pessoaContaNumeroBox?.Text,
            ContaDigito: _pessoaContaDigitoBox?.Text,
            ContaTipo: GetSelectedEnum<ContaBancariaTipo>(_pessoaContaTipoBox),
            TitularNome: _pessoaTitularNomeBox?.Text,
            TitularDocumento: _pessoaTitularDocumentoBox?.Text,
            PixTipo: GetSelectedEnum<PixChaveTipo>(_pessoaPixTipoBox),
            PixChave: _pessoaPixChaveBox?.Text,
            RepassePreferencial: GetSelectedEnum<MetodoRepassePreferencial>(_pessoaRepassePreferencialBox),
            ConjugeNome: PessoaConjugeNomeBox.Text,
            ConjugeRg: PessoaConjugeRgBox.Text,
            ConjugeCpf: PessoaConjugeCpfBox.Text,
            ConjugeEmail: _pessoaConjugeEmailBox?.Text,
            ConjugeDataNascimento: ToDateOnly(PessoaConjugeDataNascimentoBox.SelectedDate),
            ConjugeProfissao: PessoaConjugeProfissaoBox.Text,
            ConjugeNacionalidade: PessoaConjugeNacionalidadeBox.Text,
            ConjugeTelefone: PessoaConjugeTelefoneBox.Text,
            ConjugeDadosBancarios: _pessoaConjugeDadosBancariosBox?.Text,
            ConjugeObservacoes: null,
            ConjugeOutrasInformacoes: _pessoaConjugeOutrasInformacoesBox?.Text,
            ConjugePossuiTrabalho: GetPessoaConjugePossuiTrabalho(),
            ConjugeNomeEmpresaTrabalho: _pessoaConjugeNomeEmpresaTrabalhoBox?.Text,
            ConjugeCnpjEmpresaTrabalho: _pessoaConjugeCnpjEmpresaTrabalhoBox?.Text,
            ConjugeTelefoneEmpresaTrabalho: _pessoaConjugeTelefoneEmpresaTrabalhoBox?.Text,
            ConjugeEmailEmpresaTrabalho: _pessoaConjugeEmailEmpresaTrabalhoBox?.Text,
            ConjugeCargoTrabalho: _pessoaConjugeCargoTrabalhoBox?.Text,
            ConjugeRendaTrabalho: ParseNullableDecimal(_pessoaConjugeRendaTrabalhoBox?.Text),
            ConjugeTempoEmprego: _pessoaConjugeTempoEmpregoBox?.Text,
            ConjugeTipoComprovanteRenda: _pessoaConjugeTipoComprovanteRendaBox?.Text,
            ConjugeTrabalhoOutrasInformacoes: _pessoaConjugeTrabalhoOutrasInformacoesBox?.Text,
            ConjugeEmpresaRua: _pessoaConjugeEmpresaRuaBox?.Text,
            ConjugeEmpresaNumero: _pessoaConjugeEmpresaNumeroBox?.Text,
            ConjugeEmpresaComplemento: _pessoaConjugeEmpresaComplementoBox?.Text,
            ConjugeEmpresaBairro: _pessoaConjugeEmpresaBairroBox?.Text,
            ConjugeEmpresaCidade: _pessoaConjugeEmpresaCidadeBox?.Text,
            ConjugeEmpresaEstado: _pessoaConjugeEmpresaEstadoBox?.Text,
            ConjugeEmpresaCep: _pessoaConjugeEmpresaCepBox?.Text,
            NomeFantasia: _pessoaNomeFantasiaBox?.Text,
            Atividade: _pessoaAtividadeBox?.Text,
            ReceitaMensal: ParseNullableDecimal(_pessoaReceitaMensalBox?.Text),
            InscricaoEstadual: _pessoaInscricaoEstadualBox?.Text,
            InscricaoMunicipal: _pessoaInscricaoMunicipalBox?.Text,
            DataAbertura: ToDateOnly(_pessoaDataAberturaBox?.SelectedDate),
            ResponsavelNome: PessoaResponsavelNomeBox.Text,
            ResponsavelCargo: _pessoaResponsavelCargoBox?.Text,
            ResponsavelEndereco: null,
            ResponsavelRua: PessoaResponsavelRuaBox.Text,
            ResponsavelNumero: PessoaResponsavelNumeroBox.Text,
            ResponsavelComplemento: PessoaResponsavelComplementoBox.Text,
            ResponsavelBairro: PessoaResponsavelBairroBox.Text,
            ResponsavelCidade: PessoaResponsavelCidadeBox.Text,
            ResponsavelEstado: PessoaResponsavelEstadoBox.Text,
            ResponsavelCep: PessoaResponsavelCepBox.Text,
            ResponsavelEstadoCivil: PessoaResponsavelEstadoCivilBox.Text,
            ResponsavelNacionalidade: PessoaResponsavelNacionalidadeBox.Text,
            ResponsavelDataNascimento: ToDateOnly(PessoaResponsavelDataNascimentoBox.SelectedDate),
            ResponsavelTelefone: PessoaResponsavelTelefoneBox.Text,
            ResponsavelEmail: PessoaResponsavelEmailBox.Text,
            ResponsavelRg: PessoaResponsavelRgBox.Text,
            ResponsavelCpf: PessoaResponsavelCpfBox.Text,
            ResponsavelProfissao: PessoaResponsavelProfissaoBox.Text,
            ResponsavelOndeTrabalha: PessoaResponsavelOndeTrabalhaBox.Text,
            ResponsavelEnderecoTrabalho: PessoaResponsavelEnderecoTrabalhoBox.Text,
            ResponsavelNomeEmpresaTrabalho: PessoaResponsavelNomeEmpresaTrabalhoBox.Text,
            ResponsavelTelefoneEmpresaTrabalho: PessoaResponsavelTelefoneEmpresaTrabalhoBox.Text,
            ResponsavelDadosBancarios: PessoaResponsavelDadosBancariosBox.Text,
            ResponsavelBancoCodigo: GetBankCode(_pessoaResponsavelBancoBox, _pessoaResponsavelBancoCodigoBox),
            ResponsavelBancoNome: GetBankName(_pessoaResponsavelBancoBox, _pessoaResponsavelBancoNomeBox),
            ResponsavelAgenciaNumero: _pessoaResponsavelAgenciaNumeroBox?.Text,
            ResponsavelAgenciaDigito: _pessoaResponsavelAgenciaDigitoBox?.Text,
            ResponsavelContaNumero: _pessoaResponsavelContaNumeroBox?.Text,
            ResponsavelContaDigito: _pessoaResponsavelContaDigitoBox?.Text,
            ResponsavelContaTipo: GetSelectedEnum<ContaBancariaTipo>(_pessoaResponsavelContaTipoBox),
            ResponsavelTitularNome: _pessoaResponsavelTitularNomeBox?.Text,
            ResponsavelTitularDocumento: _pessoaResponsavelTitularDocumentoBox?.Text,
            ResponsavelPixTipo: GetSelectedEnum<PixChaveTipo>(_pessoaResponsavelPixTipoBox),
            ResponsavelPixChave: _pessoaResponsavelPixChaveBox?.Text,
            ResponsavelRepassePreferencial: GetSelectedEnum<MetodoRepassePreferencial>(_pessoaResponsavelRepassePreferencialBox),
            ResponsavelObservacoes: _pessoaResponsavelObservacoesBox?.Text);
    }

    private static T? GetSelectedEnum<T>(ComboBox? comboBox) where T : struct, Enum =>
        comboBox?.SelectedValue is T value ? value : null;

    private static string? GetBankCode(ComboBox? bankBox, TextBox? fallbackCodeBox)
    {
        var text = bankBox?.Text?.Trim();
        if (bankBox?.SelectedItem is BankOption bank && IsBankTextMatch(text, bank))
        {
            return bank.Code;
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, @"^\s*(\d{3})\b");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return string.IsNullOrWhiteSpace(fallbackCodeBox?.Text) ? null : fallbackCodeBox.Text.Trim();
    }

    private static string? GetBankName(ComboBox? bankBox, TextBox? fallbackNameBox)
    {
        var text = bankBox?.Text?.Trim();
        if (bankBox?.SelectedItem is BankOption bank && IsBankTextMatch(text, bank))
        {
            return bank.Name;
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, @"^\s*\d{3}\s*-\s*(.+)$");
            return match.Success ? match.Groups[1].Value.Trim() : text;
        }

        return string.IsNullOrWhiteSpace(fallbackNameBox?.Text) ? null : fallbackNameBox.Text.Trim();
    }

    private static bool IsBankTextMatch(string? text, BankOption bank) =>
        string.IsNullOrWhiteSpace(text)
        || text.Equals(bank.ToString(), StringComparison.OrdinalIgnoreCase)
        || text.Equals(bank.Name, StringComparison.OrdinalIgnoreCase)
        || text.Equals(bank.Code, StringComparison.OrdinalIgnoreCase)
        || text.Equals(bank.SearchText, StringComparison.OrdinalIgnoreCase);

    private bool? GetPessoaPossuiTrabalho()
    {
        var text = _pessoaWorkComboBox?.SelectedItem as string;
        if (string.Equals(text, "Possui trabalho", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(text, "Não possui trabalho", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return null;
    }

    private bool? GetPessoaPossuiPet()
    {
        var text = _pessoaPetComboBox?.SelectedItem as string;
        if (string.Equals(text, "Sim", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(text, "Não", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return null;
    }

    private string? GetPessoaPetQual() => GetPessoaPossuiPet() == true ? _pessoaPetQualBox?.Text : null;

    private bool? GetPessoaConjugePossuiTrabalho()
    {
        var text = _pessoaConjugeWorkComboBox?.SelectedItem as string;
        if (string.Equals(text, "Possui trabalho", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(text, "Não possui trabalho", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return null;
    }

    private bool ValidatePessoaForm()
    {
        if (!ValidateEmail(PessoaEmailBox.Text, "E-mail"))
        {
            return false;
        }

        if (!ValidateEmail(PessoaResponsavelEmailBox.Text, "E-mail do responsável"))
        {
            return false;
        }

        foreach (var (value, field) in new[]
        {
            (_pessoaEmailEmpresaTrabalhoBox?.Text, "E-mail da empresa"),
            (_pessoaConjugeEmailBox?.Text, "E-mail do cônjuge"),
            (_pessoaConjugeEmailEmpresaTrabalhoBox?.Text, "E-mail da empresa do cônjuge")
        })
        {
            if (!ValidateEmail(value ?? string.Empty, field))
            {
                return false;
            }
        }

        return TryApplyBrazilianDate(PessoaDataNascimentoBox)
            && TryApplyBrazilianDate(PessoaConjugeDataNascimentoBox)
            && TryApplyBrazilianDate(PessoaResponsavelDataNascimentoBox)
            && (_pessoaDataAberturaBox is null || TryApplyBrazilianDate(_pessoaDataAberturaBox));
    }

    private bool ValidateEmail(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (Regex.IsMatch(value.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.CultureInvariant))
        {
            return true;
        }

        PessoaErrorText.Text = $"{fieldName} não está no formato correto. Exemplo: cliente@email.com";
        return false;
    }

    private void ApplyPessoasFilter()
    {
        var query = PessoasSearchBox.Text;
        var statusFilter = PessoaStatusFilterBox.SelectedValue as string ?? "ativo";
        PessoasGrid.ItemsSource = _pessoas
            .Where(x => statusFilter switch
            {
                "inativo" => x.Status == "Inativo",
                "todos" => true,
                _ => x.Status == "Ativo"
            })
            .Where(x => ContainsSearch(query, x.Nome, x.Documento, x.Roles, x.Telefone, x.Email))
            .ToList();
    }

    private async void SavePessoaButton_Click(object sender, RoutedEventArgs e)
    {
        PessoaErrorText.Text = string.Empty;

        try
        {
            if (!ValidatePessoaForm())
            {
                return;
            }

            var request = BuildPessoaRequest();
            if (_selectedPessoaId.HasValue && _selectedPessoaDetails is not null)
            {
                await _rentalManagementService.UpdatePessoaAsync(new UpdatePessoaRequest(_selectedPessoaId.Value, request));
                SetPessoaEditMode(false, isNew: false);
            }
            else
            {
                await _rentalManagementService.CreatePessoaAsync(request);
                ClearPessoaForm();
                SetPessoaEditMode(true, isNew: true);
            }

            await LoadPessoasAsync();
        }
        catch (Exception ex)
        {
            PessoaErrorText.Text = ex.Message;
        }
    }

    private async void SavePessoaDocumentoButton_Click(object sender, RoutedEventArgs e)
    {
        PessoaDocumentoErrorText.Text = string.Empty;

        try
        {
            var pessoaId = _selectedPessoaId ?? Guid.Empty;
            var tipo = PessoaDocumentoTipoBox.SelectedValue as string ?? "outros";
            var documentoDe = PessoaDocumentoDonoBox.SelectedValue as string ?? "pessoa";

            await _rentalManagementService.CreatePessoaDocumentoAsync(new CreatePessoaDocumentoRequest(
                pessoaId,
                tipo,
                PessoaDocumentoNomeBox.Text,
                PessoaDocumentoArquivoBox.Text,
                null,
                ToDateOnly(PessoaDocumentoValidadeBox.SelectedDate),
                PessoaDocumentoObservacoesBox.Text,
                documentoDe));

            PessoaDocumentoNomeBox.Clear();
            PessoaDocumentoArquivoBox.Clear();
            PessoaDocumentoValidadeBox.SelectedDate = null;
            PessoaDocumentoObservacoesBox.Clear();
            await LoadPessoasAsync();
            await LoadPessoaDocumentosAsync(_selectedPessoaId);
        }
        catch (Exception ex)
        {
            PessoaDocumentoErrorText.Text = ex.Message;
        }
    }

    private void ClearPessoaForm()
    {
        PessoaNomeBox.Clear();
        PessoaDocumentoBox.Clear();
        PessoaTelefoneBox.Clear();
        PessoaEmailBox.Clear();
        PessoaObservacoesBox.Clear();
        PessoaProprietarioBox.IsChecked = false;
        PessoaLocatarioBox.IsChecked = false;
        PessoaFiadorBox.IsChecked = false;
        PessoaRuaBox.Clear();
        PessoaNumeroBox.Clear();
        PessoaComplementoBox.Clear();
        PessoaBairroBox.Clear();
        PessoaCidadeBox.Clear();
        PessoaEstadoBox.Clear();
        PessoaCepBox.Clear();
        PessoaRgBox.Clear();
        PessoaEstadoCivilBox.Clear();
        if (_pessoaWorkComboBox is not null)
        {
            _pessoaWorkComboBox.SelectedIndex = -1;
        }
        if (_pessoaPetComboBox is not null)
        {
            _pessoaPetComboBox.SelectedIndex = -1;
        }
        _pessoaPetQualBox?.Clear();
        PessoaNacionalidadeBox.Clear();
        PessoaDataNascimentoBox.SelectedDate = null;
        PessoaProfissaoBox.Clear();
        PessoaOndeTrabalhaBox.Clear();
        PessoaEnderecoTrabalhoBox.Clear();
        PessoaNomeEmpresaTrabalhoBox.Clear();
        PessoaTelefoneEmpresaTrabalhoBox.Clear();
        ClearDynamicTextBoxes(
            _pessoaCnpjEmpresaTrabalhoBox,
            _pessoaEmailEmpresaTrabalhoBox,
            _pessoaCargoTrabalhoBox,
            _pessoaRendaTrabalhoBox,
            _pessoaTempoEmpregoBox,
            _pessoaTipoComprovanteRendaBox,
            _pessoaOutrasInformacoesBox,
            _pessoaTrabalhoOutrasInformacoesBox,
            _pessoaTrabalhoRuaBox,
            _pessoaTrabalhoNumeroBox,
            _pessoaTrabalhoComplementoBox,
            _pessoaTrabalhoBairroBox,
            _pessoaTrabalhoEstadoBox,
            _pessoaTrabalhoCidadeBox,
            _pessoaTrabalhoCepBox,
            _pessoaAgenciaNumeroBox,
            _pessoaAgenciaDigitoBox,
            _pessoaContaNumeroBox,
            _pessoaContaDigitoBox,
            _pessoaTitularNomeBox,
            _pessoaTitularDocumentoBox,
            _pessoaPixChaveBox);
        ClearBankComboBox(_pessoaBancoBox, _pessoaBancoCodigoBox, _pessoaBancoNomeBox);
        ClearComboBoxes(_pessoaContaTipoBox, _pessoaPixTipoBox, _pessoaRepassePreferencialBox);
        PessoaDadosBancariosBox.Clear();
        PessoaConjugeNomeBox.Clear();
        PessoaConjugeRgBox.Clear();
        PessoaConjugeCpfBox.Clear();
        PessoaConjugeDataNascimentoBox.SelectedDate = null;
        PessoaConjugeProfissaoBox.Clear();
        PessoaConjugeNacionalidadeBox.Clear();
        PessoaConjugeTelefoneBox.Clear();
        if (_pessoaConjugeWorkComboBox is not null)
        {
            _pessoaConjugeWorkComboBox.SelectedIndex = -1;
        }
        ClearDynamicTextBoxes(
            _pessoaConjugeEmailBox,
            _pessoaConjugeDadosBancariosBox,
            _pessoaConjugeObservacoesBox,
            _pessoaConjugeOutrasInformacoesBox,
            _pessoaConjugeNomeEmpresaTrabalhoBox,
            _pessoaConjugeCnpjEmpresaTrabalhoBox,
            _pessoaConjugeTelefoneEmpresaTrabalhoBox,
            _pessoaConjugeEmailEmpresaTrabalhoBox,
            _pessoaConjugeCargoTrabalhoBox,
            _pessoaConjugeRendaTrabalhoBox,
            _pessoaConjugeTempoEmpregoBox,
            _pessoaConjugeTipoComprovanteRendaBox,
            _pessoaConjugeTrabalhoOutrasInformacoesBox,
            _pessoaConjugeEmpresaRuaBox,
            _pessoaConjugeEmpresaNumeroBox,
            _pessoaConjugeEmpresaComplementoBox,
            _pessoaConjugeEmpresaBairroBox,
            _pessoaConjugeEmpresaEstadoBox,
            _pessoaConjugeEmpresaCidadeBox,
            _pessoaConjugeEmpresaCepBox);
        PessoaEmpresaRuaBox.Clear();
        PessoaEmpresaNumeroBox.Clear();
        PessoaEmpresaComplementoBox.Clear();
        PessoaEmpresaBairroBox.Clear();
        PessoaEmpresaCidadeBox.Clear();
        PessoaEmpresaEstadoBox.Clear();
        PessoaEmpresaCepBox.Clear();
        ClearDynamicTextBoxes(
            _pessoaNomeFantasiaBox,
            _pessoaAtividadeBox,
            _pessoaReceitaMensalBox,
            _pessoaInscricaoEstadualBox,
            _pessoaInscricaoMunicipalBox,
            _pessoaResponsavelCargoBox,
            _pessoaResponsavelAgenciaNumeroBox,
            _pessoaResponsavelAgenciaDigitoBox,
            _pessoaResponsavelContaNumeroBox,
            _pessoaResponsavelContaDigitoBox,
            _pessoaResponsavelTitularNomeBox,
            _pessoaResponsavelTitularDocumentoBox,
            _pessoaResponsavelPixChaveBox,
            _pessoaResponsavelObservacoesBox);
        ClearBankComboBox(_pessoaResponsavelBancoBox, _pessoaResponsavelBancoCodigoBox, _pessoaResponsavelBancoNomeBox);
        ClearComboBoxes(_pessoaResponsavelContaTipoBox, _pessoaResponsavelPixTipoBox, _pessoaResponsavelRepassePreferencialBox);
        if (_pessoaDataAberturaBox is not null)
        {
            _pessoaDataAberturaBox.SelectedDate = null;
        }
        PessoaResponsavelNomeBox.Clear();
        PessoaResponsavelRuaBox.Clear();
        PessoaResponsavelNumeroBox.Clear();
        PessoaResponsavelComplementoBox.Clear();
        PessoaResponsavelBairroBox.Clear();
        PessoaResponsavelCidadeBox.Clear();
        PessoaResponsavelEstadoBox.Clear();
        PessoaResponsavelCepBox.Clear();
        PessoaResponsavelEstadoCivilBox.Clear();
        PessoaResponsavelNacionalidadeBox.Clear();
        PessoaResponsavelDataNascimentoBox.SelectedDate = null;
        PessoaResponsavelTelefoneBox.Clear();
        PessoaResponsavelEmailBox.Clear();
        PessoaResponsavelRgBox.Clear();
        PessoaResponsavelCpfBox.Clear();
        PessoaResponsavelProfissaoBox.Clear();
        PessoaResponsavelOndeTrabalhaBox.Clear();
        PessoaResponsavelEnderecoTrabalhoBox.Clear();
        PessoaResponsavelNomeEmpresaTrabalhoBox.Clear();
        PessoaResponsavelTelefoneEmpresaTrabalhoBox.Clear();
        PessoaResponsavelDadosBancariosBox.Clear();
        UpdatePessoaConditionalSections();
    }

    private static void ClearDynamicTextBoxes(params TextBox?[] textBoxes)
    {
        foreach (var textBox in textBoxes)
        {
            textBox?.Clear();
        }
    }

    private void PopulatePessoaForm(PessoaDetails details)
    {
        var dados = details.Dados;
        PessoaFormTitleText.Text = details.Summary.Nome;
        PessoaTipoBox.SelectedValue = dados.TipoPessoa;
        PessoaNomeBox.Text = dados.NomeDisplay;
        PessoaDocumentoBox.Text = dados.Documento ?? string.Empty;
        PessoaTelefoneBox.Text = dados.Telefone ?? string.Empty;
        PessoaEmailBox.Text = dados.Email ?? string.Empty;
        PessoaObservacoesBox.Text = dados.Observacoes ?? string.Empty;
        PessoaRuaBox.Text = dados.Rua ?? string.Empty;
        PessoaNumeroBox.Text = dados.Numero ?? string.Empty;
        PessoaComplementoBox.Text = dados.Complemento ?? string.Empty;
        PessoaBairroBox.Text = dados.Bairro ?? string.Empty;
        PessoaCidadeBox.Text = dados.Cidade ?? string.Empty;
        PessoaEstadoBox.Text = dados.Estado ?? string.Empty;
        PessoaCepBox.Text = dados.Cep ?? string.Empty;
        PessoaRgBox.Text = dados.Rg ?? string.Empty;
        PessoaEstadoCivilBox.Text = dados.EstadoCivil ?? string.Empty;
        if (_pessoaWorkComboBox is not null)
        {
            _pessoaWorkComboBox.SelectedItem = dados.PossuiTrabalho switch
            {
                true => "Possui trabalho",
                false => "Não possui trabalho",
                _ => null
            };
        }
        if (_pessoaPetComboBox is not null)
        {
            _pessoaPetComboBox.SelectedItem = dados.PossuiPet switch
            {
                true => "Sim",
                false => "Não",
                _ => null
            };
        }
        if (_pessoaPetQualBox is not null)
        {
            _pessoaPetQualBox.Text = dados.PetQual ?? string.Empty;
        }
        PessoaNacionalidadeBox.Text = dados.Nacionalidade ?? string.Empty;
        PessoaDataNascimentoBox.SelectedDate = ToDateTime(dados.DataNascimento);
        PessoaProfissaoBox.Text = dados.Profissao ?? string.Empty;
        PessoaOndeTrabalhaBox.Text = dados.OndeTrabalha ?? string.Empty;
        PessoaEnderecoTrabalhoBox.Text = dados.EnderecoTrabalho ?? string.Empty;
        PessoaNomeEmpresaTrabalhoBox.Text = dados.NomeEmpresaTrabalho ?? string.Empty;
        PessoaTelefoneEmpresaTrabalhoBox.Text = dados.TelefoneEmpresaTrabalho ?? string.Empty;
        SetText(_pessoaCnpjEmpresaTrabalhoBox, dados.CnpjEmpresaTrabalho);
        SetText(_pessoaEmailEmpresaTrabalhoBox, dados.EmailEmpresaTrabalho);
        SetText(_pessoaCargoTrabalhoBox, dados.CargoTrabalho);
        SetText(_pessoaRendaTrabalhoBox, FormatNullableDecimal(dados.RendaTrabalho));
        SetText(_pessoaTempoEmpregoBox, dados.TempoEmprego);
        SetText(_pessoaTipoComprovanteRendaBox, dados.TipoComprovanteRenda);
        SetText(_pessoaOutrasInformacoesBox, dados.OutrasInformacoes);
        SetText(_pessoaTrabalhoOutrasInformacoesBox, dados.TrabalhoOutrasInformacoes);
        SetText(_pessoaTrabalhoRuaBox, dados.EmpresaRua);
        SetText(_pessoaTrabalhoNumeroBox, dados.EmpresaNumero);
        SetText(_pessoaTrabalhoComplementoBox, dados.EmpresaComplemento);
        SetText(_pessoaTrabalhoBairroBox, dados.EmpresaBairro);
        SetText(_pessoaTrabalhoEstadoBox, dados.EmpresaEstado);
        SetText(_pessoaTrabalhoCidadeBox, dados.EmpresaCidade);
        SetText(_pessoaTrabalhoCepBox, dados.EmpresaCep);
        PessoaDadosBancariosBox.Text = dados.DadosBancarios ?? string.Empty;
        SetBankSelection(_pessoaBancoBox, _pessoaBancoCodigoBox, _pessoaBancoNomeBox, dados.BancoCodigo, dados.BancoNome);
        SetText(_pessoaAgenciaNumeroBox, dados.AgenciaNumero);
        SetText(_pessoaAgenciaDigitoBox, dados.AgenciaDigito);
        SetText(_pessoaContaNumeroBox, dados.ContaNumero);
        SetText(_pessoaContaDigitoBox, dados.ContaDigito);
        SetComboValue(_pessoaContaTipoBox, dados.ContaTipo);
        SetText(_pessoaTitularNomeBox, dados.TitularNome);
        SetText(_pessoaTitularDocumentoBox, dados.TitularDocumento);
        SetComboValue(_pessoaPixTipoBox, dados.PixTipo);
        SetText(_pessoaPixChaveBox, dados.PixChave);
        SetComboValue(_pessoaRepassePreferencialBox, dados.RepassePreferencial);
        PessoaConjugeNomeBox.Text = dados.ConjugeNome ?? string.Empty;
        PessoaConjugeRgBox.Text = dados.ConjugeRg ?? string.Empty;
        PessoaConjugeCpfBox.Text = dados.ConjugeCpf ?? string.Empty;
        SetText(_pessoaConjugeEmailBox, dados.ConjugeEmail);
        PessoaConjugeDataNascimentoBox.SelectedDate = ToDateTime(dados.ConjugeDataNascimento);
        PessoaConjugeProfissaoBox.Text = dados.ConjugeProfissao ?? string.Empty;
        PessoaConjugeNacionalidadeBox.Text = dados.ConjugeNacionalidade ?? string.Empty;
        PessoaConjugeTelefoneBox.Text = dados.ConjugeTelefone ?? string.Empty;
        SetText(_pessoaConjugeDadosBancariosBox, dados.ConjugeDadosBancarios);
        SetText(_pessoaConjugeObservacoesBox, dados.ConjugeObservacoes);
        SetText(_pessoaConjugeOutrasInformacoesBox, dados.ConjugeOutrasInformacoes ?? dados.ConjugeObservacoes);
        if (_pessoaConjugeWorkComboBox is not null)
        {
            _pessoaConjugeWorkComboBox.SelectedItem = dados.ConjugePossuiTrabalho switch
            {
                true => "Possui trabalho",
                false => "Não possui trabalho",
                _ => null
            };
        }
        SetText(_pessoaConjugeNomeEmpresaTrabalhoBox, dados.ConjugeNomeEmpresaTrabalho);
        SetText(_pessoaConjugeCnpjEmpresaTrabalhoBox, dados.ConjugeCnpjEmpresaTrabalho);
        SetText(_pessoaConjugeTelefoneEmpresaTrabalhoBox, dados.ConjugeTelefoneEmpresaTrabalho);
        SetText(_pessoaConjugeEmailEmpresaTrabalhoBox, dados.ConjugeEmailEmpresaTrabalho);
        SetText(_pessoaConjugeCargoTrabalhoBox, dados.ConjugeCargoTrabalho);
        SetText(_pessoaConjugeRendaTrabalhoBox, FormatNullableDecimal(dados.ConjugeRendaTrabalho));
        SetText(_pessoaConjugeTempoEmpregoBox, dados.ConjugeTempoEmprego);
        SetText(_pessoaConjugeTipoComprovanteRendaBox, dados.ConjugeTipoComprovanteRenda);
        SetText(_pessoaConjugeTrabalhoOutrasInformacoesBox, dados.ConjugeTrabalhoOutrasInformacoes);
        SetText(_pessoaConjugeEmpresaRuaBox, dados.ConjugeEmpresaRua);
        SetText(_pessoaConjugeEmpresaNumeroBox, dados.ConjugeEmpresaNumero);
        SetText(_pessoaConjugeEmpresaComplementoBox, dados.ConjugeEmpresaComplemento);
        SetText(_pessoaConjugeEmpresaBairroBox, dados.ConjugeEmpresaBairro);
        SetText(_pessoaConjugeEmpresaEstadoBox, dados.ConjugeEmpresaEstado);
        SetText(_pessoaConjugeEmpresaCidadeBox, dados.ConjugeEmpresaCidade);
        SetText(_pessoaConjugeEmpresaCepBox, dados.ConjugeEmpresaCep);
        PessoaEmpresaRuaBox.Text = dados.Rua ?? string.Empty;
        PessoaEmpresaNumeroBox.Text = dados.Numero ?? string.Empty;
        PessoaEmpresaComplementoBox.Text = dados.Complemento ?? string.Empty;
        PessoaEmpresaBairroBox.Text = dados.Bairro ?? string.Empty;
        PessoaEmpresaCidadeBox.Text = dados.Cidade ?? string.Empty;
        PessoaEmpresaEstadoBox.Text = dados.Estado ?? string.Empty;
        PessoaEmpresaCepBox.Text = dados.Cep ?? string.Empty;
        SetText(_pessoaNomeFantasiaBox, dados.NomeFantasia);
        SetText(_pessoaAtividadeBox, dados.Atividade);
        SetText(_pessoaReceitaMensalBox, FormatNullableDecimal(dados.ReceitaMensal));
        SetText(_pessoaInscricaoEstadualBox, dados.InscricaoEstadual);
        SetText(_pessoaInscricaoMunicipalBox, dados.InscricaoMunicipal);
        if (_pessoaDataAberturaBox is not null)
        {
            _pessoaDataAberturaBox.SelectedDate = ToDateTime(dados.DataAbertura);
        }
        PessoaResponsavelNomeBox.Text = dados.ResponsavelNome ?? string.Empty;
        SetText(_pessoaResponsavelCargoBox, dados.ResponsavelCargo);
        PessoaResponsavelRuaBox.Text = dados.ResponsavelRua ?? string.Empty;
        PessoaResponsavelNumeroBox.Text = dados.ResponsavelNumero ?? string.Empty;
        PessoaResponsavelComplementoBox.Text = dados.ResponsavelComplemento ?? string.Empty;
        PessoaResponsavelBairroBox.Text = dados.ResponsavelBairro ?? string.Empty;
        PessoaResponsavelCidadeBox.Text = dados.ResponsavelCidade ?? string.Empty;
        PessoaResponsavelEstadoBox.Text = dados.ResponsavelEstado ?? string.Empty;
        PessoaResponsavelCepBox.Text = dados.ResponsavelCep ?? string.Empty;
        PessoaResponsavelEstadoCivilBox.Text = dados.ResponsavelEstadoCivil ?? string.Empty;
        PessoaResponsavelNacionalidadeBox.Text = dados.ResponsavelNacionalidade ?? string.Empty;
        PessoaResponsavelDataNascimentoBox.SelectedDate = ToDateTime(dados.ResponsavelDataNascimento);
        PessoaResponsavelTelefoneBox.Text = dados.ResponsavelTelefone ?? string.Empty;
        PessoaResponsavelEmailBox.Text = dados.ResponsavelEmail ?? string.Empty;
        PessoaResponsavelRgBox.Text = dados.ResponsavelRg ?? string.Empty;
        PessoaResponsavelCpfBox.Text = dados.ResponsavelCpf ?? string.Empty;
        PessoaResponsavelProfissaoBox.Text = dados.ResponsavelProfissao ?? string.Empty;
        PessoaResponsavelOndeTrabalhaBox.Text = dados.ResponsavelOndeTrabalha ?? string.Empty;
        PessoaResponsavelEnderecoTrabalhoBox.Text = dados.ResponsavelEnderecoTrabalho ?? string.Empty;
        PessoaResponsavelNomeEmpresaTrabalhoBox.Text = dados.ResponsavelNomeEmpresaTrabalho ?? string.Empty;
        PessoaResponsavelTelefoneEmpresaTrabalhoBox.Text = dados.ResponsavelTelefoneEmpresaTrabalho ?? string.Empty;
        PessoaResponsavelDadosBancariosBox.Text = dados.ResponsavelDadosBancarios ?? string.Empty;
        SetBankSelection(_pessoaResponsavelBancoBox, _pessoaResponsavelBancoCodigoBox, _pessoaResponsavelBancoNomeBox, dados.ResponsavelBancoCodigo, dados.ResponsavelBancoNome);
        SetText(_pessoaResponsavelAgenciaNumeroBox, dados.ResponsavelAgenciaNumero);
        SetText(_pessoaResponsavelAgenciaDigitoBox, dados.ResponsavelAgenciaDigito);
        SetText(_pessoaResponsavelContaNumeroBox, dados.ResponsavelContaNumero);
        SetText(_pessoaResponsavelContaDigitoBox, dados.ResponsavelContaDigito);
        SetComboValue(_pessoaResponsavelContaTipoBox, dados.ResponsavelContaTipo);
        SetText(_pessoaResponsavelTitularNomeBox, dados.ResponsavelTitularNome);
        SetText(_pessoaResponsavelTitularDocumentoBox, dados.ResponsavelTitularDocumento);
        SetComboValue(_pessoaResponsavelPixTipoBox, dados.ResponsavelPixTipo);
        SetText(_pessoaResponsavelPixChaveBox, dados.ResponsavelPixChave);
        SetComboValue(_pessoaResponsavelRepassePreferencialBox, dados.ResponsavelRepassePreferencial);
        SetText(_pessoaResponsavelObservacoesBox, dados.ResponsavelObservacoes);
        PessoaErrorText.Text = string.Empty;
        TogglePessoaTypePanels();
        UpdatePessoaConditionalSections();
    }

    private static void SetText(TextBox? textBox, string? value)
    {
        if (textBox is not null)
        {
            textBox.Text = value ?? string.Empty;
        }
    }

    private static string? FormatNullableDecimal(decimal? value) =>
        value?.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));

    private void SetPessoaEditMode(bool isEditing, bool isNew)
    {
        _isPessoaEditing = isEditing;
        PessoaFormTitleText.Text = isNew ? "Nova pessoa" : PessoaFormTitleText.Text;
        SavePessoaButton.Visibility = isEditing ? Visibility.Visible : Visibility.Collapsed;
        CancelPessoaEditButton.Visibility = isEditing && !isNew ? Visibility.Visible : Visibility.Collapsed;
        PessoaEditButton.IsEnabled = !isEditing && _selectedPessoaId.HasValue;
        PessoaDeactivateButton.IsEnabled = !isEditing && _selectedPessoaId.HasValue;

        foreach (var textBox in GetPessoaTextBoxes())
        {
            textBox.IsReadOnly = !isEditing;
        }

        foreach (var datePicker in GetPessoaDatePickers())
        {
            datePicker.IsEnabled = isEditing;
        }

        PessoaTipoBox.IsEnabled = isEditing;
        if (_pessoaEstadoCivilComboBox is not null)
        {
            _pessoaEstadoCivilComboBox.IsEnabled = isEditing;
        }
        if (_pessoaWorkComboBox is not null)
        {
            _pessoaWorkComboBox.IsEnabled = isEditing;
        }
        if (_pessoaPetComboBox is not null)
        {
            _pessoaPetComboBox.IsEnabled = isEditing;
        }
        if (_pessoaPetQualBox is not null)
        {
            _pessoaPetQualBox.IsReadOnly = !isEditing;
        }
        if (_pessoaConjugeWorkComboBox is not null)
        {
            _pessoaConjugeWorkComboBox.IsEnabled = isEditing;
        }
        foreach (var comboBox in GetPessoaBankComboBoxes())
        {
            comboBox.IsEnabled = isEditing;
        }
        foreach (var button in GetPessoaBankActionButtons())
        {
            button.IsEnabled = isEditing;
        }

        UpdatePessoaDocumentoEditorAvailability();
    }

    private static void SetBankSelection(ComboBox? bankBox, TextBox? codeBox, TextBox? nameBox, string? code, string? name)
    {
        SetText(codeBox, code);
        SetText(nameBox, name);
        if (bankBox is null)
        {
            return;
        }

        var bank = BrazilianBankCatalog.FirstOrDefault(x =>
            (!string.IsNullOrWhiteSpace(code) && x.Code == code.Trim())
            || (!string.IsNullOrWhiteSpace(name) && x.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)));

        if (bank is not null)
        {
            bankBox.SelectedItem = bank;
            return;
        }

        bankBox.SelectedIndex = -1;
        bankBox.Text = string.Join(" - ", new[] { code, name }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static void ClearBankComboBox(ComboBox? bankBox, TextBox? codeBox, TextBox? nameBox)
    {
        if (bankBox is not null)
        {
            bankBox.SelectedIndex = -1;
            bankBox.Text = string.Empty;
        }

        codeBox?.Clear();
        nameBox?.Clear();
    }

    private static void SetComboValue<T>(ComboBox? comboBox, T? value) where T : struct, Enum
    {
        if (comboBox is not null)
        {
            comboBox.SelectedValue = value;
        }
    }

    private static void ClearComboBoxes(params ComboBox?[] comboBoxes)
    {
        foreach (var comboBox in comboBoxes)
        {
            if (comboBox is not null)
            {
                comboBox.SelectedIndex = -1;
            }
        }
    }

    private void UpdatePessoaDocumentoEditorAvailability()
    {
        var canEditDocuments = _isPessoaEditing;
        SavePessoaDocumentoButton.IsEnabled = canEditDocuments;
        SavePessoaDocumentoButton.ToolTip = canEditDocuments
            ? "Adiciona o arquivo selecionado aos documentos da pessoa."
            : "Clique em Editar para adicionar documentos.";

        PessoaDocumentoDonoBox.IsEnabled = canEditDocuments;
        PessoaDocumentoTipoBox.IsEnabled = canEditDocuments;
        PessoaDocumentoNomeBox.IsReadOnly = !canEditDocuments;
        PessoaDocumentoArquivoBox.IsReadOnly = true;
        PessoaDocumentoValidadeBox.IsEnabled = canEditDocuments;
        PessoaDocumentoObservacoesBox.IsReadOnly = !canEditDocuments;

        if (_selecionarPessoaDocumentoArquivoButton is not null)
        {
            _selecionarPessoaDocumentoArquivoButton.IsEnabled = canEditDocuments;
            _selecionarPessoaDocumentoArquivoButton.ToolTip = canEditDocuments
                ? "Escolha PDF, PNG, JPG, JPEG, BMP, TIF, TIFF ou TXT no computador."
                : "Clique em Editar para selecionar arquivos.";
        }

        if (_usarPessoaDocumentosInformacoesButton is not null)
        {
            _usarPessoaDocumentosInformacoesButton.IsEnabled = canEditDocuments && HasPessoaDocumentoItems();
        }
    }

    private IEnumerable<DatePicker> GetPessoaDatePickers()
    {
        yield return PessoaDataNascimentoBox;
        yield return PessoaConjugeDataNascimentoBox;
        yield return PessoaResponsavelDataNascimentoBox;
        if (_pessoaDataAberturaBox is not null)
        {
            yield return _pessoaDataAberturaBox;
        }
    }

    private IEnumerable<TextBox> GetPessoaTextBoxes()
    {
        yield return PessoaNomeBox;
        yield return PessoaDocumentoBox;
        yield return PessoaTelefoneBox;
        yield return PessoaEmailBox;
        yield return PessoaRuaBox;
        yield return PessoaNumeroBox;
        yield return PessoaComplementoBox;
        yield return PessoaBairroBox;
        yield return PessoaCidadeBox;
        yield return PessoaEstadoBox;
        yield return PessoaCepBox;
        yield return PessoaRgBox;
        yield return PessoaEstadoCivilBox;
        yield return PessoaNacionalidadeBox;
        yield return PessoaProfissaoBox;
        yield return PessoaOndeTrabalhaBox;
        yield return PessoaEnderecoTrabalhoBox;
        yield return PessoaNomeEmpresaTrabalhoBox;
        yield return PessoaTelefoneEmpresaTrabalhoBox;
        yield return PessoaDadosBancariosBox;
        yield return PessoaConjugeNomeBox;
        yield return PessoaConjugeRgBox;
        yield return PessoaConjugeCpfBox;
        yield return PessoaConjugeProfissaoBox;
        yield return PessoaConjugeNacionalidadeBox;
        yield return PessoaConjugeTelefoneBox;
        yield return PessoaEmpresaRuaBox;
        yield return PessoaEmpresaNumeroBox;
        yield return PessoaEmpresaComplementoBox;
        yield return PessoaEmpresaBairroBox;
        yield return PessoaEmpresaCidadeBox;
        yield return PessoaEmpresaEstadoBox;
        yield return PessoaEmpresaCepBox;
        yield return PessoaResponsavelNomeBox;
        yield return PessoaResponsavelRuaBox;
        yield return PessoaResponsavelNumeroBox;
        yield return PessoaResponsavelComplementoBox;
        yield return PessoaResponsavelBairroBox;
        yield return PessoaResponsavelCidadeBox;
        yield return PessoaResponsavelEstadoBox;
        yield return PessoaResponsavelCepBox;
        yield return PessoaResponsavelEstadoCivilBox;
        yield return PessoaResponsavelNacionalidadeBox;
        yield return PessoaResponsavelTelefoneBox;
        yield return PessoaResponsavelEmailBox;
        yield return PessoaResponsavelRgBox;
        yield return PessoaResponsavelCpfBox;
        yield return PessoaResponsavelProfissaoBox;
        yield return PessoaResponsavelOndeTrabalhaBox;
        yield return PessoaResponsavelEnderecoTrabalhoBox;
        yield return PessoaResponsavelNomeEmpresaTrabalhoBox;
        yield return PessoaResponsavelTelefoneEmpresaTrabalhoBox;
        yield return PessoaResponsavelDadosBancariosBox;
        foreach (var textBox in GetDynamicPessoaTextBoxes())
        {
            yield return textBox;
        }
        yield return PessoaObservacoesBox;
    }

    private IEnumerable<TextBox> GetDynamicPessoaTextBoxes()
    {
        foreach (var textBox in new[]
        {
            _pessoaPetQualBox,
            _pessoaCnpjEmpresaTrabalhoBox,
            _pessoaEmailEmpresaTrabalhoBox,
            _pessoaCargoTrabalhoBox,
            _pessoaRendaTrabalhoBox,
            _pessoaTempoEmpregoBox,
            _pessoaTipoComprovanteRendaBox,
            _pessoaOutrasInformacoesBox,
            _pessoaTrabalhoOutrasInformacoesBox,
            _pessoaTrabalhoRuaBox,
            _pessoaTrabalhoNumeroBox,
            _pessoaTrabalhoComplementoBox,
            _pessoaTrabalhoBairroBox,
            _pessoaTrabalhoEstadoBox,
            _pessoaTrabalhoCidadeBox,
            _pessoaTrabalhoCepBox,
            _pessoaAgenciaNumeroBox,
            _pessoaAgenciaDigitoBox,
            _pessoaContaNumeroBox,
            _pessoaContaDigitoBox,
            _pessoaTitularNomeBox,
            _pessoaTitularDocumentoBox,
            _pessoaPixChaveBox,
            _pessoaConjugeEmailBox,
            _pessoaConjugeDadosBancariosBox,
            _pessoaConjugeObservacoesBox,
            _pessoaConjugeOutrasInformacoesBox,
            _pessoaConjugeNomeEmpresaTrabalhoBox,
            _pessoaConjugeCnpjEmpresaTrabalhoBox,
            _pessoaConjugeTelefoneEmpresaTrabalhoBox,
            _pessoaConjugeEmailEmpresaTrabalhoBox,
            _pessoaConjugeCargoTrabalhoBox,
            _pessoaConjugeRendaTrabalhoBox,
            _pessoaConjugeTempoEmpregoBox,
            _pessoaConjugeTipoComprovanteRendaBox,
            _pessoaConjugeTrabalhoOutrasInformacoesBox,
            _pessoaConjugeEmpresaRuaBox,
            _pessoaConjugeEmpresaNumeroBox,
            _pessoaConjugeEmpresaComplementoBox,
            _pessoaConjugeEmpresaBairroBox,
            _pessoaConjugeEmpresaEstadoBox,
            _pessoaConjugeEmpresaCidadeBox,
            _pessoaConjugeEmpresaCepBox,
            _pessoaNomeFantasiaBox,
            _pessoaAtividadeBox,
            _pessoaReceitaMensalBox,
            _pessoaInscricaoEstadualBox,
            _pessoaInscricaoMunicipalBox,
            _pessoaResponsavelCargoBox,
            _pessoaResponsavelAgenciaNumeroBox,
            _pessoaResponsavelAgenciaDigitoBox,
            _pessoaResponsavelContaNumeroBox,
            _pessoaResponsavelContaDigitoBox,
            _pessoaResponsavelTitularNomeBox,
            _pessoaResponsavelTitularDocumentoBox,
            _pessoaResponsavelPixChaveBox,
            _pessoaResponsavelObservacoesBox
        })
        {
            if (textBox is not null)
            {
                yield return textBox;
            }
        }
    }

    private void NewPessoaButton_Click(object sender, RoutedEventArgs e)
    {
        InvalidatePessoaSelectionLoads();
        PessoasGrid.SelectedItem = null;
        _selectedPessoaId = null;
        _selectedPessoaDetails = null;
        _pendingPessoaDocumentos.Clear();
        SetPessoaDocumentoSelection(null);
        ClearPessoaDocumentoInputs();
        ClearPessoaForm();
        _pessoaDocumentos = [];
        PessoaDocumentosGrid.ItemsSource = _pessoaDocumentos;
        PessoaDocumentosTitleText.Text = "Documentos anexos";
        SetPessoaEditMode(true, isNew: true);
        SaveActiveTabState();
    }

    private IEnumerable<ComboBox> GetPessoaBankComboBoxes()
    {
        foreach (var comboBox in new[]
        {
            _pessoaBancoBox,
            _pessoaContaTipoBox,
            _pessoaPixTipoBox,
            _pessoaRepassePreferencialBox,
            _pessoaResponsavelBancoBox,
            _pessoaResponsavelContaTipoBox,
            _pessoaResponsavelPixTipoBox,
            _pessoaResponsavelRepassePreferencialBox
        })
        {
            if (comboBox is not null)
            {
                yield return comboBox;
            }
        }
    }

    private IEnumerable<Button> GetPessoaBankActionButtons()
    {
        foreach (var button in new[]
        {
            _pessoaUsarDadosPessoaBancoButton,
            _pessoaUsarPixButton,
            _pessoaUsarDadosResponsavelBancoButton,
            _pessoaResponsavelUsarPixButton
        })
        {
            if (button is not null)
            {
                yield return button;
            }
        }
    }

    private void EditPessoaButton_Click(object sender, RoutedEventArgs e)
    {
        SetPessoaEditMode(true, isNew: false);
        SaveActiveTabState();
    }

    private void CancelPessoaEditButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPessoaDetails is not null)
        {
            PopulatePessoaForm(_selectedPessoaDetails);
            SetPessoaEditMode(false, isNew: false);
            SaveActiveTabState();
        }
    }

    private async void DeactivatePessoaButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedPessoaId.HasValue)
        {
            return;
        }

        var confirm = MessageBox.Show(
            "Remover esta pessoa apenas altera o status para inativo. Deseja continuar?",
            "Confirmar remoção",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        var password = PromptPassword("Digite sua senha para confirmar a remoção.");
        if (password is null)
        {
            return;
        }

        if (!await _userService.VerifyPasswordAsync(_currentUser.Id, password))
        {
            PessoaErrorText.Text = "Senha incorreta. A pessoa não foi removida.";
            return;
        }

        await _rentalManagementService.SetPessoaActiveAsync(_selectedPessoaId.Value, false);
        _selectedPessoaId = null;
        _selectedPessoaDetails = null;
        ClearPessoaForm();
        SetPessoaDocumentoSelection(null);
        SetPessoaEditMode(true, isNew: true);
        SaveActiveTabState();
        await LoadPessoasAsync();
    }

    private PessoasPageState CapturePessoasPageState() =>
        new()
        {
            SearchText = PessoasSearchBox.Text,
            StatusFilter = PessoaStatusFilterBox.SelectedValue as string ?? "ativo",
            SelectedPessoaId = _selectedPessoaId,
            IsEditing = _isPessoaEditing,
            IsNew = _selectedPessoaId is null,
            DocumentoTipo = PessoaDocumentoTipoBox.SelectedValue as string ?? "cpf",
            DocumentoDono = PessoaDocumentoDonoBox.SelectedValue as string ?? "",
            DocumentoNome = PessoaDocumentoNomeBox.Text,
            DocumentoArquivo = PessoaDocumentoArquivoBox.Text,
            DocumentoValidade = ToDateOnly(PessoaDocumentoValidadeBox.SelectedDate),
            DocumentoObservacoes = PessoaDocumentoObservacoesBox.Text
        };

    private async Task RestorePessoasPageStateAsync(PessoasPageState state)
    {
        InvalidatePessoaSelectionLoads();
        PessoasSearchBox.Text = state.SearchText;
        PessoaStatusFilterBox.SelectedValue = state.StatusFilter;
        ApplyPessoasFilter();

        var selected = state.SelectedPessoaId.HasValue
            ? _pessoas.SingleOrDefault(x => x.Id == state.SelectedPessoaId.Value)
            : null;
        PessoasGrid.SelectedItem = selected;

        if (selected is null)
        {
            _selectedPessoaId = null;
            _selectedPessoaDetails = null;
            _pendingPessoaDocumentos.Clear();
            SetPessoaDocumentoSelection(null);
            await LoadPessoaDocumentosAsync(null);
            ClearPessoaForm();
            ClearPessoaDocumentoInputs();
            PessoaTipoBox.SelectedValue = TipoPessoa.Fisica;
            SetPessoaEditMode(true, isNew: true);
        }
        else
        {
            SetPessoaDocumentoSelection(selected);
            _selectedPessoaDetails = await _rentalManagementService.GetPessoaAsync(selected.Id);
            if (_selectedPessoaDetails is not null)
            {
                PopulatePessoaForm(_selectedPessoaDetails);
            }

            SetPessoaEditMode(state.IsEditing, isNew: false);
            await LoadPessoaDocumentosAsync(selected.Id);
        }

        PessoaDocumentoTipoBox.SelectedValue = state.DocumentoTipo;
        PessoaDocumentoDonoBox.SelectedValue = state.DocumentoDono;
        PessoaDocumentoNomeBox.Text = state.DocumentoNome;
        PessoaDocumentoArquivoBox.Text = state.DocumentoArquivo;
        PessoaDocumentoValidadeBox.SelectedDate = ToDateTime(state.DocumentoValidade);
        PessoaDocumentoObservacoesBox.Text = state.DocumentoObservacoes;
        UpdatePeopleTopRowSpacingAndRolesVisibility();
        UpdatePessoaDocumentoEditorAvailability();
    }

    private static string? PromptPassword(string message)
    {
        var window = new Window
        {
            Title = "Confirmação",
            Width = 360,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize
        };
        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 10) });
        var passwordBox = new PasswordBox { Margin = new Thickness(0, 0, 0, 14) };
        panel.Children.Add(passwordBox);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var ok = new Button { Content = "Confirmar", Width = 92, Margin = new Thickness(0, 0, 8, 0), IsDefault = true };
        var cancel = new Button { Content = "Cancelar", Width = 82, IsCancel = true };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);
        panel.Children.Add(buttons);
        ok.Click += (_, _) => window.DialogResult = true;
        window.Content = panel;
        return window.ShowDialog() == true ? passwordBox.Password : null;
    }
}




