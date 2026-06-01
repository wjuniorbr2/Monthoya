using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Monthoya.Desktop.Services;

internal sealed record GeminiDocumentData(
    string? DocumentType,
    string? Name,
    string? Cpf,
    string? Rg,
    DateOnly? BirthDate,
    string? Nationality,
    string? Email,
    string? Phone,
    string? Cep,
    string? Street,
    string? Number,
    string? Complement,
    string? Neighborhood,
    string? City,
    string? State,
    string? CompanyName,
    string? CompanyTradeName,
    string? Cnpj,
    string? CompanyActivity,
    string? CompanyStateRegistration,
    string? CompanyMunicipalRegistration,
    DateOnly? CompanyOpeningDate,
    string? JobTitle,
    string? Income,
    string? EmploymentDuration,
    string? RawJson);

internal static class GeminiDocumentDataReader
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static async Task<GeminiDocumentData> ExtractAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var settings = LocalAiSettingsStore.Load();
        if (string.IsNullOrWhiteSpace(settings.GeminiApiKey))
        {
            throw new InvalidOperationException("OCR inteligente não configurado. Abra Configurações e informe a chave da API Gemini.");
        }

        if (!File.Exists(filePath))
        {
            throw new InvalidOperationException("O arquivo do documento não foi encontrado no computador.");
        }

        var mimeType = GuessMimeType(filePath);
        if (!mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Nesta etapa, o OCR inteligente aceita imagens JPG, PNG, BMP ou TIFF. Converta PDF em imagem antes de usar.");
        }

        var bytes = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var base64 = Convert.ToBase64String(bytes);
        var model = string.IsNullOrWhiteSpace(settings.GeminiModel) ? "gemini-2.5-flash" : settings.GeminiModel.Trim();
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(settings.GeminiApiKey.Trim())}";

        var request = new GeminiGenerateContentRequest(
            [
                new GeminiContent(
                    [
                        new GeminiPart(Text: BuildPrompt()),
                        new GeminiPart(InlineData: new GeminiInlineData(mimeType, base64))
                    ])
            ],
            new GeminiGenerationConfig("application/json", 0.0));

        using var response = await HttpClient.PostAsJsonAsync(endpoint, request, JsonOptions, cancellationToken);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Gemini retornou erro: {(int)response.StatusCode} - {responseText}");
        }

        var geminiResponse = JsonSerializer.Deserialize<GeminiGenerateContentResponse>(responseText, JsonOptions);
        var json = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault(part => !string.IsNullOrWhiteSpace(part.Text))?.Text;
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Gemini não retornou dados do documento.");
        }

        return ParseStoredJson(json)
            ?? throw new InvalidOperationException("Não foi possível interpretar o retorno do Gemini.");
    }

    internal static GeminiDocumentData? ParseStoredJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        json = ExtractJsonObject(json);
        GeminiDocumentJson? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<GeminiDocumentJson>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (parsed is null)
        {
            return null;
        }

        return new GeminiDocumentData(
            DocumentType: NullIfBlank(parsed.DocumentType),
            Name: NormalizeName(parsed.Person?.Name),
            Cpf: OnlyDigits(parsed.Person?.Cpf),
            Rg: OnlyDigits(parsed.Person?.Rg),
            BirthDate: ParseDate(parsed.Person?.BirthDate),
            Nationality: NullIfBlank(parsed.Person?.Nationality),
            Email: NullIfBlank(parsed.Person?.Email),
            Phone: OnlyDigits(parsed.Person?.Phone),
            Cep: OnlyDigits(parsed.Address?.Cep),
            Street: NullIfBlank(parsed.Address?.Street),
            Number: NullIfBlank(parsed.Address?.Number),
            Complement: NullIfBlank(parsed.Address?.Complement),
            Neighborhood: NullIfBlank(parsed.Address?.Neighborhood),
            City: NullIfBlank(parsed.Address?.City),
            State: NormalizeState(parsed.Address?.State),
            CompanyName: NullIfBlank(parsed.Company?.Name),
            CompanyTradeName: NullIfBlank(parsed.Company?.TradeName),
            Cnpj: OnlyDigits(parsed.Company?.Cnpj),
            CompanyActivity: NullIfBlank(parsed.Company?.Activity),
            CompanyStateRegistration: OnlyDigits(parsed.Company?.StateRegistration),
            CompanyMunicipalRegistration: OnlyDigits(parsed.Company?.MunicipalRegistration),
            CompanyOpeningDate: ParseDate(parsed.Company?.OpeningDate),
            JobTitle: NullIfBlank(parsed.Work?.JobTitle),
            Income: NullIfBlank(parsed.Work?.Income),
            EmploymentDuration: NullIfBlank(parsed.Work?.EmploymentDuration),
            RawJson: json);
    }

    private static string BuildPrompt() =>
        """
        Analyze this Brazilian document image and extract only data that is clearly present.
        Return JSON only. Do not include explanations or markdown.

        Required JSON shape:
        {
          "document_type": "identity_document | drivers_license | cpf | residence_proof | utility_bill | company_document | work_document | unknown",
          "person": {
            "name": null,
            "cpf": null,
            "rg": null,
            "birth_date": null,
            "nationality": null,
            "email": null,
            "phone": null
          },
          "address": {
            "cep": null,
            "street": null,
            "number": null,
            "complement": null,
            "neighborhood": null,
            "city": null,
            "state": null
          },
          "company": {
            "name": null,
            "trade_name": null,
            "cnpj": null,
            "activity": null,
            "state_registration": null,
            "municipal_registration": null,
            "opening_date": null
          },
          "work": {
            "job_title": null,
            "income": null,
            "employment_duration": null
          }
        }

        Rules:
        - Do not guess.
        - If a field is not clearly visible, return null.
        - Do not use CEP as RG.
        - Do not use issue date, first-license date, due date, billing date, or document validity date as birth date.
        - For utility bills or residence proof, do not invent RG or birth date.
        - For CNH, prefer the field "Nome e sobrenome" for person.name.
        - If the document has multiple pages/front/back/QR, choose the actual person/company data, not document title text.
        - CPF and CNPJ may be returned with or without punctuation.
        - State registration and municipal registration may be returned with or without punctuation.
        - Dates must be dd/MM/yyyy.
        """;

    private static string ExtractJsonObject(string value)
    {
        var trimmed = value.Trim();
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        return start >= 0 && end > start ? trimmed[start..(end + 1)] : trimmed;
    }

    private static string GuessMimeType(string filePath) =>
        Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".bmp" => "image/bmp",
            ".tif" or ".tiff" => "image/tiff",
            _ => "application/octet-stream"
        };

    private static string? OnlyDigits(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return string.IsNullOrWhiteSpace(digits) ? null : digits;
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? NormalizeName(string? value) => NullIfBlank(value)?.ToUpperInvariant();

    private static string? NormalizeState(string? value)
    {
        var normalized = NullIfBlank(value)?.ToUpperInvariant();
        return normalized is null ? null : normalized.Length >= 2 ? normalized[..2] : normalized;
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var formats = new[] { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d-M-yyyy" };
        return DateOnly.TryParseExact(value.Trim(), formats, CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private sealed record GeminiGenerateContentRequest(
        List<GeminiContent> Contents,
        GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiContent(List<GeminiPart> Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string? Text = null,
        [property: JsonPropertyName("inline_data")] GeminiInlineData? InlineData = null);

    private sealed record GeminiInlineData(
        [property: JsonPropertyName("mime_type")] string MimeType,
        [property: JsonPropertyName("data")] string Data);

    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("response_mime_type")] string ResponseMimeType,
        [property: JsonPropertyName("temperature")] double Temperature);

    private sealed record GeminiGenerateContentResponse(List<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(GeminiContentResponse? Content);

    private sealed record GeminiContentResponse(List<GeminiTextPart>? Parts);

    private sealed record GeminiTextPart(string? Text);

    private sealed record GeminiDocumentJson(
        [property: JsonPropertyName("document_type")] string? DocumentType,
        GeminiPersonJson? Person,
        GeminiAddressJson? Address,
        GeminiCompanyJson? Company,
        GeminiWorkJson? Work);

    private sealed record GeminiPersonJson(
        string? Name,
        string? Cpf,
        string? Rg,
        [property: JsonPropertyName("birth_date")] string? BirthDate,
        string? Nationality,
        string? Email,
        string? Phone);

    private sealed record GeminiAddressJson(string? Cep, string? Street, string? Number, string? Complement, string? Neighborhood, string? City, string? State);

    private sealed record GeminiCompanyJson(
        string? Name,
        [property: JsonPropertyName("trade_name")] string? TradeName,
        string? Cnpj,
        string? Activity,
        [property: JsonPropertyName("state_registration")] string? StateRegistration,
        [property: JsonPropertyName("municipal_registration")] string? MunicipalRegistration,
        [property: JsonPropertyName("opening_date")] string? OpeningDate);

    private sealed record GeminiWorkJson(
        [property: JsonPropertyName("job_title")] string? JobTitle,
        string? Income,
        [property: JsonPropertyName("employment_duration")] string? EmploymentDuration);
}
