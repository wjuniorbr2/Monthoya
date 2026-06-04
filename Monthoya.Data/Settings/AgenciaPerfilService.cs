using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;
using Monthoya.Core.Services;

namespace Monthoya.Data.Settings;

public sealed class AgenciaPerfilService(MonthoyaDbContext dbContext) : IAgenciaPerfilService
{
    public async Task<AgenciaPerfil?> GetAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);
        return await GetProfileCoreAsync(cancellationToken);
    }

    public async Task<bool> HasProfileAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);
        await using var command = CreateCommand("SELECT EXISTS (SELECT 1 FROM agencia_perfil LIMIT 1);");
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool value && value;
    }

    public async Task<AgenciaPerfil> SaveAsync(AgenciaPerfilRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureTableAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(request.RazaoSocial))
        {
            throw new InvalidOperationException("Informe a razão social/nome da imobiliária.");
        }

        var existing = await GetProfileCoreAsync(cancellationToken);
        var profile = existing ?? new AgenciaPerfil();
        ApplyRequest(profile, request);

        if (existing is null)
        {
            await InsertAsync(profile, cancellationToken);
        }
        else
        {
            await UpdateAsync(profile, cancellationToken);
        }

        return profile;
    }

    private async Task<AgenciaPerfil?> GetProfileCoreAsync(CancellationToken cancellationToken)
    {
        await using var command = CreateCommand("""
            SELECT "Id", "RazaoSocial", "NomeFantasia", "Cnpj", "InscricaoMunicipal", "InscricaoEstadual", "Creci",
                   "ResponsavelNome", "ResponsavelCpf", "ResponsavelCargo", "Email", "Telefone", "WhatsApp", "Site",
                   "Rua", "Numero", "Complemento", "Bairro", "Cidade", "Estado", "Cep", "DadosBancarios",
                   "TextoPadraoRodape", "Observacoes", "CreatedAtUtc", "UpdatedAtUtc"
            FROM agencia_perfil
            ORDER BY "CreatedAtUtc"
            LIMIT 1;
            """);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadProfile(reader) : null;
    }

    private static void ApplyRequest(AgenciaPerfil profile, AgenciaPerfilRequest request)
    {
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
    }

    private async Task InsertAsync(AgenciaPerfil profile, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand("""
            INSERT INTO agencia_perfil
            ("Id", "RazaoSocial", "NomeFantasia", "Cnpj", "InscricaoMunicipal", "InscricaoEstadual", "Creci",
             "ResponsavelNome", "ResponsavelCpf", "ResponsavelCargo", "Email", "Telefone", "WhatsApp", "Site",
             "Rua", "Numero", "Complemento", "Bairro", "Cidade", "Estado", "Cep", "DadosBancarios",
             "TextoPadraoRodape", "Observacoes", "CreatedAtUtc", "UpdatedAtUtc")
            VALUES
            (@Id, @RazaoSocial, @NomeFantasia, @Cnpj, @InscricaoMunicipal, @InscricaoEstadual, @Creci,
             @ResponsavelNome, @ResponsavelCpf, @ResponsavelCargo, @Email, @Telefone, @WhatsApp, @Site,
             @Rua, @Numero, @Complemento, @Bairro, @Cidade, @Estado, @Cep, @DadosBancarios,
             @TextoPadraoRodape, @Observacoes, @CreatedAtUtc, @UpdatedAtUtc);
            """);
        AddProfileParameters(command, profile);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpdateAsync(AgenciaPerfil profile, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand("""
            UPDATE agencia_perfil SET
                "RazaoSocial" = @RazaoSocial,
                "NomeFantasia" = @NomeFantasia,
                "Cnpj" = @Cnpj,
                "InscricaoMunicipal" = @InscricaoMunicipal,
                "InscricaoEstadual" = @InscricaoEstadual,
                "Creci" = @Creci,
                "ResponsavelNome" = @ResponsavelNome,
                "ResponsavelCpf" = @ResponsavelCpf,
                "ResponsavelCargo" = @ResponsavelCargo,
                "Email" = @Email,
                "Telefone" = @Telefone,
                "WhatsApp" = @WhatsApp,
                "Site" = @Site,
                "Rua" = @Rua,
                "Numero" = @Numero,
                "Complemento" = @Complemento,
                "Bairro" = @Bairro,
                "Cidade" = @Cidade,
                "Estado" = @Estado,
                "Cep" = @Cep,
                "DadosBancarios" = @DadosBancarios,
                "TextoPadraoRodape" = @TextoPadraoRodape,
                "Observacoes" = @Observacoes,
                "UpdatedAtUtc" = @UpdatedAtUtc
            WHERE "Id" = @Id;
            """);
        AddProfileParameters(command, profile);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureTableAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.OpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand("""
            CREATE TABLE IF NOT EXISTS agencia_perfil (
                "Id" uuid PRIMARY KEY,
                "RazaoSocial" character varying(220) NOT NULL,
                "NomeFantasia" character varying(220) NULL,
                "Cnpj" character varying(24) NULL,
                "InscricaoMunicipal" character varying(40) NULL,
                "InscricaoEstadual" character varying(40) NULL,
                "Creci" character varying(80) NULL,
                "ResponsavelNome" character varying(220) NULL,
                "ResponsavelCpf" character varying(20) NULL,
                "ResponsavelCargo" character varying(120) NULL,
                "Email" character varying(320) NULL,
                "Telefone" character varying(50) NULL,
                "WhatsApp" character varying(50) NULL,
                "Site" character varying(220) NULL,
                "Rua" character varying(220) NULL,
                "Numero" character varying(40) NULL,
                "Complemento" character varying(120) NULL,
                "Bairro" character varying(120) NULL,
                "Cidade" character varying(120) NULL,
                "Estado" character varying(2) NULL,
                "Cep" character varying(20) NULL,
                "DadosBancarios" character varying(2000) NULL,
                "TextoPadraoRodape" character varying(2000) NULL,
                "Observacoes" character varying(4000) NULL,
                "CreatedAtUtc" timestamp with time zone NOT NULL,
                "UpdatedAtUtc" timestamp with time zone NULL
            );
            """);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand CreateCommand(string sql)
    {
        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            connection.Open();
        }

        var command = connection.CreateCommand();
        command.CommandText = sql;
        return command;
    }

    private static void AddProfileParameters(DbCommand command, AgenciaPerfil profile)
    {
        Add(command, "Id", profile.Id);
        Add(command, "RazaoSocial", profile.RazaoSocial);
        Add(command, "NomeFantasia", profile.NomeFantasia);
        Add(command, "Cnpj", profile.Cnpj);
        Add(command, "InscricaoMunicipal", profile.InscricaoMunicipal);
        Add(command, "InscricaoEstadual", profile.InscricaoEstadual);
        Add(command, "Creci", profile.Creci);
        Add(command, "ResponsavelNome", profile.ResponsavelNome);
        Add(command, "ResponsavelCpf", profile.ResponsavelCpf);
        Add(command, "ResponsavelCargo", profile.ResponsavelCargo);
        Add(command, "Email", profile.Email);
        Add(command, "Telefone", profile.Telefone);
        Add(command, "WhatsApp", profile.WhatsApp);
        Add(command, "Site", profile.Site);
        Add(command, "Rua", profile.Rua);
        Add(command, "Numero", profile.Numero);
        Add(command, "Complemento", profile.Complemento);
        Add(command, "Bairro", profile.Bairro);
        Add(command, "Cidade", profile.Cidade);
        Add(command, "Estado", profile.Estado);
        Add(command, "Cep", profile.Cep);
        Add(command, "DadosBancarios", profile.DadosBancarios);
        Add(command, "TextoPadraoRodape", profile.TextoPadraoRodape);
        Add(command, "Observacoes", profile.Observacoes);
        Add(command, "CreatedAtUtc", profile.CreatedAtUtc);
        Add(command, "UpdatedAtUtc", profile.UpdatedAtUtc);
    }

    private static void Add(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static AgenciaPerfil ReadProfile(DbDataReader reader) => new()
    {
        Id = reader.GetGuid(0),
        RazaoSocial = reader.GetString(1),
        NomeFantasia = GetString(reader, 2),
        Cnpj = GetString(reader, 3),
        InscricaoMunicipal = GetString(reader, 4),
        InscricaoEstadual = GetString(reader, 5),
        Creci = GetString(reader, 6),
        ResponsavelNome = GetString(reader, 7),
        ResponsavelCpf = GetString(reader, 8),
        ResponsavelCargo = GetString(reader, 9),
        Email = GetString(reader, 10),
        Telefone = GetString(reader, 11),
        WhatsApp = GetString(reader, 12),
        Site = GetString(reader, 13),
        Rua = GetString(reader, 14),
        Numero = GetString(reader, 15),
        Complemento = GetString(reader, 16),
        Bairro = GetString(reader, 17),
        Cidade = GetString(reader, 18),
        Estado = GetString(reader, 19),
        Cep = GetString(reader, 20),
        DadosBancarios = GetString(reader, 21),
        TextoPadraoRodape = GetString(reader, 22),
        Observacoes = GetString(reader, 23),
        CreatedAtUtc = reader.GetFieldValue<DateTimeOffset>(24),
        UpdatedAtUtc = reader.IsDBNull(25) ? null : reader.GetFieldValue<DateTimeOffset>(25)
    };

    private static string? GetString(DbDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
    private static string TrimRequired(string value) => value.Trim();
    private static string? TrimOrNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? DigitsOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }
}