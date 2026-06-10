using System;
using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
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
}

