using Microsoft.EntityFrameworkCore;

namespace Monthoya.Data.Diagnostics;

public sealed class SupabaseSchemaAudit(MonthoyaDbContext dbContext)
{
    public static readonly string[] ExpectedPrivateBuckets =
    [
        "monthoya-documents",
        "monthoya-property-images"
    ];

    public static readonly (string Table, string Column)[] RemovedPessoaAddressColumns =
    [
        ("pessoa_fisica", "Endereco"),
        ("pessoa_juridica", "EnderecoEmpresa"),
        ("pessoa_juridica", "ResponsavelEndereco")
    ];

    public static readonly string[] ExpectedRlsProtectedTables =
    [
        "boletos",
        "certificados_digitais",
        "contas_pagar_receber",
        "dimob_declaracoes",
        "dimob_itens",
        "documentos_gerados",
        "documentos_modelos",
        "imovel_imagens",
        "imoveis",
        "indices_reajuste",
        "lancamentos_financeiros",
        "locacao_fiadores",
        "locacoes",
        "manutencoes_imovel",
        "notas_fiscais",
        "pessoa_documentos",
        "pessoa_fisica",
        "pessoa_juridica",
        "pessoa_roles",
        "pessoas",
        "rescisoes",
        "users",
        "vistorias"
    ];

    public const string HistoricalLiveOnlyMigrationId = "20260526085111_AddPessoaOcrAndAddressDetails";

    public async Task<SupabaseSchemaAuditResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var appliedMigrations = await dbContext.Database
            .SqlQueryRaw<string>("select \"MigrationId\" as \"Value\" from \"__EFMigrationsHistory\"")
            .ToListAsync(cancellationToken);

        var latestKnownMigration = dbContext.Database.GetMigrations().Order().LastOrDefault();
        var latestMigrationApplied = latestKnownMigration is null || appliedMigrations.Contains(latestKnownMigration, StringComparer.Ordinal);

        var staleColumns = new List<string>();
        foreach (var (table, column) in RemovedPessoaAddressColumns)
        {
            var exists = await ColumnExistsAsync(table, column, cancellationToken);
            if (exists)
            {
                staleColumns.Add($"{table}.{column}");
            }
        }

        var bucketRows = await dbContext.Database
            .SqlQueryRaw<StorageBucketAuditRow>(
                "select id as \"Id\", coalesce(public, false) as \"Public\" from storage.buckets where id in ('monthoya-documents', 'monthoya-property-images')")
            .ToListAsync(cancellationToken);

        var bucketMap = bucketRows.ToDictionary(x => x.Id, x => x.Public, StringComparer.Ordinal);
        var missingBuckets = ExpectedPrivateBuckets.Where(bucket => !bucketMap.ContainsKey(bucket)).ToList();
        var publicBuckets = bucketMap.Where(x => x.Value).Select(x => x.Key).ToList();
        var rlsRows = await dbContext.Database
            .SqlQueryRaw<RlsAuditRow>(
                """
                select c.relname as "TableName", c.relrowsecurity as "RlsEnabled"
                from pg_class c
                join pg_namespace n on n.oid = c.relnamespace
                where n.nspname = 'public'
                  and c.relkind = 'r'
                  and c.relname = any ({0})
                """,
                ExpectedRlsProtectedTables)
            .ToListAsync(cancellationToken);

        var rlsMap = rlsRows.ToDictionary(x => x.TableName, x => x.RlsEnabled, StringComparer.Ordinal);
        var rlsDisabledTables = ExpectedRlsProtectedTables
            .Where(table => rlsMap.TryGetValue(table, out var rlsEnabled) && !rlsEnabled)
            .ToList();
        var extraLiveOnlyMigrations = appliedMigrations
            .Except(dbContext.Database.GetMigrations(), StringComparer.Ordinal)
            .Where(x => !string.Equals(x, HistoricalLiveOnlyMigrationId, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();
        var historicalLiveOnlyMigrationPresent = appliedMigrations.Contains(HistoricalLiveOnlyMigrationId, StringComparer.Ordinal);

        return new SupabaseSchemaAuditResult(
            latestKnownMigration,
            latestMigrationApplied,
            missingBuckets,
            publicBuckets,
            rlsDisabledTables,
            staleColumns,
            historicalLiveOnlyMigrationPresent,
            extraLiveOnlyMigrations);
    }

    private async Task<bool> ColumnExistsAsync(string table, string column, CancellationToken cancellationToken)
    {
        FormattableString query = $"""
            select exists (
                select 1
                from information_schema.columns
                where table_schema = 'public'
                  and table_name = {table}
                  and column_name = {column}
            ) as "Value"
            """;

        return await dbContext.Database.SqlQuery<bool>(query).SingleAsync(cancellationToken);
    }

    private sealed record StorageBucketAuditRow(string Id, bool Public);

    private sealed record RlsAuditRow(string TableName, bool RlsEnabled);
}

public sealed record SupabaseSchemaAuditResult(
    string? LatestKnownMigration,
    bool LatestMigrationApplied,
    IReadOnlyList<string> MissingBuckets,
    IReadOnlyList<string> PublicBuckets,
    IReadOnlyList<string> RlsDisabledTables,
    IReadOnlyList<string> StaleRemovedColumns,
    bool HistoricalLiveOnlyMigrationPresent,
    IReadOnlyList<string> UnexpectedLiveOnlyMigrations)
{
    public bool IsHealthy =>
        LatestMigrationApplied
        && MissingBuckets.Count == 0
        && PublicBuckets.Count == 0
        && RlsDisabledTables.Count == 0
        && StaleRemovedColumns.Count == 0
        && UnexpectedLiveOnlyMigrations.Count == 0;
}
