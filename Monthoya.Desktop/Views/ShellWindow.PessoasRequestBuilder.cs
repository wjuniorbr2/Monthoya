using System;
using System.Windows.Controls;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
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
}


