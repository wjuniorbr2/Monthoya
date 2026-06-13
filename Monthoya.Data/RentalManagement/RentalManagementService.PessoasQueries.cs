using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<IReadOnlyList<PessoaSummary>> GetPessoasAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await GetPessoasCoreAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<PessoaSummary>> GetPessoasCoreAsync(CancellationToken cancellationToken)
    {
        var pessoas = await dbContext.Pessoas
            .AsNoTracking()
            .OrderBy(x => x.NomeDisplay)
            .Select(x => new
            {
                x.Id,
                x.NomeDisplay,
                x.TipoPessoa,
                x.Telefone,
                x.Email,
                x.Status,
                Documento = x.TipoPessoa == TipoPessoa.Fisica
                    ? (x.PessoaFisica != null ? x.PessoaFisica.Cpf : null)
                    : (x.PessoaJuridica != null ? x.PessoaJuridica.Cnpj : null)
            })
            .ToListAsync(cancellationToken);

        var pessoaRoles = await dbContext.PessoaRoles
            .AsNoTracking()
            .Select(x => new { x.PessoaId, x.Role })
            .ToListAsync(cancellationToken);
        var proprietarioRoleSet = pessoaRoles
            .Where(x => x.Role == PessoaRoleTipo.Proprietario)
            .Select(x => x.PessoaId)
            .ToHashSet();
        var locatarioRoleSet = pessoaRoles
            .Where(x => x.Role == PessoaRoleTipo.Locatario)
            .Select(x => x.PessoaId)
            .ToHashSet();
        var fiadorRoleSet = pessoaRoles
            .Where(x => x.Role == PessoaRoleTipo.Fiador)
            .Select(x => x.PessoaId)
            .ToHashSet();

        var proprietarioSet = (await dbContext.Imoveis
            .AsNoTracking()
            .Where(x => x.Status != ImovelStatus.Inativo)
            .Select(x => x.ProprietarioId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        var locatarioSet = (await dbContext.Locacoes
            .AsNoTracking()
            .Where(x => x.Status == LocacaoStatus.Ativa)
            .Select(x => x.LocatarioId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        var fiadorSet = (await dbContext.LocacaoFiadores
            .AsNoTracking()
            .Where(x => x.Locacao != null && x.Locacao.Status == LocacaoStatus.Ativa)
            .Select(x => x.FiadorId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        return pessoas.Select(x =>
        {
            var isProprietario = proprietarioSet.Contains(x.Id);
            var isLocatario = locatarioRoleSet.Contains(x.Id) || locatarioSet.Contains(x.Id);
            var isFiador = fiadorRoleSet.Contains(x.Id) || fiadorSet.Contains(x.Id);

            return new PessoaSummary(
                x.Id,
                x.NomeDisplay,
                x.TipoPessoa == TipoPessoa.Fisica ? "FÃ­sica" : "JurÃ­dica",
                GetPessoaRolesLabel(isProprietario, isLocatario, isFiador),
                FormatCpfCnpjForDisplay(x.TipoPessoa, x.Documento),
                FormatPhoneForDisplay(x.Telefone),
                x.Email,
                x.Status == RegistroStatus.Ativo ? "Ativo" : "Inativo",
                isProprietario,
                isLocatario,
                isFiador);
        }).ToList();
    }

    public async Task<PessoaDetails?> GetPessoaAsync(Guid pessoaId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var pessoa = await dbContext.Pessoas
            .AsNoTracking()
            .Include(x => x.PessoaFisica)
            .Include(x => x.PessoaJuridica)
            .SingleOrDefaultAsync(x => x.Id == pessoaId, cancellationToken);

        if (pessoa is null)
        {
            return null;
        }

        var pessoaRoles = await dbContext.PessoaRoles
            .AsNoTracking()
            .Where(x => x.PessoaId == pessoa.Id)
            .Select(x => x.Role)
            .ToListAsync(cancellationToken);
        var isProprietario = await dbContext.Imoveis
            .AsNoTracking()
            .AnyAsync(x => x.Status != ImovelStatus.Inativo && x.ProprietarioId == pessoa.Id, cancellationToken);
        var isLocatario = await dbContext.Locacoes
            .AsNoTracking()
            .AnyAsync(x => x.Status == LocacaoStatus.Ativa && x.LocatarioId == pessoa.Id, cancellationToken);
        var isFiador = await dbContext.LocacaoFiadores
            .AsNoTracking()
            .AnyAsync(x => x.FiadorId == pessoa.Id && x.Locacao != null && x.Locacao.Status == LocacaoStatus.Ativa, cancellationToken);


        isLocatario = isLocatario || pessoaRoles.Contains(PessoaRoleTipo.Locatario);
        isFiador = isFiador || pessoaRoles.Contains(PessoaRoleTipo.Fiador);

        var documento = pessoa.TipoPessoa == TipoPessoa.Fisica
            ? pessoa.PessoaFisica?.Cpf
            : pessoa.PessoaJuridica?.Cnpj;

        var summary = new PessoaSummary(
            pessoa.Id,
            pessoa.NomeDisplay,
            pessoa.TipoPessoa == TipoPessoa.Fisica ? "FÃ­sica" : "JurÃ­dica",
            GetPessoaRolesLabel(isProprietario, isLocatario, isFiador),
            FormatCpfCnpjForDisplay(pessoa.TipoPessoa, documento),
            FormatPhoneForDisplay(pessoa.Telefone),
            pessoa.Email,
            pessoa.Status == RegistroStatus.Ativo ? "Ativo" : "Inativo",
            isProprietario,
            isLocatario,
            isFiador);

        return new PessoaDetails(summary, ToPessoaRequest(pessoa));
    }

    public async Task<IReadOnlyList<string>> GetStreetSuggestionsAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var ruas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var pessoaRuas = await dbContext.Pessoas
            .AsNoTracking()
            .Include(x => x.PessoaFisica)
            .Include(x => x.PessoaJuridica)
            .Select(x => new
            {
                FisicaRua = x.PessoaFisica != null ? x.PessoaFisica.Rua : null,
                FisicaEmpresaRua = x.PessoaFisica != null ? x.PessoaFisica.EmpresaRua : null,
                FisicaConjugeEmpresaRua = x.PessoaFisica != null ? x.PessoaFisica.ConjugeEmpresaRua : null,
                JuridicaEmpresaRua = x.PessoaJuridica != null ? x.PessoaJuridica.EmpresaRua : null,
                JuridicaResponsavelRua = x.PessoaJuridica != null ? x.PessoaJuridica.ResponsavelRua : null
            })
            .ToListAsync(cancellationToken);

        foreach (var pessoa in pessoaRuas)
        {
            AddStreetSuggestion(ruas, pessoa.FisicaRua);
            AddStreetSuggestion(ruas, pessoa.FisicaEmpresaRua);
            AddStreetSuggestion(ruas, pessoa.FisicaConjugeEmpresaRua);
            AddStreetSuggestion(ruas, pessoa.JuridicaEmpresaRua);
            AddStreetSuggestion(ruas, pessoa.JuridicaResponsavelRua);
        }

        var imovelRuas = await dbContext.Imoveis
            .AsNoTracking()
            .Select(x => x.Rua)
            .ToListAsync(cancellationToken);

        foreach (var rua in imovelRuas)
        {
            AddStreetSuggestion(ruas, rua);
        }

        return ruas.Order(StringComparer.CurrentCultureIgnoreCase).ToList();
    }
}
