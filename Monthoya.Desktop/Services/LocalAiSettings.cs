using System.IO;
using System.Text.Json;

namespace Monthoya.Desktop.Services;

internal sealed record LocalAiSettings(string? GeminiApiKey = null, string GeminiModel = LocalAiSettingsStore.DefaultGeminiModel);

internal static class LocalAiSettingsStore
{
    internal const string DefaultGeminiModel = "gemini-2.5-flash";
    private const string EnvironmentKeyName = "MONTHOYA_GEMINI_API_KEY";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    internal static string SettingsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Monthoya");

    internal static string SettingsPath =>
        Path.Combine(SettingsDirectory, "ai-settings.json");

    internal static LocalAiSettings Load()
    {
        var environmentKey = Environment.GetEnvironmentVariable(EnvironmentKeyName);
        if (!string.IsNullOrWhiteSpace(environmentKey))
        {
            return new LocalAiSettings(environmentKey.Trim(), DefaultGeminiModel);
        }

        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new LocalAiSettings();
            }

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<LocalAiSettings>(json, JsonOptions) ?? new LocalAiSettings();
            return string.IsNullOrWhiteSpace(settings.GeminiModel)
                ? settings with { GeminiModel = DefaultGeminiModel }
                : settings;
        }
        catch
        {
            return new LocalAiSettings();
        }
    }

    internal static void Save(LocalAiSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        var normalized = settings with
        {
            GeminiApiKey = string.IsNullOrWhiteSpace(settings.GeminiApiKey) ? null : settings.GeminiApiKey.Trim(),
            GeminiModel = string.IsNullOrWhiteSpace(settings.GeminiModel) ? DefaultGeminiModel : settings.GeminiModel.Trim()
        };
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(normalized, JsonOptions));
    }

    internal static string? GetGeminiApiKey() => Load().GeminiApiKey;

    internal static bool HasGeminiApiKey() => !string.IsNullOrWhiteSpace(GetGeminiApiKey());
}
