using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Monthoya.Core.Entities;
using Monthoya.Core.Integrations;
using Monthoya.Core.Services;
using Monthoya.Data.Storage;

namespace Monthoya.Data.RentalManagement;

public sealed class RentalManagementService(
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

        var summary = (await GetPessoasCoreAsync(cancellationToken)).Single(x => x.Id == pessoa.Id);
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

    public async Task<PessoaDocumentoSummary> CreatePessoaDocumentoAsync(CreatePessoaDocumentoRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
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
            DocumentoDe = string.IsNullOrWhiteSpace(request.DocumentoDe) ? "pessoa" : request.DocumentoDe.Trim(),
            Nome = request.Nome.Trim(),
            ContentType = TrimOrNull(request.ContentType),
            DataValidade = request.DataValidade,
            Observacoes = TrimOrNull(request.Observacoes),
            SkipOcrAutofill = !request.ApplyOcrToPessoa
        };
        documento.StoragePath = await StorePessoaDocumentoAsync(documento.Id, request.PessoaId, request.StoragePath.Trim(), documento.ContentType, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.OcrTextoExtraido))
        {
            documento.OcrTextoExtraido = TrimOrNull(request.OcrTextoExtraido);
            documento.OcrProcessadoEmUtc = DateTimeOffset.UtcNow;
            documento.OcrStatus = DocumentoOcrStatus.Processado;
        }
        else if (documentOcrService is not null)
        {
            var ocrResult = await documentOcrService.ExtractTextAsync(documento.StoragePath, documento.ContentType, cancellationToken);
            documento.OcrTextoExtraido = TrimOrNull(ocrResult.ExtractedText);
            documento.OcrProcessadoEmUtc = DateTimeOffset.UtcNow;
            documento.OcrStatus = ocrResult.Succeeded ? DocumentoOcrStatus.Processado : DocumentoOcrStatus.Erro;
            documento.OcrErroMensagem = TrimOrNull(ocrResult.ErrorMessage);

            if (request.ApplyOcrToPessoa && ocrResult.Succeeded && !string.IsNullOrWhiteSpace(ocrResult.ExtractedText))
            {
                var filledFields = await ApplyPessoaOcrFieldsAsync(request.PessoaId, documento.Tipo, documento.DocumentoDe, ocrResult.ExtractedText, cancellationToken);
                documento.OcrCamposAplicados = filledFields.Count == 0 ? null : string.Join(", ", filledFields);
            }
        }

        dbContext.PessoaDocumentos.Add(documento);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetPessoaDocumentosCoreAsync(request.PessoaId, cancellationToken)).Single(x => x.Id == documento.Id);
    }

    public async Task<PessoaDocumentoSummary> UpdatePessoaDocumentoOcrAsync(UpdatePessoaDocumentoOcrRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var documento = await dbContext.PessoaDocumentos.SingleOrDefaultAsync(x => x.Id == request.DocumentoId, cancellationToken)
            ?? throw new InvalidOperationException("Documento não encontrado.");

        documento.OcrTextoExtraido = TrimOrNull(request.OcrTextoExtraido);
        documento.OcrStatus = request.Succeeded ? DocumentoOcrStatus.Processado : DocumentoOcrStatus.Erro;
        documento.OcrProcessadoEmUtc = DateTimeOffset.UtcNow;
        documento.OcrErroMensagem = TrimOrNull(request.ErrorMessage);
        documento.OcrCamposAplicados = TrimOrNull(request.CamposAplicados);
        documento.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetPessoaDocumentosCoreAsync(documento.PessoaId, cancellationToken)).Single(x => x.Id == documento.Id);
    }

    public async Task DeletePessoaDocumentoAsync(Guid documentoId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var documento = await dbContext.PessoaDocumentos.SingleOrDefaultAsync(x => x.Id == documentoId, cancellationToken)
            ?? throw new InvalidOperationException("Documento não encontrado.");

        if (fileStorageService is not null)
        {
            await fileStorageService.DeleteAsync(documento.StoragePath, cancellationToken);
        }

        dbContext.PessoaDocumentos.Remove(documento);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<string> GetPessoaDocumentoOpenTargetAsync(Guid documentoId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var documento = await dbContext.PessoaDocumentos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == documentoId, cancellationToken)
            ?? throw new InvalidOperationException("Documento não encontrado.");

        if (Path.IsPathRooted(documento.StoragePath) || fileStorageService is null)
        {
            return documento.StoragePath;
        }

        var signedUrl = await fileStorageService.CreateSignedReadUrlAsync(documento.StoragePath, null, cancellationToken);
        return signedUrl.Url;
    }

    public async Task<IReadOnlyList<PessoaDocumentoSummary>> GetPessoaDocumentosAsync(Guid? pessoaId = null, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await GetPessoaDocumentosCoreAsync(pessoaId, cancellationToken);
    }

    private async Task<IReadOnlyList<PessoaDocumentoSummary>> GetPessoaDocumentosCoreAsync(Guid? pessoaId, CancellationToken cancellationToken)
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
            GetDocumentoDeLabel(x.DocumentoDe),
            x.Nome,
            x.StoragePath,
            x.DataValidade,
            x.Status == RegistroStatus.Ativo ? "Ativo" : "Inativo",
            GetEnumLabel(x.OcrStatus),
            x.OcrTextoExtraido,
            x.OcrProcessadoEmUtc,
            x.OcrErroMensagem,
            x.OcrCamposAplicados)).ToList();
    }

    public async Task<PessoaContratoAutofillContext?> GetPessoaContratoAutofillContextAsync(Guid pessoaId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var pessoa = (await GetPessoasCoreAsync(cancellationToken)).SingleOrDefault(x => x.Id == pessoaId);
        if (pessoa is null)
        {
            return null;
        }

        var documentos = await GetPessoaDocumentosCoreAsync(pessoaId, cancellationToken);
        var textoOcr = string.Join(
            Environment.NewLine,
            documentos
                .Where(x => !string.IsNullOrWhiteSpace(x.OcrTextoExtraido))
                .Select(x => $"[{x.Tipo} - {x.Nome}]{Environment.NewLine}{x.OcrTextoExtraido}"));

        return new PessoaContratoAutofillContext(pessoa, documentos, textoOcr);
    }

    public async Task<IReadOnlyList<ImovelSummary>> GetImoveisAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await GetImoveisCoreAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<ImovelSummary>> GetImoveisCoreAsync(CancellationToken cancellationToken)
    {
        var imoveis = await dbContext.Imoveis
            .AsNoTracking()
            .OrderBy(x => x.Rua)
            .ThenBy(x => x.Numero)
            .Select(x => new
            {
                x.Id,
                x.Rua,
                x.Numero,
                x.Bairro,
                Proprietario = x.Proprietario != null ? x.Proprietario.NomeDisplay : "-",
                x.TipoImovel,
                x.Finalidade,
                x.Status,
                x.ChavePosse,
                x.PublicarNoSite,
                x.PublicarNoApp,
                x.Destaque,
                x.ValorAluguel,
                x.ValorVenda,
                x.ChaveCodigo
            })
            .ToListAsync(cancellationToken);

        return imoveis.Select(x => new ImovelSummary(
            x.Id,
            $"{x.Rua}, {x.Numero}".Trim().Trim(','),
            x.Bairro,
            x.Proprietario,
            x.TipoImovel,
            GetImovelFinalidadeLabel(x.Finalidade),
            GetImovelStatusLabel(x.Status),
            GetImovelChavePosseLabel(x.ChavePosse),
            GetImovelPublicacaoLabel(x.PublicarNoSite, x.PublicarNoApp, x.Destaque),
            x.ValorAluguel,
            x.ValorVenda,
            x.ChaveCodigo)).ToList();
    }

    public async Task<ImovelDetails?> GetImovelAsync(Guid imovelId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var imovel = await dbContext.Imoveis
            .AsNoTracking()
            .Include(x => x.Proprietario)
            .SingleOrDefaultAsync(x => x.Id == imovelId, cancellationToken);

        if (imovel is null)
        {
            return null;
        }

        var summary = (await GetImoveisCoreAsync(cancellationToken)).Single(x => x.Id == imovel.Id);
        return new ImovelDetails(summary, ToImovelRequest(imovel));
    }

    public async Task<ImovelSummary> CreateImovelAsync(CreateImovelRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.ProprietarioId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione um proprietário.");
        }

        if (string.IsNullOrWhiteSpace(request.Rua))
        {
            throw new InvalidOperationException("Informe a rua do imóvel.");
        }

        var proprietario = await dbContext.Pessoas
            .SingleOrDefaultAsync(x => x.Id == request.ProprietarioId, cancellationToken)
            ?? throw new InvalidOperationException("Proprietário não encontrado.");

        var imovel = new Imovel();
        ApplyImovelRequest(imovel, request, proprietario.Id);

        dbContext.Imoveis.Add(imovel);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetImoveisCoreAsync(cancellationToken)).Single(x => x.Id == imovel.Id);
    }

    public async Task<ImovelSummary> UpdateImovelAsync(UpdateImovelRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione o imóvel para editar.");
        }

        ValidateImovelRequest(request.Imovel);

        var proprietario = await dbContext.Pessoas
            .SingleOrDefaultAsync(x => x.Id == request.Imovel.ProprietarioId, cancellationToken)
            ?? throw new InvalidOperationException("Proprietário não encontrado.");

        var imovel = await dbContext.Imoveis
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Imóvel não encontrado.");

        ApplyImovelRequest(imovel, request.Imovel, proprietario.Id);
        imovel.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetImoveisCoreAsync(cancellationToken)).Single(x => x.Id == imovel.Id);
    }

    public async Task SetImovelActiveAsync(Guid imovelId, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var imovel = await dbContext.Imoveis.SingleOrDefaultAsync(x => x.Id == imovelId, cancellationToken)
            ?? throw new InvalidOperationException("Imóvel não encontrado.");

        imovel.Status = isActive ? ImovelStatus.Disponivel : ImovelStatus.Inativo;
        imovel.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ImovelChaveMovimentoSummary>> GetImovelChaveMovimentosAsync(Guid? imovelId = null, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var query = dbContext.ImovelChaveMovimentos
            .AsNoTracking()
            .AsQueryable();

        if (imovelId.HasValue)
        {
            query = query.Where(x => x.ImovelId == imovelId.Value);
        }

        var now = DateTimeOffset.UtcNow;
        var movimentos = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.ImovelId,
                ImovelRua = x.Imovel != null ? x.Imovel.Rua : null,
                ImovelNumero = x.Imovel != null ? x.Imovel.Numero : null,
                x.Tipo,
                x.Status,
                x.ChaveCodigo,
                x.RetiradoPorNome,
                x.RetiradoPorTelefone,
                x.RetiradoPorDocumento,
                x.RetiradoPorRelacao,
                x.Motivo,
                x.RetiradoEm,
                x.PrevisaoDevolucaoEm,
                x.DevolvidoEm,
                x.DevolvidoParaNome,
                x.Observacoes
            })
            .ToListAsync(cancellationToken);

        return movimentos.Select(x =>
        {
            var status = x.Status == ImovelChaveMovimentoStatus.Retirada
                && x.PrevisaoDevolucaoEm.HasValue
                && x.PrevisaoDevolucaoEm.Value < now
                && !x.DevolvidoEm.HasValue
                    ? "Em atraso"
                    : GetEnumLabel(x.Status);

            return new ImovelChaveMovimentoSummary(
                x.Id,
                x.ImovelId,
                string.IsNullOrWhiteSpace(x.ImovelRua) ? "-" : $"{x.ImovelRua}, {x.ImovelNumero}".Trim().Trim(','),
                GetImovelChaveMovimentoTipoLabel(x.Tipo),
                status,
                x.ChaveCodigo,
                x.RetiradoPorNome,
                FormatPhoneForDisplay(x.RetiradoPorTelefone),
                x.RetiradoPorDocumento,
                x.RetiradoPorRelacao,
                x.Motivo,
                x.RetiradoEm,
                x.PrevisaoDevolucaoEm,
                x.DevolvidoEm,
                x.DevolvidoParaNome,
                x.Observacoes);
        }).ToList();
    }

    public async Task<ImovelChaveMovimentoSummary> CreateImovelChaveMovimentoAsync(CreateImovelChaveMovimentoRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.ImovelId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione o imóvel da chave.");
        }

        var imovel = await dbContext.Imoveis.SingleOrDefaultAsync(x => x.Id == request.ImovelId, cancellationToken)
            ?? throw new InvalidOperationException("Imóvel não encontrado.");

        if (request.Tipo == ImovelChaveMovimentoTipo.Retirada)
        {
            if (string.IsNullOrWhiteSpace(request.RetiradoPorNome))
            {
                throw new InvalidOperationException("Informe quem retirou a chave.");
            }

            if (!request.PrevisaoDevolucaoEm.HasValue)
            {
                throw new InvalidOperationException("Informe a previsão de devolução da chave.");
            }
        }

        var movimento = new ImovelChaveMovimento
        {
            ImovelId = request.ImovelId,
            ChaveCodigo = TrimOrNull(request.ChaveCodigo) ?? imovel.ChaveCodigo,
            Tipo = request.Tipo,
            RetiradoPorNome = TrimOrNull(request.RetiradoPorNome),
            RetiradoPorTelefone = DigitsOrNull(request.RetiradoPorTelefone),
            RetiradoPorDocumento = TrimOrNull(request.RetiradoPorDocumento),
            RetiradoPorRelacao = TrimOrNull(request.RetiradoPorRelacao),
            Motivo = TrimOrNull(request.Motivo),
            RetiradoEm = request.RetiradoEm ?? DateTimeOffset.Now,
            PrevisaoDevolucaoEm = request.PrevisaoDevolucaoEm,
            Status = request.Tipo == ImovelChaveMovimentoTipo.Retirada
                ? ImovelChaveMovimentoStatus.Retirada
                : ImovelChaveMovimentoStatus.ComImobiliaria,
            Observacoes = TrimOrNull(request.Observacoes)
        };

        dbContext.ImovelChaveMovimentos.Add(movimento);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetImovelChaveMovimentosAsync(request.ImovelId, cancellationToken)).Single(x => x.Id == movimento.Id);
    }

    public async Task<ImovelChaveMovimentoSummary> ReturnImovelChaveMovimentoAsync(ReturnImovelChaveMovimentoRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.MovimentoId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione a retirada de chave.");
        }

        var movimento = await dbContext.ImovelChaveMovimentos
            .SingleOrDefaultAsync(x => x.Id == request.MovimentoId, cancellationToken)
            ?? throw new InvalidOperationException("Movimentação de chave não encontrada.");

        if (movimento.DevolvidoEm.HasValue)
        {
            throw new InvalidOperationException("Esta chave já foi devolvida.");
        }

        movimento.Tipo = ImovelChaveMovimentoTipo.Devolucao;
        movimento.Status = ImovelChaveMovimentoStatus.ComImobiliaria;
        movimento.DevolvidoEm = DateTimeOffset.Now;
        movimento.DevolvidoParaNome = TrimOrNull(request.DevolvidoParaNome);
        movimento.Observacoes = MergeNotes(movimento.Observacoes, request.Observacoes);
        movimento.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetImovelChaveMovimentosAsync(movimento.ImovelId, cancellationToken)).Single(x => x.Id == movimento.Id);
    }

    public async Task<ImovelImagemSummary> CreateImovelImagemAsync(CreateImovelImagemRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.ImovelId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione o imóvel da foto.");
        }

        if (string.IsNullOrWhiteSpace(request.StoragePath))
        {
            throw new InvalidOperationException("Informe o caminho da foto do imóvel.");
        }

        var imovelExists = await dbContext.Imoveis.AnyAsync(x => x.Id == request.ImovelId, cancellationToken);
        if (!imovelExists)
        {
            throw new InvalidOperationException("Imóvel não encontrado.");
        }

        var isPublic = request.MediaCategory == ImovelMediaCategory.InspectionPhoto
            ? false
            : request.IsPublic;

        if (request.IsCover)
        {
            var currentCovers = await dbContext.ImovelImagens
                .Where(x => x.ImovelId == request.ImovelId && x.IsCover)
                .ToListAsync(cancellationToken);

            foreach (var currentCover in currentCovers)
            {
                currentCover.IsCover = false;
                currentCover.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
        }

        var imagem = new ImovelImagem
        {
            ImovelId = request.ImovelId,
            FileName = string.IsNullOrWhiteSpace(request.FileName)
                ? Path.GetFileName(request.StoragePath)
                : request.FileName.Trim(),
            StoragePath = await StoreImovelImagemAsync(request.ImovelId, request.StoragePath.Trim(), request.ContentType, request.MediaCategory, cancellationToken),
            ContentType = TrimOrNull(request.ContentType),
            DisplayOrder = request.DisplayOrder,
            Caption = TrimOrNull(request.Caption),
            IsCover = request.IsCover,
            IsPublic = isPublic,
            MediaCategory = request.MediaCategory,
            Source = request.Source
        };

        dbContext.Set<ImovelImagem>().Add(imagem);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetImovelImagensCoreAsync(request.ImovelId, cancellationToken)).Single(x => x.Id == imagem.Id);
    }

    public async Task<IReadOnlyList<ImovelImagemSummary>> GetImovelImagensAsync(Guid imovelId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await GetImovelImagensCoreAsync(imovelId, cancellationToken);
    }

    private async Task<IReadOnlyList<ImovelImagemSummary>> GetImovelImagensCoreAsync(Guid imovelId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<ImovelImagem>()
            .AsNoTracking()
            .Where(x => x.ImovelId == imovelId)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.FileName)
            .Select(x => new ImovelImagemSummary(
                x.Id,
                x.ImovelId,
                x.FileName,
                x.StoragePath,
                x.ContentType,
                x.DisplayOrder,
                x.Caption,
                x.IsCover,
                x.IsPublic,
                GetImovelMediaCategoryLabel(x.MediaCategory),
                GetImovelMediaSourceLabel(x.Source),
                x.Status == RegistroStatus.Ativo ? "Ativo" : "Inativo"))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LocacaoSummary>> GetLocacoesAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
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

    public async Task<IReadOnlyList<IndiceReajusteSummary>> GetIndicesReajusteAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await dbContext.IndicesReajuste.AsNoTracking().OrderBy(x => x.Nome)
            .Select(x => new IndiceReajusteSummary(x.Id, x.Nome, x.Codigo, x.Tipo == ReajusteTipo.Oficial ? "Oficial" : "Custom/manual", x.Percentual, x.Ativo ? "Ativo" : "Inativo"))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinanceiroSummary>> GetLancamentosFinanceirosAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await dbContext.LancamentosFinanceiros.AsNoTracking().OrderBy(x => x.DataVencimento)
            .Select(x => new FinanceiroSummary(x.Id, x.Tipo.ToString(), x.Categoria, x.Descricao, x.Valor, x.DataVencimento, x.Status.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BoletoSummary>> GetBoletosAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await dbContext.Boletos.AsNoTracking().OrderBy(x => x.DataVencimento)
            .Select(x => new BoletoSummary(x.Id, x.Status.ToString(), x.Valor, x.DataVencimento, x.BancoProvider))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotaFiscalSummary>> GetNotasFiscaisAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await dbContext.NotasFiscais.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new NotaFiscalSummary(x.Id, x.Status.ToString(), x.ValorServico, x.Provider, x.Numero, x.CodigoVerificacao))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentoModeloSummary>> GetDocumentoModelosAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await dbContext.DocumentosModelos.AsNoTracking().OrderBy(x => x.Tipo)
            .Select(x => new DocumentoModeloSummary(x.Id, x.Tipo, x.Nome, x.StatusRevisao.ToString(), x.Ativo ? "Ativo" : "Inativo"))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DimobDeclaracaoSummary>> GetDimobDeclaracoesAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await dbContext.DimobDeclaracoes.AsNoTracking().OrderByDescending(x => x.AnoCalendario)
            .Select(x => new DimobDeclaracaoSummary(x.Id, x.AnoCalendario, x.Status.ToString(), x.Observacoes))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ManutencaoSummary>> GetManutencoesAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await dbContext.ManutencoesImovel.AsNoTracking().OrderByDescending(x => x.DataSolicitacao)
            .Select(x => new ManutencaoSummary(x.Id, x.Descricao, x.Status.ToString(), x.DataSolicitacao, x.Valor))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VistoriaSummary>> GetVistoriasAsync(Guid? imovelId = null, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var query = dbContext.Vistorias
            .AsNoTracking()
            .Include(x => x.Imovel)
            .AsQueryable();

        if (imovelId.HasValue)
        {
            query = query.Where(x => x.ImovelId == imovelId.Value);
        }

        return await query.OrderByDescending(x => x.DataVistoria)
            .Select(x => new VistoriaSummary(
                x.Id,
                x.ImovelId,
                x.Imovel == null ? "-" : (x.Imovel.Rua + ", " + x.Imovel.Numero).Trim().Trim(','),
                GetVistoriaTipoLabel(x.Tipo),
                x.DataVistoria,
                x.Responsavel,
                GetVistoriaStatusLabel(x.WorkflowStatus),
                x.Observacoes))
            .ToListAsync(cancellationToken);
    }

    public async Task<VistoriaSummary> CreateVistoriaAsync(CreateVistoriaRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        if (request.ImovelId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione o imóvel da vistoria.");
        }

        var imovelExists = await dbContext.Imoveis.AnyAsync(x => x.Id == request.ImovelId, cancellationToken);
        if (!imovelExists)
        {
            throw new InvalidOperationException("Imóvel não encontrado.");
        }

        var vistoria = new Vistoria
        {
            ImovelId = request.ImovelId,
            LocacaoId = request.LocacaoId,
            Tipo = request.Tipo,
            DataVistoria = request.DataVistoria,
            Responsavel = TrimOrNull(request.Responsavel),
            WorkflowStatus = request.WorkflowStatus,
            Status = GetVistoriaStatusLabel(request.WorkflowStatus),
            Descricao = TrimOrNull(request.DescricaoGeral),
            DescricaoGeral = TrimOrNull(request.DescricaoGeral),
            Observacoes = TrimOrNull(request.Observacoes)
        };

        dbContext.Vistorias.Add(vistoria);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetVistoriasAsync(request.ImovelId, cancellationToken)).Single(x => x.Id == vistoria.Id);
    }

    private async Task<string> StorePessoaDocumentoAsync(
        Guid documentoId,
        Guid pessoaId,
        string storagePath,
        string? contentType,
        CancellationToken cancellationToken)
    {
        if (fileStorageService is null || !File.Exists(storagePath))
        {
            return NormalizeStoredPath(storagePath);
        }

        var fileName = Path.GetFileName(storagePath);
        var safeFileName = ConfiguredFileStorageService.SanitizeFileName(fileName);
        var objectPath = $"pessoas/{pessoaId}/documentos/{documentoId}/{safeFileName}";
        await using var stream = File.OpenRead(storagePath);
        var stored = await fileStorageService.SaveAsync(
            stream,
            new FileStorageSaveRequest("monthoya-documents", objectPath, fileName, contentType ?? GuessContentType(fileName)),
            cancellationToken);

        return $"{stored.Bucket}/{stored.ObjectPath}";
    }

    private async Task<string> StoreImovelImagemAsync(
        Guid imovelId,
        string storagePath,
        string? contentType,
        ImovelMediaCategory category,
        CancellationToken cancellationToken)
    {
        if (fileStorageService is null || !File.Exists(storagePath))
        {
            return NormalizeStoredPath(storagePath);
        }

        var imageId = Guid.NewGuid();
        var fileName = Path.GetFileName(storagePath);
        var safeFileName = ConfiguredFileStorageService.SanitizeFileName(fileName);
        var folder = category switch
        {
            ImovelMediaCategory.Document => "documentos",
            ImovelMediaCategory.InspectionPhoto => "vistorias",
            ImovelMediaCategory.MaintenancePhoto => "manutencoes",
            ImovelMediaCategory.Other => "outros",
            _ => "fotos"
        };
        var objectPath = $"imoveis/{imovelId}/{folder}/{imageId}/{safeFileName}";
        await using var stream = File.OpenRead(storagePath);
        var stored = await fileStorageService.SaveAsync(
            stream,
            new FileStorageSaveRequest("monthoya-property-images", objectPath, fileName, contentType ?? GuessContentType(fileName)),
            cancellationToken);

        return $"{stored.Bucket}/{stored.ObjectPath}";
    }

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

    private static PessoaOcrValues ExtractPessoaOcrValues(string text)
    {
        var normalized = text.Replace("\r", "\n", StringComparison.Ordinal);
        var cpf = FindRegex(normalized, @"\b\d{3}\.?\d{3}\.?\d{3}-?\d{2}\b");
        var cnpj = FindRegex(normalized, @"\b\d{2}\.?\d{3}\.?\d{3}/?\d{4}-?\d{2}\b");

        return new PessoaOcrValues(
            FindLabeledValue(normalized, "nome") ?? FindLabeledValue(normalized, "razão social") ?? FindLabeledValue(normalized, "razao social") ?? FindOcrNameFallback(normalized),
            cpf,
            cnpj,
            FindLabeledValue(normalized, "rg") ?? FindOcrRgFallback(normalized, cpf, cnpj),
            FindRegex(normalized, @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase),
            FindRegex(normalized, @"(?:\(?\d{2}\)?\s?)?(?:9\s?)?\d{4}[-\s]?\d{4}"),
            FindRegex(normalized, @"\b\d{5}-?\d{3}\b"),
            FindLabeledValue(normalized, "endereço") ?? FindLabeledValue(normalized, "endereco"));
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

    private static string? FindOcrNameFallback(string text)
    {
        var ignoredWords = new[]
        {
            "BRASIL", "VALIDA", "TERRITORIO", "NACIONAL", "REPUBLICA",
            "FEDERATIVA", "IDENTIDADE", "CARTEIRA", "DATA", "NASCIMENTO",
            "NATURALIDADE", "FILIACAO", "ORGAO", "EXPEDIDOR", "VIA", "CPF"
        };

        return text
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(line => Regex.Replace(line, @"[^A-Za-zÀ-ÿ\s]", " ").Trim())
            .Where(line => line.Count(char.IsLetter) >= 8)
            .Where(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2)
            .Where(line => !ignoredWords.Any(word => line.Contains(word, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(line => line.Length)
            .FirstOrDefault();
    }

    private static string? FindOcrRgFallback(string text, string? cpf, string? cnpj)
    {
        var matches = Regex.Matches(text, @"\b\d{1,2}\.?\d{3}\.?\d{3}-?[\dXx]\b|\b\d{7,10}-?[\dXx]?\b");
        foreach (Match match in matches)
        {
            var digits = DigitsOrNull(match.Value);
            if (string.IsNullOrWhiteSpace(digits)
                || digits.Length is < 7 or > 10
                || string.Equals(digits, DigitsOrNull(cpf), StringComparison.Ordinal)
                || string.Equals(digits, DigitsOrNull(cnpj), StringComparison.Ordinal))
            {
                continue;
            }

            return digits;
        }

        return null;
    }

    private static string GetPessoaRolesLabel(bool isProprietario, bool isLocatario, bool isFiador)
    {
        var roles = new List<string>();
        if (isProprietario) roles.Add("Proprietário");
        if (isLocatario) roles.Add("Locatário");
        if (isFiador) roles.Add("Fiador");
        return roles.Count == 0 ? "-" : string.Join(", ", roles);
    }

    private static CreatePessoaRequest ToPessoaRequest(Pessoa pessoa)
    {
        if (pessoa.TipoPessoa == TipoPessoa.Fisica && pessoa.PessoaFisica is not null)
        {
            var fisica = pessoa.PessoaFisica;
            return new CreatePessoaRequest(
                TipoPessoa: pessoa.TipoPessoa,
                NomeDisplay: pessoa.NomeDisplay,
                Telefone: FormatPhoneForDisplay(fisica.Telefone ?? pessoa.Telefone),
                Email: fisica.Email ?? pessoa.Email,
                Documento: FormatCpf(fisica.Cpf),
                Observacoes: pessoa.Observacoes,
                Rua: fisica.Rua,
                Numero: fisica.Numero,
                Complemento: fisica.Complemento,
                Bairro: fisica.Bairro,
                Cidade: fisica.Cidade,
                Estado: fisica.Estado,
                Cep: FormatCepForDisplay(fisica.Cep),
                EstadoCivil: fisica.EstadoCivil,
                PossuiTrabalho: fisica.PossuiTrabalho,
                PossuiPet: fisica.PossuiPet,
                PetQual: fisica.PetQual,
                Nacionalidade: fisica.Nacionalidade,
                DataNascimento: fisica.DataNascimento,
                Rg: FormatRgForDisplay(fisica.Rg),
                Profissao: fisica.Profissao,
                OndeTrabalha: fisica.OndeTrabalha,
                EnderecoTrabalho: fisica.EnderecoTrabalho,
                NomeEmpresaTrabalho: fisica.NomeEmpresaTrabalho,
                CnpjEmpresaTrabalho: FormatCnpj(fisica.CnpjEmpresaTrabalho),
                TelefoneEmpresaTrabalho: FormatPhoneForDisplay(fisica.TelefoneEmpresaTrabalho),
                EmailEmpresaTrabalho: fisica.EmailEmpresaTrabalho,
                CargoTrabalho: fisica.CargoTrabalho,
                RendaTrabalho: fisica.RendaTrabalho,
                TempoEmprego: fisica.TempoEmprego,
                TipoComprovanteRenda: fisica.TipoComprovanteRenda,
                OutrasInformacoes: fisica.OutrasInformacoes,
                TrabalhoOutrasInformacoes: fisica.TrabalhoOutrasInformacoes,
                EmpresaRua: fisica.EmpresaRua,
                EmpresaNumero: fisica.EmpresaNumero,
                EmpresaComplemento: fisica.EmpresaComplemento,
                EmpresaBairro: fisica.EmpresaBairro,
                EmpresaCidade: fisica.EmpresaCidade,
                EmpresaEstado: fisica.EmpresaEstado,
                EmpresaCep: FormatCepForDisplay(fisica.EmpresaCep),
                DadosBancarios: fisica.DadosBancarios,
                BancoCodigo: fisica.BancoCodigo,
                BancoNome: fisica.BancoNome,
                AgenciaNumero: fisica.AgenciaNumero,
                AgenciaDigito: fisica.AgenciaDigito,
                ContaNumero: fisica.ContaNumero,
                ContaDigito: fisica.ContaDigito,
                ContaTipo: fisica.ContaTipo,
                TitularNome: fisica.TitularNome,
                TitularDocumento: FormatCpfCnpjByLength(fisica.TitularDocumento),
                PixTipo: fisica.PixTipo,
                PixChave: FormatPixChaveForDisplay(fisica.PixChave, fisica.PixTipo),
                RepassePreferencial: fisica.RepassePreferencial,
                ConjugeNome: fisica.ConjugeNome,
                ConjugeRg: FormatRgForDisplay(fisica.ConjugeRg),
                ConjugeCpf: FormatCpf(fisica.ConjugeCpf),
                ConjugeEmail: fisica.ConjugeEmail,
                ConjugeDataNascimento: fisica.ConjugeDataNascimento,
                ConjugeProfissao: fisica.ConjugeProfissao,
                ConjugeNacionalidade: fisica.ConjugeNacionalidade,
                ConjugeTelefone: FormatPhoneForDisplay(fisica.ConjugeTelefone),
                ConjugeDadosBancarios: fisica.ConjugeDadosBancarios,
                ConjugeObservacoes: fisica.ConjugeObservacoes,
                ConjugeOutrasInformacoes: fisica.ConjugeOutrasInformacoes ?? fisica.ConjugeObservacoes,
                ConjugePossuiTrabalho: fisica.ConjugePossuiTrabalho,
                ConjugeNomeEmpresaTrabalho: fisica.ConjugeNomeEmpresaTrabalho,
                ConjugeCnpjEmpresaTrabalho: FormatCnpj(fisica.ConjugeCnpjEmpresaTrabalho),
                ConjugeTelefoneEmpresaTrabalho: FormatPhoneForDisplay(fisica.ConjugeTelefoneEmpresaTrabalho),
                ConjugeEmailEmpresaTrabalho: fisica.ConjugeEmailEmpresaTrabalho,
                ConjugeCargoTrabalho: fisica.ConjugeCargoTrabalho,
                ConjugeRendaTrabalho: fisica.ConjugeRendaTrabalho,
                ConjugeTempoEmprego: fisica.ConjugeTempoEmprego,
                ConjugeTipoComprovanteRenda: fisica.ConjugeTipoComprovanteRenda,
                ConjugeTrabalhoOutrasInformacoes: fisica.ConjugeTrabalhoOutrasInformacoes,
                ConjugeEmpresaRua: fisica.ConjugeEmpresaRua,
                ConjugeEmpresaNumero: fisica.ConjugeEmpresaNumero,
                ConjugeEmpresaComplemento: fisica.ConjugeEmpresaComplemento,
                ConjugeEmpresaBairro: fisica.ConjugeEmpresaBairro,
                ConjugeEmpresaCidade: fisica.ConjugeEmpresaCidade,
                ConjugeEmpresaEstado: fisica.ConjugeEmpresaEstado,
                ConjugeEmpresaCep: FormatCepForDisplay(fisica.ConjugeEmpresaCep));
        }

        var juridica = pessoa.PessoaJuridica;
        return new CreatePessoaRequest(
            TipoPessoa: pessoa.TipoPessoa,
            NomeDisplay: pessoa.NomeDisplay,
            Telefone: FormatPhoneForDisplay(pessoa.Telefone),
            Email: pessoa.Email,
            Documento: FormatCnpj(juridica?.Cnpj),
            Observacoes: pessoa.Observacoes,
            Rua: juridica?.EmpresaRua,
            Numero: juridica?.EmpresaNumero,
            Complemento: juridica?.EmpresaComplemento,
            Bairro: juridica?.EmpresaBairro,
            Cidade: juridica?.EmpresaCidade,
            Estado: juridica?.EmpresaEstado,
            Cep: FormatCepForDisplay(juridica?.EmpresaCep),
            NomeFantasia: juridica?.NomeFantasia,
            Atividade: juridica?.Atividade,
            ReceitaMensal: juridica?.ReceitaMensal,
            InscricaoEstadual: juridica?.InscricaoEstadual,
            InscricaoMunicipal: juridica?.InscricaoMunicipal,
            DataAbertura: juridica?.DataAbertura,
            ResponsavelNome: juridica?.ResponsavelNome,
            ResponsavelCargo: juridica?.ResponsavelCargo,
            ResponsavelEndereco: null,
            ResponsavelRua: juridica?.ResponsavelRua,
            ResponsavelNumero: juridica?.ResponsavelNumero,
            ResponsavelComplemento: juridica?.ResponsavelComplemento,
            ResponsavelBairro: juridica?.ResponsavelBairro,
            ResponsavelCidade: juridica?.ResponsavelCidade,
            ResponsavelEstado: juridica?.ResponsavelEstado,
            ResponsavelCep: FormatCepForDisplay(juridica?.ResponsavelCep),
            ResponsavelEstadoCivil: juridica?.ResponsavelEstadoCivil,
            ResponsavelNacionalidade: juridica?.ResponsavelNacionalidade,
            ResponsavelDataNascimento: juridica?.ResponsavelDataNascimento,
            ResponsavelTelefone: FormatPhoneForDisplay(juridica?.ResponsavelTelefone),
            ResponsavelEmail: juridica?.ResponsavelEmail,
            ResponsavelRg: FormatRgForDisplay(juridica?.ResponsavelRg),
            ResponsavelCpf: FormatCpf(juridica?.ResponsavelCpf),
            ResponsavelProfissao: juridica?.ResponsavelProfissao,
            ResponsavelOndeTrabalha: juridica?.ResponsavelOndeTrabalha,
            ResponsavelEnderecoTrabalho: juridica?.ResponsavelEnderecoTrabalho,
            ResponsavelNomeEmpresaTrabalho: juridica?.ResponsavelNomeEmpresaTrabalho,
            ResponsavelTelefoneEmpresaTrabalho: FormatPhoneForDisplay(juridica?.ResponsavelTelefoneEmpresaTrabalho),
            ResponsavelDadosBancarios: juridica?.ResponsavelDadosBancarios,
            ResponsavelBancoCodigo: juridica?.ResponsavelBancoCodigo,
            ResponsavelBancoNome: juridica?.ResponsavelBancoNome,
            ResponsavelAgenciaNumero: juridica?.ResponsavelAgenciaNumero,
            ResponsavelAgenciaDigito: juridica?.ResponsavelAgenciaDigito,
            ResponsavelContaNumero: juridica?.ResponsavelContaNumero,
            ResponsavelContaDigito: juridica?.ResponsavelContaDigito,
            ResponsavelContaTipo: juridica?.ResponsavelContaTipo,
            ResponsavelTitularNome: juridica?.ResponsavelTitularNome,
            ResponsavelTitularDocumento: FormatCpfCnpjByLength(juridica?.ResponsavelTitularDocumento),
            ResponsavelPixTipo: juridica?.ResponsavelPixTipo,
            ResponsavelPixChave: FormatPixChaveForDisplay(juridica?.ResponsavelPixChave, juridica?.ResponsavelPixTipo),
            ResponsavelRepassePreferencial: juridica?.ResponsavelRepassePreferencial,
            ResponsavelObservacoes: juridica?.ResponsavelObservacoes);
    }

    private static string NormalizeStoredPath(string storagePath) =>
        storagePath.Replace("\\", "/", StringComparison.Ordinal).Trim();

    private static string GuessContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
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

    private static string GetDocumentoDeLabel(string documentoDe) =>
        documentoDe switch
        {
            "conjuge" => "Cônjuge",
            "empresa_trabalho" => "Empresa onde trabalha",
            _ => "Pessoa"
        };

    private static string GetImovelPublicacaoLabel(Imovel imovel) =>
        GetImovelPublicacaoLabel(imovel.PublicarNoSite, imovel.PublicarNoApp, imovel.Destaque);

    private static string GetImovelPublicacaoLabel(bool publicarNoSite, bool publicarNoApp, bool destaque)
    {
        if (publicarNoSite && publicarNoApp)
        {
            return destaque ? "Site/App - destaque" : "Site/App";
        }

        if (publicarNoSite)
        {
            return destaque ? "Site - destaque" : "Site";
        }

        if (publicarNoApp)
        {
            return destaque ? "App - destaque" : "App";
        }

        return "Privado";
    }

    private static void ValidateImovelRequest(CreateImovelRequest request)
    {
        if (request.ProprietarioId == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione um proprietário.");
        }

        if (string.IsNullOrWhiteSpace(request.Rua))
        {
            throw new InvalidOperationException("Informe a rua do imóvel.");
        }
    }

    private static void ApplyImovelRequest(Imovel imovel, CreateImovelRequest request, Guid proprietarioId)
    {
        imovel.ProprietarioId = proprietarioId;
        imovel.Rua = request.Rua.Trim();
        imovel.Numero = TrimOrNull(request.Numero);
        imovel.Complemento = TrimOrNull(request.Complemento);
        imovel.Bairro = TrimOrNull(request.Bairro);
        imovel.Cidade = string.IsNullOrWhiteSpace(request.Cidade) ? "Paranavaí" : request.Cidade.Trim();
        imovel.Estado = string.IsNullOrWhiteSpace(request.Estado) ? "PR" : request.Estado.Trim().ToUpperInvariant();
        imovel.Cep = TrimOrNull(request.Cep);
        imovel.SaneparMatricula = TrimOrNull(request.SaneparMatricula);
        imovel.CopelMatricula = TrimOrNull(request.CopelMatricula);
        imovel.IptuInscricaoImobiliaria = TrimOrNull(request.IptuInscricaoImobiliaria);
        imovel.IptuCadastroImovel = TrimOrNull(request.IptuCadastroImovel);
imovel.ColetaLixo = TrimOrNull(request.ColetaLixo);
        imovel.TipoImovel = TrimOrNull(request.TipoImovel);
        imovel.Descricao = TrimOrNull(request.Descricao);
        imovel.DescricaoInterna = TrimOrNull(request.DescricaoInterna) ?? TrimOrNull(request.Descricao);
        imovel.DescricaoPublica = TrimOrNull(request.DescricaoPublica);
        imovel.ValorAluguel = request.ValorAluguel;
        imovel.ValorVenda = request.ValorVenda;
        imovel.ValorCondominio = request.ValorCondominio;
        imovel.ValorIptu = request.ValorIptu;
        imovel.Finalidade = request.Finalidade;
        imovel.Status = request.Status;
        imovel.Latitude = request.Latitude;
        imovel.Longitude = request.Longitude;
        imovel.Quartos = request.Quartos;
        imovel.Suites = request.Suites;
        imovel.Banheiros = request.Banheiros;
        imovel.VagasGaragem = request.VagasGaragem;
        imovel.AreaConstruida = request.AreaConstruida;
        imovel.AreaTerreno = request.AreaTerreno;
        imovel.Mobiliado = request.Mobiliado;
        imovel.AceitaPets = request.AceitaPets;
        imovel.PublicarNoSite = request.PublicarNoSite;
        imovel.PublicarNoApp = request.PublicarNoApp;
        imovel.Destaque = request.Destaque;
        imovel.MostrarEnderecoCompletoPublicamente = request.MostrarEnderecoCompletoPublicamente;
        imovel.ModoExibicaoEnderecoPublico = request.ModoExibicaoEnderecoPublico;
        imovel.ChavePosse = request.ChavePosse;
        imovel.ChaveCodigo = TrimOrNull(request.ChaveCodigo);
        imovel.ChaveQuemTem = TrimOrNull(request.ChaveQuemTem);
        imovel.ChaveTelefone = DigitsOrNull(request.ChaveTelefone);
        imovel.ChaveContatoNome = TrimOrNull(request.ChaveContatoNome);
        imovel.ChaveContatoDocumento = TrimOrNull(request.ChaveContatoDocumento);
        imovel.ChaveLocalRetirada = TrimOrNull(request.ChaveLocalRetirada);
        imovel.ChaveMelhorHorario = TrimOrNull(request.ChaveMelhorHorario);
        imovel.ChaveAutorizacaoNecessaria = request.ChaveAutorizacaoNecessaria;
        imovel.ChaveObservacoes = TrimOrNull(request.ChaveObservacoes);
        imovel.Observacoes = TrimOrNull(request.Observacoes);
    }

    private static CreateImovelRequest ToImovelRequest(Imovel imovel) =>
        new(
            imovel.ProprietarioId,
            imovel.Rua,
            imovel.Numero,
            imovel.Bairro,
            imovel.Cidade,
            imovel.Estado,
            imovel.ValorAluguel,
            imovel.Finalidade,
            imovel.Observacoes,
            imovel.Complemento,
            imovel.Cep,
            imovel.SaneparMatricula,
            imovel.CopelMatricula,
            imovel.IptuInscricaoImobiliaria,
            imovel.IptuCadastroImovel,
imovel.ColetaLixo,
            imovel.TipoImovel,
            imovel.Descricao,
            imovel.ValorVenda,
            imovel.Latitude,
            imovel.Longitude,
            imovel.Status,
            imovel.ValorCondominio,
            imovel.ValorIptu,
            imovel.Quartos,
            imovel.Suites,
            imovel.Banheiros,
            imovel.VagasGaragem,
            imovel.AreaConstruida,
            imovel.AreaTerreno,
            imovel.Mobiliado,
            imovel.AceitaPets,
            imovel.DescricaoInterna,
            imovel.DescricaoPublica,
            imovel.PublicarNoSite,
            imovel.PublicarNoApp,
            imovel.Destaque,
            imovel.MostrarEnderecoCompletoPublicamente,
            imovel.ModoExibicaoEnderecoPublico,
            imovel.ChavePosse,
            imovel.ChaveCodigo,
            imovel.ChaveQuemTem,
            FormatPhoneForDisplay(imovel.ChaveTelefone),
            imovel.ChaveContatoNome,
            imovel.ChaveContatoDocumento,
            imovel.ChaveLocalRetirada,
            imovel.ChaveMelhorHorario,
            imovel.ChaveAutorizacaoNecessaria,
            imovel.ChaveObservacoes);

    private static string GetImovelFinalidadeLabel(ImovelFinalidade finalidade) =>
        finalidade switch
        {
            ImovelFinalidade.Locacao => "Locação",
            ImovelFinalidade.Venda => "Venda",
            ImovelFinalidade.Ambos => "Ambos",
            _ => finalidade.ToString()
        };

    private static string GetImovelStatusLabel(ImovelStatus status) =>
        status switch
        {
            ImovelStatus.Disponivel => "Disponível",
            ImovelStatus.Reservado => "Reservado",
            ImovelStatus.Locado => "Locado",
            ImovelStatus.Vendido => "Vendido",
            ImovelStatus.Inativo => "Inativo",
            _ => status.ToString()
        };

    private static string GetImovelChavePosseLabel(ImovelChavePosse posse) =>
        posse switch
        {
            ImovelChavePosse.NaoCadastrada => "Sem chave",
            ImovelChavePosse.Imobiliaria => "Na imobiliária",
            ImovelChavePosse.Proprietario => "Com proprietário",
            ImovelChavePosse.Locatario => "Com locatário",
            ImovelChavePosse.Terceiro => "Com terceiro",
            ImovelChavePosse.Outro => "Outro",
            _ => posse.ToString()
        };

    private static string GetImovelChaveMovimentoTipoLabel(ImovelChaveMovimentoTipo tipo) =>
        tipo switch
        {
            ImovelChaveMovimentoTipo.Retirada => "Retirada",
            ImovelChaveMovimentoTipo.Devolucao => "Devolução",
            ImovelChaveMovimentoTipo.Transferencia => "Transferência",
            ImovelChaveMovimentoTipo.MarcadaPerdida => "Perdida",
            ImovelChaveMovimentoTipo.Outro => "Outro",
            _ => tipo.ToString()
        };

    private static string? MergeNotes(string? current, string? added)
    {
        if (string.IsNullOrWhiteSpace(added))
        {
            return TrimOrNull(current);
        }

        if (string.IsNullOrWhiteSpace(current))
        {
            return added.Trim();
        }

        return $"{current.Trim()}{Environment.NewLine}{added.Trim()}";
    }

    private static string GetImovelMediaCategoryLabel(ImovelMediaCategory category) =>
        category switch
        {
            ImovelMediaCategory.PropertyPhoto => "Foto pública do imóvel",
            ImovelMediaCategory.Document => "Documento",
            ImovelMediaCategory.InspectionPhoto => "Foto de vistoria",
            ImovelMediaCategory.MaintenancePhoto => "Foto de manutenção",
            ImovelMediaCategory.Other => "Outro",
            _ => category.ToString()
        };

    private static string GetImovelMediaSourceLabel(ImovelMediaSource source) =>
        source switch
        {
            ImovelMediaSource.Windows => "Windows",
            ImovelMediaSource.AndroidStaff => "Android equipe",
            ImovelMediaSource.Website => "Site",
            ImovelMediaSource.Import => "Importação",
            _ => source.ToString()
        };

    private static string GetVistoriaTipoLabel(VistoriaTipo tipo) =>
        tipo switch
        {
            VistoriaTipo.InicialProprietario => "Inicial do proprietário",
            VistoriaTipo.Entrada => "Entrada da locação",
            VistoriaTipo.Saida => "Saída da locação",
            VistoriaTipo.Periodica => "Periódica",
            VistoriaTipo.Manutencao => "Manutenção",
            VistoriaTipo.Outros => "Outra",
            _ => tipo.ToString()
        };

    private static string GetVistoriaStatusLabel(VistoriaStatus status) =>
        status switch
        {
            VistoriaStatus.Draft => "Rascunho",
            VistoriaStatus.InProgress => "Em andamento",
            VistoriaStatus.ReadyToReview => "Pronta para revisão",
            VistoriaStatus.Finished => "Finalizada",
            VistoriaStatus.SignedPaper => "Assinada em papel",
            VistoriaStatus.SignedDigitally => "Assinada digitalmente",
            VistoriaStatus.Canceled => "Cancelada",
            _ => status.ToString()
        };

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddStreetSuggestion(ISet<string> suggestions, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            suggestions.Add(value.Trim());
        }
    }

    private static string? NormalizePixChave(string? value, PixChaveTipo? tipo)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return tipo is PixChaveTipo.Cpf or PixChaveTipo.Cnpj or PixChaveTipo.Telefone
            ? DigitsOrNull(value)
            : value.Trim();
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

    private static string? NormalizeState(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string GetEnumLabel<T>(T value) where T : struct, Enum => value.ToString();

    private sealed record PessoaOcrValues(
        string? Nome,
        string? Cpf,
        string? Cnpj,
        string? Rg,
        string? Email,
        string? Telefone,
        string? Cep,
        string? Endereco);
}



