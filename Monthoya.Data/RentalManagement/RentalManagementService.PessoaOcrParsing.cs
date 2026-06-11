using System.Text.RegularExpressions;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    private static PessoaOcrValues ExtractPessoaOcrValues(string text)
    {
        var normalized = text.Replace("\r", "\n", StringComparison.Ordinal);
        var cpf = FindRegex(normalized, @"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b");
        var cnpj = FindRegex(normalized, @"\b\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}\b");

        return new PessoaOcrValues(
            FindLabeledValue(normalized, "nome") ?? FindLabeledValue(normalized, "razão social") ?? FindLabeledValue(normalized, "razao social") ?? FindOcrNameFallback(normalized),
            cpf,
            cnpj,
            FindLabeledValue(normalized, "rg") ?? FindOcrRgFallback(normalized, cpf, cnpj),
            FindRegex(normalized, @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase),
            FindRegex(normalized, @"(?:\(?\d{2}\)?\s?)?(?:9\s?)?\d{4}[-\s]?\d{4}"),
            FindRegex(normalized, @"\b\d{5}-?\d{3}\b"),
            FindLabeledValue(normalized, "endereço") ?? FindLabeledValue(normalized, "endereco"));
    }

    private static void FillIfBlank(Func<string?> getCurrent, Action<string> setValue, string? newValue, string fieldName, ICollection<string> filledFields)
    {
        if (!string.IsNullOrWhiteSpace(getCurrent()) || string.IsNullOrWhiteSpace(newValue))
        {
            return;
        }

        setValue(newValue.Trim());
        filledFields.Add(fieldName);
    }

    private static string? FindLabeledValue(string text, string label)
    {
        var match = Regex.Match(
            text,
            $@"(?im)^\s*{Regex.Escape(label)}\s*[:\-]\s*(?<value>.+?)\s*$",
            RegexOptions.CultureInvariant);

        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static string? FindRegex(string text, string pattern, RegexOptions options = RegexOptions.None)
    {
        var match = Regex.Match(text, pattern, options | RegexOptions.CultureInvariant);
        return match.Success ? match.Value.Trim() : null;
    }

    private static string? FindOcrNameFallback(string text)
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

    private static string? FindOcrRgFallback(string text, string? cpf, string? cnpj)
    {
        var matches = Regex.Matches(text, @"\b\d{1,2}\.?\d{3}\.?\d{3}-?[\dXx]\b|\b\d{7,10}-?[\dXx]?\b");
        foreach (Match match in matches)
        {
            var digits = DigitsOrNull(match.Value);
            if (string.IsNullOrWhiteSpace(digits)
                || digits.Length is < 7 or > 10
                || string.Equals(digits, DigitsOrNull(cpf), StringComparison.Ordinal)
                || string.Equals(digits, DigitsOrNull(cnpj), StringComparison.Ordinal))
            {
                continue;
            }

            return digits;
        }

        return null;
    }

    private sealed record PessoaOcrValues(
        string? Nome,
        string? Cpf,
        string? Cnpj,
        string? Rg,
        string? Email,
        string? Telefone,
        string? Cep,
        string? Endereco);
}
