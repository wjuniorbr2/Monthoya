using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed class RentalManagementService(MonthoyaDbContext dbContext) : IRentalManagementService
{
    public async Task<IReadOnlyList<PessoaSummary>> GetPessoasAsync(CancellationToken cancellationToken = default)
    {
        var pessoas = await dbContext.Pessoas
            .AsNoTracking()
            .Include(x => x.Roles)
            .OrderBy(x => x.NomeDisplay)
            .ToListAsync(cancellationToken);

        return pessoas.Select(x => new PessoaSummary(
            x.Id,
            x.NomeDisplay,
            x.TipoPessoa == TipoPessoa.Fisica ? "Física" : "Jurídica",
            string.Join(", ", x.Roles.OrderBy(r => r.Role).Select(r => GetPessoaRoleLabel(r.Role))),
            x.Telefone,
            x.Email,
            x.Status == RegistroStatus.Ativo ? "Ativo" : "Inativo")).ToList();
    }

    public async Task<PessoaSummary> CreatePessoaAsync(CreatePessoaRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NomeDisplay))
        {
            throw new InvalidOperationException("Informe o nome da pessoa.");
        }

        var roles = request.Roles.Distinct().ToArray();
        if (roles.Length == 0)
        {
            throw new InvalidOperationException("Selecione pelo menos uma função para a pessoa.");
        }

        var pessoa = new Pessoa
        {
            TipoPessoa = request.TipoPessoa,
            NomeDisplay = request.NomeDisplay.Trim(),
            Telefone = request.Telefone?.Trim(),
            Email = request.Email?.Trim(),
            Observacoes = request.Observacoes?.Trim(),
            Roles = roles.Select(role => new PessoaRole { Role = role }).ToList()
        };

        if (request.TipoPessoa == TipoPessoa.Fisica)
        {
            pessoa.PessoaFisica = new PessoaFisica
            {
                Nome = pessoa.NomeDisplay,
                Cpf = request.Documento?.Trim(),
                Telefone = pessoa.Telefone,
                Email = pessoa.Email
            };
        }
        else
        {
            pessoa.PessoaJuridica = new PessoaJuridica
            {
                NomeEmpresa = pessoa.NomeDisplay,
                Cnpj = request.Documento?.Trim(),
                ResponsavelEmail = pessoa.Email,
                ResponsavelTelefone = pessoa.Telefone
            };
        }

        dbContext.Pessoas.Add(pessoa);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetPessoasAsync(cancellationToken)).Single(x => x.Id == pessoa.Id);
    }

    public async Task<IReadOnlyList<ImovelSummary>> GetImoveisAsync(CancellationToken cancellationToken = default)
    {
        var imoveis = await dbContext.Imoveis
            .AsNoTracking()
            .Include(x => x.Proprietario)
            .OrderBy(x => x.Rua)
            .ToListAsync(cancellationToken);

        return imoveis.Select(x => new ImovelSummary(
            x.Id,
            $"{x.Rua}, {x.Numero}".Trim().Trim(','),
            x.Proprietario?.NomeDisplay ?? "-",
            GetEnumLabel(x.Finalidade),
            GetEnumLabel(x.Status),
            x.ValorAluguel)).ToList();
    }

    public async Task<ImovelSummary> CreateImovelAsync(CreateImovelRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ProprietarioId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione um proprietário.");
        }

        if (string.IsNullOrWhiteSpace(request.Rua))
        {
            throw new InvalidOperationException("Informe a rua do imóvel.");
        }

        var proprietario = await dbContext.Pessoas
            .Include(x => x.Roles)
            .SingleOrDefaultAsync(x => x.Id == request.ProprietarioId, cancellationToken)
            ?? throw new InvalidOperationException("Proprietário não encontrado.");

        if (!proprietario.Roles.Any(x => x.Role == PessoaRoleTipo.Proprietario))
        {
            dbContext.PessoaRoles.Add(new PessoaRole { PessoaId = proprietario.Id, Role = PessoaRoleTipo.Proprietario });
        }

        var imovel = new Imovel
        {
            ProprietarioId = proprietario.Id,
            Rua = request.Rua.Trim(),
            Numero = request.Numero?.Trim(),
            Bairro = request.Bairro?.Trim(),
            Cidade = string.IsNullOrWhiteSpace(request.Cidade) ? "Paranavaí" : request.Cidade.Trim(),
            Estado = string.IsNullOrWhiteSpace(request.Estado) ? "PR" : request.Estado.Trim().ToUpperInvariant(),
            ValorAluguel = request.ValorAluguel,
            Finalidade = request.Finalidade,
            Observacoes = request.Observacoes?.Trim()
        };

        dbContext.Imoveis.Add(imovel);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetImoveisAsync(cancellationToken)).Single(x => x.Id == imovel.Id);
    }

    public async Task<IReadOnlyList<LocacaoSummary>> GetLocacoesAsync(CancellationToken cancellationToken = default)
    {
        var locacoes = await dbContext.Locacoes
            .AsNoTracking()
            .Include(x => x.Imovel)
            .Include(x => x.Proprietario)
            .Include(x => x.Locatario)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return locacoes.Select(x => new LocacaoSummary(
            x.Id,
            x.Imovel is null ? "-" : $"{x.Imovel.Rua}, {x.Imovel.Numero}".Trim().Trim(','),
            x.Proprietario?.NomeDisplay ?? "-",
            x.Locatario?.NomeDisplay ?? "-",
            x.ValorAluguel,
            GetEnumLabel(x.Status))).ToList();
    }

    public async Task<IReadOnlyList<IndiceReajusteSummary>> GetIndicesReajusteAsync(CancellationToken cancellationToken = default) =>
        await dbContext.IndicesReajuste.AsNoTracking().OrderBy(x => x.Nome)
            .Select(x => new IndiceReajusteSummary(x.Id, x.Nome, x.Codigo, x.Tipo == ReajusteTipo.Oficial ? "Oficial" : "Custom/manual", x.Percentual, x.Ativo ? "Ativo" : "Inativo"))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FinanceiroSummary>> GetLancamentosFinanceirosAsync(CancellationToken cancellationToken = default) =>
        await dbContext.LancamentosFinanceiros.AsNoTracking().OrderBy(x => x.DataVencimento)
            .Select(x => new FinanceiroSummary(x.Id, x.Tipo.ToString(), x.Categoria, x.Descricao, x.Valor, x.DataVencimento, x.Status.ToString()))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<BoletoSummary>> GetBoletosAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Boletos.AsNoTracking().OrderBy(x => x.DataVencimento)
            .Select(x => new BoletoSummary(x.Id, x.Status.ToString(), x.Valor, x.DataVencimento, x.BancoProvider))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NotaFiscalSummary>> GetNotasFiscaisAsync(CancellationToken cancellationToken = default) =>
        await dbContext.NotasFiscais.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new NotaFiscalSummary(x.Id, x.Status.ToString(), x.ValorServico, x.Provider, x.Numero, x.CodigoVerificacao))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DocumentoModeloSummary>> GetDocumentoModelosAsync(CancellationToken cancellationToken = default) =>
        await dbContext.DocumentosModelos.AsNoTracking().OrderBy(x => x.Tipo)
            .Select(x => new DocumentoModeloSummary(x.Id, x.Tipo, x.Nome, x.StatusRevisao.ToString(), x.Ativo ? "Ativo" : "Inativo"))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DimobDeclaracaoSummary>> GetDimobDeclaracoesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.DimobDeclaracoes.AsNoTracking().OrderByDescending(x => x.AnoCalendario)
            .Select(x => new DimobDeclaracaoSummary(x.Id, x.AnoCalendario, x.Status.ToString(), x.Observacoes))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ManutencaoSummary>> GetManutencoesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ManutencoesImovel.AsNoTracking().OrderByDescending(x => x.DataSolicitacao)
            .Select(x => new ManutencaoSummary(x.Id, x.Descricao, x.Status.ToString(), x.DataSolicitacao, x.Valor))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<VistoriaSummary>> GetVistoriasAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Vistorias.AsNoTracking().OrderByDescending(x => x.DataVistoria)
            .Select(x => new VistoriaSummary(x.Id, x.Tipo.ToString(), x.DataVistoria, x.Responsavel, x.Status))
            .ToListAsync(cancellationToken);

    private static string GetPessoaRoleLabel(PessoaRoleTipo role) =>
        role switch
        {
            PessoaRoleTipo.Proprietario => "Proprietário",
            PessoaRoleTipo.Locatario => "Locatário",
            PessoaRoleTipo.Fiador => "Fiador",
            _ => role.ToString()
        };

    private static string GetEnumLabel<T>(T value) where T : struct, Enum => value.ToString();
}
