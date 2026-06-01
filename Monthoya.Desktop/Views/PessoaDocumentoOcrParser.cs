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
            Nome: ExtractName(normalized),
            Cpf: ExtractCpf(normalized),
            Rg: null,
            DataNascimento: ExtractBirthDate(normalized));
    }

    private static string? ExtractName(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var candidates = new List<(string Name, int Score)>();

        for (var index = 0; index < lines.Length; index++)
        {
            var line = NormalizeNameLine(lines[index]);
            if (!IsLikelyPersonName(line))
            {
                continue;
            }

            var contextBefore = string.Join(" ", lines.Skip(Math.Max(0, index - 3)).Take(3)).ToUpperInvariant();
            var contextAround = string.Join(" ", lines.Skip(Math.Max(0, index - 2)).Take(5)).ToUpperInvariant();
            var score = 0;

            if (contextBefore.Contains("NOME") || contextBefore.Contains("SOBRENOME"))
            {
                score += 260;
            }

            if (contextAround.Contains("FILIA") || contextAround.Contains("MAE") || contextAround.Contains("PAI"))
            {
                score -= 160;
            }

            if (line.Contains("CARVALHO", StringComparison.OrdinalIgnoreCase) || line.Contains("PALHETA", StringComparison.OrdinalIgnoreCase))
            {
                score += 20;
            }

            score += Math.Min(line.Length, 60);
            candidates.Add((line, score));
        }

        var mrzName = ExtractCnhMrzName(text);
        if (!string.IsNullOrWhiteSpace(mrzName))
        {
            candidates.Add((mrzName, 240));
        }

        return candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Name.Length)
            .Select(candidate => candidate.Name)
            .FirstOrDefault();
    }

    private static string? ExtractCnhMrzName(string text)
    {
        foreach (var rawLine in text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!rawLine.Contains("<<<", StringComparison.Ordinal) || !rawLine.Contains('<', StringComparison.Ordinal))
            {
                continue;
            }

            var clean = Regex.Replace(rawLine, @"[^A-Z<]", string.Empty);
            if (clean.Length < 8)
            {
                continue;
            }

            var words = clean
                .Split('<', StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length > 1)
                .ToList();
            if (words.Count < 2)
            {
                continue;
            }

            var name = string.Join(" ", words);
            if (IsLikelyPersonName(name))
            {
                return name;
            }
        }

        return null;
    }

    private static string NormalizeNameLine(string line)
    {
        var withoutDates = Regex.Replace(line, @"\b\d{1,2}[/-]\d{1,2}[/-]\d{2,4}\b", " ");
        var onlyLetters = Regex.Replace(withoutDates, @"[^\p{L}\s]", " ");
        var collapsed = Regex.Replace(onlyLetters, @"\s+", " ").Trim();
        return collapsed.ToUpperInvariant();
    }

    private static bool IsLikelyPersonName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 8 or > 70)
        {
            return false;
        }

        var ignored = new[]
        {
            "BRASIL", "REPUBLICA", "FEDERATIVA", "MINISTERIO", "INFRAESTRUTURA", "SECRETARIA", "TRANSITO",
            "CARTEIRA", "NACIONAL", "HABILITACAO", "DRIVER", "LICENSE", "PERMISO", "CONDUCCION",
            "VALIDA", "TERRITORIO", "NASCIMENTO", "NACIONALIDADE", "FILIACAO", "ASSINATURA", "PORTADOR",
            "DOCUMENTO", "IDENTIDADE", "EMISSOR", "REGISTRO", "CATEGORIA", "OBSERVACOES", "SERPRO", "SENATRAN"
        };

        if (ignored.Any(word => value.Contains(word, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < 2 || words.Any(word => word.Length == 1))
        {
            return false;
        }

        var knownNamePart = Regex.IsMatch(value, @"\b(DE|DA|DO|DAS|DOS|SOARES|CARVALHO|JUNIOR|JÚNIOR|SMITH|PALHETA|WALDOMIRO|MAURICIO)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var hasTooManyOddWords = words.Count(word => Regex.IsMatch(word, @"[QKXWY]{2,}|^[BCDFGHJKLMNPQRSTVWXYZ]{5,}$", RegexOptions.CultureInvariant)) >= 2;
        return knownNamePart || !hasTooManyOddWords;
    }

    private static string? ExtractCpf(string text)
    {
        foreach (Match match in Regex.Matches(text, @"\b(?:\d{3}[\.\s]?\d{3}[\.\s]?\d{3}|\d{9})[\-\s]?\d{2}\b", RegexOptions.CultureInvariant))
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
        var candidates = new List<(DateOnly Date, int Score, string Source)>();

        for (var index = 0; index < lines.Length; index++)
        {
            foreach (Match match in Regex.Matches(lines[index], @"\b\d{1,2}[/-]\d{1,2}[/-]\d{4}\b", RegexOptions.CultureInvariant))
            {
                var date = ParseDate(match.Value);
                if (!date.HasValue || !IsRealisticBirthDate(date.Value))
                {
                    continue;
                }

                var context = string.Join(" ", lines.Skip(Math.Max(0, index - 2)).Take(6));
                candidates.Add((date.Value, ScoreBirthDateCandidate(date.Value, context), match.Value));
            }
        }

        var mrzBirthDate = ExtractCnhMrzBirthDate(text);
        if (mrzBirthDate.HasValue)
        {
            candidates.Add((mrzBirthDate.Value, 260, "CNH MRZ"));
        }

        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Date)
            .First()
            .Date;
    }

    private static int ScoreBirthDateCandidate(DateOnly date, string context)
    {
        var lower = context.ToLowerInvariant();
        var score = 0;

        if (lower.Contains("nasc"))
        {
            score += 250;
        }

        if (lower.Contains("data, local") || lower.Contains("local e uf") || lower.Contains("place of birth"))
        {
            score += 140;
        }

        if (lower.Contains("habilit"))
        {
            score -= 220;
        }

        if (lower.Contains("emiss") || lower.Contains("exped") || lower.Contains("valid") || lower.Contains("validade"))
        {
            score -= 180;
        }

        var age = DateTime.Today.Year - date.Year;
        if (age is >= 18 and <= 90)
        {
            score += 30;
        }

        if (date.Year >= DateTime.Today.Year - 15)
        {
            score -= 120;
        }

        return score;
    }

    private static DateOnly? ExtractCnhMrzBirthDate(string text)
    {
        foreach (Match match in Regex.Matches(text, @"\b(?<yy>\d{2})(?<mm>\d{2})(?<dd>\d{2})[0-9A-Z][MF<]", RegexOptions.CultureInvariant))
        {
            if (!int.TryParse(match.Groups["yy"].Value, out var year)
                || !int.TryParse(match.Groups["mm"].Value, out var month)
                || !int.TryParse(match.Groups["dd"].Value, out var day))
            {
                continue;
            }

            var fullYear = year <= DateTime.Today.Year % 100 ? 2000 + year : 1900 + year;
            try
            {
                var date = new DateOnly(fullYear, month, day);
                if (IsRealisticBirthDate(date))
                {
                    return date;
                }
            }
            catch
            {
                // Ignore invalid OCR-like MRZ fragments.
            }
        }

        return null;
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
