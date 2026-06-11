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
