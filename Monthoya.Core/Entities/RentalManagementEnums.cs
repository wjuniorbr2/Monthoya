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
public enum LocacaoStatus
{
    Ativa = 0,
    Encerrada = 1,
    Cancelada = 2,
    Rascunho = 3,
    AguardandoDocumentos = 4,
    AguardandoAnalise = 5,
    AguardandoContrato = 6,
    AguardandoAssinatura = 7,
    AguardandoVistoria = 8,
    AguardandoEntregaChaves = 9,
    EmAtraso = 10,
    EmEncerramento = 11,
    Reaberta = 12
}
public enum TipoLocacao { Residencial = 0, Comercial = 1, Temporada = 2, Outro = 3 }
public enum TipoGarantiaLocacao { Nenhuma = 0, Fiador = 1, CaucaoDinheiro = 2, CaucaoBem = 3, SeguroFianca = 4, TituloCapitalizacao = 5, CessaoFiduciaria = 6, Outro = 7 }
public enum MetodoCalculoProporcional { DiasCorridos = 0, MesComercialTrintaDias = 1 }
public enum TipoDescontoLocacao { ValorFixo = 0, Percentual = 1 }
public enum TipoMultaLocacao { Percentual = 0, ValorFixo = 1 }
public enum ModoReajusteLocacao { Automatico = 0, SemiAutomatico = 1, Manual = 2 }
public enum TipoEncargoLocacao { Iptu = 0, Condominio = 1, ColetaLixo = 2, SeguroIncendio = 3, Agua = 4, Energia = 5, Gas = 6, Outro = 7 }
public enum TipoLancamentoLocacao { CobrarLocatario = 0, DescontoLocatario = 1, ReembolsoLocatario = 2, DescontoRepasseProprietario = 3, CobrarProprietario = 4, AbsorvidoImobiliaria = 5 }
public enum StatusLancamentoLocacao { Pendente = 0, Aprovado = 1, Cobrada = 2, Pago = 3, Cancelado = 4 }
public enum ModoCobrancaTaxaContratoLocacao { Automatico = 0, SemiAutomatico = 1, Manual = 2 }
public enum TipoCobrancaLocacao { Mensal = 0, PrimeiraProporcional = 1, FinalProporcional = 2, Avulsa = 3, Projecao = 4 }
public enum StatusCobrancaLocacao { Rascunho = 0, Aberta = 1, Enviada = 2, Paga = 3, Vencida = 4, Cancelada = 5 }
public enum TipoItemCobrancaLocacao { Aluguel = 0, Desconto = 1, Encargo = 2, Multa = 3, Juros = 4, Lancamento = 5, Outro = 6 }
public enum TipoParteLocacao { Proprietario = 0, Locatario = 1, Fiador = 2 }
public enum DestinoRepasseLocacao { PercentualProprietarios = 0, ProprietarioPrincipal = 1, Manual = 2 }
public enum ModoNotificacaoLocacao { Desabilitada = 0, NotificarApenas = 1, RequerAprovacao = 2, AcaoAutomaticaNotificarResultado = 3 }
public enum TipoDestinatarioNotificacaoLocacao { ResponsavelLocacao = 0, UsuarioEspecifico = 1, Perfil = 2, Todos = 3 }
public enum TipoNotificacaoLocacao { Geral = 0, DocumentosPendentes = 1, ContratoPendente = 2, VistoriaPendente = 3, CobrancaPendente = 4, AluguelVencido = 5, RepassePendente = 6, ReajustePendente = 7, EncerramentoProximo = 8, LancamentoPendenteAprovacao = 9 }
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
