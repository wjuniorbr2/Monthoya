param(
    [switch]$SkipBuild,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

function Find-MatchingBrace {
    param(
        [Parameter(Mandatory=$true)][string]$Text,
        [Parameter(Mandatory=$true)][int]$OpenIndex
    )

    $depth = 0
    for ($i = $OpenIndex; $i -lt $Text.Length; $i++) {
        $c = $Text[$i]
        if ($c -eq '{') { $depth++ }
        elseif ($c -eq '}') {
            $depth--
            if ($depth -eq 0) { return $i }
        }
    }

    throw 'No matching closing brace found.'
}

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

$sourcePath = 'Monthoya.Desktop/Views/ShellWindow.Imoveis.cs'
$targetPath = 'Monthoya.Desktop/Views/ShellWindow.ImoveisPageState.cs'

if (-not (Test-Path $sourcePath)) {
    throw "File not found: $sourcePath"
}

if (Test-Path $targetPath) {
    Write-Host "$targetPath already exists. Skipping extraction."
} else {
    $source = Get-Content -Path $sourcePath -Raw -Encoding UTF8
    $recordStart = $source.IndexOf('private sealed record ImoveisPageState')
    if ($recordStart -lt 0) {
        throw 'Could not find ImoveisPageState in ShellWindow.Imoveis.cs.'
    }

    $recordOpenBrace = $source.IndexOf('{', $recordStart)
    if ($recordOpenBrace -lt 0) {
        throw 'Could not find ImoveisPageState body start.'
    }

    $recordCloseBrace = Find-MatchingBrace -Text $source -OpenIndex $recordOpenBrace
    $recordBlock = $source.Substring($recordStart, $recordCloseBrace - $recordStart + 1).TrimEnd()

    # Remove the record from the original file, leaving the ShellWindow partial class closing brace intact.
    $before = $source.Substring(0, $recordStart).TrimEnd()
    $after = $source.Substring($recordCloseBrace + 1).TrimStart()
    $updatedSource = $before + "`r`n" + $after
    Set-Content -Path $sourcePath -Value $updatedSource -Encoding UTF8 -NoNewline
    Write-Host "Removed ImoveisPageState from $sourcePath"

    $targetContent = @"
using Monthoya.Core.Entities;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
$recordBlock
}
"@

    Set-Content -Path $targetPath -Value $targetContent -Encoding UTF8 -NoNewline
    Write-Host "Created $targetPath"
}

Write-Host ''
Write-Host 'Refactor extraction complete.'

if (-not $SkipBuild) {
    Write-Host ''
    Write-Host 'Running build...'
    dotnet build Monthoya.sln
}

if (-not $SkipTests) {
    Write-Host ''
    Write-Host 'Running tests...'
    dotnet test Monthoya.Tests
}
