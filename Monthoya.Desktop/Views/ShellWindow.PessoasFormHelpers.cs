using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
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
}
