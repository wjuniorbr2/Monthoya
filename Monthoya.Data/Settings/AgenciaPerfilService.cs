using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Settings;

public sealed class AgenciaPerfilService(MonthoyaDbContext dbContext) : IAgenciaPerfilService
{
    public async Task<AgenciaPerfil?> GetAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.AgenciaPerfis
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> HasProfileAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.AgenciaPerfis.AnyAsync(cancellationToken);
    }

    public async Task<AgenciaPerfil> SaveAsync(AgenciaPerfilRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RazaoSocial))
        {
            throw new InvalidOperationException("Informe a razão social/nome da imobiliária.");
        }

        var profile = await GetAsync(cancellationToken) ?? new AgenciaPerfil();
        var isNew = profile.Id == Guid.Empty || !await dbContext.AgenciaPerfis.AnyAsync(x => x.Id == profile.Id, cancellationToken);

        profile.RazaoSocial = TrimRequired(request.RazaoSocial);
        profile.NomeFantasia = TrimOrNull(request.NomeFantasia);
        profile.Cnpj = DigitsOrNull(request.Cnpj);
        profile.InscricaoMunicipal = TrimOrNull(request.InscricaoMunicipal);
        profile.InscricaoEstadual = TrimOrNull(request.InscricaoEstadual);
        profile.Creci = TrimOrNull(request.Creci);
        profile.ResponsavelNome = TrimOrNull(request.ResponsavelNome);
        profile.ResponsavelCpf = DigitsOrNull(request.ResponsavelCpf);
        profile.ResponsavelCargo = TrimOrNull(request.ResponsavelCargo);
        profile.Email = TrimOrNull(request.Email);
        profile.Telefone = TrimOrNull(request.Telefone);
        profile.WhatsApp = TrimOrNull(request.WhatsApp);
        profile.Site = TrimOrNull(request.Site);
        profile.Rua = TrimOrNull(request.Rua);
        profile.Numero = TrimOrNull(request.Numero);
        profile.Complemento = TrimOrNull(request.Complemento);
        profile.Bairro = TrimOrNull(request.Bairro);
        profile.Cidade = TrimOrNull(request.Cidade);
        profile.Estado = TrimOrNull(request.Estado)?.ToUpperInvariant();
        profile.Cep = DigitsOrNull(request.Cep);
        profile.DadosBancarios = TrimOrNull(request.DadosBancarios);
        profile.TextoPadraoRodape = TrimOrNull(request.TextoPadraoRodape);
        profile.Observacoes = TrimOrNull(request.Observacoes);
        profile.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (isNew)
        {
            dbContext.AgenciaPerfis.Add(profile);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return profile;
    }

    private static string TrimRequired(string value) => value.Trim();
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? DigitsOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }
}
