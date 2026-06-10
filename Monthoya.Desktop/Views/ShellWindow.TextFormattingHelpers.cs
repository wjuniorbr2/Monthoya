using Monthoya.Core.Entities;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private static string OnlyDigits(string? value) =>
        Regex.Replace(value ?? string.Empty, @"\D", string.Empty);

    private static string FormatCpfOrCnpj(string digits) =>
        digits.Length > 11 ? FormatCnpj(digits) : FormatCpf(digits);

    private string FormatPessoaDocumento(string digits) =>
        PessoaTipoBox.SelectedValue is TipoPessoa.Juridica ? FormatCnpj(digits) : FormatCpf(digits);

    private static string FormatCpf(string digits)
    {
        digits = digits.Length > 11 ? digits[..11] : digits;
        return digits.Length switch
        {
            <= 3 => digits,
            <= 6 => $"{digits[..3]}.{digits[3..]}",
            <= 9 => $"{digits[..3]}.{digits.Substring(3, 3)}.{digits[6..]}",
            _ => $"{digits[..3]}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits[9..]}"
        };
    }

    private static string FormatCnpj(string digits)
    {
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

    private static string FormatBrazilPhone(string digits)
    {
        digits = digits.Length > 11 ? digits[..11] : digits;
        if (digits.Length <= 2) return digits.Length == 0 ? string.Empty : $"({digits}";
        var ddd = digits[..2];
        var number = digits[2..];
        if (number.Length <= 4) return $"({ddd}) {number}";
        return number.Length <= 8
            ? $"({ddd}) {number[..4]}-{number[4..]}"
            : $"({ddd}) {number[..5]}-{number[5..]}";
    }

    private static string FormatCep(string digits)
    {
        digits = digits.Length > 8 ? digits[..8] : digits;
        return digits.Length <= 5 ? digits : $"{digits[..5]}-{digits[5..]}";
    }

    private static string FormatRg(string digits)
    {
        digits = digits.Length > 9 ? digits[..9] : digits;
        if (digits.Length <= 1)
        {
            return digits;
        }

        var verifier = digits[^1..];
        var body = digits[..^1];
        var groups = new List<string>();
        while (body.Length > 3)
        {
            groups.Insert(0, body[^3..]);
            body = body[..^3];
        }

        if (body.Length > 0)
        {
            groups.Insert(0, body);
        }

        return $"{string.Join(".", groups)}-{verifier}";
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var culture = CultureInfo.GetCultureInfo("pt-BR");
        return decimal.TryParse(value, NumberStyles.Number | NumberStyles.Currency, culture, out var parsed)
            || decimal.TryParse(value, NumberStyles.Number | NumberStyles.Currency, CultureInfo.InvariantCulture, out parsed)
            ? parsed
            : null;
    }

    private static bool ContainsSearch(string? query, params string?[] values)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        var normalizedQuery = NormalizeSearch(query);
        return values.Any(value => NormalizeSearch(value).Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeSearch(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace(".", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace("/", string.Empty, StringComparison.Ordinal)
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .Trim();
    private sealed record ViaCepResult(
        [property: JsonPropertyName("logradouro")] string? Logradouro,
        [property: JsonPropertyName("complemento")] string? Complemento,
        [property: JsonPropertyName("bairro")] string? Bairro,
        [property: JsonPropertyName("localidade")] string? Localidade,
        [property: JsonPropertyName("uf")] string? Uf,
        [property: JsonPropertyName("erro")] bool? Erro);

    private sealed record IbgeCity([property: JsonPropertyName("nome")] string Nome);
}
