using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Monthoya.Core.Services;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private bool _pessoaDocumentoOcrDebugApplied;
    private TextBox? _pessoaDocumentoOcrDebugBox;
    private TextBlock? _pessoaDocumentoOcrDebugTitle;

    private static readonly bool PessoaDocumentoOcrDebugHandlerRegistered = RegisterPessoaDocumentoOcrDebugHandler();

    private static bool RegisterPessoaDocumentoOcrDebugHandler()
    {
        EventManager.RegisterClassHandler(
            typeof(ShellWindow),
            LoadedEvent,
            new RoutedEventHandler((sender, _) => ((ShellWindow)sender).ApplyPessoaDocumentoOcrDebugPanel()));

        return true;
    }

    private void ApplyPessoaDocumentoOcrDebugPanel()
    {
        _ = PessoaDocumentoOcrDebugHandlerRegistered;

        if (_pessoaDocumentoOcrDebugApplied)
        {
            return;
        }

        _pessoaDocumentoOcrDebugApplied = true;
        Dispatcher.BeginInvoke(() =>
        {
            CreatePessoaDocumentoOcrDebugPanel();
            UpdatePessoaDocumentoOcrDebugPanel();
        }, DispatcherPriority.ApplicationIdle);

        PessoaDocumentosGrid.SelectionChanged += (_, _) => UpdatePessoaDocumentoOcrDebugPanel();
        PessoaDocumentosGrid.Items.CurrentChanged += (_, _) => UpdatePessoaDocumentoOcrDebugPanel();
    }

    private void CreatePessoaDocumentoOcrDebugPanel()
    {
        if (_pessoaDocumentoOcrDebugBox is not null || PessoaDocumentosGrid.Parent is not Grid documentsGrid)
        {
            return;
        }

        var editor = documentsGrid.Children
            .OfType<ScrollViewer>()
            .FirstOrDefault(viewer => viewer.Tag as string == "PessoaDocumentEditor");

        documentsGrid.RowDefinitions.Clear();
        documentsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        documentsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(160) });
        documentsGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        documentsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Grid.SetRow(PessoaDocumentosTitleText, 0);
        Grid.SetRow(PessoaDocumentosGrid, 1);
        if (editor is not null)
        {
            Grid.SetRow(editor, 2);
        }

        var debugPanel = new Border
        {
            BorderThickness = new Thickness(1),
            BorderBrush = TryFindResource("LineBrush") as Brush,
            Background = TryFindResource("SoftBrush") as Brush,
            Padding = new Thickness(8),
            Margin = new Thickness(8, 8, 8, 8)
        };

        var stack = new StackPanel();
        _pessoaDocumentoOcrDebugTitle = new TextBlock
        {
            Text = "Debug OCR",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        };
        _pessoaDocumentoOcrDebugBox = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 11,
            Height = 90,
            Text = "Selecione um documento para ver o texto bruto do OCR."
        };

        stack.Children.Add(_pessoaDocumentoOcrDebugTitle);
        stack.Children.Add(_pessoaDocumentoOcrDebugBox);
        debugPanel.Child = stack;
        Grid.SetRow(debugPanel, 3);
        documentsGrid.Children.Add(debugPanel);
    }

    private void UpdatePessoaDocumentoOcrDebugPanel()
    {
        if (_pessoaDocumentoOcrDebugBox is null)
        {
            return;
        }

        if (PessoaDocumentosGrid.SelectedItem is not PessoaDocumentoSummary document)
        {
            return;
        }

        _pessoaDocumentoOcrDebugBox.Text = BuildPessoaDocumentoOcrDebugText(document);
    }

    private void ShowPessoaDocumentoOcrDebugText(string documentName, string documentoDe, string? rawText)
    {
        Dispatcher.BeginInvoke(() =>
        {
            CreatePessoaDocumentoOcrDebugPanel();
            if (_pessoaDocumentoOcrDebugBox is null)
            {
                return;
            }

            _pessoaDocumentoOcrDebugBox.Text = BuildPessoaDocumentoOcrDebugText(documentName, documentoDe, rawText);
        }, DispatcherPriority.ApplicationIdle);
    }

    private static string BuildPessoaDocumentoOcrDebugText(PessoaDocumentoSummary document)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Documento: {document.Nome}");
        builder.AppendLine($"Tipo: {document.Tipo}");
        builder.AppendLine($"De: {document.DocumentoDe}");
        builder.AppendLine($"OCR status: {document.OcrStatus}");
        if (!string.IsNullOrWhiteSpace(document.OcrErroMensagem))
        {
            builder.AppendLine($"Erro: {document.OcrErroMensagem}");
        }

        AppendPessoaDocumentoParsedDebug(builder, document.OcrTextoExtraido);
        return builder.ToString().Trim();
    }

    private static string BuildPessoaDocumentoOcrDebugText(string documentName, string documentoDe, string? rawText)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Documento: {documentName}");
        builder.AppendLine($"De: {documentoDe}");
        builder.AppendLine("OCR status: Processado agora");
        AppendPessoaDocumentoParsedDebug(builder, rawText);
        return builder.ToString().Trim();
    }

    private static void AppendPessoaDocumentoParsedDebug(StringBuilder builder, string? rawText)
    {
        var parsed = PessoaDocumentoOcrParser.ExtractIdentityFields(rawText);
        builder.AppendLine();
        builder.AppendLine("Fallback atual do parser:");
        builder.AppendLine($"Nome: {parsed.Nome ?? "-"}");
        builder.AppendLine($"CPF: {parsed.Cpf ?? "-"}");
        builder.AppendLine($"Data de nascimento: {(parsed.DataNascimento.HasValue ? parsed.DataNascimento.Value.ToString("dd/MM/yyyy") : "-")}");
        builder.AppendLine();
        builder.AppendLine("Texto bruto OCR:");
        builder.AppendLine(string.IsNullOrWhiteSpace(rawText) ? "-" : rawText.Trim());
    }
}
