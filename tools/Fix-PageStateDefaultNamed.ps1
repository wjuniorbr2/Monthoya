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

$recordRegex = [regex]'private\s+sealed\s+record\s+ImoveisPageState\s*\((?<params>.*?)\)\s*:\s*IShellPageState\s*\{(?<body>.*?)\n\s*\}'
$recordMatch = $recordRegex.Match($text)
if (-not $recordMatch.Success) {
    throw 'Could not find ImoveisPageState record.'
}

$paramsText = $recordMatch.Groups['params'].Value
$paramLines = $paramsText -split "`r?`n"
$args = New-Object System.Collections.Generic.List[string]

foreach ($line in $paramLines) {
    $clean = $line.Trim().TrimEnd(',')
    if ([string]::IsNullOrWhiteSpace($clean)) { continue }

    $m = [regex]::Match($clean, '^(?<type>[\w\?]+)\s+(?<name>\w+)$')
    if (-not $m.Success) { continue }

    $type = $m.Groups['type'].Value
    $name = $m.Groups['name'].Value
    $value = '""'

    switch ($type) {
        'Guid?' { $value = 'null'; break }
        'bool?' { $value = 'false'; break }
        'bool' { $value = 'false'; break }
        'ImovelFinalidade' { $value = 'ImovelFinalidade.Locacao'; break }
        'ImovelStatus' { $value = 'ImovelStatus.Disponivel'; break }
        'ImovelEnderecoPublicoModo' { $value = 'ImovelEnderecoPublicoModo.BairroCidade'; break }
        'ImovelChavePosse' { $value = 'ImovelChavePosse.NaoCadastrada'; break }
        default {
            if ($name -eq 'Cidade') { $value = '"Paranavaí"' }
            elseif ($name -eq 'Estado') { $value = '"PR"' }
            else { $value = '""' }
        }
    }

    $args.Add("            ${name}: $value")
}

if ($args.Count -eq 0) {
    throw 'Could not parse ImoveisPageState constructor parameters.'
}

$newDefault = "public static ImoveisPageState Default { get; } = new(`r`n" + (($args.ToArray()) -join ",`r`n") + "`r`n        );"

$defaultRegex = [regex]'public\s+static\s+ImoveisPageState\s+Default\s*\{\s*get;\s*\}\s*=\s*new\s*\(.*?\n\s*\);'
$text = $defaultRegex.Replace($text, $newDefault, 1)

if ($text -eq $original) {
    Write-Host "No page-state default changes were needed in $path"
} else {
    Set-Content -Path $path -Value $text -Encoding UTF8 -NoNewline
    Write-Host "Updated: $path"
}

Write-Host ''
Write-Host 'Named page-state default repair finished. Building solution now...'

if (-not $SkipBuild) {
    dotnet build Monthoya.sln
}
