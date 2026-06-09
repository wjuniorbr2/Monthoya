$ErrorActionPreference = 'Stop'

$path = Join-Path $PSScriptRoot '..\Monthoya.Desktop\Services\GeminiDocumentDataReader.cs'

if (-not (Test-Path $path)) {
    throw "File not found: $path"
}

$content = Get-Content $path -Raw

$old = @'
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Gemini retornou erro: {(int)response.StatusCode} - {responseText}");
        }
'@

$new = @'
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(GeminiFriendlyErrorMessages.FromGeminiResponse((int)response.StatusCode, responseText));
        }
'@

if ($content -like '*GeminiFriendlyErrorMessages.FromGeminiResponse*') {
    Write-Host 'Gemini friendly error handling is already connected.'
    exit 0
}

if (-not $content.Contains($old)) {
    throw 'Expected Gemini raw error block was not found. The file may have changed.'
}

$content = $content.Replace($old, $new)
Set-Content -Path $path -Value $content -Encoding UTF8
Write-Host 'Updated GeminiDocumentDataReader.cs to use friendly Gemini error messages.'
