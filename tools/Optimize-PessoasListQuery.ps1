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

function Replace-MethodBySignature {
    param(
        [Parameter(Mandatory=$true)][string]$Text,
        [Parameter(Mandatory=$true)][string]$SignatureNeedle,
        [Parameter(Mandatory=$true)][string]$Replacement
    )

    $start = $Text.IndexOf($SignatureNeedle)
    if ($start -lt 0) {
        throw "Could not find method signature: $SignatureNeedle"
    }

    $open = $Text.IndexOf('{', $start)
    if ($open -lt 0) {
        throw "Could not find method body start for: $SignatureNeedle"
    }

    $close = Find-MatchingBrace -Text $Text -OpenIndex $open
    return $Text.Substring(0, $start) + $Replacement.TrimEnd() + $Text.Substring($close + 1)
}

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

$path = 'Monthoya.Data/RentalManagement/RentalManagementService.cs'
if (-not (Test-Path $path)) {
    throw "File not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8
$original = $text

$newPessoasCore = @'
    private async Task<IReadOnlyList<PessoaSummary>> GetPessoasCoreAsync(CancellationToken cancellationToken)
    {
        var pessoas = await dbContext.Pessoas
            .AsNoTracking()
            .OrderBy(x => x.NomeDisplay)
            .Select(x => new
            {
                x.Id,
                x.NomeDisplay,
                x.TipoPessoa,
                x.Telefone,
                x.Email,
                x.Status,
                Documento = x.TipoPessoa == TipoPessoa.Fisica
                    ? (x.PessoaFisica != null ? x.PessoaFisica.Cpf : null)
                    : (x.PessoaJuridica != null ? x.PessoaJuridica.Cnpj : null)
            })
            .ToListAsync(cancellationToken);

        var proprietarioSet = (await dbContext.Imoveis
            .AsNoTracking()
            .Where(x => x.Status != ImovelStatus.Inativo)
            .Select(x => x.ProprietarioId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        var locatarioSet = (await dbContext.Locacoes
            .AsNoTracking()
            .Where(x => x.Status == LocacaoStatus.Ativa)
            .Select(x => x.LocatarioId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        var fiadorSet = (await dbContext.LocacaoFiadores
            .AsNoTracking()
            .Where(x => x.Locacao != null && x.Locacao.Status == LocacaoStatus.Ativa)
            .Select(x => x.FiadorId)
            .Distinct()
            .ToListAsync(cancellationToken)).ToHashSet();

        return pessoas.Select(x =>
        {
            var isProprietario = proprietarioSet.Contains(x.Id);
            var isLocatario = locatarioSet.Contains(x.Id);
            var isFiador = fiadorSet.Contains(x.Id);

            return new PessoaSummary(
                x.Id,
                x.NomeDisplay,
                x.TipoPessoa == TipoPessoa.Fisica ? "Física" : "Jurídica",
                GetPessoaRolesLabel(isProprietario, isLocatario, isFiador),
                FormatCpfCnpjForDisplay(x.TipoPessoa, x.Documento),
                FormatPhoneForDisplay(x.Telefone),
                x.Email,
                x.Status == RegistroStatus.Ativo ? "Ativo" : "Inativo",
                isProprietario,
                isLocatario,
                isFiador);
        }).ToList();
    }
'@

if (-not $text.Contains('Documento = x.TipoPessoa == TipoPessoa.Fisica')) {
    $text = Replace-MethodBySignature `
        -Text $text `
        -SignatureNeedle '    private async Task<IReadOnlyList<PessoaSummary>> GetPessoasCoreAsync(CancellationToken cancellationToken)' `
        -Replacement $newPessoasCore
}

if ($text -eq $original) {
    Write-Host "No Pessoas list query optimization changes were needed in $path"
} else {
    Set-Content -Path $path -Value $text -Encoding UTF8 -NoNewline
    Write-Host "Updated: $path"
}

Write-Host ''
Write-Host 'Pessoas list query optimization complete.'

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
