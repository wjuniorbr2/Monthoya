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



    private static string? NormalizeState(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string GetEnumLabel<T>(T value) where T : struct, Enum => value.ToString();
}

