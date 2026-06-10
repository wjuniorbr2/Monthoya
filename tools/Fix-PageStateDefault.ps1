param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$path = 'Monthoya.Desktop/Views/ShellWindow.Imoveis.cs'
if (-not (Test-Path $path)) {
    throw "File not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8
$original = $text

# The previous IPTU split added one string parameter to ImoveisPageState.
# In some partially patched local files, the Default constructor is missing one final blank string.
# Add it after ChaveAutorizacao=false and before the closing of Default.
$old = @'
            false,
            "");
'@
$new = @'
            false,
            "",
            "");
'@

if ($text.Contains($old) -and -not $text.Contains($new)) {
    $text = $text.Replace($old, $new)
}

if ($text -eq $original) {
    Write-Host "No page-state default fix was needed in $path"
} else {
    Set-Content -Path $path -Value $text -Encoding UTF8 -NoNewline
    Write-Host "Updated: $path"
}

Write-Host ''
Write-Host 'Page-state default repair finished. Building solution now...'

if (-not $SkipBuild) {
    dotnet build Monthoya.sln
}
