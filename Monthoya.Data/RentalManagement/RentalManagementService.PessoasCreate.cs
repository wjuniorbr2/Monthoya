using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.RentalManagement;

public sealed partial class RentalManagementService
{
    public async Task<PessoaSummary> CreatePessoaAsync(CreatePessoaRequest request, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        ValidateCreatePessoaRequest(request);

        var pessoa = CreatePessoaBase(request);

        if (request.TipoPessoa == TipoPessoa.Fisica)
        {
            pessoa.PessoaFisica = CreatePessoaFisica(request, pessoa);
        }
        else
        {
            pessoa.PessoaJuridica = CreatePessoaJuridica(request, pessoa);
        }

        dbContext.Pessoas.Add(pessoa);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (await GetPessoasCoreAsync(cancellationToken)).Single(x => x.Id == pessoa.Id);
    }


    private PessoaFisica CreatePessoaFisica(CreatePessoaRequest request, Pessoa pessoa)
    {
        return new PessoaFisica
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

    private PessoaJuridica CreatePessoaJuridica(CreatePessoaRequest request, Pessoa pessoa)
    {
        return new PessoaJuridica
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

    private static void ValidateCreatePessoaRequest(CreatePessoaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NomeDisplay))
        {
            throw new InvalidOperationException("Informe o nome da pessoa.");
        }
    }

    private Pessoa CreatePessoaBase(CreatePessoaRequest request)
    {
        return new Pessoa
        {
            TipoPessoa = request.TipoPessoa,
            NomeDisplay = request.NomeDisplay.Trim(),
            Telefone = DigitsOrNull(request.Telefone),
            Email = request.Email?.Trim(),
            Observacoes = request.Observacoes?.Trim()
        };
    }
}
