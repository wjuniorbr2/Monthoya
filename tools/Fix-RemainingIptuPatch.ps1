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

# Replace remaining old IPTU TextBox references in helper methods.
$text = $text.Replace('        yield return ImovelIptuBox;', "        yield return ImovelIptuInscricaoBox;`r`n        yield return ImovelIptuCadastroBox;")
$text = $text.Replace('        ImovelIptuBox.Clear();', "        ImovelIptuInscricaoBox.Clear();`r`n        ImovelIptuCadastroBox.Clear();")

# If the record was split from 1 IPTU field into 2, Default needs one extra empty string
# before ImovelStatus.Disponivel. Only add it if it has not already been added.
$oldDefaultBlock = @'
            "",
            "",
            ImovelStatus.Disponivel,
'@
$newDefaultBlock = @'
            "",
            "",
            "",
            ImovelStatus.Disponivel,
'@

if ($text.Contains($oldDefaultBlock) -and -not $text.Contains($newDefaultBlock)) {
    $text = $text.Replace($oldDefaultBlock, $newDefaultBlock)
}

if ($text -eq $original) {
    Write-Host "No remaining IPTU fixes were needed in $path"
} else {
    Set-Content -Path $path -Value $text -Encoding UTF8 -NoNewline
    Write-Host "Updated: $path"
}

Write-Host ''
Write-Host 'Final IPTU repair finished. Building solution now...'

if (-not $SkipBuild) {
    dotnet build Monthoya.sln
}
