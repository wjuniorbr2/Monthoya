using Monthoya.Core.Entities;

namespace Monthoya.Core.Services;

public sealed record AgenciaPerfilRequest(
    string RazaoSocial,
    string? NomeFantasia,
    string? Cnpj,
    string? InscricaoMunicipal,
    string? InscricaoEstadual,
    string? Creci,
    string? ResponsavelNome,
    string? ResponsavelCpf,
    string? ResponsavelCargo,
    string? Email,
    string? Telefone,
    string? WhatsApp,
    string? Site,
    string? Rua,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? Cidade,
    string? Estado,
    string? Cep,
    string? DadosBancarios,
    string? TextoPadraoRodape,
    string? Observacoes);

public interface IAgenciaPerfilService
{
    Task<AgenciaPerfil?> GetAsync(CancellationToken cancellationToken = default);
    Task<bool> HasProfileAsync(CancellationToken cancellationToken = default);
    Task<AgenciaPerfil> SaveAsync(AgenciaPerfilRequest request, CancellationToken cancellationToken = default);
}
