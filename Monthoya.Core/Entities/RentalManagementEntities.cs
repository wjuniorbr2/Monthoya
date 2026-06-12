namespace Monthoya.Core.Entities;



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
    public int? Salas { get; set; }
    public int? Cozinhas { get; set; }
    public int? Copas { get; set; }
    public int? Despensas { get; set; }
    public int? Lavanderias { get; set; }
    public int? AreasServico { get; set; }
    public int? Lavabos { get; set; }
    public int? Sacadas { get; set; }
    public int? Churrasqueiras { get; set; }
    public int? Piscinas { get; set; }
    public int? Quintais { get; set; }
    public int? HallsEntrada { get; set; }
    public int? Estendais { get; set; }
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
    public string? Codigo { get; set; }
    public TipoLocacao TipoLocacao { get; set; } = TipoLocacao.Residencial;
    public Guid? ResponsavelUsuarioId { get; set; }
    public AppUser? ResponsavelUsuario { get; set; }
    public string? ResponsavelNome { get; set; }
    public DateOnly? DataCadastro { get; set; }
    public Guid ImovelId { get; set; }
    public Imovel? Imovel { get; set; }
    public Guid LocatarioId { get; set; }
    public Pessoa? Locatario { get; set; }
    public Guid ProprietarioId { get; set; }
    public Pessoa? Proprietario { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public int? PeriodoMeses { get; set; }
    public DateOnly? DataAssinaturaContrato { get; set; }
    public DateOnly? DataInicioLocacao { get; set; }
    public DateOnly? DataEntregaChaves { get; set; }
    public DateOnly? DataInicioCobranca { get; set; }
    public int DiaBase { get; set; } = 1;
    public int VencimentoLocatarioDia { get; set; } = 10;
    public int? VencimentoProprietarioDia { get; set; }
    public int? DiaVencimentoLocatario { get; set; }
    public int? DiaRepasseProprietario { get; set; }
    public int? PrazoMeses { get; set; }
    public DateOnly? DataFimPrevista { get; set; }
    public DateOnly? DataEncerramento { get; set; }
    public DateOnly? DataDesocupacao { get; set; }
    public string? MotivoEncerramento { get; set; }
    public decimal ValorAluguel { get; set; }
    public decimal? ValorAluguelInicial { get; set; }
    public decimal? ValorAluguelAtual { get; set; }
    public bool AluguelAntecipado { get; set; }
    public bool CalculoProporcionalPrimeiroMes { get; set; }
    public MetodoCalculoProporcional MetodoCalculoProporcional { get; set; } = MetodoCalculoProporcional.DiasCorridos;
    public bool TemDescontoPontualidade { get; set; }
    public TipoDescontoLocacao? TipoDescontoPontualidade { get; set; }
    public decimal? ValorDescontoPontualidade { get; set; }
    public bool DescontoValidoAteVencimento { get; set; } = true;
    public TipoMultaLocacao MultaAtrasoTipo { get; set; } = TipoMultaLocacao.Percentual;
    public decimal? MultaAtrasoValor { get; set; }
    public decimal? JurosMoraPercentualMes { get; set; }
    public int? DiasTolerancia { get; set; }
    public bool CorrecaoMonetariaAtraso { get; set; }
    public string? IndiceCorrecaoAtraso { get; set; }
    public decimal? MultaPercentual { get; set; }
    public decimal? JurosPercentual { get; set; }
    public bool DescontoAteVencimentoAtivo { get; set; }
    public decimal? DescontoAteVencimentoValor { get; set; }
    public decimal? DescontoAteVencimentoPercentual { get; set; }
    public Guid? IndiceReajusteId { get; set; }
    public IndiceReajuste? IndiceReajuste { get; set; }
    public DateOnly? DataProximoReajuste { get; set; }
    public bool TemReajuste { get; set; }
    public int? PeriodicidadeReajusteMeses { get; set; }
    public DateOnly? DataBaseReajuste { get; set; }
    public DateOnly? ProximaDataReajuste { get; set; }
    public ModoReajusteLocacao ModoReajuste { get; set; } = ModoReajusteLocacao.Manual;
    public bool ReajusteRequerAprovacao { get; set; } = true;
    public ModeloTaxaAdministracao ModeloTaxaAdministracao { get; set; } = ModeloTaxaAdministracao.PercentualAluguel;
    public decimal? TaxaAdministracaoValor { get; set; }
    public decimal? TaxaAdministracaoPercentual { get; set; }
    public decimal? MetaComissaoPrimeiroAluguelPercentual { get; set; }
    public decimal? TaxaContratoValor { get; set; }
    public decimal? TaxaContratoPercentual { get; set; }
    public bool TaxaContratoManualOverride { get; set; }
    public bool CobrarTaxaContratoInicio { get; set; }
    public bool CobrarTaxaContratoRenovacao { get; set; }
    public bool CobrarTaxaContratoReajuste { get; set; }
    public ModoCobrancaTaxaContratoLocacao ModoCobrancaTaxaContrato { get; set; } = ModoCobrancaTaxaContratoLocacao.Manual;
    public DestinoRepasseLocacao DestinoRepasse { get; set; } = DestinoRepasseLocacao.PercentualProprietarios;
    public decimal? TaxaRenovacaoValor { get; set; }
    public LocacaoStatus Status { get; set; } = LocacaoStatus.Rascunho;
    public string? Observacoes { get; set; }
    public string? ObservacoesInternas { get; set; }
    public ICollection<LocacaoFiador> Fiadores { get; set; } = new List<LocacaoFiador>();
    public ICollection<LocacaoParte> Partes { get; set; } = new List<LocacaoParte>();
    public ICollection<LocacaoGarantia> Garantias { get; set; } = new List<LocacaoGarantia>();
    public ICollection<LocacaoValorHistorico> ValoresHistoricos { get; set; } = new List<LocacaoValorHistorico>();
    public ICollection<LocacaoEncargoRecorrente> EncargosRecorrentes { get; set; } = new List<LocacaoEncargoRecorrente>();
    public ICollection<LocacaoLancamento> Lancamentos { get; set; } = new List<LocacaoLancamento>();
    public ICollection<LocacaoCobranca> Cobrancas { get; set; } = new List<LocacaoCobranca>();
    public ICollection<LocacaoNotificacaoRegra> NotificacaoRegras { get; set; } = new List<LocacaoNotificacaoRegra>();
    public ICollection<LocacaoHistorico> Historicos { get; set; } = new List<LocacaoHistorico>();
}

public sealed class LocacaoFiador : BaseEntity
{
    public Guid LocacaoId { get; set; }
    public Locacao? Locacao { get; set; }
    public Guid FiadorId { get; set; }
    public Pessoa? Fiador { get; set; }
}

public sealed class LocacaoParte : BaseEntity
{
    public Guid LocacaoId { get; set; }
    public Locacao? Locacao { get; set; }
    public Guid PessoaId { get; set; }
    public Pessoa? Pessoa { get; set; }
    public TipoParteLocacao TipoParte { get; set; }
    public bool IsPrincipal { get; set; }
    public decimal? PercentualParticipacao { get; set; }
    public bool RecebeCobranca { get; set; }
    public bool RecebeRepasse { get; set; }
    public bool RecebeNotificacao { get; set; } = true;
    public decimal? PercentualRepasse { get; set; }
    public string? Observacoes { get; set; }
}

public sealed class LocacaoGarantia : BaseEntity
{
    public Guid LocacaoId { get; set; }
    public Locacao? Locacao { get; set; }
    public TipoGarantiaLocacao TipoGarantia { get; set; } = TipoGarantiaLocacao.Nenhuma;
    public decimal? Valor { get; set; }
    public DateOnly? DataValidade { get; set; }
    public bool Ativa { get; set; } = true;
    public string? Observacoes { get; set; }
    public string? ObservacoesDocumento { get; set; }
}

public sealed class LocacaoValorHistorico : BaseEntity
{
    public Guid LocacaoId { get; set; }
    public Locacao? Locacao { get; set; }
    public DateOnly DataVigencia { get; set; }
    public decimal? ValorAnterior { get; set; }
    public decimal ValorNovo { get; set; }
    public string? Motivo { get; set; }
    public string? IndiceReajuste { get; set; }
    public decimal? PercentualAplicado { get; set; }
    public string? Usuario { get; set; }
    public DateTimeOffset CriadoEmUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LocacaoEncargoRecorrente : BaseEntity
{
    public Guid LocacaoId { get; set; }
    public Locacao? Locacao { get; set; }
    public TipoEncargoLocacao TipoEncargo { get; set; } = TipoEncargoLocacao.Outro;
    public bool ControladoPelaImobiliaria { get; set; }
    public bool CobradoComAluguel { get; set; }
    public bool PagoDiretoPeloLocatario { get; set; }
    public bool PagoPeloProprietario { get; set; }
    public decimal? Valor { get; set; }
    public bool Fixo { get; set; }
    public int? NumeroParcelas { get; set; }
    public int? DiaVencimento { get; set; }
    public bool RequerAtualizacao { get; set; }
    public string? Observacoes { get; set; }
    public bool Ativo { get; set; } = true;
}

public sealed class LocacaoLancamento : BaseEntity
{
    public Guid LocacaoId { get; set; }
    public Locacao? Locacao { get; set; }
    public TipoLancamentoLocacao TipoLancamento { get; set; } = TipoLancamentoLocacao.CobrarLocatario;
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public DateOnly? Competencia { get; set; }
    public DateOnly? DataVencimento { get; set; }
    public bool AfetaCobrancaLocatario { get; set; }
    public bool AfetaRepasseProprietario { get; set; }
    public bool RequerAprovacao { get; set; }
    public StatusLancamentoLocacao Status { get; set; } = StatusLancamentoLocacao.Pendente;
    public string? Observacoes { get; set; }
    public DateTimeOffset CriadoEmUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AprovadoEmUtc { get; set; }
    public DateTimeOffset? CanceladoEmUtc { get; set; }
}

public sealed class LocacaoCobranca : BaseEntity
{
    public Guid LocacaoId { get; set; }
    public Locacao? Locacao { get; set; }
    public TipoCobrancaLocacao TipoCobranca { get; set; } = TipoCobrancaLocacao.Mensal;
    public DateOnly Competencia { get; set; }
    public DateOnly PeriodoInicio { get; set; }
    public DateOnly PeriodoFim { get; set; }
    public DateOnly DataVencimento { get; set; }
    public StatusCobrancaLocacao Status { get; set; } = StatusCobrancaLocacao.Rascunho;
    public decimal ValorAluguel { get; set; }
    public decimal ValorDescontos { get; set; }
    public decimal ValorEncargos { get; set; }
    public decimal ValorMulta { get; set; }
    public decimal ValorJuros { get; set; }
    public decimal ValorTotal { get; set; }
    public DateTimeOffset CriadoEmUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EnviadoEmUtc { get; set; }
    public DateTimeOffset? PagoEmUtc { get; set; }
    public DateTimeOffset? CanceladoEmUtc { get; set; }
    public ICollection<LocacaoCobrancaItem> Itens { get; set; } = new List<LocacaoCobrancaItem>();
}

public sealed class LocacaoCobrancaItem : BaseEntity
{
    public Guid LocacaoCobrancaId { get; set; }
    public LocacaoCobranca? LocacaoCobranca { get; set; }
    public TipoItemCobrancaLocacao TipoItem { get; set; } = TipoItemCobrancaLocacao.Outro;
    public string Descricao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string? ReferenciaId { get; set; }
    public string? Observacoes { get; set; }
}

public sealed class LocacaoNotificacaoRegra : BaseEntity
{
    public Guid LocacaoId { get; set; }
    public Locacao? Locacao { get; set; }
    public TipoNotificacaoLocacao TipoNotificacao { get; set; } = TipoNotificacaoLocacao.Geral;
    public ModoNotificacaoLocacao Modo { get; set; } = ModoNotificacaoLocacao.NotificarApenas;
    public TipoDestinatarioNotificacaoLocacao DestinatarioTipo { get; set; } = TipoDestinatarioNotificacaoLocacao.ResponsavelLocacao;
    public Guid? DestinatarioUsuarioId { get; set; }
    public AppUser? DestinatarioUsuario { get; set; }
    public string? DestinatarioRole { get; set; }
    public int? DiasAntes { get; set; }
    public int? DiasDepois { get; set; }
    public bool RepetirAteResolver { get; set; }
    public NotificationChannel Canal { get; set; } = NotificationChannel.InApp;
    public bool Ativa { get; set; } = true;
    public string? Observacoes { get; set; }
}

public sealed class LocacaoHistorico : BaseEntity
{
    public Guid LocacaoId { get; set; }
    public Locacao? Locacao { get; set; }
    public DateTimeOffset DataHoraUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? Usuario { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string? Campo { get; set; }
    public string? ValorAnterior { get; set; }
    public string? ValorNovo { get; set; }
    public string? Motivo { get; set; }
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
