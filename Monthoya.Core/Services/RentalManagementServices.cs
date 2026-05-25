using Monthoya.Core.Entities;

namespace Monthoya.Core.Services;

public interface IRentalManagementService
{
    Task<IReadOnlyList<PessoaSummary>> GetPessoasAsync(CancellationToken cancellationToken = default);
    Task<PessoaSummary> CreatePessoaAsync(CreatePessoaRequest request, CancellationToken cancellationToken = default);
    Task<PessoaDocumentoSummary> CreatePessoaDocumentoAsync(CreatePessoaDocumentoRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PessoaDocumentoSummary>> GetPessoaDocumentosAsync(Guid? pessoaId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImovelSummary>> GetImoveisAsync(CancellationToken cancellationToken = default);
    Task<ImovelSummary> CreateImovelAsync(CreateImovelRequest request, CancellationToken cancellationToken = default);
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
    PessoaRoleTipo[] Roles,
    string? Observacoes,
    string? Endereco = null,
    string? EstadoCivil = null,
    string? Nacionalidade = null,
    DateOnly? DataNascimento = null,
    string? Rg = null,
    string? Profissao = null,
    string? OndeTrabalha = null,
    string? EnderecoTrabalho = null,
    string? NomeEmpresaTrabalho = null,
    string? TelefoneEmpresaTrabalho = null,
    string? DadosBancarios = null,
    string? ConjugeNome = null,
    string? ConjugeRg = null,
    string? ConjugeCpf = null,
    DateOnly? ConjugeDataNascimento = null,
    string? ConjugeProfissao = null,
    string? ConjugeNacionalidade = null,
    string? ConjugeTelefone = null,
    string? ResponsavelNome = null,
    string? ResponsavelEndereco = null,
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
    string? ResponsavelDadosBancarios = null);

public sealed record CreatePessoaDocumentoRequest(
    Guid PessoaId,
    string Tipo,
    string Nome,
    string StoragePath,
    string? ContentType,
    DateOnly? DataValidade,
    string? Observacoes);

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

public sealed record PessoaSummary(Guid Id, string Nome, string Tipo, string Roles, string? Documento, string? Telefone, string? Email, string Status);
public sealed record PessoaDocumentoSummary(Guid Id, Guid PessoaId, string Pessoa, string Tipo, string Nome, string StoragePath, DateOnly? DataValidade, string Status);
public sealed record ImovelSummary(Guid Id, string Endereco, string? Bairro, string Proprietario, string Finalidade, string Status, decimal? ValorAluguel);
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
    Task<NfseProviderResult> EmitirNotaFiscalAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default);
    Task<NfseProviderResult> CancelarNotaFiscalAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default);
    Task<NfseProviderResult> ConsultarNotaFiscalAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default);
    Task<NfseProviderResult> BaixarPdfNotaFiscalAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default);
    Task<NfseProviderResult> BaixarXmlNotaFiscalAsync(NotaFiscal notaFiscal, CancellationToken cancellationToken = default);
}

public sealed record NfseProviderResult(bool Succeeded, string Message, string? ExternalId = null, string? PdfUrl = null, string? XmlUrl = null);
