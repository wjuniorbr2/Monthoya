param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

function Find-MatchingParen {
    param(
        [Parameter(Mandatory=$true)][string]$Text,
        [Parameter(Mandatory=$true)][int]$OpenIndex
    )

    $depth = 0
    for ($i = $OpenIndex; $i -lt $Text.Length; $i++) {
        $c = $Text[$i]
        if ($c -eq '(') { $depth++ }
        elseif ($c -eq ')') {
            $depth--
            if ($depth -eq 0) { return $i }
        }
    }

    throw "No matching closing parenthesis found."
}

$path = 'Monthoya.Desktop/Views/ShellWindow.Imoveis.cs'
if (-not (Test-Path $path)) {
    throw "File not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8
$original = $text

# Find record parameter list without depending on exact modifiers/formatting.
$recordNameIndex = $text.IndexOf('record ImoveisPageState')
if ($recordNameIndex -lt 0) {
    throw 'Could not find record ImoveisPageState.'
}

$recordOpen = $text.IndexOf('(', $recordNameIndex)
if ($recordOpen -lt 0) {
    throw 'Could not find ImoveisPageState parameter list start.'
}

$recordClose = Find-MatchingParen -Text $text -OpenIndex $recordOpen
$paramsText = $text.Substring($recordOpen + 1, $recordClose - $recordOpen - 1)
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

# Find and replace Default initializer by matching the new(...) parentheses.
$defaultIndex = $text.IndexOf('public static ImoveisPageState Default')
if ($defaultIndex -lt 0) {
    throw 'Could not find ImoveisPageState.Default initializer.'
}

$newIndex = $text.IndexOf('new', $defaultIndex)
$defaultOpen = $text.IndexOf('(', $newIndex)
$defaultClose = Find-MatchingParen -Text $text -OpenIndex $defaultOpen
$semicolonIndex = $text.IndexOf(';', $defaultClose)
if ($semicolonIndex -lt 0) {
    throw 'Could not find end of Default initializer.'
}

$prefix = $text.Substring(0, $defaultIndex)
$suffix = $text.Substring($semicolonIndex + 1)
$newDefault = "public static ImoveisPageState Default { get; } = new(`r`n" + (($args.ToArray()) -join ",`r`n") + "`r`n        );"
$text = $prefix + $newDefault + $suffix

if ($text -eq $original) {
    Write-Host "No page-state default changes were needed in $path"
} else {
    Set-Content -Path $path -Value $text -Encoding UTF8 -NoNewline
    Write-Host "Updated: $path"
}

Write-Host ''
Write-Host 'Scanning page-state default repair finished. Building solution now...'

if (-not $SkipBuild) {
    dotnet build Monthoya.sln
}
