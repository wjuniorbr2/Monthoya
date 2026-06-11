namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
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
}
