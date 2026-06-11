using System.Globalization;

using System.Text.RegularExpressions;
using Monthoya.Core.Entities;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddStreetSuggestion(ISet<string> suggestions, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            suggestions.Add(value.Trim());
        }
    }

    private static string? NormalizePixChave(string? value, PixChaveTipo? tipo)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return tipo is PixChaveTipo.Cpf or PixChaveTipo.Cnpj or PixChaveTipo.Telefone
            ? DigitsOrNull(value)
            : value.Trim();
    }

    private static string? DigitsOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = Regex.Replace(value, @"\D", string.Empty);
        return digits.Length == 0 ? null : digits;
    }


    private static string NormalizePessoaFisicaNome(string value)
    {
        var normalized = CollapseWhitespace(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return normalized;
        }

        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var words = normalized.ToLower(culture).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lowerCaseWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "da", "de", "di", "do", "das", "dos", "e"
        };

        for (var i = 0; i < words.Length; i++)
        {
            if (i > 0 && lowerCaseWords.Contains(words[i]))
            {
                continue;
            }

            words[i] = culture.TextInfo.ToTitleCase(words[i]);
        }

        return string.Join(' ', words);
    }

    private static string CollapseWhitespace(string value) =>
        string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string? NormalizeState(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string GetEnumLabel<T>(T value) where T : struct, Enum => value.ToString();
}

