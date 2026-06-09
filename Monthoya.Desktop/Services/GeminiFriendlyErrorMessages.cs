using System.Text.Json;

namespace Monthoya.Desktop.Services;

internal static class GeminiFriendlyErrorMessages
{
    internal static string FromGeminiResponse(int statusCode, string responseText)
    {
        var status = ReadGeminiErrorValue(responseText, "status");
        var message = ReadGeminiErrorValue(responseText, "message");
        var errorText = $"{status} {message}";

        if (statusCode == 503 || ContainsAny(errorText, "UNAVAILABLE", "high demand", "overloaded"))
        {
            return "O serviço de leitura por IA está temporariamente indisponível ou sobrecarregado. Tente novamente em alguns minutos.";
        }

        if (statusCode == 429 || ContainsAny(errorText, "RESOURCE_EXHAUSTED", "quota", "rate limit", "TooManyRequests"))
        {
            return "O limite temporário de uso da IA foi atingido. Aguarde alguns minutos e tente novamente. Se o problema continuar, verifique a cota do projeto no Google AI Studio.";
        }

        if (statusCode is 401 or 403 || ContainsAny(errorText, "UNAUTHENTICATED", "PERMISSION_DENIED"))
        {
            return "Não foi possível acessar a IA. Verifique se a configuração da IA está correta.";
        }

        return "Não foi possível ler o documento com IA neste momento. Tente novamente ou preencha os dados manualmente.";
    }

    private static string? ReadGeminiErrorValue(string responseText, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseText);
            return document.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty(propertyName, out var value)
                    ? value.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool ContainsAny(string? value, params string[] terms) =>
        !string.IsNullOrWhiteSpace(value)
        && terms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase));
}
