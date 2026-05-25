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
            .Include(x => x.PessoaFisica)
            .Include(x => x.PessoaJuridica)
            .OrderBy(x => x.NomeDisplay)
            .ToListAsync(cancellationToken);

        return pessoas.Select(x => new PessoaSummary(
            x.Id,
            x.NomeDisplay,
            x.TipoPessoa == TipoPessoa.Fisica ? "Física" : "Jurídica",
            string.Join(", ", x.Roles.OrderBy(r => r.Role).Select(r => GetPessoaRoleLabel(r.Role))),
            x.TipoPessoa == TipoPessoa.Fisica ? x.PessoaFisica?.Cpf : x.PessoaJuridica?.Cnpj,
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
                Endereco = TrimOrNull(request.Endereco),
                EstadoCivil = TrimOrNull(request.EstadoCivil),
                Nacionalidade = TrimOrNull(request.Nacionalidade),
                DataNascimento = request.DataNascimento,
                Rg = TrimOrNull(request.Rg),
                Cpf = request.Documento?.Trim(),
                Telefone = pessoa.Telefone,
                Email = pessoa.Email,
                Profissao = TrimOrNull(request.Profissao),
                OndeTrabalha = TrimOrNull(request.OndeTrabalha),
                EnderecoTrabalho = TrimOrNull(request.EnderecoTrabalho),
                NomeEmpresaTrabalho = TrimOrNull(request.NomeEmpresaTrabalho),
                TelefoneEmpresaTrabalho = TrimOrNull(request.TelefoneEmpresaTrabalho),
                DadosBancarios = TrimOrNull(request.DadosBancarios),
                ConjugeNome = TrimOrNull(request.ConjugeNome),
                ConjugeRg = TrimOrNull(request.ConjugeRg),
                ConjugeCpf = TrimOrNull(request.ConjugeCpf),
                ConjugeDataNascimento = request.ConjugeDataNascimento,
                ConjugeProfissao = TrimOrNull(request.ConjugeProfissao),
                ConjugeNacionalidade = TrimOrNull(request.ConjugeNacionalidade),
                ConjugeTelefone = TrimOrNull(request.ConjugeTelefone)
            };
        }
        else
        {
            pessoa.PessoaJuridica = new PessoaJuridica
            {
                NomeEmpresa = pessoa.NomeDisplay,
                Cnpj = request.Documento?.Trim(),
                EnderecoEmpresa = TrimOrNull(request.Endereco),
                ResponsavelNome = TrimOrNull(request.ResponsavelNome),
                ResponsavelEndereco = TrimOrNull(request.ResponsavelEndereco),
                ResponsavelEstadoCivil = TrimOrNull(request.ResponsavelEstadoCivil),
                ResponsavelNacionalidade = TrimOrNull(request.ResponsavelNacionalidade),
                ResponsavelDataNascimento = request.ResponsavelDataNascimento,
                ResponsavelEmail = pessoa.Email,
                ResponsavelTelefone = pessoa.Telefone,
                ResponsavelRg = TrimOrNull(request.ResponsavelRg),
                ResponsavelCpf = TrimOrNull(request.ResponsavelCpf),
                ResponsavelProfissao = TrimOrNull(request.ResponsavelProfissao),
                ResponsavelOndeTrabalha = TrimOrNull(request.ResponsavelOndeTrabalha),
                ResponsavelEnderecoTrabalho = TrimOrNull(request.ResponsavelEnderecoTrabalho),
                ResponsavelNomeEmpresaTrabalho = TrimOrNull(request.ResponsavelNomeEmpresaTrabalho),
                ResponsavelTelefoneEmpresaTrabalho = TrimOrNull(request.ResponsavelTelefoneEmpresaTrabalho),
                ResponsavelDadosBancarios = TrimOrNull(request.ResponsavelDadosBancarios)
            };
        }

        dbContext.Pessoas.Add(pessoa);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetPessoasAsync(cancellationToken)).Single(x => x.Id == pessoa.Id);
    }

    public async Task<PessoaDocumentoSummary> CreatePessoaDocumentoAsync(CreatePessoaDocumentoRequest request, CancellationToken cancellationToken = default)
    {
        if (request.PessoaId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione a pessoa do documento.");
        }

        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            throw new InvalidOperationException("Informe o nome do documento.");
        }

        if (string.IsNullOrWhiteSpace(request.StoragePath))
        {
            throw new InvalidOperationException("Informe o caminho do arquivo digitalizado.");
        }

        var pessoaExists = await dbContext.Pessoas.AnyAsync(x => x.Id == request.PessoaId, cancellationToken);
        if (!pessoaExists)
        {
            throw new InvalidOperationException("Pessoa não encontrada.");
        }

        var documento = new PessoaDocumento
        {
            PessoaId = request.PessoaId,
            Tipo = string.IsNullOrWhiteSpace(request.Tipo) ? "outros" : request.Tipo.Trim(),
            Nome = request.Nome.Trim(),
            StoragePath = request.StoragePath.Trim(),
            ContentType = TrimOrNull(request.ContentType),
            DataValidade = request.DataValidade,
            Observacoes = TrimOrNull(request.Observacoes)
        };

        dbContext.PessoaDocumentos.Add(documento);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetPessoaDocumentosAsync(request.PessoaId, cancellationToken)).Single(x => x.Id == documento.Id);
    }

    public async Task<IReadOnlyList<PessoaDocumentoSummary>> GetPessoaDocumentosAsync(Guid? pessoaId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.PessoaDocumentos
            .AsNoTracking()
            .Include(x => x.Pessoa)
            .AsQueryable();

        if (pessoaId.HasValue)
        {
            query = query.Where(x => x.PessoaId == pessoaId.Value);
        }

        var documentos = await query
            .OrderBy(x => x.Pessoa!.NomeDisplay)
            .ThenBy(x => x.Tipo)
            .ToListAsync(cancellationToken);

        return documentos.Select(x => new PessoaDocumentoSummary(
            x.Id,
            x.PessoaId,
            x.Pessoa?.NomeDisplay ?? "-",
            GetPessoaDocumentoTipoLabel(x.Tipo),
            x.Nome,
            x.StoragePath,
            x.DataValidade,
            x.Status == RegistroStatus.Ativo ? "Ativo" : "Inativo")).ToList();
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
            x.Bairro,
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
            Complemento = TrimOrNull(request.Complemento),
            Bairro = request.Bairro?.Trim(),
            Cidade = string.IsNullOrWhiteSpace(request.Cidade) ? "Paranavaí" : request.Cidade.Trim(),
            Estado = string.IsNullOrWhiteSpace(request.Estado) ? "PR" : request.Estado.Trim().ToUpperInvariant(),
            Cep = TrimOrNull(request.Cep),
            SaneparMatricula = TrimOrNull(request.SaneparMatricula),
            CopelMatricula = TrimOrNull(request.CopelMatricula),
            IptuMatricula = TrimOrNull(request.IptuMatricula),
            TipoImovel = TrimOrNull(request.TipoImovel),
            Descricao = TrimOrNull(request.Descricao),
            ValorAluguel = request.ValorAluguel,
            ValorVenda = request.ValorVenda,
            Finalidade = request.Finalidade,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
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
            .Include(x => x.Fiadores)
                .ThenInclude(x => x.Fiador)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return locacoes.Select(x => new LocacaoSummary(
            x.Id,
            x.Imovel is null ? "-" : $"{x.Imovel.Rua}, {x.Imovel.Numero}".Trim().Trim(','),
            x.Proprietario?.NomeDisplay ?? "-",
            x.Locatario?.NomeDisplay ?? "-",
            string.Join(", ", x.Fiadores.Select(f => f.Fiador!.NomeDisplay).Order()),
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

    private static string GetPessoaDocumentoTipoLabel(string tipo) =>
        tipo switch
        {
            "cpf" => "CPF",
            "rg" => "RG",
            "comprovante_residencia" => "Comprovante de residência",
            "comprovante_renda" => "Comprovante de renda",
            "estado_civil" => "Comprovante de estado civil",
            "contrato_social" => "Contrato social",
            "cartao_cnpj" => "Cartão CNPJ",
            "procuracao" => "Procuração/autorização",
            "dados_bancarios" => "Dados bancários",
            _ => "Outros"
        };

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string GetEnumLabel<T>(T value) where T : struct, Enum => value.ToString();
}
