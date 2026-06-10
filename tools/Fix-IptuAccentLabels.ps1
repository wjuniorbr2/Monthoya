param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

$path = 'Monthoya.Desktop/Views/ShellWindow.xaml'
if (-not (Test-Path $path)) {
    throw "File not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8
$original = $text

# Use XML numeric entities so WPF renders accents correctly even if the file encoding is later changed.
$correctInscricao = 'Inscri&#xE7;&#xE3;o imobili&#xE1;ria'
$correctCadastro = 'Cadastro do im&#xF3;vel'

# Replace common mojibake variants and any direct accented/plain variants inside the TextBlock labels.
$text = $text -replace 'Text="Inscri[^"
]*imobili[^"
]*ria"', "Text=\"$correctInscricao\""
$text = $text -replace 'Text="Cadastro do im[^"
]*vel"', "Text=\"$correctCadastro\""

if ($text -eq $original) {
    Write-Host "No IPTU accent label fixes were needed in $path"
} else {
    Set-Content -Path $path -Value $text -Encoding UTF8 -NoNewline
    Write-Host "Updated: $path"
}

Write-Host ''
Write-Host 'IPTU accent label repair finished. Building solution now...'

if (-not $SkipBuild) {
    dotnet build Monthoya.sln
}
