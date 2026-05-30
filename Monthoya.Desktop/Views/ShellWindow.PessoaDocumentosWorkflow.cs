using System.IO;
using System.Windows;
using System.Windows.Threading;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private readonly List<PessoaDocumentoDraft> _pendingPessoaDocumentos = [];
    private bool _pessoaDocumentosWorkflowApplied;

    private static readonly bool PessoaDocumentosWorkflowClassHandlerRegistered = RegisterPessoaDocumentosWorkflowClassHandler();

    private static bool RegisterPessoaDocumentosWorkflowClassHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler((sender, _) => ((ShellWindow)sender).ApplyPessoaDocumentosWorkflow()));

        return true;
    }

    private void ApplyPessoaDocumentosWorkflow()
    {
        _ = PessoaDocumentosWorkflowClassHandlerRegistered;

        if (_pessoaDocumentosWorkflowApplied)
        {
            return;
        }

        _pessoaDocumentosWorkflowApplied = true;

        SavePessoaDocumentoButton.Click -= SavePessoaDocumentoButton_Click;
        SavePessoaDocumentoButton.Click += SavePessoaDocumentoForActiveFormButton_Click;

        SavePessoaButton.Click -= SavePessoaButton_Click;
        SavePessoaButton.Click += SavePessoaWithPendingDocumentsButton_Click;

        PessoasGrid.SelectionChanged += (_, _) =>
        {
            _pendingPessoaDocumentos.Clear();
            QueueRefreshPessoaDocumentosForActiveForm();
        };

        PessoaNomeBox.TextChanged += (_, _) => QueueRefreshPessoaDocumentosForActiveForm();
        PessoaTipoBox.SelectionChanged += (_, _) => QueueRefreshPessoaDocumentosForActiveForm();

        SavePessoaDocumentoButton.IsEnabled = true;
        QueueRefreshPessoaDocumentosForActiveForm();
    }

    private async void SavePessoaDocumentoForActiveFormButton_Click(object sender, RoutedEventArgs e)
    {
        PessoaDocumentoErrorText.Text = string.Empty;

        try
        {
            var draft = BuildPessoaDocumentoDraft();
            if (draft is null)
            {
                return;
            }

            var extractedText = await ExtractPessoaDocumentoTextForFormAsync(draft.StoragePath, draft.ContentType);
            draft = draft with { OcrText = extractedText };
            ApplyPessoaDocumentoOcrTextToForm(draft.DocumentoDe, extractedText);

            if (_selectedPessoaId.HasValue)
            {
                await SavePessoaDocumentoDraftAsync(_selectedPessoaId.Value, draft);
                ClearPessoaDocumentoInputs();
                await LoadPessoaDocumentosAsync(_selectedPessoaId);
                QueuePessoaDocumentosCardPruning();
                return;
            }

            _pendingPessoaDocumentos.Add(draft);
            ClearPessoaDocumentoInputs();
            RefreshPendingPessoaDocumentosGrid();
            QueuePessoaDocumentosCardPruning();
        }
        catch (Exception ex)
        {
            PessoaDocumentoErrorText.Text = ex.Message;
        }
    }

    private async void SavePessoaWithPendingDocumentsButton_Click(object sender, RoutedEventArgs e)
    {
        PessoaErrorText.Text = string.Empty;
        PessoaDocumentoErrorText.Text = string.Empty;

        try
        {
            if (!ValidatePessoaForm())
            {
                return;
            }

            var request = BuildPessoaRequest();
            PessoaSummary savedPessoa;

            if (_selectedPessoaId.HasValue && _selectedPessoaDetails is not null)
            {
                savedPessoa = await _rentalManagementService.UpdatePessoaAsync(new UpdatePessoaRequest(_selectedPessoaId.Value, request));
                await SavePendingPessoaDocumentosAsync(savedPessoa.Id);
                SetPessoaEditMode(false, isNew: false);
            }
            else
            {
                savedPessoa = await _rentalManagementService.CreatePessoaAsync(request);
                _selectedPessoaId = savedPessoa.Id;
                await SavePendingPessoaDocumentosAsync(savedPessoa.Id);
                SetPessoaEditMode(false, isNew: false);
            }

            await LoadPessoasAsync();
            var selected = _pessoas.FirstOrDefault(x => x.Id == savedPessoa.Id) ?? savedPessoa;
            PessoasGrid.SelectedItem = selected;
            SetPessoaDocumentoSelection(selected);
            _selectedPessoaDetails = await _rentalManagementService.GetPessoaAsync(savedPessoa.Id);
            if (_selectedPessoaDetails is not null)
            {
                PopulatePessoaForm(_selectedPessoaDetails);
                SetPessoaEditMode(false, isNew: false);
            }

            await LoadPessoaDocumentosAsync(savedPessoa.Id);
            QueuePessoaDocumentosCardPruning();
        }
        catch (Exception ex)
        {
            PessoaErrorText.Text = ex.Message;
            if (string.IsNullOrWhiteSpace(PessoaDocumentoErrorText.Text) && _pendingPessoaDocumentos.Count > 0)
            {
                PessoaDocumentoErrorText.Text = ex.Message;
            }
        }
    }

    private PessoaDocumentoDraft? BuildPessoaDocumentoDraft()
    {
        var filePath = PessoaDocumentoArquivoBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(PessoaDocumentoNomeBox.Text))
        {
            PessoaDocumentoErrorText.Text = "Informe a descrição do documento.";
            return null;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            PessoaDocumentoErrorText.Text = "Selecione o arquivo digitalizado.";
            return null;
        }

        if (!File.Exists(filePath))
        {
            PessoaDocumentoErrorText.Text = "O arquivo selecionado não foi encontrado no computador.";
            return null;
        }

        return new PessoaDocumentoDraft(
            Tipo: PessoaDocumentoTipoBox.SelectedValue as string ?? "outros",
            DocumentoDe: PessoaDocumentoDonoBox.SelectedValue as string ?? "pessoa",
            Nome: PessoaDocumentoNomeBox.Text.Trim(),
            StoragePath: filePath,
            ContentType: GuessPessoaDocumentoContentType(filePath),
            DataValidade: ToDateOnly(PessoaDocumentoValidadeBox.SelectedDate),
            Observacoes: PessoaDocumentoObservacoesBox.Text?.Trim(),
            OcrText: null);
    }

    private async Task SavePendingPessoaDocumentosAsync(Guid pessoaId)
    {
        if (_pendingPessoaDocumentos.Count == 0)
        {
            return;
        }

        foreach (var draft in _pendingPessoaDocumentos.ToList())
        {
            await SavePessoaDocumentoDraftAsync(pessoaId, draft);
        }

        _pendingPessoaDocumentos.Clear();
    }

    private Task<PessoaDocumentoSummary> SavePessoaDocumentoDraftAsync(Guid pessoaId, PessoaDocumentoDraft draft) =>
        _rentalManagementService.CreatePessoaDocumentoAsync(new CreatePessoaDocumentoRequest(
            pessoaId,
            string.IsNullOrWhiteSpace(draft.Tipo) ? "outros" : draft.Tipo,
            draft.Nome,
            draft.StoragePath,
            draft.ContentType,
            draft.DataValidade,
            draft.Observacoes,
            draft.DocumentoDe));

    private void ClearPessoaDocumentoInputs()
    {
        PessoaDocumentoNomeBox.Clear();
        PessoaDocumentoArquivoBox.Clear();
        PessoaDocumentoValidadeBox.SelectedDate = null;
        PessoaDocumentoObservacoesBox.Clear();
    }

    private void RefreshPendingPessoaDocumentosGrid()
    {
        PessoaDocumentosTitleText.Text = "Documentos anexos";
        PessoaDocumentosGrid.ItemsSource = _pendingPessoaDocumentos
            .Select((draft, index) => new PessoaDocumentoSummary(
                Guid.Empty,
                Guid.Empty,
                "Nova pessoa",
                "Pendente",
                GetDocumentoDeDisplayLabel(draft.DocumentoDe),
                draft.Nome,
                Path.GetFileName(draft.StoragePath),
                draft.DataValidade,
                "Pendente",
                string.IsNullOrWhiteSpace(draft.OcrText) ? "Pendente" : "Processado",
                draft.OcrText,
                null,
                null,
                string.IsNullOrWhiteSpace(draft.OcrText) ? null : "Aplicado aos campos em branco"))
            .ToList();
    }

    private void QueueRefreshPessoaDocumentosForActiveForm()
    {
        Dispatcher.BeginInvoke(() =>
        {
            SavePessoaDocumentoButton.IsEnabled = true;
            if (!_selectedPessoaId.HasValue && _pendingPessoaDocumentos.Count > 0)
            {
                RefreshPendingPessoaDocumentosGrid();
            }
            else if (!_selectedPessoaId.HasValue)
            {
                PessoaDocumentosTitleText.Text = "Documentos anexos";
                PessoaDocumentosGrid.ItemsSource = Array.Empty<PessoaDocumentoSummary>();
            }

            QueuePessoaDocumentosCardPruning();
        }, DispatcherPriority.Background);
    }

    private static string GetDocumentoDeDisplayLabel(string documentoDe) =>
        documentoDe switch
        {
            "empresa_trabalho" => "Trabalho da pessoa",
            "conjuge" => "Cônjuge",
            "trabalho_conjuge" => "Trabalho do cônjuge",
            "empresa" => "Empresa",
            "responsavel" => "Responsável",
            "conjuge_responsavel" => "Cônjuge do responsável",
            "trabalho_conjuge_responsavel" => "Trabalho do cônjuge do responsável",
            _ => "Pessoa"
        };

    private static string GuessPessoaDocumentoContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };

    private sealed record PessoaDocumentoDraft(
        string Tipo,
        string DocumentoDe,
        string Nome,
        string StoragePath,
        string? ContentType,
        DateOnly? DataValidade,
        string? Observacoes,
        string? OcrText);
}
