using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Monthoya.Core.Services;
using Monthoya.Desktop.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private Button? _usarPessoaDocumentosInformacoesButton;

    private void EnsurePessoaDocumentosBatchUseDataButton()
    {
        if (_usarPessoaDocumentosInformacoesButton is null)
        {
            _usarPessoaDocumentosInformacoesButton = new Button
            {
                Content = "Usar informações",
                Style = TryFindResource("PrimaryButtonSmall") as Style ?? TryFindResource("PrimaryButton") as Style,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 6, 12, 6),
                MinWidth = 145,
                ToolTip = "Lê os documentos anexados e tenta preencher apenas campos em branco."
            };
            _usarPessoaDocumentosInformacoesButton.Click += UsarPessoaDocumentosInformacoesButton_Click;
        }

        if (PessoaDocumentosTitleText.Parent is Grid titleGrid && !titleGrid.Children.Contains(_usarPessoaDocumentosInformacoesButton))
        {
            if (_usarPessoaDocumentosInformacoesButton.Parent is Panel oldParent)
            {
                oldParent.Children.Remove(_usarPessoaDocumentosInformacoesButton);
            }

            titleGrid.Children.Add(_usarPessoaDocumentosInformacoesButton);
            Grid.SetRow(_usarPessoaDocumentosInformacoesButton, Grid.GetRow(PessoaDocumentosTitleText));
        }

        _usarPessoaDocumentosInformacoesButton.Visibility = Visibility.Visible;
        _usarPessoaDocumentosInformacoesButton.IsEnabled = _isPessoaEditing && HasPessoaDocumentoItems();
    }

    private async void UsarPessoaDocumentosInformacoesButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isPessoaEditing)
        {
            PessoaDocumentoErrorText.Text = "Clique em Editar antes de usar informações dos documentos.";
            return;
        }

        var documents = PessoaDocumentosGrid.ItemsSource?.Cast<object>().OfType<PessoaDocumentoSummary>()
            .Where(document => !string.IsNullOrWhiteSpace(document.StoragePath))
            .ToList() ?? [];

        if (documents.Count == 0)
        {
            PessoaDocumentoErrorText.Text = "Adicione ao menos um documento antes de usar informações digitalizadas.";
            return;
        }

        SetPessoaDocumentosBatchUseDataBusy(true);
        try
        {
            if (LocalAiSettingsStore.HasGeminiApiKey())
            {
                await UsePessoaDocumentosBatchGeminiAsync(documents);
            }
            else
            {
                await UsePessoaDocumentosBatchLocalAsync(documents);
            }
        }
        catch (Exception ex)
        {
            PessoaDocumentoErrorText.Text = ex.Message;
        }
        finally
        {
            SetPessoaDocumentosBatchUseDataBusy(false);
        }
    }

    private async Task UsePessoaDocumentosBatchGeminiAsync(IReadOnlyList<PessoaDocumentoSummary> documents)
    {
        PessoaDocumentoErrorText.Text = string.Empty;
        var results = new List<(PessoaDocumentoSummary Document, GeminiDocumentData Data)>();
        var localFallbacks = new List<(PessoaDocumentoSummary Document, string RawText)>();
        var errors = new List<string>();

        foreach (var document in documents)
        {
            try
            {
                var storedData = GeminiDocumentDataReader.ParseStoredJson(document.OcrTextoExtraido);
                if (storedData is not null)
                {
                    results.Add((document, storedData));
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(document.OcrTextoExtraido))
                {
                    localFallbacks.Add((document, document.OcrTextoExtraido));
                    continue;
                }

                if (IsPessoaDocumentoOcrAlreadyAttempted(document))
                {
                    if (!string.IsNullOrWhiteSpace(document.OcrErroMensagem))
                    {
                        errors.Add($"{document.Nome}: {document.OcrErroMensagem}");
                    }

                    continue;
                }

                if (!File.Exists(document.StoragePath))
                {
                    errors.Add($"{document.Nome}: arquivo não encontrado.");
                    continue;
                }

                var data = await GeminiDocumentDataReader.ExtractAsync(document.StoragePath);
                results.Add((document, data));
                await StorePessoaDocumentoOcrResultAsync(document, data.RawJson, succeeded: true);
            }
            catch (Exception ex)
            {
                await StorePessoaDocumentoOcrResultAsync(document, document.OcrTextoExtraido, succeeded: false, errorMessage: ex.Message);
                errors.Add($"{document.Nome}: {ex.Message}");
            }
        }

        if (results.Count == 0 && localFallbacks.Count == 0)
        {
            PessoaDocumentoErrorText.Text = errors.Count == 0 ? "Nenhuma informação foi encontrada nos documentos." : string.Join(Environment.NewLine, errors);
            return;
        }

        var confirmation = BuildBatchConfirmationText(results, localFallbacks, errors);
        var answer = MessageBox.Show(this, confirmation, "Usar informações encontradas?", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes)
        {
            PessoaDocumentoErrorText.Text = "Dados encontrados, mas não aplicados.";
            return;
        }

        foreach (var (document, data) in results)
        {
            ApplyGeminiDocumentDataToPessoaForm(NormalizeDocumentoDeFromDisplay(document.DocumentoDe), data);
        }

        foreach (var (document, rawText) in localFallbacks)
        {
            var owner = NormalizeDocumentoDeFromDisplay(document.DocumentoDe);
            ApplyPessoaDocumentoOcrTextToForm(document.Tipo, owner, rawText);
            ApplyPessoaDocumentoOcrIdentityFallbackToForm(owner, rawText);
        }

        await SavePessoaAfterBatchDocumentDataAsync();
        PessoaDocumentoErrorText.Text = errors.Count == 0 ? string.Empty : $"Informações aplicadas com avisos: {string.Join(" | ", errors)}";
    }

    private async Task UsePessoaDocumentosBatchLocalAsync(IReadOnlyList<PessoaDocumentoSummary> documents)
    {
        PessoaDocumentoErrorText.Text = "OCR inteligente não configurado. Usando OCR local experimental...";
        var processed = 0;
        foreach (var document in documents)
        {
            if (!File.Exists(document.StoragePath) && string.IsNullOrWhiteSpace(document.OcrTextoExtraido))
            {
                continue;
            }

            var rawText = string.IsNullOrWhiteSpace(document.OcrTextoExtraido)
                ? await ExtractPessoaDocumentoTextForFormAsync(document.StoragePath, GuessPessoaDocumentoContentType(document.StoragePath))
                : document.OcrTextoExtraido;

            if (string.IsNullOrWhiteSpace(rawText))
            {
                continue;
            }

            var owner = NormalizeDocumentoDeFromDisplay(document.DocumentoDe);
            ApplyPessoaDocumentoOcrTextToForm(document.Tipo, owner, rawText);
            ApplyPessoaDocumentoOcrIdentityFallbackToForm(owner, rawText);
            await StorePessoaDocumentoOcrResultAsync(document, rawText, succeeded: true);
            processed++;
        }

        await SavePessoaAfterBatchDocumentDataAsync();
        PessoaDocumentoErrorText.Text = processed > 0 ? string.Empty : "Nenhuma informação foi encontrada nos documentos.";
    }

    private static string BuildBatchConfirmationText(
        IReadOnlyList<(PessoaDocumentoSummary Document, GeminiDocumentData Data)> results,
        IReadOnlyList<(PessoaDocumentoSummary Document, string RawText)> localFallbacks,
        IReadOnlyList<string> errors)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Foram encontradas informações nos documentos abaixo.");
        builder.AppendLine("Somente campos em branco serão preenchidos.");
        builder.AppendLine();

        foreach (var (document, data) in results)
        {
            builder.AppendLine($"Documento: {document.Nome}");
            builder.AppendLine(BuildGeminiConfirmationText(data));
            builder.AppendLine();
        }

        foreach (var (document, rawText) in localFallbacks)
        {
            var parsed = PessoaDocumentoOcrParser.ExtractIdentityFields(rawText);
            builder.AppendLine($"Documento: {document.Nome}");
            builder.AppendLine($"Nome: {parsed.Nome ?? "-"}");
            builder.AppendLine($"CPF: {parsed.Cpf ?? "-"}");
            builder.AppendLine($"Data de nascimento: {(parsed.DataNascimento.HasValue ? parsed.DataNascimento.Value.ToString("dd/MM/yyyy") : "-")}");
            builder.AppendLine();
        }

        if (errors.Count > 0)
        {
            builder.AppendLine("Avisos:");
            foreach (var error in errors)
            {
                builder.AppendLine($"- {error}");
            }
            builder.AppendLine();
        }

        builder.AppendLine("Aplicar essas informações agora?");
        return builder.ToString();
    }

    private async Task SavePessoaAfterBatchDocumentDataAsync()
    {
        if (_selectedPessoaId.HasValue && _isPessoaEditing)
        {
            await _rentalManagementService.UpdatePessoaAsync(new UpdatePessoaRequest(_selectedPessoaId.Value, BuildPessoaRequest()));
            await RefreshSelectedPessoaAfterDocumentoOcrAsync(_selectedPessoaId.Value);
            await LoadPessoaDocumentosAsync(_selectedPessoaId);
        }
        else
        {
            RefreshPendingPessoaDocumentosGrid();
        }
    }

    private async Task StorePessoaDocumentoOcrResultAsync(PessoaDocumentoSummary document, string? text, bool succeeded, string? errorMessage = null)
    {
        if (string.IsNullOrWhiteSpace(text) && succeeded)
        {
            return;
        }

        if (document.Id != Guid.Empty)
        {
            await _rentalManagementService.UpdatePessoaDocumentoOcrAsync(new UpdatePessoaDocumentoOcrRequest(
                document.Id,
                text,
                succeeded,
                errorMessage));
            return;
        }

        UpdatePendingPessoaDocumentoOcrText(document, text);
    }

    private void UpdatePendingPessoaDocumentoOcrText(PessoaDocumentoSummary document, string? text)
    {
        if (document.Id != Guid.Empty || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var index = _pendingPessoaDocumentos.FindIndex(draft =>
            string.Equals(draft.Nome, document.Nome, StringComparison.OrdinalIgnoreCase)
            && string.Equals(draft.StoragePath, document.StoragePath, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            _pendingPessoaDocumentos[index] = _pendingPessoaDocumentos[index] with { OcrText = text };
        }
    }

    private void SetPessoaDocumentosBatchUseDataBusy(bool isBusy)
    {
        if (_usarPessoaDocumentosInformacoesButton is not null)
        {
            _usarPessoaDocumentosInformacoesButton.IsEnabled = !isBusy && _isPessoaEditing && HasPessoaDocumentoItems();
            _usarPessoaDocumentosInformacoesButton.Content = isBusy ? "Lendo..." : "Usar informações";
        }

        SavePessoaDocumentoButton.IsEnabled = !isBusy && _isPessoaEditing;
    }

    private bool HasPessoaDocumentoItems() =>
        PessoaDocumentosGrid.ItemsSource?.Cast<object>().OfType<PessoaDocumentoSummary>().Any() == true;

    private static bool IsPessoaDocumentoOcrAlreadyAttempted(PessoaDocumentoSummary document) =>
        document.OcrProcessadoEmUtc.HasValue
        || string.Equals(document.OcrStatus, "Processado", StringComparison.OrdinalIgnoreCase)
        || string.Equals(document.OcrStatus, "Erro", StringComparison.OrdinalIgnoreCase);
}
