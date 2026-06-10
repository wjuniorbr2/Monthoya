param([switch]$SkipBuild, [switch]$SkipTests)

$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

$path = 'Monthoya.Desktop/Views/ShellWindow.Pessoas.cs'
$text = Get-Content -Path $path -Raw -Encoding UTF8
$original = $text

$text = $text.Replace('        _streetSuggestions = await _rentalManagementService.GetStreetSuggestionsAsync();', '        _ = LoadPessoasStreetSuggestionsInBackgroundAsync();')

if (-not $text.Contains('private async Task LoadPessoasStreetSuggestionsInBackgroundAsync()')) {
    $marker = '    private async void ReloadPessoasButton_Click(object sender, RoutedEventArgs e) => await LoadPessoasAsync();'
    $method = @'

    private async Task LoadPessoasStreetSuggestionsInBackgroundAsync()
    {
        try
        {
            _streetSuggestions = await _rentalManagementService.GetStreetSuggestionsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Street suggestions failed: {ex.Message}");
        }
    }
'@
    if (-not $text.Contains($marker)) { throw 'Could not find ReloadPessoasButton_Click marker.' }
    $text = $text.Replace($marker, $method + "`r`n" + $marker)
}

if ($text -ne $original) {
    Set-Content -Path $path -Value $text -Encoding UTF8 -NoNewline
    Write-Host "Updated: $path"
} else {
    Write-Host "No changes needed: $path"
}

if (-not $SkipBuild) { dotnet build Monthoya.sln }
if (-not $SkipTests) { dotnet test Monthoya.Tests }
