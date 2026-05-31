using System.Globalization;
using System.Text.RegularExpressions;

namespace Monthoya.Desktop.Views;

internal sealed record PessoaDocumentoOcrParseResult(
    string? Nome = null,
    string? Cpf = null,
    string? Rg = null,
    DateOnly? DataNascimento = null);

internal static class PessoaDocumentoOcrParser
{
    internal static PessoaDocumentoOcrParseResult ExtractIdentityFields(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new PessoaDocumentoOcrParseResult();
        }

        var normalized = text.Replace("\r", "\n", StringComparison.Ordinal);
        return new PessoaDocumentoOcrParseResult(
            Nome: null,
            Cpf: ExtractCpf(normalized),
            Rg: null,
            DataNascimento: null);
    }

    private static string? ExtractCpf(string text)
    {
        foreach (Match match in Regex.Matches(text, @"\b\d{3}[\.\s]?\d{3}[\.\s]?\d{3}[\-\s]?\d{2}\b", RegexOptions.CultureInvariant))
        {
            var digits = OnlyDigits(match.Value);
            if (digits?.Length == 11)
            {
                return digits;
            }
        }

        return null;
    }

    private static string? OnlyDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = Regex.Replace(value, @"\D", string.Empty);
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }
}
