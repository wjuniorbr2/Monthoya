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
            .Include(x => x.PessoaFisica)
            .Include(x => x.PessoaJuridica)
            .OrderBy(x => x.NomeDisplay)
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
                FormatCpfCnpjForDisplay(x.TipoPessoa, x.TipoPessoa == TipoPessoa.Fisica ? x.PessoaFisica?.Cpf : x.PessoaJuridica?.Cnpj),
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

        if (documentOcrService is not null)
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

        return (await GetImoveisCoreAsync(cancellationToken)).Single(x => x.Id == imovel.Id);
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

        var imagem = new ImovelImagem
        {
            ImovelId = request.ImovelId,
            FileName = string.IsNullOrWhiteSpace(request.FileName)
                ? Path.GetFileName(request.StoragePath)
                : request.FileName.Trim(),
            StoragePath = await StoreImovelImagemAsync(request.ImovelId, request.StoragePath.Trim(), request.ContentType, cancellationToken),
            ContentType = TrimOrNull(request.ContentType),
            DisplayOrder = request.DisplayOrder
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

    public async Task<IReadOnlyList<VistoriaSummary>> GetVistoriasAsync(CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        return await dbContext.Vistorias.AsNoTracking().OrderByDescending(x => x.DataVistoria)
            .Select(x => new VistoriaSummary(x.Id, x.Tipo.ToString(), x.DataVistoria, x.Responsavel, x.Status))
            .ToListAsync(cancellationToken);
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
        CancellationToken cancellationToken)
    {
        if (fileStorageService is null || !File.Exists(storagePath))
        {
            return NormalizeStoredPath(storagePath);
        }

        var imageId = Guid.NewGuid();
        var fileName = Path.GetFileName(storagePath);
        var safeFileName = ConfiguredFileStorageService.SanitizeFileName(fileName);
        var objectPath = $"imoveis/{imovelId}/fotos/{imageId}/{safeFileName}";
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

    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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



