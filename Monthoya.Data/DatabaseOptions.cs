using Microsoft.Extensions.Configuration;

namespace Monthoya.Data;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string ConnectionString { get; init; } = string.Empty;

    public static string? GetConnectionString(IConfiguration configuration)
    {
        return configuration[$"{SectionName}:{nameof(ConnectionString)}"]
            ?? configuration.GetConnectionString("DefaultConnection");
    }
}