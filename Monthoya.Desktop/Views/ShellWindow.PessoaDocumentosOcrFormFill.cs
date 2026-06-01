using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private async Task<string?> ExtractPessoaDocumentoTextForFormAsync(string filePath, string? contentType)
    {
        try
        {
            if (IsPessoaDocumentoPlainText(filePath, contentType))
            {
                var text = await File.ReadAllTextAsync(filePath);
                return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            }

            if (!IsPessoaDocumentoImage(filePath, contentType))
            {
                return null;
            }

            var outputs = new List<string>();
            var originalText = await RunPessoaDocumentoTesseractAsync(filePath);
            if (!string.IsNullOrWhiteSpace(originalText))
            {
                outputs.Add(originalText);
            }

            foreach (var cropPath in CreatePessoaDocumentoRightSideImages(filePath))
            {
                try
                {
                    var cropText = await RunPessoaDocumentoTesseractAsync(cropPath);
                    if (!string.IsNullOrWhiteSpace(cropText))
                    {
                        outputs.Add(cropText);
                    }
                }
                finally
                {
                    TryDeletePessoaDocumentoTempFile(cropPath);
                }
            }

            foreach (var angle in new[] { 90, 180, 270 })
            {
                var rotatedPath = CreatePessoaDocumentoRotatedImage(filePath, angle);
                if (rotatedPath is null)
                {
                    continue;
                }

                try
                {
                    var rotatedText = await RunPessoaDocumentoTesseractAsync(rotatedPath);
                    if (!string.IsNullOrWhiteSpace(rotatedText))
                    {
                        outputs.Add(rotatedText);
                    }
                }
                finally
                {
                    TryDeletePessoaDocumentoTempFile(rotatedPath);
                }
            }

            var combined = string.Join("\n", outputs.Distinct(StringComparer.OrdinalIgnoreCase));
            return string.IsNullOrWhiteSpace(combined) ? null : combined.Trim();
        }
        catch
        {
            return null;
        }
    }

    private void ApplyPessoaDocumentoOcrTextToForm(string documentoTipo, string documentoDe, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var values = ExtractPessoaDocumentoOcrValues(text);
        var tipo = PessoaTipoBox.SelectedValue is Monthoya.Core.Entities.TipoPessoa selectedTipo
            ? selectedTipo
            : Monthoya.Core.Entities.TipoPessoa.Fisica;

        if (tipo == Monthoya.Core.Entities.TipoPessoa.Fisica)
        {
            switch (documentoDe)
            {
                case "empresa_trabalho":
                    if (_pessoaWorkComboBox is not null)
                    {
                        _pessoaWorkComboBox.SelectedItem = "Possui trabalho";
                    }

                    FillIfBlank(PessoaNomeEmpresaTrabalhoBox, values.Empresa ?? values.Nome);
                    FillIfBlank(PessoaTelefoneEmpresaTrabalhoBox, values.Telefone);
                    FillIfBlank(_pessoaCnpjEmpresaTrabalhoBox, values.Cnpj);
                    FillIfBlank(_pessoaEmailEmpresaTrabalhoBox, values.Email);
                    FillIfBlank(_pessoaCargoTrabalhoBox, values.Cargo);
                    FillIfBlank(_pessoaTrabalhoCepBox, values.Cep);
                    FillIfBlank(_pessoaTrabalhoRuaBox, values.Rua ?? values.Endereco);
                    FillIfBlank(_pessoaTrabalhoNumeroBox, values.Numero);
                    FillIfBlank(_pessoaTrabalhoBairroBox, values.Bairro);
                    FillIfBlank(_pessoaTrabalhoCidadeBox, values.Cidade);
                    FillIfBlank(_pessoaTrabalhoEstadoBox, values.Estado);
                    break;
                case "conjuge":
                    FillIfBlank(PessoaConjugeNomeBox, values.Nome);
                    FillIfBlank(PessoaConjugeCpfBox, values.Cpf);
                    FillIfBlank(PessoaConjugeRgBox, values.Rg);
                    FillIfBlank(PessoaConjugeProfissaoBox, values.Profissao);
                    FillIfBlank(PessoaConjugeNacionalidadeBox, values.Nacionalidade);
                    FillDateIfBlank(PessoaConjugeDataNascimentoBox, values.DataNascimento);
                    break;
                case "trabalho_conjuge":
                    if (_pessoaConjugeWorkComboBox is not null)
                    {
                        _pessoaConjugeWorkComboBox.SelectedItem = "Possui trabalho";
                    }

                    FillIfBlank(_pessoaConjugeNomeEmpresaTrabalhoBox, values.Empresa ?? values.Nome);
                    FillIfBlank(_pessoaConjugeCnpjEmpresaTrabalhoBox, values.Cnpj);
                    FillIfBlank(_pessoaConjugeTelefoneEmpresaTrabalhoBox, values.Telefone);
                    FillIfBlank(_pessoaConjugeEmailEmpresaTrabalhoBox, values.Email);
                    FillIfBlank(_pessoaConjugeCargoTrabalhoBox, values.Cargo);
                    FillIfBlank(_pessoaConjugeEmpresaCepBox, values.Cep);
                    FillIfBlank(_pessoaConjugeEmpresaRuaBox, values.Rua ?? values.Endereco);
                    FillIfBlank(_pessoaConjugeEmpresaNumeroBox, values.Numero);
                    FillIfBlank(_pessoaConjugeEmpresaBairroBox, values.Bairro);
                    FillIfBlank(_pessoaConjugeEmpresaCidadeBox, values.Cidade);
                    FillIfBlank(_pessoaConjugeEmpresaEstadoBox, values.Estado);
                    break;
                case "pessoa":
                    // For personal identity documents, the old generic parser may briefly fill
                    // a bad rotated/cropped name. The safer fallback parser fills Nome, CPF and
                    // Data de nascimento after this method. Keep only RG and secondary fields here.
                    FillIfBlank(PessoaRgBox, values.Rg);
                    FillIfBlank(PessoaEstadoCivilBox, values.EstadoCivil);
                    FillIfBlank(PessoaNacionalidadeBox, values.Nacionalidade);
                    FillIfBlank(PessoaProfissaoBox, values.Profissao);
                    if (IsResidencePessoaDocumento(documentoTipo))
                    {
                        FillIfBlank(PessoaCepBox, values.Cep);
                        FillIfBlank(PessoaRuaBox, values.Rua ?? values.Endereco);
                        FillIfBlank(PessoaNumeroBox, values.Numero);
                        FillIfBlank(PessoaBairroBox, values.Bairro);
                        FillIfBlank(PessoaCidadeBox, values.Cidade);
                        FillIfBlank(PessoaEstadoBox, values.Estado);
                    }
                    break;
                default:
                    break;
            }

            UpdatePessoaConditionalSections();
            return;
        }

        switch (documentoDe)
        {
            case "empresa":
                FillIfBlank(PessoaNomeBox, values.Empresa ?? values.Nome);
                FillIfBlank(PessoaDocumentoBox, values.Cnpj);
                FillIfBlank(_pessoaNomeFantasiaBox, values.NomeFantasia);
                FillIfBlank(PessoaEmpresaCepBox, values.Cep);
                FillIfBlank(PessoaEmpresaRuaBox, values.Rua ?? values.Endereco);
                FillIfBlank(PessoaEmpresaNumeroBox, values.Numero);
                FillIfBlank(PessoaEmpresaBairroBox, values.Bairro);
                FillIfBlank(PessoaEmpresaCidadeBox, values.Cidade);
                FillIfBlank(PessoaEmpresaEstadoBox, values.Estado);
                break;
            case "responsavel":
                FillIfBlank(PessoaResponsavelNomeBox, values.Nome);
                FillIfBlank(PessoaResponsavelCpfBox, values.Cpf);
                FillIfBlank(PessoaResponsavelRgBox, values.Rg);
                FillIfBlank(PessoaResponsavelEmailBox, values.Email);
                FillIfBlank(_pessoaResponsavelCargoBox, values.Cargo);
                FillIfBlank(PessoaResponsavelProfissaoBox, values.Profissao);
                FillIfBlank(PessoaResponsavelNacionalidadeBox, values.Nacionalidade);
                FillIfBlank(PessoaResponsavelEstadoCivilBox, values.EstadoCivil);
                FillDateIfBlank(PessoaResponsavelDataNascimentoBox, values.DataNascimento);
                if (IsResidencePessoaDocumento(documentoTipo))
                {
                    FillIfBlank(PessoaResponsavelCepBox, values.Cep);
                    FillIfBlank(PessoaResponsavelRuaBox, values.Rua ?? values.Endereco);
                    FillIfBlank(PessoaResponsavelNumeroBox, values.Numero);
                    FillIfBlank(PessoaResponsavelBairroBox, values.Bairro);
                    FillIfBlank(PessoaResponsavelCidadeBox, values.Cidade);
                    FillIfBlank(PessoaResponsavelEstadoBox, values.Estado);
                }
                break;
            default:
                break;
        }
    }

    private static PessoaDocumentoOcrValues ExtractPessoaDocumentoOcrValues(string text)
    {
        var normalized = text.Replace("\r", "\n", StringComparison.Ordinal);
        var birthDateText = FindPessoaDocumentoBirthDateText(normalized);
        var cpf = FindPessoaDocumentoCpfFallback(normalized);
        var cnpj = DigitsOnlyOrNull(FindPessoaDocumentoRegex(normalized, @"\b\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}\b"));

        return new PessoaDocumentoOcrValues(
            Nome: FindPessoaDocumentoLabeledValue(normalized, "nome") ?? FindPessoaDocumentoLabeledValue(normalized, "nome civil") ?? FindPessoaDocumentoNameFallbackStrict(normalized) ?? FindPessoaDocumentoNameFallback(normalized),
            Empresa: FindPessoaDocumentoLabeledValue(normalized, "razão social") ?? FindPessoaDocumentoLabeledValue(normalized, "razao social") ?? FindPessoaDocumentoLabeledValue(normalized, "empresa"),
            NomeFantasia: FindPessoaDocumentoLabeledValue(normalized, "nome fantasia"),
            Cpf: cpf,
            Cnpj: cnpj,
            Rg: DigitsOnlyOrNull(FindPessoaDocumentoLabeledValue(normalized, "rg") ?? FindPessoaDocumentoLabeledValue(normalized, "registro geral") ?? FindPessoaDocumentoLabeledValue(normalized, "identidade")) ?? FindPessoaDocumentoRgFallback(normalized, cpf, cnpj),
            Email: FindPessoaDocumentoRegex(normalized, @"[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase),
            Telefone: DigitsOnlyOrNull(FindPessoaDocumentoLabeledValue(normalized, "telefone") ?? FindPessoaDocumentoLabeledValue(normalized, "celular") ?? FindPessoaDocumentoRegex(normalized, @"\(?\d{2}\)?\s?9?\d{4}[\-\s]?\d{4}")),
            Cep: DigitsOnlyOrNull(FindPessoaDocumentoLabeledValue(normalized, "cep") ?? FindPessoaDocumentoRegex(normalized, @"\b\d{5}-?\d{3}\b")),
            Endereco: FindPessoaDocumentoLabeledValue(normalized, "endereço") ?? FindPessoaDocumentoLabeledValue(normalized, "endereco"),
            Rua: FindPessoaDocumentoLabeledValue(normalized, "rua") ?? FindPessoaDocumentoLabeledValue(normalized, "logradouro"),
            Numero: FindPessoaDocumentoLabeledValue(normalized, "número") ?? FindPessoaDocumentoLabeledValue(normalized, "numero"),
            Bairro: FindPessoaDocumentoLabeledValue(normalized, "bairro"),
            Cidade: FindPessoaDocumentoLabeledValue(normalized, "cidade") ?? FindPessoaDocumentoLabeledValue(normalized, "município") ?? FindPessoaDocumentoLabeledValue(normalized, "municipio"),
            Estado: NormalizeState(FindPessoaDocumentoLabeledValue(normalized, "estado") ?? FindPessoaDocumentoLabeledValue(normalized, "uf")),
            Nacionalidade: FindPessoaDocumentoLabeledValue(normalized, "nacionalidade"),
            EstadoCivil: FindPessoaDocumentoLabeledValue(normalized, "estado civil"),
            Profissao: FindPessoaDocumentoLabeledValue(normalized, "profissão") ?? FindPessoaDocumentoLabeledValue(normalized, "profissao"),
            Cargo: FindPessoaDocumentoLabeledValue(normalized, "cargo"),
            DataNascimento: ParsePessoaDocumentoBirthDateOnly(birthDateText));
    }

    private static void FillIfBlank(TextBox? textBox, string? value)
    {
        if (textBox is null || !string.IsNullOrWhiteSpace(textBox.Text) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        textBox.Text = value.Trim();
    }

    private static void FillDateIfBlank(DatePicker? datePicker, DateOnly? value)
    {
        if (datePicker is null || datePicker.SelectedDate.HasValue || !value.HasValue)
        {
            return;
        }

        datePicker.SelectedDate = value.Value.ToDateTime(TimeOnly.MinValue);
    }

    private static string? FindPessoaDocumentoLabeledValue(string text, string label)
    {
        var match = Regex.Match(
            text,
            $@"(?im)^\s*{Regex.Escape(label)}\s*[:\-]\s*(?<value>.+?)\s*$",
            RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static string? FindPessoaDocumentoRegex(string text, string pattern, RegexOptions options = RegexOptions.None)
    {
        var match = Regex.Match(text, pattern, options | RegexOptions.CultureInvariant);
        return match.Success ? match.Value.Trim() : null;
    }

    private static string? FindPessoaDocumentoCpfFallback(string text)
    {
        var labeled = Regex.Match(
            text,
            @"(?is)\bcpf\b\D{0,35}(?<value>\d[\d\.\-\s]{8,25}\d)",
            RegexOptions.CultureInvariant);
        var candidate = labeled.Success
            ? labeled.Groups["value"].Value
            : FindPessoaDocumentoRegex(text, @"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b");

        var digits = DigitsOnlyOrNull(candidate);
        return digits?.Length == 11 ? digits : null;
    }

    private static string? FindPessoaDocumentoBirthDateFallback(string text)
    {
        var matches = Regex.Matches(text, @"\b\d{1,2}[/-]\d{1,2}[/-]\d{4}\b", RegexOptions.CultureInvariant)
            .Select(match => match.Value)
            .ToList();

        foreach (var candidate in matches)
        {
            var parsed = ParsePessoaDocumentoDateOnly(candidate);
            if (parsed.HasValue && parsed.Value.Year <= DateTime.Today.Year - 15)
            {
                return candidate;
            }
        }

        var splitDate = Regex.Match(text, @"(?m)^\s*(?<day>\d{1,2})\s*$\s*^\s*(?<monthYear>\d{1,2}[/-]\d{4})\s*$", RegexOptions.CultureInvariant);
        if (splitDate.Success)
        {
            return $"{splitDate.Groups["day"].Value}/{splitDate.Groups["monthYear"].Value}";
        }

        return null;
    }

    private static string? FindPessoaDocumentoBirthDateText(string text)
    {
        var lines = text
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            if (!line.Contains("nasc", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var context = string.Join(" ", lines.Skip(index).Take(3));
            var date = FindPessoaDocumentoDateInText(context);
            if (date is not null)
            {
                return date;
            }
        }

        return FindPessoaDocumentoBirthDateFallback(text);
    }

    private static string? FindPessoaDocumentoDateInText(string text)
    {
        var match = Regex.Match(text, @"\b\d{1,2}[/-]\d{1,2}[/-]\d{4}\b", RegexOptions.CultureInvariant);
        return match.Success ? match.Value : null;
    }

    private static string? FindPessoaDocumentoNameFallbackStrict(string text)
    {
        var ignoredWords = new[]
        {
            "BRASIL", "VALIDA", "TERRITORIO", "NACIONAL", "REPUBLICA",
            "FEDERATIVA", "IDENTIDADE", "CARTEIRA", "DATA", "NASCIMENTO",
            "NATURALIDADE", "FILIACAO", "ORGAO", "EXPEDIDOR", "VIA", "CPF"
        };

        return text
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(line => Regex.Replace(line, @"[^\p{L}\s]", " ").Trim())
            .Select(line => Regex.Replace(line, @"\s+", " ").Trim())
            .Where(line => line.Length is >= 8 and <= 60)
            .Where(line => line.Count(char.IsLetter) >= 8)
            .Where(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2)
            .Where(line => !Regex.IsMatch(line, @"[a-z]{2,}", RegexOptions.CultureInvariant))
            .Where(line => !Regex.IsMatch(line, @"\b\p{L}\b", RegexOptions.CultureInvariant))
            .Where(line => !line.Contains("WOW", StringComparison.OrdinalIgnoreCase))
            .Where(line => !ignoredWords.Any(word => line.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length)
            .ThenByDescending(line => line.Length)
            .FirstOrDefault();
    }

    private static string? FindPessoaDocumentoNameFallback(string text)
    {
        var ignoredWords = new[]
        {
            "BRASIL", "VALIDA", "TERRITORIO", "NACIONAL", "REPUBLICA",
            "FEDERATIVA", "IDENTIDADE", "CARTEIRA", "DATA", "NASCIMENTO",
            "NATURALIDADE", "FILIACAO", "ORGAO", "EXPEDIDOR", "VIA", "CPF"
        };

        return text
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(line => Regex.Replace(line, @"[^A-Za-zÀ-ÿ\s]", " ").Trim())
            .Where(line => line.Count(char.IsLetter) >= 8)
            .Where(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2)
            .Where(line => !ignoredWords.Any(word => line.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(line => line.Length)
            .FirstOrDefault();
    }

    private static string? FindPessoaDocumentoRgFallback(string text, string? cpf, string? cnpj)
    {
        var matches = Regex.Matches(text, @"\b\d{1,2}\.?\d{3}\.?\d{3}-?[\dXx]\b|\b\d{7,10}-?[\dXx]?\b");
        foreach (Match match in matches)
        {
            var digits = DigitsOnlyOrNull(match.Value);
            if (string.IsNullOrWhiteSpace(digits)
                || digits.Length is < 7 or > 10
                || string.Equals(digits, cpf, StringComparison.Ordinal)
                || string.Equals(digits, cnpj, StringComparison.Ordinal))
            {
                continue;
            }

            return digits;
        }

        return null;
    }

    private static string? DigitsOnlyOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = Regex.Replace(value, @"\D", string.Empty);
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    private static DateOnly? ParsePessoaDocumentoDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd" };
        return DateOnly.TryParseExact(value.Trim(), formats, CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static DateOnly? ParsePessoaDocumentoBirthDateOnly(string? value)
    {
        var date = ParsePessoaDocumentoDateOnly(value);
        if (!date.HasValue)
        {
            return null;
        }

        var currentYear = DateTime.Today.Year;
        return date.Value.Year is >= 1900 && date.Value.Year <= currentYear - 10
            ? date
            : null;
    }

    private static bool IsPessoaDocumentoPlainText(string storagePath, string? contentType) =>
        contentType?.StartsWith("text/", StringComparison.OrdinalIgnoreCase) == true
        || Path.GetExtension(storagePath).Equals(".txt", StringComparison.OrdinalIgnoreCase);

    private static bool IsPessoaDocumentoImage(string storagePath, string? contentType) =>
        contentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true
        || Path.GetExtension(storagePath).Equals(".png", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(storagePath).Equals(".jpg", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(storagePath).Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(storagePath).Equals(".tif", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(storagePath).Equals(".tiff", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(storagePath).Equals(".bmp", StringComparison.OrdinalIgnoreCase);

    private static bool IsPessoaDocumentoPdf(string storagePath, string? contentType) =>
        contentType?.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) == true
        || Path.GetExtension(storagePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    private static async Task<string?> RunPessoaDocumentoTesseractAsync(string filePath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "tesseract",
            Arguments = $"\"{filePath}\" stdout -l por+eng",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(startInfo);
        if (process is null)
        {
            return null;
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        var output = await outputTask;
        return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output.Trim() : null;
    }

    private static string? CreatePessoaDocumentoRotatedImage(string filePath, double angle)
    {
        try
        {
            using var imageStream = File.OpenRead(filePath);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = imageStream;
            bitmap.EndInit();
            bitmap.Freeze();

            var rotated = new TransformedBitmap(bitmap, new RotateTransform(angle));
            rotated.Freeze();

            var tempPath = Path.Combine(Path.GetTempPath(), $"monthoya-ocr-{Guid.NewGuid():N}.png");
            using var stream = File.Create(tempPath);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rotated));
            encoder.Save(stream);
            return tempPath;
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<string> CreatePessoaDocumentoRightSideImages(string filePath)
    {
        var tempFiles = new List<string>();
        try
        {
            using var imageStream = File.OpenRead(filePath);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = imageStream;
            bitmap.EndInit();
            bitmap.Freeze();

            var cropRect = new Int32Rect(
                Math.Max(0, (int)(bitmap.PixelWidth * 0.49)),
                Math.Max(0, (int)(bitmap.PixelHeight * 0.04)),
                Math.Max(1, (int)(bitmap.PixelWidth * 0.49)),
                Math.Max(1, (int)(bitmap.PixelHeight * 0.92)));
            var cropped = new CroppedBitmap(bitmap, cropRect);
            cropped.Freeze();

            foreach (var angle in new[] { 0, 90, 270 })
            {
                BitmapSource source = cropped;
                if (angle != 0)
                {
                    var rotated = new TransformedBitmap(cropped, new RotateTransform(angle));
                    rotated.Freeze();
                    source = rotated;
                }

                var tempPath = Path.Combine(Path.GetTempPath(), $"monthoya-ocr-crop-{Guid.NewGuid():N}.png");
                using var stream = File.Create(tempPath);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(stream);
                tempFiles.Add(tempPath);
            }
        }
        catch
        {
            foreach (var tempFile in tempFiles)
            {
                TryDeletePessoaDocumentoTempFile(tempFile);
            }

            return [];
        }

        return tempFiles;
    }

    private static void TryDeletePessoaDocumentoTempFile(string filePath)
    {
        try
        {
            File.Delete(filePath);
        }
        catch
        {
        }
    }

    private static string? NormalizeState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim().ToUpperInvariant();
        return trimmed.Length >= 2 ? trimmed[..2] : trimmed;
    }

    private sealed record PessoaDocumentoOcrValues(
        string? Nome,
        string? Empresa,
        string? NomeFantasia,
        string? Cpf,
        string? Cnpj,
        string? Rg,
        string? Email,
        string? Telefone,
        string? Cep,
        string? Endereco,
        string? Rua,
        string? Numero,
        string? Bairro,
        string? Cidade,
        string? Estado,
        string? Nacionalidade,
        string? EstadoCivil,
        string? Profissao,
        string? Cargo,
        DateOnly? DataNascimento);
}
