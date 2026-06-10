param(
    [int]$LargeFileLineThreshold = 500,
    [switch]$SkipBuild,
    [switch]$SkipTests,
    [switch]$IncludeGenerated
)

$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

Write-Host 'Monthoya refactor readiness audit'
Write-Host '================================'
Write-Host ''

$extensions = @('.cs', '.xaml', '.csproj', '.json', '.md', '.ps1')
$ignoredParts = @('\bin\', '\obj\', '\.git\')
$generatedPatterns = @(
    '\Monthoya.Data\Migrations\',
    '.Designer.cs',
    'MonthoyaDbContextModelSnapshot.cs'
)

$files = Get-ChildItem -Recurse -File | Where-Object {
    $path = $_.FullName
    ($extensions -contains $_.Extension) -and -not ($ignoredParts | Where-Object { $path.Contains($_) })
}

$generatedFiles = $files | Where-Object {
    $path = $_.FullName
    $generatedPatterns | Where-Object { $path.Contains($_) }
}

$auditFiles = if ($IncludeGenerated) {
    $files
} else {
    $files | Where-Object {
        $path = $_.FullName
        -not ($generatedPatterns | Where-Object { $path.Contains($_) })
    }
}

$largeFiles = foreach ($file in $auditFiles) {
    $lineCount = (Get-Content -Path $file.FullName -ErrorAction SilentlyContinue | Measure-Object -Line).Lines
    if ($lineCount -ge $LargeFileLineThreshold) {
        [PSCustomObject]@{
            Lines = $lineCount
            Path = Resolve-Path -Relative $file.FullName
        }
    }
}

if ($IncludeGenerated) {
    Write-Host "Large files ($LargeFileLineThreshold+ lines, including generated files):"
} else {
    Write-Host "Large files ($LargeFileLineThreshold+ lines, generated files hidden):"
}

$largeFiles | Sort-Object Lines -Descending | Format-Table -AutoSize

if (-not $IncludeGenerated) {
    Write-Host ''
    Write-Host ('Generated files hidden from large-file list: {0}' -f (($generatedFiles | Measure-Object).Count))
    Write-Host 'Run with -IncludeGenerated to include EF migrations/model snapshots.'
}

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
    'record PessoasPageState',
    'ShellWindow.xaml',
    'ShellWindow.Imoveis.cs',
    'ShellWindow.Pessoas.cs',
    'IptuMatricula'
)

foreach ($pattern in $patterns) {
    $matches = Select-String -Path ($auditFiles.FullName) -Pattern $pattern -SimpleMatch:$false -ErrorAction SilentlyContinue
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
