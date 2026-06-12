using Monthoya.Core.Entities;

namespace Monthoya.Core.Services;

public interface IRentalManagementService
{
    Task<IReadOnlyList<PessoaSummary>> GetPessoasAsync(CancellationToken cancellationToken = default);
    Task<PessoaDetails?> GetPessoaAsync(Guid pessoaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetStreetSuggestionsAsync(CancellationToken cancellationToken = default);
    Task<PessoaSummary> CreatePessoaAsync(CreatePessoaRequest request, CancellationToken cancellationToken = default);
    Task<PessoaSummary> UpdatePessoaAsync(UpdatePessoaRequest request, CancellationToken cancellationToken = default);
    Task SetPessoaActiveAsync(Guid pessoaId, bool isActive, CancellationToken cancellationToken = default);
    Task<PessoaDocumentoSummary> CreatePessoaDocumentoAsync(CreatePessoaDocumentoRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PessoaDocumentoSummary>> GetPessoaDocumentosAsync(Guid? pessoaId = null, CancellationToken cancellationToken = default);
    Task<PessoaDocumentoSummary> UpdatePessoaDocumentoOcrAsync(UpdatePessoaDocumentoOcrRequest request, CancellationToken cancellationToken = default);
    Task DeletePessoaDocumentoAsync(Guid documentoId, CancellationToken cancellationToken = default);
    Task<string> GetPessoaDocumentoOpenTargetAsync(Guid documentoId, CancellationToken cancellationToken = default);
    Task<PessoaContratoAutofillContext?> GetPessoaContratoAutofillContextAsync(Guid pessoaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImovelSummary>> GetImoveisAsync(CancellationToken cancellationToken = default);
    Task<ImovelDetails?> GetImovelAsync(Guid imovelId, CancellationToken cancellationToken = default);
    Task<ImovelSummary> CreateImovelAsync(CreateImovelRequest request, CancellationToken cancellationToken = default);
    Task<ImovelSummary> UpdateImovelAsync(UpdateImovelRequest request, CancellationToken cancellationToken = default);
    Task SetImovelActiveAsync(Guid imovelId, bool isActive, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImovelChaveMovimentoSummary>> GetImovelChaveMovimentosAsync(Guid? imovelId = null, CancellationToken cancellationToken = default);
    Task<ImovelChaveMovimentoSummary> CreateImovelChaveMovimentoAsync(CreateImovelChaveMovimentoRequest request, CancellationToken cancellationToken = default);
    Task<ImovelChaveMovimentoSummary> ReturnImovelChaveMovimentoAsync(ReturnImovelChaveMovimentoRequest request, CancellationToken cancellationToken = default);
    Task<ImovelImagemSummary> CreateImovelImagemAsync(CreateImovelImagemRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImovelImagemSummary>> GetImovelImagensAsync(Guid imovelId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocacaoSummary>> GetLocacoesAsync(CancellationToken cancellationToken = default);
    Task<LocacaoDetails> GetLocacaoAsync(Guid id, CancellationToken cancellationToken = default);
    Task<LocacaoDetails> CreateLocacaoAsync(CreateLocacaoRequest request, CancellationToken cancellationToken = default);
    Task<LocacaoDetails> UpdateLocacaoAsync(UpdateLocacaoRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IndiceReajusteSummary>> GetIndicesReajusteAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinanceiroSummary>> GetLancamentosFinanceirosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BoletoSummary>> GetBoletosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotaFiscalSummary>> GetNotasFiscaisAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentoModeloSummary>> GetDocumentoModelosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DimobDeclaracaoSummary>> GetDimobDeclaracoesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManutencaoSummary>> GetManutencoesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VistoriaSummary>> GetVistoriasAsync(Guid? imovelId = null, CancellationToken cancellationToken = default);
    Task<VistoriaSummary> CreateVistoriaAsync(CreateVistoriaRequest request, CancellationToken cancellationToken = default);
}

public sealed record CreatePessoaRequest(
    TipoPessoa TipoPessoa,
    string NomeDisplay,
    string? Telefone,
    string? Email,
    string? Documento,
    PessoaRoleTipo[]? Roles = null,
    string? Observacoes = null,
    string? Endereco = null,
    string? Rua = null,
    string? Numero = null,
    string? Complemento = null,
    string? Bairro = null,
    string? Cidade = null,
    string? Estado = null,
    string? Cep = null,
    string? EstadoCivil = null,
    bool? PossuiTrabalho = null,
    bool? PossuiPet = null,
    string? PetQual = null,
    string? Nacionalidade = null,
    DateOnly? DataNascimento = null,
    string? Rg = null,
    string? Profissao = null,
    string? OndeTrabalha = null,
    string? EnderecoTrabalho = null,
    string? NomeEmpresaTrabalho = null,
    string? CnpjEmpresaTrabalho = null,
    string? TelefoneEmpresaTrabalho = null,
    string? EmailEmpresaTrabalho = null,
    string? CargoTrabalho = null,
    decimal? RendaTrabalho = null,
    string? TempoEmprego = null,
    string? TipoComprovanteRenda = null,
    string? OutrasInformacoes = null,
    string? TrabalhoOutrasInformacoes = null,
    string? EmpresaRua = null,
    string? EmpresaNumero = null,
    string? EmpresaComplemento = null,
    string? EmpresaBairro = null,
    string? EmpresaCidade = null,
    string? EmpresaEstado = null,
    string? EmpresaCep = null,
    string? DadosBancarios = null,
    string? BancoCodigo = null,
    string? BancoNome = null,
    string? AgenciaNumero = null,
    string? AgenciaDigito = null,
    string? ContaNumero = null,
    string? ContaDigito = null,
    ContaBancariaTipo? ContaTipo = null,
    string? TitularNome = null,
    string? TitularDocumento = null,
    PixChaveTipo? PixTipo = null,
    string? PixChave = null,
    MetodoRepassePreferencial? RepassePreferencial = null,
    string? ConjugeNome = null,
    string? ConjugeRg = null,
    string? ConjugeCpf = null,
    string? ConjugeEmail = null,
    DateOnly? ConjugeDataNascimento = null,
    string? ConjugeProfissao = null,
    string? ConjugeNacionalidade = null,
    string? ConjugeTelefone = null,
    string? ConjugeDadosBancarios = null,
    string? ConjugeObservacoes = null,
    string? ConjugeOutrasInformacoes = null,
    bool? ConjugePossuiTrabalho = null,
    string? ConjugeNomeEmpresaTrabalho = null,
    string? ConjugeCnpjEmpresaTrabalho = null,
    string? ConjugeTelefoneEmpresaTrabalho = null,
    string? ConjugeEmailEmpresaTrabalho = null,
    string? ConjugeCargoTrabalho = null,
    decimal? ConjugeRendaTrabalho = null,
    string? ConjugeTempoEmprego = null,
    string? ConjugeTipoComprovanteRenda = null,
    string? ConjugeTrabalhoOutrasInformacoes = null,
    string? ConjugeEmpresaRua = null,
    string? ConjugeEmpresaNumero = null,
    string? ConjugeEmpresaComplemento = null,
    string? ConjugeEmpresaBairro = null,
    string? ConjugeEmpresaCidade = null,
    string? ConjugeEmpresaEstado = null,
    string? ConjugeEmpresaCep = null,
    string? NomeFantasia = null,
    string? Atividade = null,
    decimal? ReceitaMensal = null,
    string? InscricaoEstadual = null,
    string? InscricaoMunicipal = null,
    DateOnly? DataAbertura = null,
    string? ResponsavelNome = null,
    string? ResponsavelCargo = null,
    string? ResponsavelEndereco = null,
    string? ResponsavelRua = null,
    string? ResponsavelNumero = null,
    string? ResponsavelComplemento = null,
    string? ResponsavelBairro = null,
    string? ResponsavelCidade = null,
    string? ResponsavelEstado = null,
    string? ResponsavelCep = null,
    string? ResponsavelEstadoCivil = null,
    string? ResponsavelNacionalidade = null,
    DateOnly? ResponsavelDataNascimento = null,
    string? ResponsavelTelefone = null,
    string? ResponsavelEmail = null,
    string? ResponsavelRg = null,
    string? ResponsavelCpf = null,
    string? ResponsavelProfissao = null,
    string? ResponsavelOndeTrabalha = null,
    string? ResponsavelEnderecoTrabalho = null,
    string? ResponsavelNomeEmpresaTrabalho = null,
    string? ResponsavelTelefoneEmpresaTrabalho = null,
    string? ResponsavelDadosBancarios = null,
    string? ResponsavelBancoCodigo = null,
    string? ResponsavelBancoNome = null,
    string? ResponsavelAgenciaNumero = null,
    string? ResponsavelAgenciaDigito = null,
    string? ResponsavelContaNumero = null,
    string? ResponsavelContaDigito = null,
    ContaBancariaTipo? ResponsavelContaTipo = null,
    string? ResponsavelTitularNome = null,
    string? ResponsavelTitularDocumento = null,
    PixChaveTipo? ResponsavelPixTipo = null,
    string? ResponsavelPixChave = null,
    MetodoRepassePreferencial? ResponsavelRepassePreferencial = null,
    string? ResponsavelObservacoes = null);

public sealed record CreatePessoaDocumentoRequest(
    Guid PessoaId,
    string Tipo,
    string Nome,
    string StoragePath,
    string? ContentType,
    DateOnly? DataValidade,
    string? Observacoes,
    string? DocumentoDe = null,
    bool ApplyOcrToPessoa = true,
    string? OcrTextoExtraido = null);

public sealed record UpdatePessoaDocumentoOcrRequest(
    Guid DocumentoId,
    string? OcrTextoExtraido,
    bool Succeeded,
    string? ErrorMessage = null,
    string? CamposAplicados = null);

public sealed record UpdatePessoaRequest(Guid Id, CreatePessoaRequest Pessoa);
public sealed record UpdateImovelRequest(Guid Id, CreateImovelRequest Imovel);

public sealed record CreateImovelRequest(
    Guid ProprietarioId,
    string Rua,
    string? Numero,
    string? Bairro,
    string Cidade,
    string Estado,
    decimal? ValorAluguel,
    ImovelFinalidade Finalidade,
    string? Observacoes,
    string? Complemento = null,
    string? Cep = null,
    string? SaneparMatricula = null,
    string? CopelMatricula = null,
    string? IptuInscricaoImobiliaria = null,
    string? IptuCadastroImovel = null,
string? ColetaLixo = null,
    string? TipoImovel = null,
    string? Descricao = null,
    decimal? ValorVenda = null,
    decimal? Latitude = null,
    decimal? Longitude = null,
    ImovelStatus Status = ImovelStatus.Disponivel,
    decimal? ValorCondominio = null,
    decimal? ValorIptu = null,
    int? Quartos = null,
    int? Suites = null,
    int? Banheiros = null,
    int? VagasGaragem = null,
    int? Salas = null,
    int? Cozinhas = null,
    int? Copas = null,
    int? Despensas = null,
    int? Lavanderias = null,
    int? AreasServico = null,
    int? Lavabos = null,
    int? Sacadas = null,
    int? Churrasqueiras = null,
    int? Piscinas = null,
    int? Quintais = null,
    int? HallsEntrada = null,
    int? Estendais = null,
    decimal? AreaConstruida = null,
    decimal? AreaTerreno = null,
    bool? Mobiliado = null,
    bool? AceitaPets = null,
    string? DescricaoInterna = null,
    string? DescricaoPublica = null,
    bool PublicarNoSite = false,
    bool PublicarNoApp = false,
    bool Destaque = false,
    bool MostrarEnderecoCompletoPublicamente = false,
    ImovelEnderecoPublicoModo ModoExibicaoEnderecoPublico = ImovelEnderecoPublicoModo.BairroCidade,
    ImovelChavePosse ChavePosse = ImovelChavePosse.NaoCadastrada,
    string? ChaveCodigo = null,
    string? ChaveQuemTem = null,
    string? ChaveTelefone = null,
    string? ChaveContatoNome = null,
    string? ChaveContatoDocumento = null,
    string? ChaveLocalRetirada = null,
    string? ChaveMelhorHorario = null,
    bool ChaveAutorizacaoNecessaria = false,
    string? ChaveObservacoes = null);

public sealed record CreateImovelImagemRequest(
    Guid ImovelId,
    string FileName,
    string StoragePath,
    string? ContentType,
    int DisplayOrder = 0,
    string? Caption = null,
    bool IsCover = false,
    bool IsPublic = false,
    ImovelMediaCategory MediaCategory = ImovelMediaCategory.PropertyPhoto,
    ImovelMediaSource Source = ImovelMediaSource.Windows);

public sealed record CreateImovelChaveMovimentoRequest(
    Guid ImovelId,
    string? ChaveCodigo,
    ImovelChaveMovimentoTipo Tipo,
    string? RetiradoPorNome,
    string? RetiradoPorTelefone,
    string? RetiradoPorDocumento,
    string? RetiradoPorRelacao,
    string? Motivo,
    DateTimeOffset? RetiradoEm,
    DateTimeOffset? PrevisaoDevolucaoEm,
    string? Observacoes);

public sealed record ReturnImovelChaveMovimentoRequest(
    Guid MovimentoId,
    string? DevolvidoParaNome,
    string? Observacoes,
    DateTimeOffset? DevolvidoEm = null);

public sealed record CreateVistoriaRequest(
    Guid ImovelId,
    Guid? LocacaoId,
    VistoriaTipo Tipo,
    DateOnly DataVistoria,
    string? Responsavel,
    VistoriaStatus WorkflowStatus,
    string? DescricaoGeral,
    string? Observacoes);

public sealed record LocacaoParteRequest(
    Guid PessoaId,
    TipoParteLocacao TipoParte,
    bool IsPrincipal = false,
    decimal? PercentualParticipacao = null,
    bool RecebeCobranca = false,
    bool RecebeRepasse = false,
    bool RecebeNotificacao = true,
    decimal? PercentualRepasse = null,
    string? Observacoes = null);

public sealed record LocacaoGarantiaRequest(
    TipoGarantiaLocacao TipoGarantia,
    decimal? Valor = null,
    DateOnly? DataValidade = null,
    bool Ativa = true,
    string? Observacoes = null,
    string? ObservacoesDocumento = null);

public sealed record LocacaoEncargoRequest(
    TipoEncargoLocacao TipoEncargo,
    bool ControladoPelaImobiliaria,
    bool CobradoComAluguel,
    bool PagoDiretoPeloLocatario,
    bool PagoPeloProprietario,
    decimal? Valor = null,
    bool Fixo = false,
    int? NumeroParcelas = null,
    int? DiaVencimento = null,
    bool RequerAtualizacao = false,
    string? Observacoes = null,
    bool Ativo = true);

public sealed record LocacaoLancamentoRequest(
    TipoLancamentoLocacao TipoLancamento,
    string Descricao,
    decimal Valor,
    DateOnly? Competencia = null,
    DateOnly? DataVencimento = null,
    bool AfetaCobrancaLocatario = false,
    bool AfetaRepasseProprietario = false,
    bool RequerAprovacao = false,
    StatusLancamentoLocacao Status = StatusLancamentoLocacao.Pendente,
    string? Observacoes = null);

public sealed record CreateLocacaoRequest(
    Guid ImovelId,
    IReadOnlyList<LocacaoParteRequest> Partes,
    string? Codigo = null,
    TipoLocacao TipoLocacao = TipoLocacao.Residencial,
    LocacaoStatus? Status = null,
    Guid? ResponsavelUsuarioId = null,
    string? ResponsavelNome = null,
    DateOnly? DataCadastro = null,
    DateOnly? DataAssinaturaContrato = null,
    DateOnly? DataInicioLocacao = null,
    DateOnly? DataEntregaChaves = null,
    DateOnly? DataInicioCobranca = null,
    int? DiaBase = null,
    int? DiaVencimentoLocatario = null,
    int? DiaRepasseProprietario = null,
    int? PrazoMeses = null,
    DateOnly? DataFimPrevista = null,
    DateOnly? DataEncerramento = null,
    DateOnly? DataDesocupacao = null,
    string? MotivoEncerramento = null,
    decimal ValorAluguelInicial = 0,
    decimal? ValorAluguelAtual = null,
    bool AluguelAntecipado = false,
    bool CalculoProporcionalPrimeiroMes = false,
    MetodoCalculoProporcional MetodoCalculoProporcional = MetodoCalculoProporcional.DiasCorridos,
    bool TemDescontoPontualidade = false,
    TipoDescontoLocacao? TipoDescontoPontualidade = null,
    decimal? ValorDescontoPontualidade = null,
    bool DescontoValidoAteVencimento = true,
    TipoMultaLocacao MultaAtrasoTipo = TipoMultaLocacao.Percentual,
    decimal? MultaAtrasoValor = null,
    decimal? JurosMoraPercentualMes = null,
    int? DiasTolerancia = null,
    bool CorrecaoMonetariaAtraso = false,
    string? IndiceCorrecaoAtraso = null,
    bool TemReajuste = false,
    Guid? IndiceReajusteId = null,
    int? PeriodicidadeReajusteMeses = null,
    DateOnly? DataBaseReajuste = null,
    DateOnly? ProximaDataReajuste = null,
    ModoReajusteLocacao ModoReajuste = ModoReajusteLocacao.Manual,
    bool ReajusteRequerAprovacao = true,
    decimal? TaxaAdministracaoPercentual = null,
    decimal? MetaComissaoPrimeiroAluguelPercentual = null,
    decimal? TaxaContratoPercentual = null,
    bool TaxaContratoManualOverride = false,
    bool CobrarTaxaContratoInicio = false,
    bool CobrarTaxaContratoRenovacao = false,
    bool CobrarTaxaContratoReajuste = false,
    ModoCobrancaTaxaContratoLocacao ModoCobrancaTaxaContrato = ModoCobrancaTaxaContratoLocacao.Manual,
    LocacaoGarantiaRequest? Garantia = null,
    IReadOnlyList<LocacaoEncargoRequest>? Encargos = null,
    IReadOnlyList<LocacaoLancamentoRequest>? Lancamentos = null,
    string? Observacoes = null,
    string? ObservacoesInternas = null);

public sealed record UpdateLocacaoRequest(Guid Id, CreateLocacaoRequest Locacao);

public sealed record PessoaSummary(
    Guid Id,
    string Nome,
    string Tipo,
    string Roles,
    string? Documento,
    string? Telefone,
    string? Email,
    string Status,
    bool IsProprietario,
    bool IsLocatario,
    bool IsFiador);
public sealed record PessoaDetails(PessoaSummary Summary, CreatePessoaRequest Dados);
public sealed record PessoaDocumentoSummary(
    Guid Id,
    Guid PessoaId,
    string Pessoa,
    string Tipo,
    string DocumentoDe,
    string Nome,
    string StoragePath,
    DateOnly? DataValidade,
    string Status,
    string OcrStatus,
    string? OcrTextoExtraido,
    DateTimeOffset? OcrProcessadoEmUtc,
    string? OcrErroMensagem,
    string? OcrCamposAplicados)
{
    public string Arquivo => string.IsNullOrWhiteSpace(StoragePath)
        ? "-"
        : Path.GetFileName(StoragePath);
}
public sealed record PessoaContratoAutofillContext(
    PessoaSummary Pessoa,
    IReadOnlyList<PessoaDocumentoSummary> Documentos,
    string TextoDocumentosOcr);
public sealed record ImovelSummary(
    Guid Id,
    string Endereco,
    string? Bairro,
    string Proprietario,
    string? TipoImovel,
    string Finalidade,
    string Status,
    string Chaves,
    string Publicacao,
    decimal? ValorAluguel,
    decimal? ValorVenda,
    string? ChaveCodigo = null);
public sealed record ImovelDetails(ImovelSummary Summary, CreateImovelRequest Dados);
public sealed record ImovelImagemSummary(
    Guid Id,
    Guid ImovelId,
    string FileName,
    string StoragePath,
    string? ContentType,
    int DisplayOrder,
    string? Caption,
    bool IsCover,
    bool IsPublic,
    string MediaCategory,
    string Source,
    string Status);
public sealed record ImovelChaveMovimentoSummary(
    Guid Id,
    Guid ImovelId,
    string Imovel,
    string Tipo,
    string Status,
    string? ChaveCodigo,
    string? RetiradoPorNome,
    string? RetiradoPorTelefone,
    string? RetiradoPorDocumento,
    string? RetiradoPorRelacao,
    string? Motivo,
    DateTimeOffset? RetiradoEm,
    DateTimeOffset? PrevisaoDevolucaoEm,
    DateTimeOffset? DevolvidoEm,
    string? DevolvidoParaNome,
    string? Observacoes);
public sealed record LocacaoSummary(
    Guid Id,
    string? Codigo,
    string Status,
    string TipoLocacao,
    Guid ImovelId,
    string ImovelResumo,
    string LocatarioPrincipalNome,
    string ProprietarioPrincipalNome,
    decimal? ValorAluguelAtual,
    int? DiaVencimentoLocatario,
    DateOnly? ProximaDataVencimento,
    DateOnly? DataInicioLocacao,
    DateOnly? DataFimPrevista,
    IReadOnlyList<string> Alertas)
{
    public int AlertasCount => Alertas.Count;
    public string AlertasTexto => string.Join("; ", Alertas);
    public string Imovel => ImovelResumo;
    public string Proprietario => ProprietarioPrincipalNome;
    public string Locatario => LocatarioPrincipalNome;
    public string Fiadores => string.Empty;
    public decimal ValorAluguel => ValorAluguelAtual ?? 0m;
}

public sealed record LocacaoDetails(
    LocacaoSummary Summary,
    CreateLocacaoRequest Dados,
    IReadOnlyList<LocacaoParteSummary> Partes,
    LocacaoGarantiaSummary? Garantia,
    IReadOnlyList<LocacaoEncargoSummary> Encargos,
    IReadOnlyList<LocacaoLancamentoSummary> Lancamentos,
    IReadOnlyList<LocacaoCobrancaSummary> Cobrancas,
    IReadOnlyList<LocacaoHistoricoSummary> Historicos);

public sealed record LocacaoParteSummary(
    Guid Id,
    Guid PessoaId,
    string PessoaNome,
    TipoParteLocacao TipoParte,
    bool IsPrincipal,
    decimal? PercentualParticipacao,
    bool RecebeCobranca,
    bool RecebeRepasse,
    bool RecebeNotificacao,
    decimal? PercentualRepasse,
    string? Observacoes);

public sealed record LocacaoGarantiaSummary(
    Guid Id,
    TipoGarantiaLocacao TipoGarantia,
    decimal? Valor,
    DateOnly? DataValidade,
    bool Ativa,
    string? Observacoes,
    string? ObservacoesDocumento);

public sealed record LocacaoEncargoSummary(
    Guid Id,
    TipoEncargoLocacao TipoEncargo,
    bool ControladoPelaImobiliaria,
    bool CobradoComAluguel,
    bool PagoDiretoPeloLocatario,
    bool PagoPeloProprietario,
    decimal? Valor,
    bool Fixo,
    int? NumeroParcelas,
    int? DiaVencimento,
    bool RequerAtualizacao,
    string? Observacoes,
    bool Ativo);

public sealed record LocacaoLancamentoSummary(
    Guid Id,
    TipoLancamentoLocacao TipoLancamento,
    string Descricao,
    decimal Valor,
    DateOnly? Competencia,
    DateOnly? DataVencimento,
    bool AfetaCobrancaLocatario,
    bool AfetaRepasseProprietario,
    bool RequerAprovacao,
    StatusLancamentoLocacao Status,
    string? Observacoes);

public sealed record LocacaoCobrancaSummary(
    Guid Id,
    TipoCobrancaLocacao TipoCobranca,
    DateOnly Competencia,
    DateOnly PeriodoInicio,
    DateOnly PeriodoFim,
    DateOnly DataVencimento,
    StatusCobrancaLocacao Status,
    decimal ValorTotal,
    IReadOnlyList<LocacaoCobrancaItemSummary> Itens);

public sealed record LocacaoCobrancaItemSummary(
    Guid Id,
    TipoItemCobrancaLocacao TipoItem,
    string Descricao,
    decimal Valor,
    string? ReferenciaId,
    string? Observacoes);

public sealed record LocacaoHistoricoSummary(
    Guid Id,
    DateTimeOffset DataHoraUtc,
    string? Usuario,
    string Acao,
    string? Campo,
    string? ValorAnterior,
    string? ValorNovo,
    string? Motivo);
public sealed record IndiceReajusteSummary(Guid Id, string Nome, string Codigo, string Tipo, decimal? Percentual, string Ativo);
public sealed record FinanceiroSummary(Guid Id, string Tipo, string Categoria, string Descricao, decimal Valor, DateOnly DataVencimento, string Status);
public sealed record BoletoSummary(Guid Id, string Status, decimal Valor, DateOnly DataVencimento, string? BancoProvider);
public sealed record NotaFiscalSummary(Guid Id, string Status, decimal ValorServico, string Provider, string? Numero, string? CodigoVerificacao);
public sealed record DocumentoModeloSummary(Guid Id, string Tipo, string Nome, string StatusRevisao, string Ativo);
public sealed record DimobDeclaracaoSummary(Guid Id, int AnoCalendario, string Status, string? Observacoes);
public sealed record ManutencaoSummary(Guid Id, string Descricao, string Status, DateOnly DataSolicitacao, decimal? Valor);
public sealed record VistoriaSummary(Guid Id, Guid ImovelId, string Imovel, string Tipo, DateOnly DataVistoria, string? Responsavel, string? Status, string? Observacoes);

public interface IBoletoProvider
{
    Task<BoletoProviderResult> GenerateBoletoAsync(Boleto boleto, CancellationToken cancellationToken = default);
    Task<BoletoProviderResult> RegisterBoletoAsync(Boleto boleto, CancellationToken cancellationToken = default);
    Task<BoletoProviderStatusResult> QueryStatusAsync(Boleto boleto, CancellationToken cancellationToken = default);
    Task<BoletoProviderResult> CancelAsync(Boleto boleto, CancellationToken cancellationToken = default);
}

public sealed record BoletoProviderResult(bool Success, string? ProviderId, string? PayloadJson, string? ErrorMessage = null);
public sealed record BoletoProviderStatusResult(BoletoStatus Status, DateOnly? PaidAt = null, string? ErrorMessage = null);

public interface INfseProvider
{
    Task<NfseProviderResult> IssueAsync(NotaFiscal nota, CancellationToken cancellationToken = default);
    Task<NfseProviderResult> CancelAsync(NotaFiscal nota, string reason, CancellationToken cancellationToken = default);
}

public sealed record NfseProviderResult(bool Success, string? Numero, string? CodigoVerificacao, string? PayloadJson, string? ErrorMessage = null);
