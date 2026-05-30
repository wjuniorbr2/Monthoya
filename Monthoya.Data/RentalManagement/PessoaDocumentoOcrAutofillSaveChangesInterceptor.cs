using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Monthoya.Core.Entities;

namespace Monthoya.Data.RentalManagement;

public sealed class PessoaDocumentoOcrAutofillSaveChangesInterceptor : SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is MonthoyaDbContext dbContext)
        {
            await ApplyPessoaDocumentoOcrAutofillAsync(dbContext, cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static async Task ApplyPessoaDocumentoOcrAutofillAsync(MonthoyaDbContext dbContext, CancellationToken cancellationToken)
    {
        var documentEntries = dbContext.ChangeTracker
            .Entries<PessoaDocumento>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified)
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Entity.OcrTextoExtraido))
            .ToList();

        foreach (var entry in documentEntries)
        {
            var documento = entry.Entity;
            var pessoa = await dbContext.Pessoas
                .Include(x => x.PessoaFisica)
                .Include(x => x.PessoaJuridica)
                .SingleOrDefaultAsync(x => x.Id == documento.PessoaId, cancellationToken);

            if (pessoa is null || string.IsNullOrWhiteSpace(documento.OcrTextoExtraido))
            {
                continue;
            }

            var values = ExtractPessoaOcrValues(documento.OcrTextoExtraido);
            var filledFields = new List<string>();
            ApplyByDocumentoDe(pessoa, documento.DocumentoDe, values, filledFields);

            if (filledFields.Count > 0)
            {
                pessoa.UpdatedAtUtc = DateTimeOffset.UtcNow;
                documento.OcrCamposAplicados = string.IsNullOrWhiteSpace(documento.OcrCamposAplicados)
                    ? string.Join(", ", filledFields)
                    : MergeFieldLists(documento.OcrCamposAplicados, filledFields);
            }
        }
    }

    private static void ApplyByDocumentoDe(Pessoa pessoa, string? documentoDe, PessoaOcrValues values, ICollection<string> filledFields)
    {
        var target = (documentoDe ?? "pessoa").Trim().ToLowerInvariant();

        if (pessoa.TipoPessoa == TipoPessoa.Fisica && pessoa.PessoaFisica is not null)
        {
            switch (target)
            {
                case "empresa_trabalho":
                    ApplyPessoaFisicaTrabalho(pessoa.PessoaFisica, values, filledFields);
                    return;
                case "conjuge":
                    ApplyPessoaFisicaConjuge(pessoa.PessoaFisica, values, filledFields);
                    return;
                case "trabalho_conjuge":
                    ApplyPessoaFisicaConjugeTrabalho(pessoa.PessoaFisica, values, filledFields);
                    return;
                default:
                    ApplyPessoaFisicaPessoal(pessoa, pessoa.PessoaFisica, values, filledFields);
                    return;
            }
        }

        if (pessoa.TipoPessoa == TipoPessoa.Juridica && pessoa.PessoaJuridica is not null)
        {
            switch (target)
            {
                case "responsavel":
                    ApplyPessoaJuridicaResponsavel(pessoa, pessoa.PessoaJuridica, values, filledFields);
                    return;
                case "empresa":
                    ApplyPessoaJuridicaEmpresa(pessoa, pessoa.PessoaJuridica, values, filledFields);
                    return;
                // These options are prepared in the UI, but their database fields will be added in the next phase.
                case "conjuge_responsavel":
                case "trabalho_conjuge_responsavel":
                    return;
                default:
                    ApplyPessoaJuridicaEmpresa(pessoa, pessoa.PessoaJuridica, values, filledFields);
                    return;
            }
        }
    }

    private static void ApplyPessoaFisicaPessoal(Pessoa pessoa, PessoaFisica fisica, PessoaOcrValues values, ICollection<string> fields)
    {
        FillIfBlank(() => pessoa.Telefone, value => pessoa.Telefone = value, values.Telefone, "Telefone", fields);
        FillIfBlank(() => pessoa.Email, value => pessoa.Email = value, values.Email, "E-mail", fields);
        FillIfBlank(() => fisica.Telefone, value => fisica.Telefone = value, values.Telefone, "Telefone", fields);
        FillIfBlank(() => fisica.Email, value => fisica.Email = value, values.Email, "E-mail", fields);
        FillIfBlank(() => fisica.Nome, value =>
        {
            fisica.Nome = value;
            if (string.IsNullOrWhiteSpace(pessoa.NomeDisplay))
            {
                pessoa.NomeDisplay = value;
            }
        }, values.Nome, "Nome", fields);
        FillIfBlank(() => fisica.Cpf, value => fisica.Cpf = value, DigitsOrNull(values.Cpf), "CPF", fields);
        FillIfBlank(() => fisica.Rg, value => fisica.Rg = value, DigitsOrNull(values.Rg), "RG", fields);
        FillIfBlank(() => fisica.Nacionalidade, value => fisica.Nacionalidade = value, values.Nacionalidade, "Nacionalidade", fields);
        FillIfBlank(() => fisica.EstadoCivil, value => fisica.EstadoCivil = value, values.EstadoCivil, "Estado civil", fields);
        FillIfBlank(() => fisica.Profissao, value => fisica.Profissao = value, values.Profissao, "Profissão", fields);
        FillDateIfBlank(() => fisica.DataNascimento, value => fisica.DataNascimento = value, values.DataNascimento, "Data de nascimento", fields);
        FillIfBlank(() => fisica.Cep, value => fisica.Cep = value, DigitsOrNull(values.Cep), "CEP", fields);
        FillAddressIfBlank(fisica, values, fields);
    }

    private static void ApplyPessoaFisicaTrabalho(PessoaFisica fisica, PessoaOcrValues values, ICollection<string> fields)
    {
        fisica.PossuiTrabalho ??= true;
        FillIfBlank(() => fisica.NomeEmpresaTrabalho, value => fisica.NomeEmpresaTrabalho = value, values.NomeEmpresa ?? values.Nome, "Nome da empresa", fields);
        FillIfBlank(() => fisica.CnpjEmpresaTrabalho, value => fisica.CnpjEmpresaTrabalho = value, DigitsOrNull(values.Cnpj), "CNPJ da empresa", fields);
        FillIfBlank(() => fisica.TelefoneEmpresaTrabalho, value => fisica.TelefoneEmpresaTrabalho = value, DigitsOrNull(values.Telefone), "Telefone da empresa", fields);
        FillIfBlank(() => fisica.EmailEmpresaTrabalho, value => fisica.EmailEmpresaTrabalho = value, values.Email, "E-mail da empresa", fields);
        FillIfBlank(() => fisica.CargoTrabalho, value => fisica.CargoTrabalho = value, values.Cargo, "Cargo", fields);
        FillDecimalIfBlank(() => fisica.RendaTrabalho, value => fisica.RendaTrabalho = value, values.Renda, "Renda", fields);
        FillIfBlank(() => fisica.EmpresaCep, value => fisica.EmpresaCep = value, DigitsOrNull(values.Cep), "CEP da empresa", fields);
        FillEmpresaAddressIfBlank(fisica, values, fields);
    }

    private static void ApplyPessoaFisicaConjuge(PessoaFisica fisica, PessoaOcrValues values, ICollection<string> fields)
    {
        FillIfBlank(() => fisica.ConjugeNome, value => fisica.ConjugeNome = value, values.Nome, "Nome do cônjuge", fields);
        FillIfBlank(() => fisica.ConjugeCpf, value => fisica.ConjugeCpf = value, DigitsOrNull(values.Cpf), "CPF do cônjuge", fields);
        FillIfBlank(() => fisica.ConjugeRg, value => fisica.ConjugeRg = value, DigitsOrNull(values.Rg), "RG do cônjuge", fields);
        FillIfBlank(() => fisica.ConjugeTelefone, value => fisica.ConjugeTelefone = value, DigitsOrNull(values.Telefone), "Telefone do cônjuge", fields);
        FillIfBlank(() => fisica.ConjugeEmail, value => fisica.ConjugeEmail = value, values.Email, "E-mail do cônjuge", fields);
        FillIfBlank(() => fisica.ConjugeProfissao, value => fisica.ConjugeProfissao = value, values.Profissao, "Profissão do cônjuge", fields);
        FillIfBlank(() => fisica.ConjugeNacionalidade, value => fisica.ConjugeNacionalidade = value, values.Nacionalidade, "Nacionalidade do cônjuge", fields);
        FillDateIfBlank(() => fisica.ConjugeDataNascimento, value => fisica.ConjugeDataNascimento = value, values.DataNascimento, "Nascimento do cônjuge", fields);
    }

    private static void ApplyPessoaFisicaConjugeTrabalho(PessoaFisica fisica, PessoaOcrValues values, ICollection<string> fields)
    {
        fisica.ConjugePossuiTrabalho ??= true;
        FillIfBlank(() => fisica.ConjugeNomeEmpresaTrabalho, value => fisica.ConjugeNomeEmpresaTrabalho = value, values.NomeEmpresa ?? values.Nome, "Empresa do cônjuge", fields);
        FillIfBlank(() => fisica.ConjugeCnpjEmpresaTrabalho, value => fisica.ConjugeCnpjEmpresaTrabalho = value, DigitsOrNull(values.Cnpj), "CNPJ da empresa do cônjuge", fields);
        FillIfBlank(() => fisica.ConjugeTelefoneEmpresaTrabalho, value => fisica.ConjugeTelefoneEmpresaTrabalho = value, DigitsOrNull(values.Telefone), "Telefone da empresa do cônjuge", fields);
        FillIfBlank(() => fisica.ConjugeEmailEmpresaTrabalho, value => fisica.ConjugeEmailEmpresaTrabalho = value, values.Email, "E-mail da empresa do cônjuge", fields);
        FillIfBlank(() => fisica.ConjugeCargoTrabalho, value => fisica.ConjugeCargoTrabalho = value, values.Cargo, "Cargo do cônjuge", fields);
        FillDecimalIfBlank(() => fisica.ConjugeRendaTrabalho, value => fisica.ConjugeRendaTrabalho = value, values.Renda, "Renda do cônjuge", fields);
        FillIfBlank(() => fisica.ConjugeEmpresaCep, value => fisica.ConjugeEmpresaCep = value, DigitsOrNull(values.Cep), "CEP da empresa do cônjuge", fields);
        FillConjugeEmpresaAddressIfBlank(fisica, values, fields);
    }

    private static void ApplyPessoaJuridicaEmpresa(Pessoa pessoa, PessoaJuridica juridica, PessoaOcrValues values, ICollection<string> fields)
    {
        FillIfBlank(() => juridica.NomeEmpresa, value =>
        {
            juridica.NomeEmpresa = value;
            if (string.IsNullOrWhiteSpace(pessoa.NomeDisplay))
            {
                pessoa.NomeDisplay = value;
            }
        }, values.NomeEmpresa ?? values.Nome, "Razão social", fields);
        FillIfBlank(() => juridica.NomeFantasia, value => juridica.NomeFantasia = value, values.NomeFantasia, "Nome fantasia", fields);
        FillIfBlank(() => juridica.Cnpj, value => juridica.Cnpj = value, DigitsOrNull(values.Cnpj), "CNPJ", fields);
        FillIfBlank(() => juridica.EmpresaCep, value => juridica.EmpresaCep = value, DigitsOrNull(values.Cep), "CEP da empresa", fields);
        FillIfBlank(() => juridica.EmpresaRua, value => juridica.EmpresaRua = value, values.Rua ?? values.Endereco, "Rua da empresa", fields);
        FillIfBlank(() => juridica.EmpresaNumero, value => juridica.EmpresaNumero = value, values.Numero, "Número da empresa", fields);
        FillIfBlank(() => juridica.EmpresaBairro, value => juridica.EmpresaBairro = value, values.Bairro, "Bairro da empresa", fields);
        FillIfBlank(() => juridica.EmpresaCidade, value => juridica.EmpresaCidade = value, values.Cidade, "Cidade da empresa", fields);
        FillIfBlank(() => juridica.EmpresaEstado, value => juridica.EmpresaEstado = value, NormalizeState(values.Estado), "Estado da empresa", fields);
    }

    private static void ApplyPessoaJuridicaResponsavel(Pessoa pessoa, PessoaJuridica juridica, PessoaOcrValues values, ICollection<string> fields)
    {
        FillIfBlank(() => juridica.ResponsavelNome, value => juridica.ResponsavelNome = value, values.Nome, "Nome do responsável", fields);
        FillIfBlank(() => juridica.ResponsavelCpf, value => juridica.ResponsavelCpf = value, DigitsOrNull(values.Cpf), "CPF do responsável", fields);
        FillIfBlank(() => juridica.ResponsavelRg, value => juridica.ResponsavelRg = value, DigitsOrNull(values.Rg), "RG do responsável", fields);
        FillIfBlank(() => juridica.ResponsavelTelefone, value => juridica.ResponsavelTelefone = value, DigitsOrNull(values.Telefone), "Telefone do responsável", fields);
        FillIfBlank(() => juridica.ResponsavelEmail, value => juridica.ResponsavelEmail = value, values.Email, "E-mail do responsável", fields);
        FillIfBlank(() => juridica.ResponsavelCargo, value => juridica.ResponsavelCargo = value, values.Cargo, "Cargo do responsável", fields);
        FillIfBlank(() => juridica.ResponsavelProfissao, value => juridica.ResponsavelProfissao = value, values.Profissao, "Profissão do responsável", fields);
        FillIfBlank(() => juridica.ResponsavelNacionalidade, value => juridica.ResponsavelNacionalidade = value, values.Nacionalidade, "Nacionalidade do responsável", fields);
        FillIfBlank(() => juridica.ResponsavelEstadoCivil, value => juridica.ResponsavelEstadoCivil = value, values.EstadoCivil, "Estado civil do responsável", fields);
        FillDateIfBlank(() => juridica.ResponsavelDataNascimento, value => juridica.ResponsavelDataNascimento = value, values.DataNascimento, "Nascimento do responsável", fields);
        FillIfBlank(() => juridica.ResponsavelCep, value => juridica.ResponsavelCep = value, DigitsOrNull(values.Cep), "CEP do responsável", fields);
        FillIfBlank(() => juridica.ResponsavelRua, value => juridica.ResponsavelRua = value, values.Rua ?? values.Endereco, "Rua do responsável", fields);
        FillIfBlank(() => juridica.ResponsavelNumero, value => juridica.ResponsavelNumero = value, values.Numero, "Número do responsável", fields);
        FillIfBlank(() => juridica.ResponsavelBairro, value => juridica.ResponsavelBairro = value, values.Bairro, "Bairro do responsável", fields);
        FillIfBlank(() => juridica.ResponsavelCidade, value => juridica.ResponsavelCidade = value, values.Cidade, "Cidade do responsável", fields);
        FillIfBlank(() => juridica.ResponsavelEstado, value => juridica.ResponsavelEstado = value, NormalizeState(values.Estado), "Estado do responsável", fields);
        FillIfBlank(() => pessoa.Telefone, value => pessoa.Telefone = value, DigitsOrNull(values.Telefone), "Telefone", fields);
        FillIfBlank(() => pessoa.Email, value => pessoa.Email = value, values.Email, "E-mail", fields);
    }

    private static PessoaOcrValues ExtractPessoaOcrValues(string text)
    {
        var normalized = text.Replace("\r", "\n", StringComparison.Ordinal);
        var dataNascimentoText = FindLabeledValue(normalized, "data de nascimento") ?? FindLabeledValue(normalized, "nascimento");
        var rendaText = FindLabeledValue(normalized, "renda") ?? FindLabeledValue(normalized, "salário") ?? FindLabeledValue(normalized, "salario");

        return new PessoaOcrValues(
            Nome: FindLabeledValue(normalized, "nome") ?? FindLabeledValue(normalized, "responsável") ?? FindLabeledValue(normalized, "responsavel"),
            NomeEmpresa: FindLabeledValue(normalized, "razão social") ?? FindLabeledValue(normalized, "razao social") ?? FindLabeledValue(normalized, "empresa") ?? FindLabeledValue(normalized, "empregador"),
            NomeFantasia: FindLabeledValue(normalized, "nome fantasia"),
            Cpf: FindRegex(normalized, @"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b"),
            Cnpj: FindRegex(normalized, @"\b\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}\b"),
            Rg: FindLabeledValue(normalized, "rg") ?? FindLabeledValue(normalized, "identidade"),
            Email: FindRegex(normalized, @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase),
            Telefone: FindRegex(normalized, @"(?:\(?\d{2}\)?\s?)?(?:9\s?)?\d{4}[-\s]?\d{4}"),
            Cep: FindRegex(normalized, @"\b\d{5}-?\d{3}\b"),
            Endereco: FindLabeledValue(normalized, "endereço") ?? FindLabeledValue(normalized, "endereco"),
            Rua: FindLabeledValue(normalized, "rua") ?? FindLabeledValue(normalized, "logradouro"),
            Numero: FindLabeledValue(normalized, "número") ?? FindLabeledValue(normalized, "numero") ?? FindLabeledValue(normalized, "nº"),
            Bairro: FindLabeledValue(normalized, "bairro"),
            Cidade: FindLabeledValue(normalized, "cidade") ?? FindLabeledValue(normalized, "município") ?? FindLabeledValue(normalized, "municipio"),
            Estado: FindLabeledValue(normalized, "estado") ?? FindLabeledValue(normalized, "uf"),
            Nacionalidade: FindLabeledValue(normalized, "nacionalidade"),
            EstadoCivil: FindLabeledValue(normalized, "estado civil"),
            Profissao: FindLabeledValue(normalized, "profissão") ?? FindLabeledValue(normalized, "profissao"),
            Cargo: FindLabeledValue(normalized, "cargo") ?? FindLabeledValue(normalized, "função") ?? FindLabeledValue(normalized, "funcao"),
            DataNascimento: ParseDateOnly(dataNascimentoText),
            Renda: ParseDecimal(rendaText));
    }

    private static void FillAddressIfBlank(PessoaFisica fisica, PessoaOcrValues values, ICollection<string> fields)
    {
        FillIfBlank(() => fisica.Rua, value => fisica.Rua = value, values.Rua ?? values.Endereco, "Rua", fields);
        FillIfBlank(() => fisica.Numero, value => fisica.Numero = value, values.Numero, "Número", fields);
        FillIfBlank(() => fisica.Bairro, value => fisica.Bairro = value, values.Bairro, "Bairro", fields);
        FillIfBlank(() => fisica.Cidade, value => fisica.Cidade = value, values.Cidade, "Cidade", fields);
        FillIfBlank(() => fisica.Estado, value => fisica.Estado = value, NormalizeState(values.Estado), "Estado", fields);
    }

    private static void FillEmpresaAddressIfBlank(PessoaFisica fisica, PessoaOcrValues values, ICollection<string> fields)
    {
        FillIfBlank(() => fisica.EmpresaRua, value => fisica.EmpresaRua = value, values.Rua ?? values.Endereco, "Rua da empresa", fields);
        FillIfBlank(() => fisica.EmpresaNumero, value => fisica.EmpresaNumero = value, values.Numero, "Número da empresa", fields);
        FillIfBlank(() => fisica.EmpresaBairro, value => fisica.EmpresaBairro = value, values.Bairro, "Bairro da empresa", fields);
        FillIfBlank(() => fisica.EmpresaCidade, value => fisica.EmpresaCidade = value, values.Cidade, "Cidade da empresa", fields);
        FillIfBlank(() => fisica.EmpresaEstado, value => fisica.EmpresaEstado = value, NormalizeState(values.Estado), "Estado da empresa", fields);
    }

    private static void FillConjugeEmpresaAddressIfBlank(PessoaFisica fisica, PessoaOcrValues values, ICollection<string> fields)
    {
        FillIfBlank(() => fisica.ConjugeEmpresaRua, value => fisica.ConjugeEmpresaRua = value, values.Rua ?? values.Endereco, "Rua da empresa do cônjuge", fields);
        FillIfBlank(() => fisica.ConjugeEmpresaNumero, value => fisica.ConjugeEmpresaNumero = value, values.Numero, "Número da empresa do cônjuge", fields);
        FillIfBlank(() => fisica.ConjugeEmpresaBairro, value => fisica.ConjugeEmpresaBairro = value, values.Bairro, "Bairro da empresa do cônjuge", fields);
        FillIfBlank(() => fisica.ConjugeEmpresaCidade, value => fisica.ConjugeEmpresaCidade = value, values.Cidade, "Cidade da empresa do cônjuge", fields);
        FillIfBlank(() => fisica.ConjugeEmpresaEstado, value => fisica.ConjugeEmpresaEstado = value, NormalizeState(values.Estado), "Estado da empresa do cônjuge", fields);
    }

    private static void FillIfBlank(Func<string?> getCurrent, Action<string> setValue, string? newValue, string fieldName, ICollection<string> filledFields)
    {
        if (!string.IsNullOrWhiteSpace(getCurrent()) || string.IsNullOrWhiteSpace(newValue))
        {
            return;
        }

        setValue(newValue.Trim());
        filledFields.Add(fieldName);
    }

    private static void FillDateIfBlank(Func<DateOnly?> getCurrent, Action<DateOnly> setValue, DateOnly? newValue, string fieldName, ICollection<string> filledFields)
    {
        if (getCurrent().HasValue || !newValue.HasValue)
        {
            return;
        }

        setValue(newValue.Value);
        filledFields.Add(fieldName);
    }

    private static void FillDecimalIfBlank(Func<decimal?> getCurrent, Action<decimal> setValue, decimal? newValue, string fieldName, ICollection<string> filledFields)
    {
        if (getCurrent().HasValue || !newValue.HasValue)
        {
            return;
        }

        setValue(newValue.Value);
        filledFields.Add(fieldName);
    }

    private static string MergeFieldLists(string existing, IEnumerable<string> additions)
    {
        var fields = existing
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Concat(additions)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return string.Join(", ", fields);
    }

    private static string? FindLabeledValue(string text, string label)
    {
        var match = Regex.Match(
            text,
            $@"(?im)^\s*{Regex.Escape(label)}\s*[:\-]\s*(?<value>.+?)\s*$",
            RegexOptions.CultureInvariant);

        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static string? FindRegex(string text, string pattern, RegexOptions options = RegexOptions.None)
    {
        var match = Regex.Match(text, pattern, options | RegexOptions.CultureInvariant);
        return match.Success ? match.Value.Trim() : null;
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

    private static string? NormalizeState(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim().ToUpperInvariant();
        return trimmed.Length >= 2 ? trimmed[..2] : trimmed;
    }

    private static DateOnly? ParseDateOnly(string? value)
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

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var cleaned = Regex.Replace(value, @"[^\d,\.]", string.Empty);
        if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.GetCultureInfo("pt-BR"), out var amount))
        {
            return amount;
        }

        return decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out amount) ? amount : null;
    }

    private sealed record PessoaOcrValues(
        string? Nome,
        string? NomeEmpresa,
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
        DateOnly? DataNascimento,
        decimal? Renda);
}
