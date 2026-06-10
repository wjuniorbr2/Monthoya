param(
    [int]$LargeFileLineThreshold = 500,
    [switch]$SkipBuild,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

Write-Host 'Monthoya refactor readiness audit'
Write-Host '================================'
Write-Host ''

$extensions = @('.cs', '.xaml', '.csproj', '.json', '.md', '.ps1')
$ignoredParts = @('\bin\', '\obj\', '\.git\')

$files = Get-ChildItem -Recurse -File | Where-Object {
    $path = $_.FullName
    ($extensions -contains $_.Extension) -and -not ($ignoredParts | Where-Object { $path.Contains($_) })
}

$largeFiles = foreach ($file in $files) {
    $lineCount = (Get-Content -Path $file.FullName -ErrorAction SilentlyContinue | Measure-Object -Line).Lines
    if ($lineCount -ge $LargeFileLineThreshold) {
        [PSCustomObject]@{
            Lines = $lineCount
            Path = Resolve-Path -Relative $file.FullName
        }
    }
}

Write-Host "Large files ($LargeFileLineThreshold+ lines):"
$largeFiles | Sort-Object Lines -Descending | Format-Table -AutoSize

Write-Host ''
Write-Host 'Temporary repair scripts in tools/:'
Get-ChildItem .\tools -Filter '*.ps1' -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match 'Fix-|Apply-Split|Repair' } |
    Select-Object Name, Length, LastWriteTime |
    Format-Table -AutoSize

Write-Host ''
Write-Host 'Likely high-risk patterns:'
$patterns = @(
    'record ImoveisPageState',
    'ShellWindow.xaml',
    'ShellWindow.Imoveis.cs',
    'new\(',
    'IptuMatricula'
)

foreach ($pattern in $patterns) {
    $matches = Select-String -Path ($files.FullName) -Pattern $pattern -SimpleMatch:$false -ErrorAction SilentlyContinue
    $count = ($matches | Measure-Object).Count
    Write-Host ("{0}: {1}" -f $pattern, $count)
}

Write-Host ''
if (-not $SkipBuild) {
    Write-Host 'Running build...'
    dotnet build Monthoya.sln
}

if (-not $SkipTests) {
    Write-Host ''
    Write-Host 'Running tests...'
    dotnet test Monthoya.Tests
}

Write-Host ''
Write-Host 'Audit complete.'
