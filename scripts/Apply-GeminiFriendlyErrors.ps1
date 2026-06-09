$ErrorActionPreference = 'Stop'

$path = Join-Path $PSScriptRoot '..\Monthoya.Desktop\Services\GeminiDocumentDataReader.cs'

if (-not (Test-Path $path)) {
    throw "File not found: $path"
}

$content = Get-Content $path -Raw

if ($content -like '*GeminiFriendlyErrorMessages.FromGeminiResponse*') {
    Write-Host 'Gemini friendly error handling is already connected.'
    exit 0
}

$pattern = 'throw\s+new\s+InvalidOperationException\s*\(\s*\$"Gemini retornou erro:\s*\{\(int\)response\.StatusCode\}\s*-\s*\{responseText\}"\s*\)\s*;'
$replacement = 'throw new InvalidOperationException(GeminiFriendlyErrorMessages.FromGeminiResponse((int)response.StatusCode, responseText));'

$updated = [regex]::Replace($content, $pattern, $replacement)

if ($updated -eq $content) {
    throw 'Expected Gemini raw error throw was not found. Send the output of: Select-String -Path Monthoya.Desktop/Services/GeminiDocumentDataReader.cs -Pattern "Gemini retornou erro|InvalidOperationException"'
}

Set-Content -Path $path -Value $updated -Encoding UTF8
Write-Host 'Updated GeminiDocumentDataReader.cs to use friendly Gemini error messages.'
