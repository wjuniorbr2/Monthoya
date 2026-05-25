namespace Monthoya.Core.Entities;

public enum TipoPessoa { Fisica = 0, Juridica = 1 }
public enum PessoaRoleTipo { Proprietario = 0, Locatario = 1, Fiador = 2 }
public enum RegistroStatus { Ativo = 0, Inativo = 1 }
public enum ImovelFinalidade { Locacao = 0, Venda = 1, Ambos = 2 }
public enum ImovelStatus { Disponivel = 0, Locado = 1, Vendido = 2, Inativo = 3 }
public enum LocacaoStatus { Ativa = 0, Encerrada = 1, Cancelada = 2 }
public enum ReajusteTipo { Oficial = 0, Custom = 1 }
public enum ModeloTaxaAdministracao { FixaMensal = 0, PercentualAluguel = 1, PrimeiroAluguel = 2, TaxaContrato = 3, TaxaRenovacao = 4, SemTaxa = 5, Custom = 6 }
public enum FinanceiroTipo { Pagar = 0, Receber = 1 }
public enum FinanceiroStatus { Pendente = 0, Pago = 1, Atrasado = 2, Cancelado = 3 }
public enum BoletoStatus { Rascunho = 0, Emitido = 1, Registrado = 2, Pago = 3, Cancelado = 4, Erro = 5 }
public enum NotaFiscalStatus { Rascunho = 0, Emitida = 1, Cancelada = 2, Erro = 3 }
public enum CertificadoTipo { A1 = 0 }
public enum CertificadoStatus { Ativo = 0, Vencido = 1, Revogado = 2, Inativo = 3 }
public enum DocumentoModeloStatusRevisao { Inicial = 0, PendenteRevisao = 1, Aprovado = 2 }
public enum DimobStatus { Rascunho = 0, Conferida = 1, Exportada = 2, Entregue = 3, Retificada = 4 }
public enum ManutencaoStatus { Solicitada = 0, EmAndamento = 1, Concluida = 2, Cancelada = 3 }
public enum VistoriaTipo { Entrada = 0, Saida = 1, Periodica = 2, Outros = 3 }

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
    public string? Endereco { get; set; }
    public string? EstadoCivil { get; set; }
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
    public string? TelefoneEmpresaTrabalho { get; set; }
    public string? DadosBancarios { get; set; }
    public string? ConjugeNome { get; set; }
    public string? ConjugeRg { get; set; }
    public string? ConjugeCpf { get; set; }
    public DateOnly? ConjugeDataNascimento { get; set; }
    public string? ConjugeProfissao { get; set; }
    public string? ConjugeNacionalidade { get; set; }
    public string? ConjugeTelefone { get; set; }
}

public sealed class PessoaJuridica
{
    public Guid PessoaId { get; set; }
    public Pessoa? Pessoa { get; set; }
    public string NomeEmpresa { get; set; } = string.Empty;
    public string? Cnpj { get; set; }
    public string? EnderecoEmpresa { get; set; }
    public string? ResponsavelNome { get; set; }
    public string? ResponsavelEndereco { get; set; }
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
}

public sealed class PessoaDocumento : BaseEntity
{
    public Guid PessoaId { get; set; }
    public Pessoa? Pessoa { get; set; }
    public string Tipo { get; set; } = "outros";
    public string Nome { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public DateOnly? DataValidade { get; set; }
    public RegistroStatus Status { get; set; } = RegistroStatus.Ativo;
    public string? Observacoes { get; set; }
}

public sealed class Imovel : BaseEntity
{
    public Guid ProprietarioId { get; set; }
    public Pessoa? Proprietario { get; set; }
    public string Rua { get; set; } = string.Empty;
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string Cidade { get; set; } = "Paranavaí";
    public string Estado { get; set; } = "PR";
    public string? Cep { get; set; }
    public string? SaneparMatricula { get; set; }
    public string? CopelMatricula { get; set; }
    public string? IptuMatricula { get; set; }
    public string? TipoImovel { get; set; }
    public string? Descricao { get; set; }
    public decimal? ValorAluguel { get; set; }
    public decimal? ValorVenda { get; set; }
    public ImovelFinalidade Finalidade { get; set; } = ImovelFinalidade.Locacao;
    public ImovelStatus Status { get; set; } = ImovelStatus.Disponivel;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Observacoes { get; set; }
}

public sealed class Locacao : BaseEntity
{
    public Guid ImovelId { get; set; }
    public Imovel? Imovel { get; set; }
    public Guid LocatarioId { get; set; }
    public Pessoa? Locatario { get; set; }
    public Guid ProprietarioId { get; set; }
    public Pessoa? Proprietario { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public int? PeriodoMeses { get; set; }
    public int DiaBase { get; set; } = 1;
    public int VencimentoLocatarioDia { get; set; } = 10;
    public int? VencimentoProprietarioDia { get; set; }
    public decimal ValorAluguel { get; set; }
    public bool AluguelAntecipado { get; set; }
    public decimal? MultaPercentual { get; set; }
    public decimal? JurosPercentual { get; set; }
    public bool DescontoAteVencimentoAtivo { get; set; }
    public decimal? DescontoAteVencimentoValor { get; set; }
    public decimal? DescontoAteVencimentoPercentual { get; set; }
    public Guid? IndiceReajusteId { get; set; }
    public IndiceReajuste? IndiceReajuste { get; set; }
    public DateOnly? DataProximoReajuste { get; set; }
    public ModeloTaxaAdministracao ModeloTaxaAdministracao { get; set; } = ModeloTaxaAdministracao.PercentualAluguel;
    public decimal? TaxaAdministracaoValor { get; set; }
    public decimal? TaxaAdministracaoPercentual { get; set; }
    public decimal? TaxaContratoValor { get; set; }
    public decimal? TaxaRenovacaoValor { get; set; }
    public LocacaoStatus Status { get; set; } = LocacaoStatus.Ativa;
    public string? Observacoes { get; set; }
    public ICollection<LocacaoFiador> Fiadores { get; set; } = new List<LocacaoFiador>();
}

public sealed class LocacaoFiador : BaseEntity
{
    public Guid LocacaoId { get; set; }
    public Locacao? Locacao { get; set; }
    public Guid FiadorId { get; set; }
    public Pessoa? Fiador { get; set; }
}

public sealed class IndiceReajuste : BaseEntity
{
    public string Nome { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public ReajusteTipo Tipo { get; set; }
    public decimal? Percentual { get; set; }
    public DateOnly? DataReferencia { get; set; }
    public string? Observacoes { get; set; }
    public bool Ativo { get; set; } = true;
}

public sealed class LancamentoFinanceiro : BaseEntity
{
    public Guid? LocacaoId { get; set; }
    public Guid? ImovelId { get; set; }
    public Guid? PessoaId { get; set; }
    public FinanceiroTipo Tipo { get; set; }
    public string Categoria { get; set; } = "outros";
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateOnly? DataCompetencia { get; set; }
    public DateOnly DataVencimento { get; set; }
    public DateOnly? DataPagamento { get; set; }
    public FinanceiroStatus Status { get; set; } = FinanceiroStatus.Pendente;
    public string? Origem { get; set; }
    public string? Observacoes { get; set; }
}

public sealed class ContaPagarReceber : BaseEntity
{
    public string Escopo { get; set; } = "imobiliaria";
    public FinanceiroTipo Tipo { get; set; }
    public string Categoria { get; set; } = "outros";
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateOnly DataVencimento { get; set; }
    public DateOnly? DataPagamento { get; set; }
    public FinanceiroStatus Status { get; set; } = FinanceiroStatus.Pendente;
    public Guid? PessoaId { get; set; }
    public Guid? LocacaoId { get; set; }
    public Guid? ImovelId { get; set; }
    public string? Observacoes { get; set; }
}

public sealed class Boleto : BaseEntity
{
    public Guid? LocacaoId { get; set; }
    public Guid? LancamentoFinanceiroId { get; set; }
    public Guid? PessoaPagadoraId { get; set; }
    public string? BancoProvider { get; set; }
    public string? NossoNumero { get; set; }
    public string? LinhaDigitavel { get; set; }
    public string? CodigoBarras { get; set; }
    public decimal Valor { get; set; }
    public DateOnly DataVencimento { get; set; }
    public DateOnly? DataEmissao { get; set; }
    public DateOnly? DataPagamento { get; set; }
    public BoletoStatus Status { get; set; } = BoletoStatus.Rascunho;
    public string? UrlPdf { get; set; }
    public string? ExternalId { get; set; }
    public string? PayloadRequest { get; set; }
    public string? PayloadResponse { get; set; }
    public string? ErroMensagem { get; set; }
}

public sealed class NotaFiscal : BaseEntity
{
    public Guid? LocacaoId { get; set; }
    public Guid? LancamentoFinanceiroId { get; set; }
    public Guid? PessoaTomadorId { get; set; }
    public string Municipio { get; set; } = "Paranavaí";
    public string Provider { get; set; } = "manual_portal";
    public string? Numero { get; set; }
    public string? CodigoVerificacao { get; set; }
    public decimal ValorServico { get; set; }
    public decimal? Aliquota { get; set; }
    public decimal? IssValor { get; set; }
    public DateOnly? DataEmissao { get; set; }
    public NotaFiscalStatus Status { get; set; } = NotaFiscalStatus.Rascunho;
    public string? XmlUrl { get; set; }
    public string? PdfUrl { get; set; }
    public string? XmlConteudo { get; set; }
    public string? Observacoes { get; set; }
    public string? ExternalId { get; set; }
    public string? PayloadRequest { get; set; }
    public string? PayloadResponse { get; set; }
    public string? ErroMensagem { get; set; }
}

public sealed class CertificadoDigital : BaseEntity
{
    public Guid? PessoaJuridicaId { get; set; }
    public CertificadoTipo Tipo { get; set; } = CertificadoTipo.A1;
    public string Nome { get; set; } = string.Empty;
    public DateOnly? ValidadeInicio { get; set; }
    public DateOnly? ValidadeFim { get; set; }
    public CertificadoStatus Status { get; set; } = CertificadoStatus.Ativo;
    public string? Observacoes { get; set; }
}

public sealed class DocumentoModelo : BaseEntity
{
    public string Tipo { get; set; } = "outros";
    public string Nome { get; set; } = string.Empty;
    public string ConteudoTemplate { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public DocumentoModeloStatusRevisao StatusRevisao { get; set; } = DocumentoModeloStatusRevisao.Inicial;
}

public sealed class DocumentoGerado : BaseEntity
{
    public Guid? ModeloId { get; set; }
    public Guid? LocacaoId { get; set; }
    public Guid? PessoaId { get; set; }
    public Guid? ImovelId { get; set; }
    public string Tipo { get; set; } = "outros";
    public string Titulo { get; set; } = string.Empty;
    public string? PdfUrl { get; set; }
    public string? ConteudoFinal { get; set; }
}

public sealed class DimobDeclaracao : BaseEntity
{
    public int AnoCalendario { get; set; }
    public DimobStatus Status { get; set; } = DimobStatus.Rascunho;
    public DateTimeOffset? DataGeracao { get; set; }
    public string? ArquivoUrl { get; set; }
    public string? Observacoes { get; set; }
}

public sealed class DimobItem : BaseEntity
{
    public Guid DimobDeclaracaoId { get; set; }
    public Guid? LocacaoId { get; set; }
    public Guid? ImovelId { get; set; }
    public Guid? ProprietarioId { get; set; }
    public Guid? LocatarioId { get; set; }
    public int AnoCalendario { get; set; }
    public int Mes { get; set; }
    public decimal ValorAluguel { get; set; }
    public decimal ValorComissao { get; set; }
    public decimal ValorImpostoRetido { get; set; }
    public decimal ValorPagoProprietario { get; set; }
    public string? Observacoes { get; set; }
}

public sealed class ManutencaoImovel : BaseEntity
{
    public Guid ImovelId { get; set; }
    public Guid? LocacaoId { get; set; }
    public Guid? PessoaResponsavelId { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public string? Categoria { get; set; }
    public decimal? Valor { get; set; }
    public DateOnly DataSolicitacao { get; set; }
    public DateOnly? DataExecucao { get; set; }
    public ManutencaoStatus Status { get; set; } = ManutencaoStatus.Solicitada;
    public string? Observacoes { get; set; }
}

public sealed class Vistoria : BaseEntity
{
    public Guid ImovelId { get; set; }
    public Guid? LocacaoId { get; set; }
    public VistoriaTipo Tipo { get; set; } = VistoriaTipo.Entrada;
    public DateOnly DataVistoria { get; set; }
    public string? Responsavel { get; set; }
    public string? Descricao { get; set; }
    public string? Status { get; set; }
    public string? Observacoes { get; set; }
}

public sealed class Rescisao : BaseEntity
{
    public Guid LocacaoId { get; set; }
    public DateOnly? DataSolicitacao { get; set; }
    public DateOnly? DataRescisao { get; set; }
    public string? Motivo { get; set; }
    public string? Status { get; set; }
    public decimal? DebitosTotal { get; set; }
    public Guid? VistoriaSaidaId { get; set; }
    public string? Observacoes { get; set; }
}
