using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    private async Task<IReadOnlyList<string>> ApplyPessoaOcrFieldsAsync(Guid pessoaId, string documentoTipo, string documentoDe, string ocrText, CancellationToken cancellationToken)
    {
        var pessoa = await dbContext.Pessoas
            .Include(x => x.PessoaFisica)
            .Include(x => x.PessoaJuridica)
            .SingleOrDefaultAsync(x => x.Id == pessoaId, cancellationToken);

        if (pessoa is null)
        {
            return [];
        }

        var values = ExtractPessoaOcrValues(ocrText);
        var filledFields = new List<string>();
        var target = documentoDe.Trim().ToLowerInvariant();

        if (pessoa.TipoPessoa == TipoPessoa.Fisica && pessoa.PessoaFisica is not null)
        {
            var fisica = pessoa.PessoaFisica;
            switch (target)
            {
                case "empresa_trabalho":
                    fisica.PossuiTrabalho ??= true;
                    FillIfBlank(() => fisica.NomeEmpresaTrabalho, value => fisica.NomeEmpresaTrabalho = value, values.Nome, "Nome da empresa", filledFields);
                    FillIfBlank(() => fisica.CnpjEmpresaTrabalho, value => fisica.CnpjEmpresaTrabalho = value, DigitsOrNull(values.Cnpj), "CNPJ da empresa", filledFields);
                    FillIfBlank(() => fisica.TelefoneEmpresaTrabalho, value => fisica.TelefoneEmpresaTrabalho = value, DigitsOrNull(values.Telefone), "Telefone da empresa", filledFields);
                    FillIfBlank(() => fisica.EmailEmpresaTrabalho, value => fisica.EmailEmpresaTrabalho = value, values.Email, "Email da empresa", filledFields);
                    FillIfBlank(() => fisica.EmpresaCep, value => fisica.EmpresaCep = value, DigitsOrNull(values.Cep), "CEP da empresa", filledFields);
                    FillIfBlank(() => fisica.EmpresaRua, value => fisica.EmpresaRua = value, values.Endereco, "Rua da empresa", filledFields);
                    await SavePessoaOcrChangesAsync(pessoa, filledFields, cancellationToken);
                    return filledFields;
                case "conjuge":
                    FillIfBlank(() => fisica.ConjugeNome, value => fisica.ConjugeNome = value, values.Nome, "Nome do cônjuge", filledFields);
                    FillIfBlank(() => fisica.ConjugeCpf, value => fisica.ConjugeCpf = value, DigitsOrNull(values.Cpf), "CPF do cônjuge", filledFields);
                    FillIfBlank(() => fisica.ConjugeRg, value => fisica.ConjugeRg = value, DigitsOrNull(values.Rg), "RG do cônjuge", filledFields);
                    await SavePessoaOcrChangesAsync(pessoa, filledFields, cancellationToken);
                    return filledFields;
                case "trabalho_conjuge":
                    fisica.ConjugePossuiTrabalho ??= true;
                    FillIfBlank(() => fisica.ConjugeNomeEmpresaTrabalho, value => fisica.ConjugeNomeEmpresaTrabalho = value, values.Nome, "Empresa do cônjuge", filledFields);
                    FillIfBlank(() => fisica.ConjugeCnpjEmpresaTrabalho, value => fisica.ConjugeCnpjEmpresaTrabalho = value, DigitsOrNull(values.Cnpj), "CNPJ da empresa do cônjuge", filledFields);
                    FillIfBlank(() => fisica.ConjugeTelefoneEmpresaTrabalho, value => fisica.ConjugeTelefoneEmpresaTrabalho = value, DigitsOrNull(values.Telefone), "Telefone da empresa do cônjuge", filledFields);
                    FillIfBlank(() => fisica.ConjugeEmailEmpresaTrabalho, value => fisica.ConjugeEmailEmpresaTrabalho = value, values.Email, "Email da empresa do cônjuge", filledFields);
                    FillIfBlank(() => fisica.ConjugeEmpresaCep, value => fisica.ConjugeEmpresaCep = value, DigitsOrNull(values.Cep), "CEP da empresa do cônjuge", filledFields);
                    FillIfBlank(() => fisica.ConjugeEmpresaRua, value => fisica.ConjugeEmpresaRua = value, values.Endereco, "Rua da empresa do cônjuge", filledFields);
                    await SavePessoaOcrChangesAsync(pessoa, filledFields, cancellationToken);
                    return filledFields;
                case "outros":
                    return [];
            }
        }

        if (pessoa.TipoPessoa == TipoPessoa.Juridica && pessoa.PessoaJuridica is not null)
        {
            var juridica = pessoa.PessoaJuridica;
            switch (target)
            {
                case "responsavel":
                    FillIfBlank(() => juridica.ResponsavelNome, value => juridica.ResponsavelNome = value, values.Nome, "Nome do responsável", filledFields);
                    FillIfBlank(() => juridica.ResponsavelCpf, value => juridica.ResponsavelCpf = value, DigitsOrNull(values.Cpf), "CPF do responsável", filledFields);
                    FillIfBlank(() => juridica.ResponsavelRg, value => juridica.ResponsavelRg = value, DigitsOrNull(values.Rg), "RG do responsável", filledFields);
                    await SavePessoaOcrChangesAsync(pessoa, filledFields, cancellationToken);
                    return filledFields;
                case "conjuge_responsavel":
                case "trabalho_conjuge_responsavel":
                case "outros":
                    return [];
            }
        }

        FillIfBlank(() => pessoa.Email, value => pessoa.Email = value, values.Email, "Email", filledFields);

        if (pessoa.TipoPessoa == TipoPessoa.Fisica && pessoa.PessoaFisica is not null)
        {
            FillIfBlank(() => pessoa.PessoaFisica.Cpf, value => pessoa.PessoaFisica.Cpf = value, DigitsOrNull(values.Cpf), "CPF", filledFields);
            FillIfBlank(() => pessoa.PessoaFisica.Rg, value => pessoa.PessoaFisica.Rg = value, DigitsOrNull(values.Rg), "RG", filledFields);
            if (IsResidencePessoaDocumento(documentoTipo))
            {
                FillIfBlank(() => pessoa.PessoaFisica.Cep, value => pessoa.PessoaFisica.Cep = value, DigitsOrNull(values.Cep), "CEP", filledFields);
                FillIfBlank(() => pessoa.PessoaFisica.Rua, value => pessoa.PessoaFisica.Rua = value, values.Endereco, "Rua", filledFields);
            }
            FillIfBlank(() => pessoa.PessoaFisica.Nome, value =>
            {
                pessoa.PessoaFisica.Nome = value;
                if (string.IsNullOrWhiteSpace(pessoa.NomeDisplay))
                {
                    pessoa.NomeDisplay = value;
                }
            }, values.Nome, "Nome", filledFields);
        }

        if (pessoa.TipoPessoa == TipoPessoa.Juridica && pessoa.PessoaJuridica is not null)
        {
            FillIfBlank(() => pessoa.PessoaJuridica.Cnpj, value => pessoa.PessoaJuridica.Cnpj = value, DigitsOrNull(values.Cnpj), "CNPJ", filledFields);
            FillIfBlank(() => pessoa.PessoaJuridica.EmpresaCep, value => pessoa.PessoaJuridica.EmpresaCep = value, DigitsOrNull(values.Cep), "CEP da empresa", filledFields);
            FillIfBlank(() => pessoa.PessoaJuridica.EmpresaRua, value => pessoa.PessoaJuridica.EmpresaRua = value, values.Endereco, "Rua da empresa", filledFields);
            FillIfBlank(() => pessoa.PessoaJuridica.NomeEmpresa, value =>
            {
                pessoa.PessoaJuridica.NomeEmpresa = value;
                if (string.IsNullOrWhiteSpace(pessoa.NomeDisplay))
                {
                    pessoa.NomeDisplay = value;
                }
            }, values.Nome, "Nome da empresa", filledFields);
        }

        if (filledFields.Count > 0)
        {
            pessoa.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return filledFields;
    }

    private async Task SavePessoaOcrChangesAsync(Pessoa pessoa, IReadOnlyCollection<string> filledFields, CancellationToken cancellationToken)
    {
        if (filledFields.Count == 0)
        {
            return;
        }

        pessoa.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsResidencePessoaDocumento(string documentoTipo) =>
        documentoTipo.Equals("residencia", StringComparison.OrdinalIgnoreCase)
        || documentoTipo.Equals("endereco_residencia", StringComparison.OrdinalIgnoreCase);

}

