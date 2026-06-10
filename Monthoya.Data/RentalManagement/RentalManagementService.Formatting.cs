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

    private static string? FormatCpfCnpjForDisplay(TipoPessoa tipoPessoa, string? value)
    {
        var digits = DigitsOrNull(value);
        return tipoPessoa == TipoPessoa.Juridica ? FormatCnpj(digits) : FormatCpf(digits);
    }

    private static string? FormatCpf(string? value)
    {
        var digits = DigitsOrNull(value);
        if (digits is null)
        {
            return null;
        }

        digits = digits.Length > 11 ? digits[..11] : digits;
        return digits.Length switch
        {
            <= 3 => digits,
            <= 6 => $"{digits[..3]}.{digits[3..]}",
            <= 9 => $"{digits[..3]}.{digits.Substring(3, 3)}.{digits[6..]}",
            _ => $"{digits[..3]}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits[9..]}"
        };
    }

    private static string? FormatCnpj(string? value)
    {
        var digits = DigitsOrNull(value);
        if (digits is null)
        {
            return null;
        }

        digits = digits.Length > 14 ? digits[..14] : digits;
        return digits.Length switch
        {
            <= 2 => digits,
            <= 5 => $"{digits[..2]}.{digits[2..]}",
            <= 8 => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits[5..]}",
            <= 12 => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}/{digits[8..]}",
            _ => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}/{digits.Substring(8, 4)}-{digits[12..]}"
        };
    }

    private static string? FormatCpfCnpjByLength(string? value)
    {
        var digits = DigitsOrNull(value);
        if (digits is null)
        {
            return null;
        }

        return digits.Length > 11 ? FormatCnpj(digits) : FormatCpf(digits);
    }

    private static string? FormatPixChaveForDisplay(string? value, PixChaveTipo? tipo) =>
        tipo switch
        {
            PixChaveTipo.Cpf => FormatCpf(value),
            PixChaveTipo.Cnpj => FormatCnpj(value),
            PixChaveTipo.Telefone => FormatPhoneForDisplay(value),
            _ => value
        };

    private static string? FormatPhoneForDisplay(string? value)
    {
        var digits = DigitsOrNull(value);
        if (digits is null)
        {
            return null;
        }

        digits = digits.Length > 11 ? digits[..11] : digits;
        if (digits.Length <= 2)
        {
            return digits;
        }

        var ddd = digits[..2];
        var number = digits[2..];
        if (number.Length <= 4)
        {
            return $"({ddd}) {number}";
        }

        return number.Length <= 8
            ? $"({ddd}) {number[..4]}-{number[4..]}"
            : $"({ddd}) {number[..5]}-{number[5..]}";
    }

    private static string? FormatCepForDisplay(string? value)
    {
        var digits = DigitsOrNull(value);
        if (digits is null)
        {
            return null;
        }

        digits = digits.Length > 8 ? digits[..8] : digits;
        return digits.Length <= 5 ? digits : $"{digits[..5]}-{digits[5..]}";
    }

    private static string? FormatRgForDisplay(string? value)
    {
        var digits = DigitsOrNull(value);
        if (digits is null)
        {
            return null;
        }

        digits = digits.Length > 9 ? digits[..9] : digits;
        return digits.Length switch
        {
            <= 2 => digits,
            <= 5 => $"{digits[..2]}.{digits[2..]}",
            <= 8 => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits[5..]}",
            _ => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}-{digits[8..]}"
        };
    }

    private static string? NormalizeState(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string GetEnumLabel<T>(T value) where T : struct, Enum => value.ToString();
}

