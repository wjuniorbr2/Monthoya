using Monthoya.Core.Entities;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
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
}
