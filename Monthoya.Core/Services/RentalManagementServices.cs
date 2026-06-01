using Monthoya.Core.Entities;

namespace Monthoya.Core.Services;

public interface IRentalManagementService
{
    Task<IReadOnlyList<PessoaSummary>> GetPessoasAsync(CancellationToken cancellationToken = default);
    Task<PessoaDetails?> GetPessoaAsync(Guid pessoaId, CancellationToken cancellationToken = default);
    Task<PessoaSummary> CreatePessoaAsync(CreatePessoaRequest request, CancellationToken cancellationToken = default);
    Task<PessoaSummary> UpdatePessoaAsync(UpdatePessoaRequest request, CancellationToken cancellationToken = default);
    Task SetPessoaActiveAsync(Guid pessoaId, bool isActive, CancellationToken cancellationToken = default);
    Task<PessoaDocumentoSummary> CreatePessoaDocumentoAsync(CreatePessoaDocumentoRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PessoaDocumentoSummary>> GetPessoaDocumentosAsync(Guid? pessoaId = null, CancellationToken cancellationToken = default);
    Task<PessoaDocumentoSummary> UpdatePessoaDocumentoOcrAsync(UpdatePessoaDocumentoOcrRequest request, CancellationToken cancellationToken = default);
    Task<PessoaContratoAutofillContext?> GetPessoaContratoAutofillContextAsync(Guid pessoaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImovelSummary>> GetImoveisAsync(CancellationToken cancellationToken = default);
    Task<ImovelSummary> CreateImovelAsync(CreateImovelRequest request, CancellationToken cancellationToken = default);
    Task<ImovelImagemSummary> CreateImovelImagemAsync(CreateImovelImagemRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImovelImagemSummary>> GetImovelImagensAsync(Guid imovelId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LocacaoSummary>> GetLocacoesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IndiceReajusteSummary>> GetIndicesReajusteAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FinanceiroSummary>> GetLancamentosFinanceirosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BoletoSummary>> GetBoletosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NotaFiscalSummary>> GetNotasFiscaisAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentoModeloSummary>> GetDocumentoModelosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DimobDeclaracaoSummary>> GetDimobDeclaracoesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManutencaoSummary>> GetManutencoesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VistoriaSummary>> GetVistoriasAsync(CancellationToken cancellationToken = default);
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
    string? IptuMatricula = null,
    string? TipoImovel = null,
    string? Descricao = null,
    decimal? ValorVenda = null,
    decimal? Latitude = null,
    decimal? Longitude = null);

public sealed record CreateImovelImagemRequest(
    Guid ImovelId,
    string FileName,
    string StoragePath,
    string? ContentType,
    int DisplayOrder = 0);

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
public sealed record ImovelSummary(Guid Id, string Endereco, string? Bairro, string Proprietario, string Finalidade, string Status, decimal? ValorAluguel);
public sealed record ImovelImagemSummary(Guid Id, Guid ImovelId, string FileName, string StoragePath, string? ContentType, int DisplayOrder, string Status);
public sealed record LocacaoSummary(Guid Id, string Imovel, string Proprietario, string Locatario, string Fiadores, decimal ValorAluguel, string Status);
public sealed record IndiceReajusteSummary(Guid Id, string Nome, string Codigo, string Tipo, decimal? Percentual, string Ativo);
public sealed record FinanceiroSummary(Guid Id, string Tipo, string Categoria, string Descricao, decimal Valor, DateOnly DataVencimento, string Status);
public sealed record BoletoSummary(Guid Id, string Status, decimal Valor, DateOnly DataVencimento, string? BancoProvider);
public sealed record NotaFiscalSummary(Guid Id, string Status, decimal ValorServico, string Provider, string? Numero, string? CodigoVerificacao);
public sealed record DocumentoModeloSummary(Guid Id, string Tipo, string Nome, string StatusRevisao, string Ativo);
public sealed record DimobDeclaracaoSummary(Guid Id, int AnoCalendario, string Status, string? Observacoes);
public sealed record ManutencaoSummary(Guid Id, string Descricao, string Status, DateOnly DataSolicitacao, decimal? Valor);
public sealed record VistoriaSummary(Guid Id, string Tipo, DateOnly DataVistoria, string? Responsavel, string? Status);

public interface IBoletoProvider
{
    Task<BoletoProviderResult> GenerateBoletoAsync(Boleto boleto, CancellationToken cancellationToken = default);
    Task<BoletoProviderResult> RegisterBoletoAsync(Boleto boleto, CancellationToken cancellationToken = default);
    Task<BoletoProviderResult> CancelBoletoAsync(Boleto boleto, CancellationToken cancellationToken = default);
    Task<BoletoProviderResult> GetBoletoStatusAsync(Boleto boleto, CancellationToken cancellationToken = default);
    Task<BoletoProviderResult> DownloadBoletoPdfAsync(Boleto boleto, CancellationToken cancellationToken = default);
}

public sealed record BoletoProviderResult(bool Succeeded, string Message, string? ExternalId = null, string? PdfUrl = null);

public interface INfseProvider
{
    Task<NfseProviderResult> IssueNotaFiscalAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default);
    Task<NfseProviderResult> CancelNotaFiscalAsync(NotaFiscal notaFiscal, string motivo, CancellationToken cancellationToken = default);
    Task<NfseProviderResult> DownloadNotaFiscalPdfAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default);
}

public sealed record NfseProviderResult(bool Succeeded, string Message, string? Numero = null, string? CodigoVerificacao = null, string? PdfUrl = null);
