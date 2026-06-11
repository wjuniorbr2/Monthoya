using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<PessoaSummary> UpdatePessoaAsync(UpdatePessoaRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        ValidateUpdatePessoaRequest(request);

        var pessoa = await dbContext.Pessoas
            .Include(x => x.PessoaFisica)
            .Include(x => x.PessoaJuridica)
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Pessoa não encontrada.");

        UpdatePessoaBase(pessoa, request);

        if (request.Pessoa.TipoPessoa == TipoPessoa.Fisica)
        {
            UpdatePessoaFisica(pessoa, request);
        }
        else
        {
            UpdatePessoaJuridica(pessoa, request);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetPessoasCoreAsync(cancellationToken)).Single(x => x.Id == pessoa.Id);
    }


    private void UpdatePessoaFisica(Pessoa pessoa, UpdatePessoaRequest request)
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


    private void UpdatePessoaJuridica(Pessoa pessoa, UpdatePessoaRequest request)
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

    private static void ValidateUpdatePessoaRequest(UpdatePessoaRequest request)
    {
        if (request.Id == Guid.Empty)
        {
            throw new InvalidOperationException("Selecione a pessoa para editar.");
        }

        if (string.IsNullOrWhiteSpace(request.Pessoa.NomeDisplay))
        {
            throw new InvalidOperationException("Informe o nome da pessoa.");
        }
    }

    private void UpdatePessoaBase(Pessoa pessoa, UpdatePessoaRequest request)
    {
        pessoa.TipoPessoa = request.Pessoa.TipoPessoa;
        pessoa.NomeDisplay = request.Pessoa.NomeDisplay.Trim();
        pessoa.Telefone = DigitsOrNull(request.Pessoa.Telefone);
        pessoa.Email = TrimOrNull(request.Pessoa.Email);
        pessoa.Observacoes = TrimOrNull(request.Pessoa.Observacoes);
        pessoa.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
