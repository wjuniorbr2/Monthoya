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
            DataNascimento: ExtractBirthDate(normalized));
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

    private static DateOnly? ExtractBirthDate(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var candidates = new List<(DateOnly Date, int Score)>();

        for (var index = 0; index < lines.Length; index++)
        {
            foreach (Match match in Regex.Matches(lines[index], @"\b\d{1,2}[/-]\d{1,2}[/-]\d{4}\b", RegexOptions.CultureInvariant))
            {
                var date = ParseDate(match.Value);
                if (!date.HasValue || !IsRealisticBirthDate(date.Value))
                {
                    continue;
                }

                var context = string.Join(" ", lines.Skip(Math.Max(0, index - 2)).Take(5));
                candidates.Add((date.Value, ScoreBirthDateCandidate(date.Value, context)));
            }
        }

        return candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Date)
            .FirstOrDefault()
            .Date;
    }

    private static int ScoreBirthDateCandidate(DateOnly date, string context)
    {
        var lower = context.ToLowerInvariant();
        var score = 0;

        if (lower.Contains("nasc"))
        {
            score += 100;
        }

        if (lower.Contains("emiss") || lower.Contains("exped") || lower.Contains("valid"))
        {
            score -= 100;
        }

        var age = DateTime.Today.Year - date.Year;
        if (age is >= 18 and <= 90)
        {
            score += 20;
        }

        if (date.Year >= DateTime.Today.Year - 15)
        {
            score -= 80;
        }

        return score;
    }

    private static bool IsRealisticBirthDate(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (date > today)
        {
            return false;
        }

        var age = today.Year - date.Year;
        if (date > today.AddYears(-age))
        {
            age--;
        }

        return age is >= 16 and <= 120;
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy" };
        return DateOnly.TryParseExact(value.Trim(), formats, CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var date)
            ? date
            : null;
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
