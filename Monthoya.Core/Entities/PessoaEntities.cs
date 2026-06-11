namespace Monthoya.Core.Entities;

public sealed class Pessoa : BaseEntity
{
    public TipoPessoa TipoPessoa { get; set; } = TipoPessoa.Fisica;
    public string NomeDisplay { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Observacoes { get; set; }
    public RegistroStatus Status { get; set; } = RegistroStatus.Ativo;
    public ICollection<PessoaRole> Roles { get; set; } = new List<PessoaRole>();
    public PessoaFisica? PessoaFisica { get; set; }
    public PessoaJuridica? PessoaJuridica { get; set; }
    public ICollection<PessoaDocumento> Documentos { get; set; } = new List<PessoaDocumento>();
}

public sealed class PessoaRole : BaseEntity
{
    public Guid PessoaId { get; set; }
    public Pessoa? Pessoa { get; set; }
    public PessoaRoleTipo Role { get; set; }
}

public sealed class PessoaFisica
{
    public Guid PessoaId { get; set; }
    public Pessoa? Pessoa { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Rua { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }
    public string? Cep { get; set; }
    public string? EstadoCivil { get; set; }
    public bool? PossuiTrabalho { get; set; }
    public bool? PossuiPet { get; set; }
    public string? PetQual { get; set; }
    public string? Nacionalidade { get; set; }
    public DateOnly? DataNascimento { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Rg { get; set; }
    public string? Cpf { get; set; }
    public string? Profissao { get; set; }
    public string? OndeTrabalha { get; set; }
    public string? EnderecoTrabalho { get; set; }
    public string? NomeEmpresaTrabalho { get; set; }
    public string? CnpjEmpresaTrabalho { get; set; }
    public string? TelefoneEmpresaTrabalho { get; set; }
    public string? EmailEmpresaTrabalho { get; set; }
    public string? CargoTrabalho { get; set; }
    public decimal? RendaTrabalho { get; set; }
    public string? TempoEmprego { get; set; }
    public string? TipoComprovanteRenda { get; set; }
    public string? OutrasInformacoes { get; set; }
    public string? TrabalhoOutrasInformacoes { get; set; }
    public string? EmpresaRua { get; set; }
    public string? EmpresaNumero { get; set; }
    public string? EmpresaComplemento { get; set; }
    public string? EmpresaBairro { get; set; }
    public string? EmpresaCidade { get; set; }
    public string? EmpresaEstado { get; set; }
    public string? EmpresaCep { get; set; }
    public string? DadosBancarios { get; set; }
    public string? BancoCodigo { get; set; }
    public string? BancoNome { get; set; }
    public string? AgenciaNumero { get; set; }
    public string? AgenciaDigito { get; set; }
    public string? ContaNumero { get; set; }
    public string? ContaDigito { get; set; }
    public ContaBancariaTipo? ContaTipo { get; set; }
    public string? TitularNome { get; set; }
    public string? TitularDocumento { get; set; }
    public PixChaveTipo? PixTipo { get; set; }
    public string? PixChave { get; set; }
    public MetodoRepassePreferencial? RepassePreferencial { get; set; }
    public string? ConjugeNome { get; set; }
    public string? ConjugeRg { get; set; }
    public string? ConjugeCpf { get; set; }
    public string? ConjugeEmail { get; set; }
    public DateOnly? ConjugeDataNascimento { get; set; }
    public string? ConjugeProfissao { get; set; }
    public string? ConjugeNacionalidade { get; set; }
    public string? ConjugeTelefone { get; set; }
    public string? ConjugeDadosBancarios { get; set; }
    public string? ConjugeObservacoes { get; set; }
    public string? ConjugeOutrasInformacoes { get; set; }
    public bool? ConjugePossuiTrabalho { get; set; }
    public string? ConjugeNomeEmpresaTrabalho { get; set; }
    public string? ConjugeCnpjEmpresaTrabalho { get; set; }
    public string? ConjugeTelefoneEmpresaTrabalho { get; set; }
    public string? ConjugeEmailEmpresaTrabalho { get; set; }
    public string? ConjugeCargoTrabalho { get; set; }
    public decimal? ConjugeRendaTrabalho { get; set; }
    public string? ConjugeTempoEmprego { get; set; }
    public string? ConjugeTipoComprovanteRenda { get; set; }
    public string? ConjugeTrabalhoOutrasInformacoes { get; set; }
    public string? ConjugeEmpresaRua { get; set; }
    public string? ConjugeEmpresaNumero { get; set; }
    public string? ConjugeEmpresaComplemento { get; set; }
    public string? ConjugeEmpresaBairro { get; set; }
    public string? ConjugeEmpresaCidade { get; set; }
    public string? ConjugeEmpresaEstado { get; set; }
    public string? ConjugeEmpresaCep { get; set; }
}

public sealed class PessoaJuridica
{
    public Guid PessoaId { get; set; }
    public Pessoa? Pessoa { get; set; }
    public string NomeEmpresa { get; set; } = string.Empty;
    public string? NomeFantasia { get; set; }
    public string? Atividade { get; set; }
    public decimal? ReceitaMensal { get; set; }
    public string? Cnpj { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public DateOnly? DataAbertura { get; set; }
    public string? EmpresaRua { get; set; }
    public string? EmpresaNumero { get; set; }
    public string? EmpresaComplemento { get; set; }
    public string? EmpresaBairro { get; set; }
    public string? EmpresaCidade { get; set; }
    public string? EmpresaEstado { get; set; }
    public string? EmpresaCep { get; set; }
    public string? ResponsavelNome { get; set; }
    public string? ResponsavelCargo { get; set; }
    public string? ResponsavelRua { get; set; }
    public string? ResponsavelNumero { get; set; }
    public string? ResponsavelComplemento { get; set; }
    public string? ResponsavelBairro { get; set; }
    public string? ResponsavelCidade { get; set; }
    public string? ResponsavelEstado { get; set; }
    public string? ResponsavelCep { get; set; }
    public string? ResponsavelEstadoCivil { get; set; }
    public string? ResponsavelNacionalidade { get; set; }
    public DateOnly? ResponsavelDataNascimento { get; set; }
    public string? ResponsavelTelefone { get; set; }
    public string? ResponsavelEmail { get; set; }
    public string? ResponsavelRg { get; set; }
    public string? ResponsavelCpf { get; set; }
    public string? ResponsavelProfissao { get; set; }
    public string? ResponsavelOndeTrabalha { get; set; }
    public string? ResponsavelEnderecoTrabalho { get; set; }
    public string? ResponsavelNomeEmpresaTrabalho { get; set; }
    public string? ResponsavelTelefoneEmpresaTrabalho { get; set; }
    public string? ResponsavelDadosBancarios { get; set; }
    public string? ResponsavelBancoCodigo { get; set; }
    public string? ResponsavelBancoNome { get; set; }
    public string? ResponsavelAgenciaNumero { get; set; }
    public string? ResponsavelAgenciaDigito { get; set; }
    public string? ResponsavelContaNumero { get; set; }
    public string? ResponsavelContaDigito { get; set; }
    public ContaBancariaTipo? ResponsavelContaTipo { get; set; }
    public string? ResponsavelTitularNome { get; set; }
    public string? ResponsavelTitularDocumento { get; set; }
    public PixChaveTipo? ResponsavelPixTipo { get; set; }
    public string? ResponsavelPixChave { get; set; }
    public MetodoRepassePreferencial? ResponsavelRepassePreferencial { get; set; }
    public string? ResponsavelObservacoes { get; set; }
}

public sealed class PessoaDocumento : BaseEntity
{
    public Guid PessoaId { get; set; }
    public Pessoa? Pessoa { get; set; }
    public string Tipo { get; set; } = "outros";
    public string DocumentoDe { get; set; } = "pessoa";
    public string Nome { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public DateOnly? DataValidade { get; set; }
    public RegistroStatus Status { get; set; } = RegistroStatus.Ativo;
    public string? Observacoes { get; set; }
    public DocumentoOcrStatus OcrStatus { get; set; } = DocumentoOcrStatus.NaoProcessado;
    public string? OcrTextoExtraido { get; set; }
    public DateTimeOffset? OcrProcessadoEmUtc { get; set; }
    public string? OcrErroMensagem { get; set; }
    public string? OcrCamposAplicados { get; set; }
    public bool SkipOcrAutofill { get; set; }
}
