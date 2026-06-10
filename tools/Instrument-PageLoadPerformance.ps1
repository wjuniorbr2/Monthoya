param(
    [switch]$SkipBuild,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

$path = 'Monthoya.Desktop/Views/ShellWindow.Tabs.cs'
if (-not (Test-Path $path)) {
    throw "File not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8
$original = $text

if (-not $text.Contains('using Monthoya.Desktop.Diagnostics;')) {
    $text = $text.Replace('using System.Windows.Media;', "using System.Windows.Media;`r`nusing Monthoya.Desktop.Diagnostics;")
}

$oldSignature = @'
    private async Task ShowPageAsync(ShellPage page, bool loadData)
    {
'@
$newSignature = @'
    private async Task ShowPageAsync(ShellPage page, bool loadData)
    {
        using var pageLoadTrace = PerformanceTrace.Measure($"ShowPageAsync {page} loadData={loadData}");

'@
if (-not $text.Contains('PerformanceTrace.Measure($"ShowPageAsync {page} loadData={loadData}")')) {
    if (-not $text.Contains($oldSignature)) {
        throw 'Could not find ShowPageAsync method signature.'
    }
    $text = $text.Replace($oldSignature, $newSignature)
}

$replacements = @(
    @('await LoadDashboardAsync();', 'using (PerformanceTrace.Measure("LoadDashboardAsync"))`r`n                {`r`n                    await LoadDashboardAsync();`r`n                }'),
    @('await LoadUsersAsync();', 'using (PerformanceTrace.Measure("LoadUsersAsync"))`r`n                {`r`n                    await LoadUsersAsync();`r`n                }'),
    @('await LoadPessoasAsync();', 'using (PerformanceTrace.Measure("LoadPessoasAsync"))`r`n                {`r`n                    await LoadPessoasAsync();`r`n                }'),
    @('await LoadImoveisAsync();', 'using (PerformanceTrace.Measure("LoadImoveisAsync"))`r`n                {`r`n                    await LoadImoveisAsync();`r`n                }'),
    @('await LoadChavesAsync();', 'using (PerformanceTrace.Measure("LoadChavesAsync"))`r`n                {`r`n                    await LoadChavesAsync();`r`n                }'),
    @('await LoadNotificationsAsync();', 'using (PerformanceTrace.Measure("LoadNotificationsAsync"))`r`n                {`r`n                    await LoadNotificationsAsync();`r`n                }'),
    @('await LoadGenericModuleAsync(page);', 'using (PerformanceTrace.Measure($"LoadGenericModuleAsync {page}"))`r`n                {`r`n                    await LoadGenericModuleAsync(page);`r`n                }')
)

foreach ($pair in $replacements) {
    $old = $pair[0]
    $new = $pair[1]
    if ($text.Contains($old) -and -not $text.Contains($new)) {
        $text = $text.Replace($old, $new)
    }
}

$oldRestore = 'await RestoreActiveTabStateAsync(page);'
$newRestore = @'
        using (PerformanceTrace.Measure($"RestoreActiveTabStateAsync {page}"))
        {
            await RestoreActiveTabStateAsync(page);
        }
'@
if ($text.Contains($oldRestore) -and -not $text.Contains('PerformanceTrace.Measure($"RestoreActiveTabStateAsync {page}"')) {
    $text = $text.Replace($oldRestore, $newRestore.TrimEnd())
}

if ($text -eq $original) {
    Write-Host "No page-load instrumentation changes were needed in $path"
} else {
    Set-Content -Path $path -Value $text -Encoding UTF8 -NoNewline
    Write-Host "Updated: $path"
}

Write-Host ''
Write-Host 'Page-load performance instrumentation complete.'

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
