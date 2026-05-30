using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private async Task<string?> ExtractPessoaDocumentoTextForFormAsync(string filePath, string? contentType)
    {
        try
        {
            if (IsPessoaDocumentoPlainText(filePath, contentType))
            {
                var text = await File.ReadAllTextAsync(filePath);
                return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
            }

            if (!IsPessoaDocumentoImage(filePath, contentType))
            {
                return null;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = "tesseract",
                Arguments = $"\"{filePath}\" stdout -l por+eng",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            var output = await outputTask;
            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output.Trim() : null;
        }
        catch
        {
            return null;
        }
    }

    private void ApplyPessoaDocumentoOcrTextToForm(string documentoDe, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var values = ExtractPessoaDocumentoOcrValues(text);
        var tipo = PessoaTipoBox.SelectedValue is Monthoya.Core.Entities.TipoPessoa selectedTipo
            ? selectedTipo
            : Monthoya.Core.Entities.TipoPessoa.Fisica;

        if (tipo == Monthoya.Core.Entities.TipoPessoa.Fisica)
        {
            switch (documentoDe)
            {
                case "empresa_trabalho":
                    FillIfBlank(PessoaNomeEmpresaTrabalhoBox, values.Empresa ?? values.Nome);
                    FillIfBlank(PessoaTelefoneEmpresaTrabalhoBox, values.Telefone);
                    FillIfBlank(_pessoaCnpjEmpresaTrabalhoBox, values.Cnpj);
                    FillIfBlank(_pessoaEmailEmpresaTrabalhoBox, values.Email);
                    FillIfBlank(_pessoaCargoTrabalhoBox, values.Cargo);
                    FillIfBlank(_pessoaTrabalhoCepBox, values.Cep);
                    FillIfBlank(_pessoaTrabalhoRuaBox, values.Rua ?? values.Endereco);
                    FillIfBlank(_pessoaTrabalhoNumeroBox, values.Numero);
                    FillIfBlank(_pessoaTrabalhoBairroBox, values.Bairro);
                    FillIfBlank(_pessoaTrabalhoCidadeBox, values.Cidade);
                    FillIfBlank(_pessoaTrabalhoEstadoBox, values.Estado);
                    break;
                case "conjuge":
                    FillIfBlank(PessoaConjugeNomeBox, values.Nome);
                    FillIfBlank(PessoaConjugeCpfBox, values.Cpf);
                    FillIfBlank(PessoaConjugeRgBox, values.Rg);
                    FillIfBlank(PessoaConjugeTelefoneBox, values.Telefone);
                    FillIfBlank(_pessoaConjugeEmailBox, values.Email);
                    FillIfBlank(PessoaConjugeProfissaoBox, values.Profissao);
                    FillIfBlank(PessoaConjugeNacionalidadeBox, values.Nacionalidade);
                    FillDateIfBlank(PessoaConjugeDataNascimentoBox, values.DataNascimento);
                    break;
                case "trabalho_conjuge":
                    FillIfBlank(_pessoaConjugeNomeEmpresaTrabalhoBox, values.Empresa ?? values.Nome);
                    FillIfBlank(_pessoaConjugeCnpjEmpresaTrabalhoBox, values.Cnpj);
                    FillIfBlank(_pessoaConjugeTelefoneEmpresaTrabalhoBox, values.Telefone);
                    FillIfBlank(_pessoaConjugeEmailEmpresaTrabalhoBox, values.Email);
                    FillIfBlank(_pessoaConjugeCargoTrabalhoBox, values.Cargo);
                    FillIfBlank(_pessoaConjugeEmpresaCepBox, values.Cep);
                    FillIfBlank(_pessoaConjugeEmpresaRuaBox, values.Rua ?? values.Endereco);
                    FillIfBlank(_pessoaConjugeEmpresaNumeroBox, values.Numero);
                    FillIfBlank(_pessoaConjugeEmpresaBairroBox, values.Bairro);
                    FillIfBlank(_pessoaConjugeEmpresaCidadeBox, values.Cidade);
                    FillIfBlank(_pessoaConjugeEmpresaEstadoBox, values.Estado);
                    break;
                default:
                    FillIfBlank(PessoaNomeBox, values.Nome);
                    FillIfBlank(PessoaDocumentoBox, values.Cpf);
                    FillIfBlank(PessoaRgBox, values.Rg);
                    FillIfBlank(PessoaTelefoneBox, values.Telefone);
                    FillIfBlank(PessoaEmailBox, values.Email);
                    FillIfBlank(PessoaCepBox, values.Cep);
                    FillIfBlank(PessoaRuaBox, values.Rua ?? values.Endereco);
                    FillIfBlank(PessoaNumeroBox, values.Numero);
                    FillIfBlank(PessoaBairroBox, values.Bairro);
                    FillIfBlank(PessoaCidadeBox, values.Cidade);
                    FillIfBlank(PessoaEstadoBox, values.Estado);
                    FillIfBlank(PessoaEstadoCivilBox, values.EstadoCivil);
                    FillIfBlank(PessoaNacionalidadeBox, values.Nacionalidade);
                    FillIfBlank(PessoaProfissaoBox, values.Profissao);
                    FillDateIfBlank(PessoaDataNascimentoBox, values.DataNascimento);
                    break;
            }

            return;
        }

        switch (documentoDe)
        {
            case "responsavel":
                FillIfBlank(PessoaResponsavelNomeBox, values.Nome);
                FillIfBlank(PessoaResponsavelCpfBox, values.Cpf);
                FillIfBlank(PessoaResponsavelRgBox, values.Rg);
                FillIfBlank(PessoaResponsavelTelefoneBox, values.Telefone);
                FillIfBlank(PessoaResponsavelEmailBox, values.Email);
                FillIfBlank(_pessoaResponsavelCargoBox, values.Cargo);
                FillIfBlank(PessoaResponsavelProfissaoBox, values.Profissao);
                FillIfBlank(PessoaResponsavelNacionalidadeBox, values.Nacionalidade);
                FillIfBlank(PessoaResponsavelEstadoCivilBox, values.EstadoCivil);
                FillDateIfBlank(PessoaResponsavelDataNascimentoBox, values.DataNascimento);
                FillIfBlank(PessoaResponsavelCepBox, values.Cep);
                FillIfBlank(PessoaResponsavelRuaBox, values.Rua ?? values.Endereco);
                FillIfBlank(PessoaResponsavelNumeroBox, values.Numero);
                FillIfBlank(PessoaResponsavelBairroBox, values.Bairro);
                FillIfBlank(PessoaResponsavelCidadeBox, values.Cidade);
                FillIfBlank(PessoaResponsavelEstadoBox, values.Estado);
                break;
            default:
                FillIfBlank(PessoaNomeBox, values.Empresa ?? values.Nome);
                FillIfBlank(PessoaDocumentoBox, values.Cnpj);
                FillIfBlank(_pessoaNomeFantasiaBox, values.NomeFantasia);
                FillIfBlank(PessoaEmpresaCepBox, values.Cep);
                FillIfBlank(PessoaEmpresaRuaBox, values.Rua ?? values.Endereco);
                FillIfBlank(PessoaEmpresaNumeroBox, values.Numero);
                FillIfBlank(PessoaEmpresaBairroBox, values.Bairro);
                FillIfBlank(PessoaEmpresaCidadeBox, values.Cidade);
                FillIfBlank(PessoaEmpresaEstadoBox, values.Estado);
                break;
        }
    }

    private static PessoaDocumentoOcrValues ExtractPessoaDocumentoOcrValues(string text)
    {
        var normalized = text.Replace("\r", "\n", StringComparison.Ordinal);
        var birthDateText = FindPessoaDocumentoLabeledValue(normalized, "data de nascimento")
            ?? FindPessoaDocumentoLabeledValue(normalized, "nascimento")
            ?? FindPessoaDocumentoRegex(normalized, @"\b\d{2}/\d{2}/\d{4}\b");

        return new PessoaDocumentoOcrValues(
            Nome: FindPessoaDocumentoLabeledValue(normalized, "nome") ?? FindPessoaDocumentoLabeledValue(normalized, "nome civil"),
            Empresa: FindPessoaDocumentoLabeledValue(normalized, "razão social") ?? FindPessoaDocumentoLabeledValue(normalized, "razao social") ?? FindPessoaDocumentoLabeledValue(normalized, "empresa"),
            NomeFantasia: FindPessoaDocumentoLabeledValue(normalized, "nome fantasia"),
            Cpf: DigitsOnlyOrNull(FindPessoaDocumentoRegex(normalized, @"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b")),
            Cnpj: DigitsOnlyOrNull(FindPessoaDocumentoRegex(normalized, @"\b\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}\b")),
            Rg: DigitsOnlyOrNull(FindPessoaDocumentoLabeledValue(normalized, "rg") ?? FindPessoaDocumentoLabeledValue(normalized, "registro geral") ?? FindPessoaDocumentoLabeledValue(normalized, "identidade")),
            Email: FindPessoaDocumentoRegex(normalized, @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase),
            Telefone: DigitsOnlyOrNull(FindPessoaDocumentoRegex(normalized, @"(?:\(?\d{2}\)?\s?)?(?:9\s?)?\d{4}[-\s]?\d{4}")),
            Cep: DigitsOnlyOrNull(FindPessoaDocumentoRegex(normalized, @"\b\d{5}-?\d{3}\b")),
            Endereco: FindPessoaDocumentoLabeledValue(normalized, "endereço") ?? FindPessoaDocumentoLabeledValue(normalized, "endereco"),
            Rua: FindPessoaDocumentoLabeledValue(normalized, "rua") ?? FindPessoaDocumentoLabeledValue(normalized, "logradouro"),
            Numero: FindPessoaDocumentoLabeledValue(normalized, "número") ?? FindPessoaDocumentoLabeledValue(normalized, "numero") ?? FindPessoaDocumentoLabeledValue(normalized, "nº"),
            Bairro: FindPessoaDocumentoLabeledValue(normalized, "bairro"),
            Cidade: FindPessoaDocumentoLabeledValue(normalized, "cidade") ?? FindPessoaDocumentoLabeledValue(normalized, "município") ?? FindPessoaDocumentoLabeledValue(normalized, "municipio"),
            Estado: NormalizeState(FindPessoaDocumentoLabeledValue(normalized, "estado") ?? FindPessoaDocumentoLabeledValue(normalized, "uf")),
            Nacionalidade: FindPessoaDocumentoLabeledValue(normalized, "nacionalidade"),
            EstadoCivil: FindPessoaDocumentoLabeledValue(normalized, "estado civil"),
            Profissao: FindPessoaDocumentoLabeledValue(normalized, "profissão") ?? FindPessoaDocumentoLabeledValue(normalized, "profissao"),
            Cargo: FindPessoaDocumentoLabeledValue(normalized, "cargo") ?? FindPessoaDocumentoLabeledValue(normalized, "função") ?? FindPessoaDocumentoLabeledValue(normalized, "funcao"),
            DataNascimento: ParsePessoaDocumentoDateOnly(birthDateText));
    }

    private static void FillIfBlank(TextBox? textBox, string? value)
    {
        if (textBox is null || !string.IsNullOrWhiteSpace(textBox.Text) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        textBox.Text = value.Trim();
    }

    private static void FillDateIfBlank(DatePicker? datePicker, DateOnly? value)
    {
        if (datePicker is null || datePicker.SelectedDate.HasValue || !value.HasValue)
        {
            return;
        }

        datePicker.SelectedDate = value.Value.ToDateTime(TimeOnly.MinValue);
    }

    private static string? FindPessoaDocumentoLabeledValue(string text, string label)
    {
        var match = Regex.Match(
            text,
            $@"(?im)^\s*{Regex.Escape(label)}\s*[:\-]\s*(?<value>.+?)\s*$",
            RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static string? FindPessoaDocumentoRegex(string text, string pattern, RegexOptions options = RegexOptions.None)
    {
        var match = Regex.Match(text, pattern, options | RegexOptions.CultureInvariant);
        return match.Success ? match.Value.Trim() : null;
    }

    private static string? DigitsOnlyOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = Regex.Replace(value, @"\D", string.Empty);
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    private static DateOnly? ParsePessoaDocumentoDateOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy", "yyyy-MM-dd" };
        return DateOnly.TryParseExact(value.Trim(), formats, CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private static bool IsPessoaDocumentoPlainText(string storagePath, string? contentType) =>
        contentType?.StartsWith("text/", StringComparison.OrdinalIgnoreCase) == true
        || Path.GetExtension(storagePath).Equals(".txt", StringComparison.OrdinalIgnoreCase);

    private static bool IsPessoaDocumentoImage(string storagePath, string? contentType) =>
        contentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true
        || Path.GetExtension(storagePath).Equals(".png", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(storagePath).Equals(".jpg", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(storagePath).Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(storagePath).Equals(".tif", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(storagePath).Equals(".tiff", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(storagePath).Equals(".bmp", StringComparison.OrdinalIgnoreCase);

    private static string? NormalizeState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim().ToUpperInvariant();
        return trimmed.Length >= 2 ? trimmed[..2] : trimmed;
    }

    private sealed record PessoaDocumentoOcrValues(
        string? Nome,
        string? Empresa,
        string? NomeFantasia,
        string? Cpf,
        string? Cnpj,
        string? Rg,
        string? Email,
        string? Telefone,
        string? Cep,
        string? Endereco,
        string? Rua,
        string? Numero,
        string? Bairro,
        string? Cidade,
        string? Estado,
        string? Nacionalidade,
        string? EstadoCivil,
        string? Profissao,
        string? Cargo,
        DateOnly? DataNascimento);
}
