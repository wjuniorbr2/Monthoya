namespace Monthoya.Core.Entities;

public enum TipoPessoa { Fisica = 0, Juridica = 1 }
public enum PessoaRoleTipo { Proprietario = 0, Locatario = 1, Fiador = 2 }
public enum RegistroStatus { Ativo = 0, Inativo = 1 }
public enum ContaBancariaTipo { Corrente = 0, Poupanca = 1, Pagamento = 2, Outro = 3 }
public enum PixChaveTipo { Cpf = 0, Cnpj = 1, Email = 2, Telefone = 3, Aleatoria = 4, Outro = 5 }
public enum MetodoRepassePreferencial { Pix = 0, TransferenciaBancaria = 1, Manual = 2 }
public enum ImovelFinalidade { Locacao = 0, Venda = 1, Ambos = 2 }
public enum ImovelStatus { Disponivel = 0, Reservado = 1, Locado = 2, Vendido = 3, Inativo = 4 }
public enum ImovelEnderecoPublicoModo { BairroCidade = 0, EnderecoAproximado = 1, EnderecoCompleto = 2 }
public enum ImovelChavePosse { NaoCadastrada = 0, Imobiliaria = 1, Proprietario = 2, Locatario = 3, Terceiro = 4, Outro = 5 }
public enum ImovelChaveMovimentoTipo { Retirada = 0, Devolucao = 1, Transferencia = 2, MarcadaPerdida = 3, Outro = 4 }
public enum ImovelChaveMovimentoStatus { ComImobiliaria = 0, Retirada = 1, EmAtraso = 2, Perdida = 3, Inativa = 4 }
public enum ImovelMediaCategory { PropertyPhoto = 0, Document = 1, InspectionPhoto = 2, MaintenancePhoto = 3, Other = 4 }
public enum ImovelMediaSource { Windows = 0, AndroidStaff = 1, Website = 2, Import = 3 }
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
public enum DocumentoOcrStatus { NaoProcessado = 0, Processado = 1, Erro = 2 }
public enum DimobStatus { Rascunho = 0, Conferida = 1, Exportada = 2, Entregue = 3, Retificada = 4 }
public enum ManutencaoStatus { Solicitada = 0, EmAndamento = 1, Concluida = 2, Cancelada = 3 }
public enum VistoriaTipo { Entrada = 0, Saida = 1, Periodica = 2, Outros = 3, InicialProprietario = 4, Manutencao = 5 }
public enum VistoriaStatus { Draft = 0, InProgress = 1, ReadyToReview = 2, Finished = 3, SignedPaper = 4, SignedDigitally = 5, Canceled = 6 }
public enum VistoriaAmbienteTipo { Sala = 0, SalaTv = 1, Cozinha = 2, Banheiro = 3, Quarto = 4, Suite = 5, Garagem = 6, Lavanderia = 7, AreaExterna = 8, Quintal = 9, Corredor = 10, Sacada = 11, Outro = 12 }
public enum VistoriaItemCategoria { Parede = 0, Piso = 1, Teto = 2, Porta = 3, Janela = 4, Pintura = 5, Tomada = 6, Interruptor = 7, Torneira = 8, Pia = 9, VasoSanitario = 10, Chuveiro = 11, Armario = 12, Outro = 13 }
public enum VistoriaItemCondicao { Bom = 0, Regular = 1, Ruim = 2, Danificado = 3, Ausente = 4, NaoSeAplica = 5 }
public enum VistoriaFotoUploadStatus { LocalOnly = 0, PendingUpload = 1, Uploaded = 2, Failed = 3 }

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
    public string? IptuInscricaoImobiliaria { get; set; }
    public string? IptuCadastroImovel { get; set; }
public string? ColetaLixo { get; set; }
    public string? TipoImovel { get; set; }
    public string? Descricao { get; set; }
    public string? DescricaoInterna { get; set; }
    public string? DescricaoPublica { get; set; }
    public decimal? ValorAluguel { get; set; }
    public decimal? ValorVenda { get; set; }
    public decimal? ValorCondominio { get; set; }
    public decimal? ValorIptu { get; set; }
    public ImovelFinalidade Finalidade { get; set; } = ImovelFinalidade.Locacao;
    public ImovelStatus Status { get; set; } = ImovelStatus.Disponivel;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? Quartos { get; set; }
    public int? Suites { get; set; }
    public int? Banheiros { get; set; }
    public int? VagasGaragem { get; set; }
    public decimal? AreaConstruida { get; set; }
    public decimal? AreaTerreno { get; set; }
    public bool? Mobiliado { get; set; }
    public bool? AceitaPets { get; set; }
    public bool PublicarNoSite { get; set; }
    public bool PublicarNoApp { get; set; }
    public bool Destaque { get; set; }
    public bool MostrarEnderecoCompletoPublicamente { get; set; }
    public ImovelEnderecoPublicoModo ModoExibicaoEnderecoPublico { get; set; } = ImovelEnderecoPublicoModo.BairroCidade;
    public ImovelChavePosse ChavePosse { get; set; } = ImovelChavePosse.NaoCadastrada;
    public string? ChaveCodigo { get; set; }
    public string? ChaveQuemTem { get; set; }
    public string? ChaveTelefone { get; set; }
    public string? ChaveContatoNome { get; set; }
    public string? ChaveContatoDocumento { get; set; }
    public string? ChaveLocalRetirada { get; set; }
    public string? ChaveMelhorHorario { get; set; }
    public bool ChaveAutorizacaoNecessaria { get; set; }
    public string? ChaveObservacoes { get; set; }
    public string? Observacoes { get; set; }
    public ICollection<ImovelImagem> Imagens { get; set; } = new List<ImovelImagem>();
    public ICollection<ImovelChaveMovimento> ChaveMovimentos { get; set; } = new List<ImovelChaveMovimento>();
}

public sealed class ImovelChaveMovimento : BaseEntity
{
    public Guid ImovelId { get; set; }
    public Imovel? Imovel { get; set; }
    public string? ChaveCodigo { get; set; }
    public ImovelChaveMovimentoTipo Tipo { get; set; } = ImovelChaveMovimentoTipo.Retirada;
    public string? RetiradoPorNome { get; set; }
    public string? RetiradoPorTelefone { get; set; }
    public string? RetiradoPorDocumento { get; set; }
    public string? RetiradoPorRelacao { get; set; }
    public string? Motivo { get; set; }
    public DateTimeOffset? RetiradoEm { get; set; }
    public DateTimeOffset? PrevisaoDevolucaoEm { get; set; }
    public DateTimeOffset? DevolvidoEm { get; set; }
    public string? DevolvidoParaNome { get; set; }
    public ImovelChaveMovimentoStatus Status { get; set; } = ImovelChaveMovimentoStatus.Retirada;
    public string? Observacoes { get; set; }
    public Guid? CreatedByUserId { get; set; }
}

public sealed class ImovelImagem : BaseEntity
{
    public Guid ImovelId { get; set; }
    public Imovel? Imovel { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public int DisplayOrder { get; set; }
    public string? Caption { get; set; }
    public bool IsCover { get; set; }
    public bool IsPublic { get; set; }
    public ImovelMediaCategory MediaCategory { get; set; } = ImovelMediaCategory.PropertyPhoto;
    public ImovelMediaSource Source { get; set; } = ImovelMediaSource.Windows;
    public RegistroStatus Status { get; set; } = RegistroStatus.Ativo;
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
    public Guid StableId { get; set; } = Guid.NewGuid();
    public Guid ImovelId { get; set; }
    public Imovel? Imovel { get; set; }
    public Guid? LocacaoId { get; set; }
    public VistoriaTipo Tipo { get; set; } = VistoriaTipo.Entrada;
    public DateOnly DataVistoria { get; set; }
    public string? Responsavel { get; set; }
    public string? Descricao { get; set; }
    public string? DescricaoGeral { get; set; }
    public string? Status { get; set; }
    public VistoriaStatus WorkflowStatus { get; set; } = VistoriaStatus.Draft;
    public string? Observacoes { get; set; }
    public string? PdfPath { get; set; }
    public string? AiSummary { get; set; }
    public string? AiStatus { get; set; }
    public DateTimeOffset? AiProcessedAt { get; set; }
    public string? AiErrorMessage { get; set; }
    public ICollection<VistoriaAmbiente> Ambientes { get; set; } = new List<VistoriaAmbiente>();
    public ICollection<VistoriaFoto> Fotos { get; set; } = new List<VistoriaFoto>();
}

public sealed class VistoriaAmbiente : BaseEntity
{
    public Guid StableId { get; set; } = Guid.NewGuid();
    public Guid VistoriaId { get; set; }
    public Vistoria? Vistoria { get; set; }
    public string Nome { get; set; } = string.Empty;
    public VistoriaAmbienteTipo TipoAmbiente { get; set; } = VistoriaAmbienteTipo.Outro;
    public int DisplayOrder { get; set; }
    public string? Observacoes { get; set; }
    public string? CondicaoGeral { get; set; }
    public ICollection<VistoriaItem> Itens { get; set; } = new List<VistoriaItem>();
    public ICollection<VistoriaFoto> Fotos { get; set; } = new List<VistoriaFoto>();
}

public sealed class VistoriaItem : BaseEntity
{
    public Guid StableId { get; set; } = Guid.NewGuid();
    public Guid VistoriaAmbienteId { get; set; }
    public VistoriaAmbiente? Ambiente { get; set; }
    public string Nome { get; set; } = string.Empty;
    public VistoriaItemCategoria Categoria { get; set; } = VistoriaItemCategoria.Outro;
    public VistoriaItemCondicao Condicao { get; set; } = VistoriaItemCondicao.Bom;
    public string? Descricao { get; set; }
    public string? Observacoes { get; set; }
    public string? ResponsabilidadeSugerida { get; set; }
    public bool? AiDetectedDamage { get; set; }
    public string? AiSuggestedDescription { get; set; }
    public decimal? AiConfidence { get; set; }
    public string? AiStatus { get; set; }
    public DateTimeOffset? AiProcessedAt { get; set; }
    public string? AiErrorMessage { get; set; }
    public ICollection<VistoriaFoto> Fotos { get; set; } = new List<VistoriaFoto>();
}

public sealed class VistoriaFoto : BaseEntity
{
    public Guid StableId { get; set; } = Guid.NewGuid();
    public Guid VistoriaId { get; set; }
    public Vistoria? Vistoria { get; set; }
    public Guid? VistoriaAmbienteId { get; set; }
    public VistoriaAmbiente? Ambiente { get; set; }
    public Guid? VistoriaItemId { get; set; }
    public VistoriaItem? Item { get; set; }
    public Guid ImovelId { get; set; }
    public Imovel? Imovel { get; set; }
    public Guid? LocacaoId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? LocalDevicePath { get; set; }
    public string? StoragePath { get; set; }
    public string? ContentType { get; set; }
    public int DisplayOrder { get; set; }
    public string? Caption { get; set; }
    public DateTimeOffset? TakenAt { get; set; }
    public DateTimeOffset? UploadedAt { get; set; }
    public VistoriaFotoUploadStatus UploadStatus { get; set; } = VistoriaFotoUploadStatus.LocalOnly;
    public ImovelMediaSource Source { get; set; } = ImovelMediaSource.AndroidStaff;
    public bool IsPublicWebsite { get; set; }
    public bool? VisibleToClientApp { get; set; }
    public string? AiDescription { get; set; }
    public bool? AiDetectedDamage { get; set; }
    public string? AiSuggestedCaption { get; set; }
    public decimal? AiConfidence { get; set; }
    public string? AiStatus { get; set; }
    public DateTimeOffset? AiProcessedAt { get; set; }
    public string? AiErrorMessage { get; set; }
    public string? MetadataJson { get; set; }
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
