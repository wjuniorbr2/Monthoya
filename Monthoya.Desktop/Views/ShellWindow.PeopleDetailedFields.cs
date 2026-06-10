using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private void AddMissingPessoaDetailedFields()
    {
        if (PessoaFisicaFieldsPanel.Children.OfType<StackPanel>().Any(panel => panel.Tag as string == "PessoaDetailedFields"))
        {
            return;
        }

        CreateDynamicPessoaControls();
        AddPessoaFisicaDetailedFields();
        AddPessoaJuridicaDetailedFields();
        ConfigureDynamicPessoaInputBehavior();
        ConfigurePessoaBankInputBehavior();
        _pessoaEstadoCivilComboBox!.SelectionChanged += (_, _) => UpdatePessoaConditionalSections();
        _pessoaWorkComboBox!.SelectionChanged += (_, _) => UpdatePessoaConditionalSections();
        _pessoaConjugeWorkComboBox!.SelectionChanged += (_, _) => UpdatePessoaConditionalSections();
    }

    private void CreateDynamicPessoaControls()
    {
        _pessoaCnpjEmpresaTrabalhoBox ??= NewTextBox(190);
        _pessoaEmailEmpresaTrabalhoBox ??= NewTextBox(260);
        _pessoaCargoTrabalhoBox ??= NewTextBox(160);
        _pessoaRendaTrabalhoBox ??= NewTextBox(120);
        _pessoaTempoEmpregoBox ??= NewTextBox(140);
        _pessoaTipoComprovanteRendaBox ??= NewTextBox(210);
        _pessoaOutrasInformacoesBox ??= NewMultilineBox(360, 64);
        _pessoaTrabalhoOutrasInformacoesBox ??= NewMultilineBox(360, 64);
        _pessoaTrabalhoRuaBox ??= NewTextBox(300);
        _pessoaTrabalhoNumeroBox ??= NewTextBox(90);
        _pessoaTrabalhoComplementoBox ??= NewTextBox(220);
        _pessoaTrabalhoBairroBox ??= NewTextBox(180);
        _pessoaTrabalhoEstadoBox ??= NewTextBox(80);
        _pessoaTrabalhoCidadeBox ??= NewTextBox(180);
        _pessoaTrabalhoCepBox ??= NewTextBox(120);
        CreatePessoaBankControls();

        _pessoaConjugeEmailBox ??= NewTextBox(260);
        _pessoaConjugeDadosBancariosBox ??= NewMultilineBox(64);
        _pessoaConjugeObservacoesBox ??= NewMultilineBox(64);
        _pessoaConjugeOutrasInformacoesBox ??= NewMultilineBox(360, 64);
        _pessoaConjugeWorkComboBox ??= new ComboBox
        {
            Width = 180,
            Margin = new Thickness(0, 6, 0, 0),
            ItemsSource = new[] { "Não possui trabalho", "Possui trabalho" },
            SelectedIndex = -1
        };
        _pessoaConjugeNomeEmpresaTrabalhoBox ??= NewTextBox(260);
        _pessoaConjugeCnpjEmpresaTrabalhoBox ??= NewTextBox(190);
        _pessoaConjugeTelefoneEmpresaTrabalhoBox ??= NewTextBox(160);
        _pessoaConjugeEmailEmpresaTrabalhoBox ??= NewTextBox(260);
        _pessoaConjugeCargoTrabalhoBox ??= NewTextBox(160);
        _pessoaConjugeRendaTrabalhoBox ??= NewTextBox(120);
        _pessoaConjugeTempoEmpregoBox ??= NewTextBox(140);
        _pessoaConjugeTipoComprovanteRendaBox ??= NewTextBox(210);
        _pessoaConjugeTrabalhoOutrasInformacoesBox ??= NewMultilineBox(360, 64);
        _pessoaConjugeEmpresaRuaBox ??= NewTextBox(300);
        _pessoaConjugeEmpresaNumeroBox ??= NewTextBox(90);
        _pessoaConjugeEmpresaComplementoBox ??= NewTextBox(220);
        _pessoaConjugeEmpresaBairroBox ??= NewTextBox(180);
        _pessoaConjugeEmpresaEstadoBox ??= NewTextBox(80);
        _pessoaConjugeEmpresaCidadeBox ??= NewTextBox(180);
        _pessoaConjugeEmpresaCepBox ??= NewTextBox(120);

        _pessoaNomeFantasiaBox ??= NewTextBox(260);
        _pessoaAtividadeBox ??= NewTextBox(220);
        _pessoaReceitaMensalBox ??= NewTextBox(140);
        _pessoaInscricaoEstadualBox ??= NewTextBox(160);
        _pessoaInscricaoMunicipalBox ??= NewTextBox(160);
        _pessoaDataAberturaBox ??= NewDatePicker();
        _pessoaResponsavelCargoBox ??= NewTextBox(160);
        CreatePessoaResponsavelBankControls();
        _pessoaResponsavelObservacoesBox ??= NewMultilineBox(64);
    }

    private void CreatePessoaBankControls()
    {
        _pessoaBancoBox ??= NewBankComboBox();
        _pessoaBancoCodigoBox ??= NewTextBox(90);
        _pessoaBancoNomeBox ??= NewTextBox(180);
        _pessoaAgenciaNumeroBox ??= NewTextBox(100);
        _pessoaAgenciaDigitoBox ??= NewTextBox(60);
        _pessoaContaNumeroBox ??= NewTextBox(130);
        _pessoaContaDigitoBox ??= NewTextBox(60);
        _pessoaContaTipoBox ??= NewContaTipoComboBox();
        _pessoaTitularNomeBox ??= NewTextBox(220);
        _pessoaTitularDocumentoBox ??= NewTextBox(170);
        _pessoaPixTipoBox ??= NewPixTipoComboBox();
        _pessoaPixChaveBox ??= NewTextBox(220);
        _pessoaRepassePreferencialBox ??= NewRepassePreferencialComboBox();
        _pessoaUsarDadosPessoaBancoButton ??= NewSmallBankActionButton("Usar dados da pessoa", (_, _) => FillPessoaBankHolderFromPessoa());
        _pessoaUsarPixButton ??= NewSmallBankActionButton("Usar como PIX", (_, _) => FillPessoaPixFromSelectedType());
        if (!_pessoaBankSelectionConfigured)
        {
            _pessoaBankSelectionConfigured = true;
            AttachBankSelection(_pessoaBancoBox, _pessoaBancoCodigoBox, _pessoaBancoNomeBox);
        }
    }

    private void CreatePessoaResponsavelBankControls()
    {
        _pessoaResponsavelBancoBox ??= NewBankComboBox();
        _pessoaResponsavelBancoCodigoBox ??= NewTextBox(90);
        _pessoaResponsavelBancoNomeBox ??= NewTextBox(180);
        _pessoaResponsavelAgenciaNumeroBox ??= NewTextBox(100);
        _pessoaResponsavelAgenciaDigitoBox ??= NewTextBox(60);
        _pessoaResponsavelContaNumeroBox ??= NewTextBox(130);
        _pessoaResponsavelContaDigitoBox ??= NewTextBox(60);
        _pessoaResponsavelContaTipoBox ??= NewContaTipoComboBox();
        _pessoaResponsavelTitularNomeBox ??= NewTextBox(220);
        _pessoaResponsavelTitularDocumentoBox ??= NewTextBox(170);
        _pessoaResponsavelPixTipoBox ??= NewPixTipoComboBox();
        _pessoaResponsavelPixChaveBox ??= NewTextBox(220);
        _pessoaResponsavelRepassePreferencialBox ??= NewRepassePreferencialComboBox();
        _pessoaUsarDadosResponsavelBancoButton ??= NewSmallBankActionButton("Usar dados do responsável", (_, _) => FillResponsavelBankHolderFromResponsavel());
        _pessoaResponsavelUsarPixButton ??= NewSmallBankActionButton("Usar como PIX", (_, _) => FillResponsavelPixFromSelectedType());
        if (!_pessoaResponsavelBankSelectionConfigured)
        {
            _pessoaResponsavelBankSelectionConfigured = true;
            AttachBankSelection(_pessoaResponsavelBancoBox, _pessoaResponsavelBancoCodigoBox, _pessoaResponsavelBancoNomeBox);
        }
    }

    private void AddPessoaFisicaDetailedFields()
    {
        var personalRow = new WrapPanel { Tag = "PessoaDetailedFields", Margin = new Thickness(0, 0, 0, 12) };
        MoveFieldToWrapPanel(PessoaFisicaFieldsPanel, personalRow, "Data de nascimento", PessoaDataNascimentoBox, 140);
        MoveFieldToWrapPanel(PessoaFisicaFieldsPanel, personalRow, "Profissão", PessoaProfissaoBox, 170);
        MoveFieldToWrapPanel(PessoaFisicaFieldsPanel, personalRow, "Nacionalidade", PessoaNacionalidadeBox, 150);
        personalRow.Children.Add(FieldStack("Outras informações", _pessoaOutrasInformacoesBox!, 360));
        PessoaFisicaFieldsPanel.Children.Insert(0, personalRow);
        PessoaFisicaFieldsPanel.Children.Insert(1, CreateBankSection(
            PessoaDadosBancariosBox,
            _pessoaBancoBox!, _pessoaAgenciaNumeroBox!, _pessoaAgenciaDigitoBox!,
            _pessoaContaNumeroBox!, _pessoaContaDigitoBox!, _pessoaContaTipoBox!, _pessoaTitularNomeBox!,
            _pessoaTitularDocumentoBox!, _pessoaPixTipoBox!, _pessoaPixChaveBox!, _pessoaRepassePreferencialBox!,
            _pessoaUsarDadosPessoaBancoButton!, _pessoaUsarPixButton!));
        RemoveTextBlock(PessoaFisicaFieldsPanel, "CPF");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "CPF/CNPJ");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Dados bancários");

        RenameSection(PessoaFisicaFieldsPanel, "Endereço:", "ENDEREÇO DE RESIDÊNCIA:");
        ReplaceSectionHeaderWithCep(PessoaFisicaFieldsPanel, "ENDEREÇO DE RESIDÊNCIA:", PessoaCepBox);

        _pessoaFisicaWorkSection = new StackPanel { Tag = "PessoaDetailedFields", Margin = new Thickness(0, 10, 0, 12) };
        _pessoaFisicaWorkSection.Children.Add(SectionHeader("DADOS DO TRABALHO:"));
        _pessoaFisicaWorkSection.Children.Add(WrapFields(
            ("Nome da empresa", PessoaNomeEmpresaTrabalhoBox, 260),
            ("CNPJ", _pessoaCnpjEmpresaTrabalhoBox!, 190),
            ("Telefone", PessoaTelefoneEmpresaTrabalhoBox, 160),
            ("E-mail", _pessoaEmailEmpresaTrabalhoBox!, 260),
            ("Cargo", _pessoaCargoTrabalhoBox!, 160),
            ("Renda", _pessoaRendaTrabalhoBox!, 120),
            ("Tempo no emprego", _pessoaTempoEmpregoBox!, 140),
            ("Tipo de comprovante de renda", _pessoaTipoComprovanteRendaBox!, 210),
            ("Outras informações", _pessoaTrabalhoOutrasInformacoesBox!, 360)));
        _pessoaFisicaWorkSection.Children.Add(AddressHeader("ENDEREÇO DA EMPRESA:", _pessoaTrabalhoCepBox!));
        _pessoaFisicaWorkSection.Children.Add(WrapFields(
            ("Rua", _pessoaTrabalhoRuaBox!, 300),
            ("Número", _pessoaTrabalhoNumeroBox!, 90),
            ("Complemento", _pessoaTrabalhoComplementoBox!, 220),
            ("Bairro", _pessoaTrabalhoBairroBox!, 180),
            ("Estado", _pessoaTrabalhoEstadoBox!, 80),
            ("Cidade", _pessoaTrabalhoCidadeBox!, 180)));
        PessoaFisicaFieldsPanel.Children.Add(_pessoaFisicaWorkSection);
        RemoveLegacyPessoaFisicaWorkFields();

        _pessoaConjugeSection = new StackPanel { Tag = "PessoaDetailedFields", Margin = new Thickness(0, 10, 0, 12) };
        _pessoaConjugeSection.Children.Add(SectionHeader("DADOS DO CÔNJUGE:"));
        _pessoaConjugeSection.Children.Add(WrapFields(
            ("Trabalho", _pessoaConjugeWorkComboBox!, 180),
            ("Nome", PessoaConjugeNomeBox, 260),
            ("CPF", PessoaConjugeCpfBox, 190),
            ("RG", PessoaConjugeRgBox, 150),
            ("Telefone", PessoaConjugeTelefoneBox, 160),
            ("E-mail", _pessoaConjugeEmailBox!, 260),
            ("Data de nascimento", PessoaConjugeDataNascimentoBox, 140),
            ("Profissão", PessoaConjugeProfissaoBox, 170),
            ("Nacionalidade", PessoaConjugeNacionalidadeBox, 150),
            ("Dados bancários", _pessoaConjugeDadosBancariosBox!, 360),
            ("Outras informações", _pessoaConjugeOutrasInformacoesBox!, 360)));
        PessoaFisicaFieldsPanel.Children.Add(_pessoaConjugeSection);
        RemoveLegacyPessoaConjugeLabels();

        _pessoaConjugeWorkSection = new StackPanel { Tag = "PessoaDetailedFields", Margin = new Thickness(0, 10, 0, 12) };
        _pessoaConjugeWorkSection.Children.Add(SectionHeader("DADOS DO TRABALHO (CÔNJUGE):"));
        _pessoaConjugeWorkSection.Children.Add(WrapFields(
            ("Nome da empresa", _pessoaConjugeNomeEmpresaTrabalhoBox!, 260),
            ("CNPJ", _pessoaConjugeCnpjEmpresaTrabalhoBox!, 190),
            ("Telefone", _pessoaConjugeTelefoneEmpresaTrabalhoBox!, 160),
            ("E-mail", _pessoaConjugeEmailEmpresaTrabalhoBox!, 260),
            ("Cargo", _pessoaConjugeCargoTrabalhoBox!, 160),
            ("Renda", _pessoaConjugeRendaTrabalhoBox!, 120),
            ("Tempo no emprego", _pessoaConjugeTempoEmpregoBox!, 140),
            ("Tipo de comprovante de renda", _pessoaConjugeTipoComprovanteRendaBox!, 210),
            ("Outras informações", _pessoaConjugeTrabalhoOutrasInformacoesBox!, 360)));
        _pessoaConjugeWorkSection.Children.Add(AddressHeader("ENDEREÇO DA EMPRESA:", _pessoaConjugeEmpresaCepBox!));
        _pessoaConjugeWorkSection.Children.Add(WrapFields(
            ("Rua", _pessoaConjugeEmpresaRuaBox!, 300),
            ("Número", _pessoaConjugeEmpresaNumeroBox!, 90),
            ("Complemento", _pessoaConjugeEmpresaComplementoBox!, 220),
            ("Bairro", _pessoaConjugeEmpresaBairroBox!, 180),
            ("Estado", _pessoaConjugeEmpresaEstadoBox!, 80),
            ("Cidade", _pessoaConjugeEmpresaCidadeBox!, 180)));
        PessoaFisicaFieldsPanel.Children.Add(_pessoaConjugeWorkSection);
    }

    private void AddPessoaJuridicaDetailedFields()
    {
        RenameSection(PessoaJuridicaFieldsPanel, "Endereço da empresa:", "ENDEREÇO DA EMPRESA:");
        ReplaceSectionHeaderWithCep(PessoaJuridicaFieldsPanel, "ENDEREÇO DA EMPRESA:", PessoaEmpresaCepBox);
        var companyRow = new WrapPanel { Tag = "PessoaDetailedFields", Margin = new Thickness(0, 0, 0, 12) };
        companyRow.Children.Insert(0, FieldStack("Nome fantasia", _pessoaNomeFantasiaBox!, 260));
        companyRow.Children.Add(FieldStack("Receita mensal", _pessoaReceitaMensalBox!, 140));
        companyRow.Children.Add(FieldStack("Atividade", _pessoaAtividadeBox!, 220));
        companyRow.Children.Add(FieldStack("Inscrição estadual", _pessoaInscricaoEstadualBox!, 160));
        companyRow.Children.Add(FieldStack("Inscrição municipal", _pessoaInscricaoMunicipalBox!, 160));
        companyRow.Children.Add(FieldStack("Data de abertura", _pessoaDataAberturaBox!, 140));
        PessoaJuridicaFieldsPanel.Children.Insert(0, companyRow);

        InsertSectionHeaderBefore(PessoaJuridicaFieldsPanel, "Nome do responsável", "DADOS DO REPRESENTANTE LEGAL:");
        var responsibleRow = new WrapPanel { Tag = "PessoaDetailedFields", Margin = new Thickness(0, 0, 0, 12) };
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "Nome do responsável", PessoaResponsavelNomeBox, 260);
        responsibleRow.Children.Insert(0, FieldStack("Cargo", _pessoaResponsavelCargoBox!, 160));
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "CPF do responsável", PessoaResponsavelCpfBox, 190);
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "RG do responsável", PessoaResponsavelRgBox, 150);
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "Telefone do responsável", PessoaResponsavelTelefoneBox, 160);
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "E-mail do responsável", PessoaResponsavelEmailBox, 260);
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "Nascimento do responsável", PessoaResponsavelDataNascimentoBox, 140);
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "Profissão do responsável", PessoaResponsavelProfissaoBox, 170);
        MoveFieldToWrapPanel(PessoaJuridicaFieldsPanel, responsibleRow, "Nacionalidade do responsável", PessoaResponsavelNacionalidadeBox, 150);
        responsibleRow.Children.Add(FieldStack("Outras informações", _pessoaResponsavelObservacoesBox!, 360));
        PessoaJuridicaFieldsPanel.Children.Add(responsibleRow);
        PessoaJuridicaFieldsPanel.Children.Add(CreateBankSection(
            PessoaResponsavelDadosBancariosBox,
            _pessoaResponsavelBancoBox!, _pessoaResponsavelAgenciaNumeroBox!, _pessoaResponsavelAgenciaDigitoBox!,
            _pessoaResponsavelContaNumeroBox!, _pessoaResponsavelContaDigitoBox!, _pessoaResponsavelContaTipoBox!, _pessoaResponsavelTitularNomeBox!,
            _pessoaResponsavelTitularDocumentoBox!, _pessoaResponsavelPixTipoBox!, _pessoaResponsavelPixChaveBox!, _pessoaResponsavelRepassePreferencialBox!,
            _pessoaUsarDadosResponsavelBancoButton!, _pessoaResponsavelUsarPixButton!));
        RemoveTextBlock(PessoaJuridicaFieldsPanel, "Dados bancários");
        RemoveLegacyPessoaResponsavelWorkFields();

        RenameSection(PessoaJuridicaFieldsPanel, "Endereço do responsável:", "ENDEREÇO DE RESIDÊNCIA:");
        ReplaceSectionHeaderWithCep(PessoaJuridicaFieldsPanel, "ENDEREÇO DE RESIDÊNCIA:", PessoaResponsavelCepBox);
    }

    private void RemoveLegacyPessoaFisicaWorkFields()
    {
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Nome da empresa");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Telefone da empresa");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Onde trabalha");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Endereço do trabalho");
        RemoveChild(PessoaFisicaFieldsPanel, PessoaOndeTrabalhaBox);
        RemoveChild(PessoaFisicaFieldsPanel, PessoaEnderecoTrabalhoBox);
    }

    private void RemoveLegacyPessoaConjugeLabels()
    {
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Cônjuge");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "RG do cônjuge");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "CPF do cônjuge");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Nascimento do cônjuge");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Profissão do cônjuge");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Nacionalidade do cônjuge");
        RemoveTextBlock(PessoaFisicaFieldsPanel, "Telefone do cônjuge");
    }

    private void RemoveLegacyPessoaResponsavelWorkFields()
    {
        RemoveTextBlock(PessoaJuridicaFieldsPanel, "Estado civil do responsável");
        RemoveTextBlock(PessoaJuridicaFieldsPanel, "Onde trabalha");
        RemoveTextBlock(PessoaJuridicaFieldsPanel, "Endereço do trabalho");
        RemoveTextBlock(PessoaJuridicaFieldsPanel, "Empresa onde trabalha");
        RemoveTextBlock(PessoaJuridicaFieldsPanel, "Telefone da empresa");
        RemoveChild(PessoaJuridicaFieldsPanel, PessoaResponsavelEstadoCivilBox);
        RemoveChild(PessoaJuridicaFieldsPanel, PessoaResponsavelOndeTrabalhaBox);
        RemoveChild(PessoaJuridicaFieldsPanel, PessoaResponsavelEnderecoTrabalhoBox);
        RemoveChild(PessoaJuridicaFieldsPanel, PessoaResponsavelNomeEmpresaTrabalhoBox);
        RemoveChild(PessoaJuridicaFieldsPanel, PessoaResponsavelTelefoneEmpresaTrabalhoBox);
    }

    private void FillPessoaBankHolderFromPessoa()
    {
        SetText(_pessoaTitularNomeBox, PessoaNomeBox.Text);
        SetText(_pessoaTitularDocumentoBox, PessoaDocumentoBox.Text);
    }

    private void FillResponsavelBankHolderFromResponsavel()
    {
        SetText(_pessoaResponsavelTitularNomeBox, PessoaResponsavelNomeBox.Text);
        SetText(_pessoaResponsavelTitularDocumentoBox, PessoaResponsavelCpfBox.Text);
    }

    private void FillPessoaPixFromSelectedType() =>
        FillPixFromSelectedType(_pessoaPixTipoBox, _pessoaPixChaveBox, PessoaDocumentoBox.Text, null, PessoaEmailBox.Text, PessoaTelefoneBox.Text);

    private void FillResponsavelPixFromSelectedType() =>
        FillPixFromSelectedType(_pessoaResponsavelPixTipoBox, _pessoaResponsavelPixChaveBox, PessoaResponsavelCpfBox.Text, null, PessoaResponsavelEmailBox.Text, PessoaResponsavelTelefoneBox.Text);

    private static void FillPixFromSelectedType(ComboBox? tipoBox, TextBox? chaveBox, string? cpf, string? cnpj, string? email, string? telefone)
    {
        if (tipoBox?.SelectedValue is not Monthoya.Core.Entities.PixChaveTipo pixTipo || chaveBox is null)
        {
            return;
        }

        var source = pixTipo switch
        {
            Monthoya.Core.Entities.PixChaveTipo.Cpf => cpf,
            Monthoya.Core.Entities.PixChaveTipo.Cnpj => cnpj,
            Monthoya.Core.Entities.PixChaveTipo.Email => email,
            Monthoya.Core.Entities.PixChaveTipo.Telefone => telefone,
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(source))
        {
            chaveBox.Text = source.Trim();
        }
    }
}
