using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Monthoya.Core.Entities;
using Monthoya.Core.Integrations;
using Monthoya.Core.Services;
using Monthoya.Data.Storage;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService(
    MonthoyaDbContext dbContext,
    IDocumentOcrService? documentOcrService = null,
    IFileStorageService? fileStorageService = null) : IRentalManagementService
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
            var isLocatario = locatarioSet.Contains(x.Id);
            var isFiador = fiadorSet.Contains(x.Id);

            return new PessoaSummary(
                x.Id,
                x.NomeDisplay,
                x.TipoPessoa == TipoPessoa.Fisica ? "Física" : "Jurídica",
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

        var isProprietario = await dbContext.Imoveis
            .AsNoTracking()
            .AnyAsync(x => x.Status != ImovelStatus.Inativo && x.ProprietarioId == pessoa.Id, cancellationToken);
        var isLocatario = await dbContext.Locacoes
            .AsNoTracking()
            .AnyAsync(x => x.Status == LocacaoStatus.Ativa && x.LocatarioId == pessoa.Id, cancellationToken);
        var isFiador = await dbContext.LocacaoFiadores
            .AsNoTracking()
            .AnyAsync(x => x.FiadorId == pessoa.Id && x.Locacao != null && x.Locacao.Status == LocacaoStatus.Ativa, cancellationToken);

        var documento = pessoa.TipoPessoa == TipoPessoa.Fisica
            ? pessoa.PessoaFisica?.Cpf
            : pessoa.PessoaJuridica?.Cnpj;

        var summary = new PessoaSummary(
            pessoa.Id,
            pessoa.NomeDisplay,
            pessoa.TipoPessoa == TipoPessoa.Fisica ? "Física" : "Jurídica",
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

    public async Task<PessoaSummary> CreatePessoaAsync(CreatePessoaRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(request.NomeDisplay))
        {
            throw new InvalidOperationException("Informe o nome da pessoa.");
        }

        var pessoa = new Pessoa
        {
            TipoPessoa = request.TipoPessoa,
            NomeDisplay = request.NomeDisplay.Trim(),
            Telefone = DigitsOrNull(request.Telefone),
            Email = request.Email?.Trim(),
            Observacoes = request.Observacoes?.Trim()
        };

        if (request.TipoPessoa == TipoPessoa.Fisica)
        {
            pessoa.PessoaFisica = new PessoaFisica
            {
                Nome = pessoa.NomeDisplay,
                Rua = TrimOrNull(request.Rua),
                Numero = TrimOrNull(request.Numero),
                Complemento = TrimOrNull(request.Complemento),
                Bairro = TrimOrNull(request.Bairro),
                Cidade = TrimOrNull(request.Cidade),
                Estado = NormalizeState(request.Estado),
                Cep = DigitsOrNull(request.Cep),
                EstadoCivil = TrimOrNull(request.EstadoCivil),
                PossuiTrabalho = request.PossuiTrabalho,
                PossuiPet = request.PossuiPet,
                PetQual = TrimOrNull(request.PetQual),
                Nacionalidade = TrimOrNull(request.Nacionalidade),
                DataNascimento = request.DataNascimento,
                Rg = DigitsOrNull(request.Rg),
                Cpf = DigitsOrNull(request.Documento),
                Telefone = pessoa.Telefone,
                Email = pessoa.Email,
                Profissao = TrimOrNull(request.Profissao),
                OndeTrabalha = TrimOrNull(request.OndeTrabalha),
                EnderecoTrabalho = TrimOrNull(request.EnderecoTrabalho),
                NomeEmpresaTrabalho = TrimOrNull(request.NomeEmpresaTrabalho),
                CnpjEmpresaTrabalho = DigitsOrNull(request.CnpjEmpresaTrabalho),
                TelefoneEmpresaTrabalho = DigitsOrNull(request.TelefoneEmpresaTrabalho),
                EmailEmpresaTrabalho = TrimOrNull(request.EmailEmpresaTrabalho),
                CargoTrabalho = TrimOrNull(request.CargoTrabalho),
                RendaTrabalho = request.RendaTrabalho,
                TempoEmprego = TrimOrNull(request.TempoEmprego),
                TipoComprovanteRenda = TrimOrNull(request.TipoComprovanteRenda),
                OutrasInformacoes = TrimOrNull(request.OutrasInformacoes),
                TrabalhoOutrasInformacoes = TrimOrNull(request.TrabalhoOutrasInformacoes),
                EmpresaRua = TrimOrNull(request.EmpresaRua),
                EmpresaNumero = TrimOrNull(request.EmpresaNumero),
                EmpresaComplemento = TrimOrNull(request.EmpresaComplemento),
                EmpresaBairro = TrimOrNull(request.EmpresaBairro),
                EmpresaCidade = TrimOrNull(request.EmpresaCidade),
                EmpresaEstado = NormalizeState(request.EmpresaEstado),
                EmpresaCep = DigitsOrNull(request.EmpresaCep),
                DadosBancarios = TrimOrNull(request.DadosBancarios),
                BancoCodigo = DigitsOrNull(request.BancoCodigo),
                BancoNome = TrimOrNull(request.BancoNome),
                AgenciaNumero = DigitsOrNull(request.AgenciaNumero),
                AgenciaDigito = TrimOrNull(request.AgenciaDigito),
                ContaNumero = DigitsOrNull(request.ContaNumero),
                ContaDigito = TrimOrNull(request.ContaDigito),
                ContaTipo = request.ContaTipo,
                TitularNome = TrimOrNull(request.TitularNome),
                TitularDocumento = DigitsOrNull(request.TitularDocumento),
                PixTipo = request.PixTipo,
                PixChave = NormalizePixChave(request.PixChave, request.PixTipo),
                RepassePreferencial = request.RepassePreferencial,
                ConjugeNome = TrimOrNull(request.ConjugeNome),
                ConjugeRg = DigitsOrNull(request.ConjugeRg),
                ConjugeCpf = DigitsOrNull(request.ConjugeCpf),
                ConjugeEmail = TrimOrNull(request.ConjugeEmail),
                ConjugeDataNascimento = request.ConjugeDataNascimento,
                ConjugeProfissao = TrimOrNull(request.ConjugeProfissao),
                ConjugeNacionalidade = TrimOrNull(request.ConjugeNacionalidade),
                ConjugeTelefone = DigitsOrNull(request.ConjugeTelefone),
                ConjugeDadosBancarios = TrimOrNull(request.ConjugeDadosBancarios),
                ConjugeObservacoes = TrimOrNull(request.ConjugeObservacoes),
                ConjugeOutrasInformacoes = TrimOrNull(request.ConjugeOutrasInformacoes),
                ConjugePossuiTrabalho = request.ConjugePossuiTrabalho,
                ConjugeNomeEmpresaTrabalho = TrimOrNull(request.ConjugeNomeEmpresaTrabalho),
                ConjugeCnpjEmpresaTrabalho = DigitsOrNull(request.ConjugeCnpjEmpresaTrabalho),
                ConjugeTelefoneEmpresaTrabalho = DigitsOrNull(request.ConjugeTelefoneEmpresaTrabalho),
                ConjugeEmailEmpresaTrabalho = TrimOrNull(request.ConjugeEmailEmpresaTrabalho),
                ConjugeCargoTrabalho = TrimOrNull(request.ConjugeCargoTrabalho),
                ConjugeRendaTrabalho = request.ConjugeRendaTrabalho,
                ConjugeTempoEmprego = TrimOrNull(request.ConjugeTempoEmprego),
                ConjugeTipoComprovanteRenda = TrimOrNull(request.ConjugeTipoComprovanteRenda),
                ConjugeTrabalhoOutrasInformacoes = TrimOrNull(request.ConjugeTrabalhoOutrasInformacoes),
                ConjugeEmpresaRua = TrimOrNull(request.ConjugeEmpresaRua),
                ConjugeEmpresaNumero = TrimOrNull(request.ConjugeEmpresaNumero),
                ConjugeEmpresaComplemento = TrimOrNull(request.ConjugeEmpresaComplemento),
                ConjugeEmpresaBairro = TrimOrNull(request.ConjugeEmpresaBairro),
                ConjugeEmpresaCidade = TrimOrNull(request.ConjugeEmpresaCidade),
                ConjugeEmpresaEstado = NormalizeState(request.ConjugeEmpresaEstado),
                ConjugeEmpresaCep = DigitsOrNull(request.ConjugeEmpresaCep)
            };
        }
        else
        {
            pessoa.PessoaJuridica = new PessoaJuridica
            {
                NomeEmpresa = pessoa.NomeDisplay,
                NomeFantasia = TrimOrNull(request.NomeFantasia),
                Atividade = TrimOrNull(request.Atividade),
                ReceitaMensal = request.ReceitaMensal,
                Cnpj = DigitsOrNull(request.Documento),
                InscricaoEstadual = DigitsOrNull(request.InscricaoEstadual),
                InscricaoMunicipal = DigitsOrNull(request.InscricaoMunicipal),
                DataAbertura = request.DataAbertura,
                EmpresaRua = TrimOrNull(request.Rua),
                EmpresaNumero = TrimOrNull(request.Numero),
                EmpresaComplemento = TrimOrNull(request.Complemento),
                EmpresaBairro = TrimOrNull(request.Bairro),
                EmpresaCidade = TrimOrNull(request.Cidade),
                EmpresaEstado = NormalizeState(request.Estado),
                EmpresaCep = DigitsOrNull(request.Cep),
                ResponsavelNome = TrimOrNull(request.ResponsavelNome),
                ResponsavelCargo = TrimOrNull(request.ResponsavelCargo),
                ResponsavelRua = TrimOrNull(request.ResponsavelRua),
                ResponsavelNumero = TrimOrNull(request.ResponsavelNumero),
                ResponsavelComplemento = TrimOrNull(request.ResponsavelComplemento),
                ResponsavelBairro = TrimOrNull(request.ResponsavelBairro),
                ResponsavelCidade = TrimOrNull(request.ResponsavelCidade),
                ResponsavelEstado = NormalizeState(request.ResponsavelEstado),
                ResponsavelCep = DigitsOrNull(request.ResponsavelCep),
                ResponsavelEstadoCivil = TrimOrNull(request.ResponsavelEstadoCivil),
                ResponsavelNacionalidade = TrimOrNull(request.ResponsavelNacionalidade),
                ResponsavelDataNascimento = request.ResponsavelDataNascimento,
                ResponsavelEmail = pessoa.Email,
                ResponsavelTelefone = pessoa.Telefone,
                ResponsavelRg = DigitsOrNull(request.ResponsavelRg),
                ResponsavelCpf = DigitsOrNull(request.ResponsavelCpf),
                ResponsavelProfissao = TrimOrNull(request.ResponsavelProfissao),
                ResponsavelOndeTrabalha = TrimOrNull(request.ResponsavelOndeTrabalha),
                ResponsavelEnderecoTrabalho = TrimOrNull(request.ResponsavelEnderecoTrabalho),
                ResponsavelNomeEmpresaTrabalho = TrimOrNull(request.ResponsavelNomeEmpresaTrabalho),
                ResponsavelTelefoneEmpresaTrabalho = DigitsOrNull(request.ResponsavelTelefoneEmpresaTrabalho),
                ResponsavelDadosBancarios = TrimOrNull(request.ResponsavelDadosBancarios),
                ResponsavelBancoCodigo = DigitsOrNull(request.ResponsavelBancoCodigo),
                ResponsavelBancoNome = TrimOrNull(request.ResponsavelBancoNome),
                ResponsavelAgenciaNumero = DigitsOrNull(request.ResponsavelAgenciaNumero),
                ResponsavelAgenciaDigito = TrimOrNull(request.ResponsavelAgenciaDigito),
                ResponsavelContaNumero = DigitsOrNull(request.ResponsavelContaNumero),
                ResponsavelContaDigito = TrimOrNull(request.ResponsavelContaDigito),
                ResponsavelContaTipo = request.ResponsavelContaTipo,
                ResponsavelTitularNome = TrimOrNull(request.ResponsavelTitularNome),
                ResponsavelTitularDocumento = DigitsOrNull(request.ResponsavelTitularDocumento),
                ResponsavelPixTipo = request.ResponsavelPixTipo,
                ResponsavelPixChave = NormalizePixChave(request.ResponsavelPixChave, request.ResponsavelPixTipo),
                ResponsavelRepassePreferencial = request.ResponsavelRepassePreferencial,
                ResponsavelObservacoes = TrimOrNull(request.ResponsavelObservacoes)
            };
        }

        dbContext.Pessoas.Add(pessoa);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetPessoasCoreAsync(cancellationToken)).Single(x => x.Id == pessoa.Id);
    }

    public async Task<PessoaSummary> UpdatePessoaAsync(UpdatePessoaRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione a pessoa para editar.");
        }

        if (string.IsNullOrWhiteSpace(request.Pessoa.NomeDisplay))
        {
            throw new InvalidOperationException("Informe o nome da pessoa.");
        }

        var pessoa = await dbContext.Pessoas
            .Include(x => x.PessoaFisica)
            .Include(x => x.PessoaJuridica)
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Pessoa não encontrada.");

        pessoa.TipoPessoa = request.Pessoa.TipoPessoa;
        pessoa.NomeDisplay = request.Pessoa.NomeDisplay.Trim();
        pessoa.Telefone = DigitsOrNull(request.Pessoa.Telefone);
        pessoa.Email = TrimOrNull(request.Pessoa.Email);
        pessoa.Observacoes = TrimOrNull(request.Pessoa.Observacoes);
        pessoa.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (request.Pessoa.TipoPessoa == TipoPessoa.Fisica)
        {
            if (pessoa.PessoaJuridica is not null)
            {
                dbContext.PessoasJuridicas.Remove(pessoa.PessoaJuridica);
                pessoa.PessoaJuridica = null;
            }

            pessoa.PessoaFisica ??= new PessoaFisica { PessoaId = pessoa.Id };
            pessoa.PessoaFisica.Nome = pessoa.NomeDisplay;
            pessoa.PessoaFisica.Rua = TrimOrNull(request.Pessoa.Rua);
            pessoa.PessoaFisica.Numero = TrimOrNull(request.Pessoa.Numero);
            pessoa.PessoaFisica.Complemento = TrimOrNull(request.Pessoa.Complemento);
            pessoa.PessoaFisica.Bairro = TrimOrNull(request.Pessoa.Bairro);
            pessoa.PessoaFisica.Cidade = TrimOrNull(request.Pessoa.Cidade);
            pessoa.PessoaFisica.Estado = NormalizeState(request.Pessoa.Estado);
            pessoa.PessoaFisica.Cep = DigitsOrNull(request.Pessoa.Cep);
            pessoa.PessoaFisica.EstadoCivil = TrimOrNull(request.Pessoa.EstadoCivil);
            pessoa.PessoaFisica.PossuiTrabalho = request.Pessoa.PossuiTrabalho;
            pessoa.PessoaFisica.PossuiPet = request.Pessoa.PossuiPet;
            pessoa.PessoaFisica.PetQual = TrimOrNull(request.Pessoa.PetQual);
            pessoa.PessoaFisica.Nacionalidade = TrimOrNull(request.Pessoa.Nacionalidade);
            pessoa.PessoaFisica.DataNascimento = request.Pessoa.DataNascimento;
            pessoa.PessoaFisica.Rg = DigitsOrNull(request.Pessoa.Rg);
            pessoa.PessoaFisica.Cpf = DigitsOrNull(request.Pessoa.Documento);
            pessoa.PessoaFisica.Telefone = pessoa.Telefone;
            pessoa.PessoaFisica.Email = pessoa.Email;
            pessoa.PessoaFisica.Profissao = TrimOrNull(request.Pessoa.Profissao);
            pessoa.PessoaFisica.OndeTrabalha = TrimOrNull(request.Pessoa.OndeTrabalha);
            pessoa.PessoaFisica.EnderecoTrabalho = TrimOrNull(request.Pessoa.EnderecoTrabalho);
            pessoa.PessoaFisica.NomeEmpresaTrabalho = TrimOrNull(request.Pessoa.NomeEmpresaTrabalho);
            pessoa.PessoaFisica.CnpjEmpresaTrabalho = DigitsOrNull(request.Pessoa.CnpjEmpresaTrabalho);
            pessoa.PessoaFisica.TelefoneEmpresaTrabalho = DigitsOrNull(request.Pessoa.TelefoneEmpresaTrabalho);
            pessoa.PessoaFisica.EmailEmpresaTrabalho = TrimOrNull(request.Pessoa.EmailEmpresaTrabalho);
            pessoa.PessoaFisica.CargoTrabalho = TrimOrNull(request.Pessoa.CargoTrabalho);
            pessoa.PessoaFisica.RendaTrabalho = request.Pessoa.RendaTrabalho;
            pessoa.PessoaFisica.TempoEmprego = TrimOrNull(request.Pessoa.TempoEmprego);
            pessoa.PessoaFisica.TipoComprovanteRenda = TrimOrNull(request.Pessoa.TipoComprovanteRenda);
            pessoa.PessoaFisica.OutrasInformacoes = TrimOrNull(request.Pessoa.OutrasInformacoes);
            pessoa.PessoaFisica.TrabalhoOutrasInformacoes = TrimOrNull(request.Pessoa.TrabalhoOutrasInformacoes);
            pessoa.PessoaFisica.EmpresaRua = TrimOrNull(request.Pessoa.EmpresaRua);
            pessoa.PessoaFisica.EmpresaNumero = TrimOrNull(request.Pessoa.EmpresaNumero);
            pessoa.PessoaFisica.EmpresaComplemento = TrimOrNull(request.Pessoa.EmpresaComplemento);
            pessoa.PessoaFisica.EmpresaBairro = TrimOrNull(request.Pessoa.EmpresaBairro);
            pessoa.PessoaFisica.EmpresaCidade = TrimOrNull(request.Pessoa.EmpresaCidade);
            pessoa.PessoaFisica.EmpresaEstado = NormalizeState(request.Pessoa.EmpresaEstado);
            pessoa.PessoaFisica.EmpresaCep = DigitsOrNull(request.Pessoa.EmpresaCep);
            pessoa.PessoaFisica.DadosBancarios = TrimOrNull(request.Pessoa.DadosBancarios);
            pessoa.PessoaFisica.BancoCodigo = DigitsOrNull(request.Pessoa.BancoCodigo);
            pessoa.PessoaFisica.BancoNome = TrimOrNull(request.Pessoa.BancoNome);
            pessoa.PessoaFisica.AgenciaNumero = DigitsOrNull(request.Pessoa.AgenciaNumero);
            pessoa.PessoaFisica.AgenciaDigito = TrimOrNull(request.Pessoa.AgenciaDigito);
            pessoa.PessoaFisica.ContaNumero = DigitsOrNull(request.Pessoa.ContaNumero);
            pessoa.PessoaFisica.ContaDigito = TrimOrNull(request.Pessoa.ContaDigito);
            pessoa.PessoaFisica.ContaTipo = request.Pessoa.ContaTipo;
            pessoa.PessoaFisica.TitularNome = TrimOrNull(request.Pessoa.TitularNome);
            pessoa.PessoaFisica.TitularDocumento = DigitsOrNull(request.Pessoa.TitularDocumento);
            pessoa.PessoaFisica.PixTipo = request.Pessoa.PixTipo;
            pessoa.PessoaFisica.PixChave = NormalizePixChave(request.Pessoa.PixChave, request.Pessoa.PixTipo);
            pessoa.PessoaFisica.RepassePreferencial = request.Pessoa.RepassePreferencial;
            pessoa.PessoaFisica.ConjugeNome = TrimOrNull(request.Pessoa.ConjugeNome);
            pessoa.PessoaFisica.ConjugeRg = DigitsOrNull(request.Pessoa.ConjugeRg);
            pessoa.PessoaFisica.ConjugeCpf = DigitsOrNull(request.Pessoa.ConjugeCpf);
            pessoa.PessoaFisica.ConjugeEmail = TrimOrNull(request.Pessoa.ConjugeEmail);
            pessoa.PessoaFisica.ConjugeDataNascimento = request.Pessoa.ConjugeDataNascimento;
            pessoa.PessoaFisica.ConjugeProfissao = TrimOrNull(request.Pessoa.ConjugeProfissao);
            pessoa.PessoaFisica.ConjugeNacionalidade = TrimOrNull(request.Pessoa.ConjugeNacionalidade);
            pessoa.PessoaFisica.ConjugeTelefone = DigitsOrNull(request.Pessoa.ConjugeTelefone);
            pessoa.PessoaFisica.ConjugeDadosBancarios = TrimOrNull(request.Pessoa.ConjugeDadosBancarios);
            pessoa.PessoaFisica.ConjugeObservacoes = TrimOrNull(request.Pessoa.ConjugeObservacoes);
            pessoa.PessoaFisica.ConjugeOutrasInformacoes = TrimOrNull(request.Pessoa.ConjugeOutrasInformacoes);
            pessoa.PessoaFisica.ConjugePossuiTrabalho = request.Pessoa.ConjugePossuiTrabalho;
            pessoa.PessoaFisica.ConjugeNomeEmpresaTrabalho = TrimOrNull(request.Pessoa.ConjugeNomeEmpresaTrabalho);
            pessoa.PessoaFisica.ConjugeCnpjEmpresaTrabalho = DigitsOrNull(request.Pessoa.ConjugeCnpjEmpresaTrabalho);
            pessoa.PessoaFisica.ConjugeTelefoneEmpresaTrabalho = DigitsOrNull(request.Pessoa.ConjugeTelefoneEmpresaTrabalho);
            pessoa.PessoaFisica.ConjugeEmailEmpresaTrabalho = TrimOrNull(request.Pessoa.ConjugeEmailEmpresaTrabalho);
            pessoa.PessoaFisica.ConjugeCargoTrabalho = TrimOrNull(request.Pessoa.ConjugeCargoTrabalho);
            pessoa.PessoaFisica.ConjugeRendaTrabalho = request.Pessoa.ConjugeRendaTrabalho;
            pessoa.PessoaFisica.ConjugeTempoEmprego = TrimOrNull(request.Pessoa.ConjugeTempoEmprego);
            pessoa.PessoaFisica.ConjugeTipoComprovanteRenda = TrimOrNull(request.Pessoa.ConjugeTipoComprovanteRenda);
            pessoa.PessoaFisica.ConjugeTrabalhoOutrasInformacoes = TrimOrNull(request.Pessoa.ConjugeTrabalhoOutrasInformacoes);
            pessoa.PessoaFisica.ConjugeEmpresaRua = TrimOrNull(request.Pessoa.ConjugeEmpresaRua);
            pessoa.PessoaFisica.ConjugeEmpresaNumero = TrimOrNull(request.Pessoa.ConjugeEmpresaNumero);
            pessoa.PessoaFisica.ConjugeEmpresaComplemento = TrimOrNull(request.Pessoa.ConjugeEmpresaComplemento);
            pessoa.PessoaFisica.ConjugeEmpresaBairro = TrimOrNull(request.Pessoa.ConjugeEmpresaBairro);
            pessoa.PessoaFisica.ConjugeEmpresaCidade = TrimOrNull(request.Pessoa.ConjugeEmpresaCidade);
            pessoa.PessoaFisica.ConjugeEmpresaEstado = NormalizeState(request.Pessoa.ConjugeEmpresaEstado);
            pessoa.PessoaFisica.ConjugeEmpresaCep = DigitsOrNull(request.Pessoa.ConjugeEmpresaCep);
        }
        else
        {
            if (pessoa.PessoaFisica is not null)
            {
                dbContext.PessoasFisicas.Remove(pessoa.PessoaFisica);
                pessoa.PessoaFisica = null;
            }

            pessoa.PessoaJuridica ??= new PessoaJuridica { PessoaId = pessoa.Id };
            pessoa.PessoaJuridica.NomeEmpresa = pessoa.NomeDisplay;
            pessoa.PessoaJuridica.NomeFantasia = TrimOrNull(request.Pessoa.NomeFantasia);
            pessoa.PessoaJuridica.Atividade = TrimOrNull(request.Pessoa.Atividade);
            pessoa.PessoaJuridica.ReceitaMensal = request.Pessoa.ReceitaMensal;
            pessoa.PessoaJuridica.Cnpj = DigitsOrNull(request.Pessoa.Documento);
            pessoa.PessoaJuridica.InscricaoEstadual = DigitsOrNull(request.Pessoa.InscricaoEstadual);
            pessoa.PessoaJuridica.InscricaoMunicipal = DigitsOrNull(request.Pessoa.InscricaoMunicipal);
            pessoa.PessoaJuridica.DataAbertura = request.Pessoa.DataAbertura;
            pessoa.PessoaJuridica.EmpresaRua = TrimOrNull(request.Pessoa.Rua);
            pessoa.PessoaJuridica.EmpresaNumero = TrimOrNull(request.Pessoa.Numero);
            pessoa.PessoaJuridica.EmpresaComplemento = TrimOrNull(request.Pessoa.Complemento);
            pessoa.PessoaJuridica.EmpresaBairro = TrimOrNull(request.Pessoa.Bairro);
            pessoa.PessoaJuridica.EmpresaCidade = TrimOrNull(request.Pessoa.Cidade);
            pessoa.PessoaJuridica.EmpresaEstado = NormalizeState(request.Pessoa.Estado);
            pessoa.PessoaJuridica.EmpresaCep = DigitsOrNull(request.Pessoa.Cep);
            pessoa.PessoaJuridica.ResponsavelNome = TrimOrNull(request.Pessoa.ResponsavelNome);
            pessoa.PessoaJuridica.ResponsavelCargo = TrimOrNull(request.Pessoa.ResponsavelCargo);
            pessoa.PessoaJuridica.ResponsavelRua = TrimOrNull(request.Pessoa.ResponsavelRua);
            pessoa.PessoaJuridica.ResponsavelNumero = TrimOrNull(request.Pessoa.ResponsavelNumero);
            pessoa.PessoaJuridica.ResponsavelComplemento = TrimOrNull(request.Pessoa.ResponsavelComplemento);
            pessoa.PessoaJuridica.ResponsavelBairro = TrimOrNull(request.Pessoa.ResponsavelBairro);
            pessoa.PessoaJuridica.ResponsavelCidade = TrimOrNull(request.Pessoa.ResponsavelCidade);
            pessoa.PessoaJuridica.ResponsavelEstado = NormalizeState(request.Pessoa.ResponsavelEstado);
            pessoa.PessoaJuridica.ResponsavelCep = DigitsOrNull(request.Pessoa.ResponsavelCep);
            pessoa.PessoaJuridica.ResponsavelEstadoCivil = TrimOrNull(request.Pessoa.ResponsavelEstadoCivil);
            pessoa.PessoaJuridica.ResponsavelNacionalidade = TrimOrNull(request.Pessoa.ResponsavelNacionalidade);
            pessoa.PessoaJuridica.ResponsavelDataNascimento = request.Pessoa.ResponsavelDataNascimento;
            pessoa.PessoaJuridica.ResponsavelTelefone = DigitsOrNull(request.Pessoa.ResponsavelTelefone) ?? pessoa.Telefone;
            pessoa.PessoaJuridica.ResponsavelEmail = TrimOrNull(request.Pessoa.ResponsavelEmail) ?? pessoa.Email;
            pessoa.PessoaJuridica.ResponsavelRg = DigitsOrNull(request.Pessoa.ResponsavelRg);
            pessoa.PessoaJuridica.ResponsavelCpf = DigitsOrNull(request.Pessoa.ResponsavelCpf);
            pessoa.PessoaJuridica.ResponsavelProfissao = TrimOrNull(request.Pessoa.ResponsavelProfissao);
            pessoa.PessoaJuridica.ResponsavelOndeTrabalha = TrimOrNull(request.Pessoa.ResponsavelOndeTrabalha);
            pessoa.PessoaJuridica.ResponsavelEnderecoTrabalho = TrimOrNull(request.Pessoa.ResponsavelEnderecoTrabalho);
            pessoa.PessoaJuridica.ResponsavelNomeEmpresaTrabalho = TrimOrNull(request.Pessoa.ResponsavelNomeEmpresaTrabalho);
            pessoa.PessoaJuridica.ResponsavelTelefoneEmpresaTrabalho = DigitsOrNull(request.Pessoa.ResponsavelTelefoneEmpresaTrabalho);
            pessoa.PessoaJuridica.ResponsavelDadosBancarios = TrimOrNull(request.Pessoa.ResponsavelDadosBancarios);
            pessoa.PessoaJuridica.ResponsavelBancoCodigo = DigitsOrNull(request.Pessoa.ResponsavelBancoCodigo);
            pessoa.PessoaJuridica.ResponsavelBancoNome = TrimOrNull(request.Pessoa.ResponsavelBancoNome);
            pessoa.PessoaJuridica.ResponsavelAgenciaNumero = DigitsOrNull(request.Pessoa.ResponsavelAgenciaNumero);
            pessoa.PessoaJuridica.ResponsavelAgenciaDigito = TrimOrNull(request.Pessoa.ResponsavelAgenciaDigito);
            pessoa.PessoaJuridica.ResponsavelContaNumero = DigitsOrNull(request.Pessoa.ResponsavelContaNumero);
            pessoa.PessoaJuridica.ResponsavelContaDigito = TrimOrNull(request.Pessoa.ResponsavelContaDigito);
            pessoa.PessoaJuridica.ResponsavelContaTipo = request.Pessoa.ResponsavelContaTipo;
            pessoa.PessoaJuridica.ResponsavelTitularNome = TrimOrNull(request.Pessoa.ResponsavelTitularNome);
            pessoa.PessoaJuridica.ResponsavelTitularDocumento = DigitsOrNull(request.Pessoa.ResponsavelTitularDocumento);
            pessoa.PessoaJuridica.ResponsavelPixTipo = request.Pessoa.ResponsavelPixTipo;
            pessoa.PessoaJuridica.ResponsavelPixChave = NormalizePixChave(request.Pessoa.ResponsavelPixChave, request.Pessoa.ResponsavelPixTipo);
            pessoa.PessoaJuridica.ResponsavelRepassePreferencial = request.Pessoa.ResponsavelRepassePreferencial;
            pessoa.PessoaJuridica.ResponsavelObservacoes = TrimOrNull(request.Pessoa.ResponsavelObservacoes);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetPessoasCoreAsync(cancellationToken)).Single(x => x.Id == pessoa.Id);
    }

    public async Task SetPessoaActiveAsync(Guid pessoaId, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var pessoa = await dbContext.Pessoas.SingleOrDefaultAsync(x => x.Id == pessoaId, cancellationToken)
            ?? throw new InvalidOperationException("Pessoa não encontrada.");

        pessoa.Status = isActive ? RegistroStatus.Ativo : RegistroStatus.Inativo;
        pessoa.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }







}










