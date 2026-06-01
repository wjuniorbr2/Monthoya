using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Monthoya.Core.Services;
using Monthoya.Desktop.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoaDocumentosContextMenuApplied;
    private static readonly bool PessoaDocumentosContextMenuRegistered = RegisterPessoaDocumentosContextMenu();

    private static bool RegisterPessoaDocumentosContextMenu()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler((sender, _) => ((ShellWindow)sender).ApplyPessoaDocumentosContextMenu()));

        return true;
    }

    private void ApplyPessoaDocumentosContextMenu()
    {
        _ = PessoaDocumentosContextMenuRegistered;

        if (_pessoaDocumentosContextMenuApplied)
        {
            return;
        }

        _pessoaDocumentosContextMenuApplied = true;

        PessoaDocumentosGrid.MouseRightButtonDown += PessoaDocumentosGrid_MouseRightButtonDown;
        PessoaDocumentosGrid.ContextMenu = BuildPessoaDocumentosContextMenu();
    }

    private ContextMenu BuildPessoaDocumentosContextMenu()
    {
        var menu = new ContextMenu();

        var useDataItem = new MenuItem { Header = "Usar dados do documento" };
        useDataItem.Click += UsePessoaDocumentoDataMenuItem_Click;
        menu.Items.Add(useDataItem);

        var openItem = new MenuItem { Header = "Abrir documento" };
        openItem.Click += OpenPessoaDocumentoMenuItem_Click;
        menu.Items.Add(openItem);

        menu.Items.Add(new Separator());

        var removeItem = new MenuItem { Header = "Remover documento" };
        removeItem.Click += RemovePessoaDocumentoMenuItem_Click;
        menu.Items.Add(removeItem);

        menu.Opened += (_, _) =>
        {
            var selected = PessoaDocumentosGrid.SelectedItem as PessoaDocumentoSummary;
            var hasSelection = selected is not null;
            useDataItem.IsEnabled = hasSelection && _isPessoaEditing;
            openItem.IsEnabled = hasSelection;
            removeItem.IsEnabled = hasSelection && _isPessoaEditing;
        };

        return menu;
    }

    private void PessoaDocumentosGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var row = FindPessoaDocumentoAncestor<DataGridRow>((DependencyObject)e.OriginalSource);
        if (row is null)
        {
            return;
        }

        row.IsSelected = true;
        row.Focus();
    }

    private async void UsePessoaDocumentoDataMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PessoaDocumentoErrorText.Text = string.Empty;

        if (!_isPessoaEditing)
        {
            PessoaDocumentoErrorText.Text = "Clique em Editar antes de usar dados do documento.";
            return;
        }

        if (PessoaDocumentosGrid.SelectedItem is not PessoaDocumentoSummary document)
        {
            PessoaDocumentoErrorText.Text = "Selecione um documento na lista.";
            return;
        }

        try
        {
            if (LocalAiSettingsStore.HasGeminiApiKey())
            {
                PessoaDocumentoErrorText.Text = "Lendo documento com Gemini...";
                var storedData = GeminiDocumentDataReader.ParseStoredJson(document.OcrTextoExtraido);
                if (storedData is null && IsPessoaDocumentoOcrAlreadyAttempted(document))
                {
                    PessoaDocumentoErrorText.Text = string.IsNullOrWhiteSpace(document.OcrErroMensagem)
                        ? "Este documento já foi processado e não possui dados reaproveitáveis."
                        : document.OcrErroMensagem;
                    return;
                }

                var geminiData = storedData ?? await GeminiDocumentDataReader.ExtractAsync(document.StoragePath);
                if (storedData is null)
                {
                    await StorePessoaDocumentoOcrResultAsync(document, geminiData.RawJson, succeeded: true);
                }
                var confirmText = BuildGeminiConfirmationText(geminiData);
                if (!ShowPessoaDocumentoInformationDialog("Usar dados encontrados?", confirmText))
                {
                    PessoaDocumentoErrorText.Text = "Dados encontrados, mas não aplicados.";
                    return;
                }

                ApplyGeminiDocumentDataToPessoaForm(NormalizeDocumentoDeFromDisplay(document.DocumentoDe), geminiData);
                ShowPessoaDocumentoOcrDebugText(document.Nome, NormalizeDocumentoDeFromDisplay(document.DocumentoDe), geminiData.RawJson);
            }
            else
            {
                var rawText = document.OcrTextoExtraido;
                if (string.IsNullOrWhiteSpace(rawText))
                {
                    rawText = await ExtractPessoaDocumentoTextForFormAsync(document.StoragePath, GuessPessoaDocumentoContentType(document.StoragePath));
                }

                ShowPessoaDocumentoOcrDebugText(document.Nome, NormalizeDocumentoDeFromDisplay(document.DocumentoDe), rawText);
                ApplyPessoaDocumentoOcrTextToForm(document.Tipo, NormalizeDocumentoDeFromDisplay(document.DocumentoDe), rawText);
                ApplyPessoaDocumentoOcrIdentityFallbackToForm(NormalizeDocumentoDeFromDisplay(document.DocumentoDe), rawText);
            }

            if (_selectedPessoaId.HasValue && _isPessoaEditing)
            {
                await _rentalManagementService.UpdatePessoaAsync(new UpdatePessoaRequest(_selectedPessoaId.Value, BuildPessoaRequest()));
                await RefreshSelectedPessoaAfterDocumentoOcrAsync(_selectedPessoaId.Value);
                await LoadPessoaDocumentosAsync(_selectedPessoaId);
            }

            PessoaDocumentoErrorText.Text = "Dados do documento aplicados para revisão.";
        }
        catch (Exception ex)
        {
            PessoaDocumentoErrorText.Text = ex.Message;
        }
    }

    private static string BuildGeminiConfirmationText(GeminiDocumentData data)
    {
        var builder = new StringBuilder();
        builder.AppendLine("O Gemini encontrou os seguintes dados:");
        builder.AppendLine();
        builder.AppendLine($"Tipo: {data.DocumentType ?? "-"}");
        builder.AppendLine($"Nome: {data.Name ?? "-"}");
        builder.AppendLine($"CPF: {data.Cpf ?? "-"}");
        builder.AppendLine($"RG: {data.Rg ?? "-"}");
        builder.AppendLine($"Data de nascimento: {(data.BirthDate.HasValue ? data.BirthDate.Value.ToString("dd/MM/yyyy") : "-")}");
        builder.AppendLine($"Nacionalidade: {data.Nationality ?? "-"}");
        builder.AppendLine($"Telefone: {data.Phone ?? "-"}");
        builder.AppendLine($"E-mail: {data.Email ?? "-"}");
        builder.AppendLine($"Empresa: {data.CompanyName ?? "-"}");
        builder.AppendLine($"Nome fantasia: {data.CompanyTradeName ?? "-"}");
        builder.AppendLine($"CNPJ: {data.Cnpj ?? "-"}");
        builder.AppendLine($"Atividade: {data.CompanyActivity ?? "-"}");
        builder.AppendLine($"Inscrição estadual: {data.CompanyStateRegistration ?? "-"}");
        builder.AppendLine($"Inscrição municipal: {data.CompanyMunicipalRegistration ?? "-"}");
        builder.AppendLine($"Data de abertura: {(data.CompanyOpeningDate.HasValue ? data.CompanyOpeningDate.Value.ToString("dd/MM/yyyy") : "-")}");
        builder.AppendLine($"Cargo: {data.JobTitle ?? "-"}");
        builder.AppendLine($"Renda: {data.Income ?? "-"}");
        builder.AppendLine($"Tempo no emprego: {data.EmploymentDuration ?? "-"}");
        builder.AppendLine($"CEP: {data.Cep ?? "-"}");
        builder.AppendLine($"Rua: {data.Street ?? "-"}");
        builder.AppendLine($"Número: {data.Number ?? "-"}");
        builder.AppendLine($"Complemento: {data.Complement ?? "-"}");
        builder.AppendLine($"Bairro: {data.Neighborhood ?? "-"}");
        builder.AppendLine($"Cidade/UF: {data.City ?? "-"}/{data.State ?? "-"}");
        builder.AppendLine();
        builder.AppendLine("Aplicar esses dados aos campos em branco?");
        return builder.ToString();
    }

    private void ApplyGeminiDocumentDataToPessoaForm(string documentoDe, GeminiDocumentData data)
    {
        switch (documentoDe)
        {
            case "pessoa":
                FillIfBlank(PessoaNomeBox, data.Name);
                FillIfBlank(PessoaDocumentoBox, data.Cpf);
                FillIfBlank(PessoaRgBox, data.Rg);
                FillIfBlank(PessoaNacionalidadeBox, data.Nationality);
                FillIfBlank(PessoaTelefoneBox, data.Phone);
                FillIfBlank(PessoaEmailBox, data.Email);
                FillIfBlank(PessoaCepBox, data.Cep);
                FillIfBlank(PessoaRuaBox, data.Street);
                FillIfBlank(PessoaNumeroBox, data.Number);
                FillIfBlank(PessoaComplementoBox, data.Complement);
                FillIfBlank(PessoaBairroBox, data.Neighborhood);
                FillIfBlank(PessoaCidadeBox, data.City);
                FillIfBlank(PessoaEstadoBox, data.State);
                ReplaceRecentDate(PessoaDataNascimentoBox, data.BirthDate);
                break;
            case "conjuge":
                FillIfBlank(PessoaConjugeNomeBox, data.Name);
                FillIfBlank(PessoaConjugeCpfBox, data.Cpf);
                FillIfBlank(PessoaConjugeRgBox, data.Rg);
                FillIfBlank(PessoaConjugeNacionalidadeBox, data.Nationality);
                FillIfBlank(PessoaConjugeTelefoneBox, data.Phone);
                FillIfBlank(_pessoaConjugeEmailBox, data.Email);
                ReplaceRecentDate(PessoaConjugeDataNascimentoBox, data.BirthDate);
                break;
            case "empresa_trabalho":
                if (_pessoaWorkComboBox is not null)
                {
                    _pessoaWorkComboBox.SelectedItem = "Possui trabalho";
                }

                FillIfBlank(PessoaNomeEmpresaTrabalhoBox, data.CompanyName ?? data.Name);
                FillIfBlank(_pessoaCnpjEmpresaTrabalhoBox, data.Cnpj);
                FillIfBlank(PessoaTelefoneEmpresaTrabalhoBox, data.Phone);
                FillIfBlank(_pessoaEmailEmpresaTrabalhoBox, data.Email);
                FillIfBlank(_pessoaCargoTrabalhoBox, data.JobTitle);
                FillIfBlank(_pessoaRendaTrabalhoBox, data.Income);
                FillIfBlank(_pessoaTempoEmpregoBox, data.EmploymentDuration);
                FillIfBlank(_pessoaTrabalhoCepBox, data.Cep);
                FillIfBlank(_pessoaTrabalhoRuaBox, data.Street);
                FillIfBlank(_pessoaTrabalhoNumeroBox, data.Number);
                FillIfBlank(_pessoaTrabalhoComplementoBox, data.Complement);
                FillIfBlank(_pessoaTrabalhoBairroBox, data.Neighborhood);
                FillIfBlank(_pessoaTrabalhoCidadeBox, data.City);
                FillIfBlank(_pessoaTrabalhoEstadoBox, data.State);
                break;
            case "trabalho_conjuge":
                if (_pessoaConjugeWorkComboBox is not null)
                {
                    _pessoaConjugeWorkComboBox.SelectedItem = "Possui trabalho";
                }

                FillIfBlank(_pessoaConjugeNomeEmpresaTrabalhoBox, data.CompanyName ?? data.Name);
                FillIfBlank(_pessoaConjugeCnpjEmpresaTrabalhoBox, data.Cnpj);
                FillIfBlank(_pessoaConjugeTelefoneEmpresaTrabalhoBox, data.Phone);
                FillIfBlank(_pessoaConjugeEmailEmpresaTrabalhoBox, data.Email);
                FillIfBlank(_pessoaConjugeCargoTrabalhoBox, data.JobTitle);
                FillIfBlank(_pessoaConjugeRendaTrabalhoBox, data.Income);
                FillIfBlank(_pessoaConjugeTempoEmpregoBox, data.EmploymentDuration);
                FillIfBlank(_pessoaConjugeEmpresaCepBox, data.Cep);
                FillIfBlank(_pessoaConjugeEmpresaRuaBox, data.Street);
                FillIfBlank(_pessoaConjugeEmpresaNumeroBox, data.Number);
                FillIfBlank(_pessoaConjugeEmpresaComplementoBox, data.Complement);
                FillIfBlank(_pessoaConjugeEmpresaBairroBox, data.Neighborhood);
                FillIfBlank(_pessoaConjugeEmpresaCidadeBox, data.City);
                FillIfBlank(_pessoaConjugeEmpresaEstadoBox, data.State);
                break;
            case "responsavel":
                FillIfBlank(PessoaResponsavelNomeBox, data.Name);
                FillIfBlank(PessoaResponsavelCpfBox, data.Cpf);
                FillIfBlank(PessoaResponsavelRgBox, data.Rg);
                FillIfBlank(PessoaResponsavelNacionalidadeBox, data.Nationality);
                FillIfBlank(PessoaResponsavelTelefoneBox, data.Phone);
                FillIfBlank(PessoaResponsavelEmailBox, data.Email);
                FillIfBlank(PessoaResponsavelCepBox, data.Cep);
                FillIfBlank(PessoaResponsavelRuaBox, data.Street);
                FillIfBlank(PessoaResponsavelNumeroBox, data.Number);
                FillIfBlank(PessoaResponsavelComplementoBox, data.Complement);
                FillIfBlank(PessoaResponsavelBairroBox, data.Neighborhood);
                FillIfBlank(PessoaResponsavelCidadeBox, data.City);
                FillIfBlank(PessoaResponsavelEstadoBox, data.State);
                ReplaceRecentDate(PessoaResponsavelDataNascimentoBox, data.BirthDate);
                break;
            case "empresa":
                FillIfBlank(PessoaNomeBox, data.CompanyName ?? data.Name);
                FillIfBlank(PessoaDocumentoBox, data.Cnpj ?? data.Cpf);
                FillIfBlank(_pessoaNomeFantasiaBox, data.CompanyTradeName);
                FillIfBlank(_pessoaAtividadeBox, data.CompanyActivity);
                FillIfBlank(_pessoaInscricaoEstadualBox, data.CompanyStateRegistration);
                FillIfBlank(_pessoaInscricaoMunicipalBox, data.CompanyMunicipalRegistration);
                FillDateIfBlank(_pessoaDataAberturaBox, data.CompanyOpeningDate);
                FillIfBlank(PessoaEmpresaCepBox, data.Cep);
                FillIfBlank(PessoaEmpresaRuaBox, data.Street);
                FillIfBlank(PessoaEmpresaNumeroBox, data.Number);
                FillIfBlank(PessoaEmpresaComplementoBox, data.Complement);
                FillIfBlank(PessoaEmpresaBairroBox, data.Neighborhood);
                FillIfBlank(PessoaEmpresaCidadeBox, data.City);
                FillIfBlank(PessoaEmpresaEstadoBox, data.State);
                break;
            default:
                break;
        }
    }

    private void OpenPessoaDocumentoMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (PessoaDocumentosGrid.SelectedItem is not PessoaDocumentoSummary document)
        {
            return;
        }

        try
        {
            if (!File.Exists(document.StoragePath))
            {
                PessoaDocumentoErrorText.Text = "O arquivo do documento não foi encontrado no computador.";
                return;
            }

            Process.Start(new ProcessStartInfo(document.StoragePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            PessoaDocumentoErrorText.Text = ex.Message;
        }
    }

    private void RemovePessoaDocumentoMenuItem_Click(object sender, RoutedEventArgs e)
    {
        PessoaDocumentoErrorText.Text = string.Empty;

        if (!_isPessoaEditing)
        {
            PessoaDocumentoErrorText.Text = "Clique em Editar antes de remover documentos.";
            return;
        }

        if (PessoaDocumentosGrid.SelectedItem is not PessoaDocumentoSummary document)
        {
            return;
        }

        if (document.Id != Guid.Empty)
        {
            PessoaDocumentoErrorText.Text = "Remoção de documento já salvo será habilitada na próxima etapa.";
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Deseja remover o documento '{document.Nome}'?",
            "Remover documento",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        RemovePendingPessoaDocumento(document);
        RefreshPendingPessoaDocumentosGrid();
        PessoaDocumentoErrorText.Text = "Documento removido.";
        QueuePessoaDocumentosCardPruning();
    }

    private void RemovePendingPessoaDocumento(PessoaDocumentoSummary document)
    {
        var index = _pendingPessoaDocumentos.FindIndex(draft =>
            string.Equals(draft.Nome, document.Nome, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Path.GetFileName(draft.StoragePath), document.StoragePath, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            _pendingPessoaDocumentos.RemoveAt(index);
        }
    }

    private static string NormalizeDocumentoDeFromDisplay(string? documentoDe)
    {
        if (string.IsNullOrWhiteSpace(documentoDe))
        {
            return "pessoa";
        }

        return documentoDe.Trim().ToLowerInvariant() switch
        {
            "cônjuge" or "conjuge" => "conjuge",
            "cônjuge do responsável" or "conjuge do responsavel" => "conjuge",
            "responsável" or "responsavel" => "responsavel",
            "empresa" => "empresa",
            "trabalho da pessoa" or "empresa_trabalho" => "empresa_trabalho",
            "trabalho do cônjuge" or "trabalho do conjuge" or "trabalho_conjuge" => "trabalho_conjuge",
            "trabalho do cônjuge do responsável" or "trabalho do conjuge do responsavel" => "trabalho_conjuge",
            "outros" => "outros",
            "conjuge_responsavel" => "conjuge",
            "trabalho_conjuge_responsavel" => "trabalho_conjuge",
            _ => "pessoa"
        };
    }

    private static T? FindPessoaDocumentoAncestor<T>(DependencyObject source) where T : DependencyObject
    {
        var current = source;
        while (current is not null)
        {
            if (current is T typed)
            {
                return typed;
            }

            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
