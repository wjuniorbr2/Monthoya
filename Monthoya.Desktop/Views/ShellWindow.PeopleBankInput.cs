using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private void ConfigurePessoaBankInputBehavior()
    {
        if (_pessoaBankInputBehaviorConfigured)
        {
            return;
        }

        _pessoaBankInputBehaviorConfigured = true;
        RegisterDigitsOnly(_pessoaAgenciaNumeroBox, 6);
        RegisterDigitsOnly(_pessoaContaNumeroBox, 14);
        RegisterDigitOrX(_pessoaAgenciaDigitoBox);
        RegisterDigitOrX(_pessoaContaDigitoBox);
        RegisterCpfCnpjFormatter(_pessoaTitularDocumentoBox);
        RegisterPixFormatter(_pessoaPixTipoBox, _pessoaPixChaveBox);

        RegisterDigitsOnly(_pessoaResponsavelAgenciaNumeroBox, 6);
        RegisterDigitsOnly(_pessoaResponsavelContaNumeroBox, 14);
        RegisterDigitOrX(_pessoaResponsavelAgenciaDigitoBox);
        RegisterDigitOrX(_pessoaResponsavelContaDigitoBox);
        RegisterCpfCnpjFormatter(_pessoaResponsavelTitularDocumentoBox);
        RegisterPixFormatter(_pessoaResponsavelPixTipoBox, _pessoaResponsavelPixChaveBox);
    }

    private void RegisterDigitsOnly(TextBox? textBox, int maxDigits)
    {
        if (textBox is null)
        {
            return;
        }

        textBox.PreviewTextInput += (_, e) => e.Handled = e.Text.Any(ch => !char.IsDigit(ch));
        textBox.TextChanged += (_, _) => FormatPessoaBankTextBox(textBox, OnlyDigits(textBox.Text, maxDigits));
        DataObject.AddPastingHandler(textBox, (_, e) => ReplacePastedText(e, value => OnlyDigits(value, maxDigits)));
    }

    private void RegisterDigitOrX(TextBox? textBox)
    {
        if (textBox is null)
        {
            return;
        }

        textBox.PreviewTextInput += (_, e) => e.Handled = e.Text.Any(ch => !char.IsDigit(ch) && char.ToUpperInvariant(ch) != 'X');
        textBox.TextChanged += (_, _) => FormatPessoaBankTextBox(textBox, NormalizeDigitOrX(textBox.Text));
        DataObject.AddPastingHandler(textBox, (_, e) => ReplacePastedText(e, NormalizeDigitOrX));
    }

    private void RegisterCpfCnpjFormatter(TextBox? textBox)
    {
        if (textBox is null)
        {
            return;
        }

        textBox.PreviewTextInput += (_, e) => e.Handled = e.Text.Any(ch => !char.IsDigit(ch));
        textBox.TextChanged += (_, _) => FormatPessoaBankTextBox(textBox, FormatCpfCnpjDocument(textBox.Text));
        DataObject.AddPastingHandler(textBox, (_, e) => ReplacePastedText(e, FormatCpfCnpjDocument));
    }

    private void RegisterPixFormatter(ComboBox? pixTipoBox, TextBox? pixChaveBox)
    {
        if (pixTipoBox is null || pixChaveBox is null)
        {
            return;
        }

        pixChaveBox.PreviewTextInput += (_, e) =>
        {
            if (GetPessoaPixType(pixTipoBox) is Monthoya.Core.Entities.PixChaveTipo.Cpf
                or Monthoya.Core.Entities.PixChaveTipo.Cnpj
                or Monthoya.Core.Entities.PixChaveTipo.Telefone)
            {
                e.Handled = e.Text.Any(ch => !char.IsDigit(ch));
            }
        };
        pixChaveBox.TextChanged += (_, _) => FormatPessoaBankTextBox(pixChaveBox, FormatPixByType(pixChaveBox.Text, GetPessoaPixType(pixTipoBox)));
        pixTipoBox.SelectionChanged += (_, _) => FormatPessoaBankTextBox(pixChaveBox, FormatPixByType(pixChaveBox.Text, GetPessoaPixType(pixTipoBox)));
        DataObject.AddPastingHandler(pixChaveBox, (_, e) => ReplacePastedText(e, value => FormatPixByType(value, GetPessoaPixType(pixTipoBox))));
    }

    private void FormatPessoaBankTextBox(TextBox textBox, string formatted)
    {
        if (_isFormattingPessoaBankFields || textBox.Text == formatted)
        {
            return;
        }

        _isFormattingPessoaBankFields = true;
        textBox.Text = formatted;
        textBox.CaretIndex = textBox.Text.Length;
        _isFormattingPessoaBankFields = false;
    }

    private static void ReplacePastedText(DataObjectPastingEventArgs e, Func<string, string> formatter)
    {
        if (!e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        var text = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;
        e.DataObject = new DataObject(DataFormats.Text, formatter(text));
    }

    private static Monthoya.Core.Entities.PixChaveTipo? GetPessoaPixType(ComboBox comboBox) =>
        comboBox.SelectedValue is Monthoya.Core.Entities.PixChaveTipo value ? value : null;

    private static string OnlyDigits(string? value, int maxDigits)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).Take(maxDigits).ToArray());
        return digits;
    }

    private static string NormalizeDigitOrX(string? value)
    {
        var first = (value ?? string.Empty)
            .Select(char.ToUpperInvariant)
            .FirstOrDefault(ch => char.IsDigit(ch) || ch == 'X');
        return first == default ? string.Empty : first.ToString();
    }

    private static string FormatCpfCnpjDocument(string? value)
    {
        var digits = OnlyDigits(value, 14);
        return digits.Length <= 11 ? FormatCpfDigits(digits) : FormatCnpjDigits(digits);
    }

    private static string FormatPixByType(string? value, Monthoya.Core.Entities.PixChaveTipo? pixType) =>
        pixType switch
        {
            Monthoya.Core.Entities.PixChaveTipo.Cpf => FormatCpfDigits(OnlyDigits(value, 11)),
            Monthoya.Core.Entities.PixChaveTipo.Cnpj => FormatCnpjDigits(OnlyDigits(value, 14)),
            Monthoya.Core.Entities.PixChaveTipo.Telefone => FormatPhoneDigits(OnlyDigits(value, 11)),
            Monthoya.Core.Entities.PixChaveTipo.Email => (value ?? string.Empty).Trim(),
            _ => value ?? string.Empty
        };

    private static string FormatCpfDigits(string digits) =>
        digits.Length switch
        {
            <= 3 => digits,
            <= 6 => $"{digits[..3]}.{digits[3..]}",
            <= 9 => $"{digits[..3]}.{digits.Substring(3, 3)}.{digits[6..]}",
            _ => $"{digits[..3]}.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-{digits[9..]}"
        };

    private static string FormatCnpjDigits(string digits) =>
        digits.Length switch
        {
            <= 2 => digits,
            <= 5 => $"{digits[..2]}.{digits[2..]}",
            <= 8 => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits[5..]}",
            <= 12 => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}/{digits[8..]}",
            _ => $"{digits[..2]}.{digits.Substring(2, 3)}.{digits.Substring(5, 3)}/{digits.Substring(8, 4)}-{digits[12..]}"
        };

    private static string FormatPhoneDigits(string digits)
    {
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
}

