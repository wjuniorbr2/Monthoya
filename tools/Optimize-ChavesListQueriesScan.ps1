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

$newImoveisCore = @'
    private async Task<IReadOnlyList<ImovelSummary>> GetImoveisCoreAsync(CancellationToken cancellationToken)
    {
        var imoveis = await dbContext.Imoveis
            .AsNoTracking()
            .OrderBy(x => x.Rua)
            .ThenBy(x => x.Numero)
            .Select(x => new
            {
                x.Id,
                x.Rua,
                x.Numero,
                x.Bairro,
                Proprietario = x.Proprietario != null ? x.Proprietario.NomeDisplay : "-",
                x.TipoImovel,
                x.Finalidade,
                x.Status,
                x.ChavePosse,
                x.PublicarNoSite,
                x.PublicarNoApp,
                x.Destaque,
                x.ValorAluguel,
                x.ValorVenda,
                x.ChaveCodigo
            })
            .ToListAsync(cancellationToken);

        return imoveis.Select(x => new ImovelSummary(
            x.Id,
            $"{x.Rua}, {x.Numero}".Trim().Trim(','),
            x.Bairro,
            x.Proprietario,
            x.TipoImovel,
            GetImovelFinalidadeLabel(x.Finalidade),
            GetImovelStatusLabel(x.Status),
            GetImovelChavePosseLabel(x.ChavePosse),
            GetImovelPublicacaoLabel(x.PublicarNoSite, x.PublicarNoApp, x.Destaque),
            x.ValorAluguel,
            x.ValorVenda,
            x.ChaveCodigo)).ToList();
    }
'@

if (-not $text.Contains('GetImovelPublicacaoLabel(x.PublicarNoSite, x.PublicarNoApp, x.Destaque)')) {
    $text = Replace-MethodBySignature `
        -Text $text `
        -SignatureNeedle '    private async Task<IReadOnlyList<ImovelSummary>> GetImoveisCoreAsync(CancellationToken cancellationToken)' `
        -Replacement $newImoveisCore
}

$newMovimentos = @'
    public async Task<IReadOnlyList<ImovelChaveMovimentoSummary>> GetImovelChaveMovimentosAsync(Guid? imovelId = null, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var query = dbContext.ImovelChaveMovimentos
            .AsNoTracking()
            .AsQueryable();

        if (imovelId.HasValue)
        {
            query = query.Where(x => x.ImovelId == imovelId.Value);
        }

        var now = DateTimeOffset.UtcNow;
        var movimentos = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new
            {
                x.Id,
                x.ImovelId,
                ImovelRua = x.Imovel != null ? x.Imovel.Rua : null,
                ImovelNumero = x.Imovel != null ? x.Imovel.Numero : null,
                x.Tipo,
                x.Status,
                x.ChaveCodigo,
                x.RetiradoPorNome,
                x.RetiradoPorTelefone,
                x.RetiradoPorDocumento,
                x.RetiradoPorRelacao,
                x.Motivo,
                x.RetiradoEm,
                x.PrevisaoDevolucaoEm,
                x.DevolvidoEm,
                x.DevolvidoParaNome,
                x.Observacoes
            })
            .ToListAsync(cancellationToken);

        return movimentos.Select(x =>
        {
            var status = x.Status == ImovelChaveMovimentoStatus.Retirada
                && x.PrevisaoDevolucaoEm.HasValue
                && x.PrevisaoDevolucaoEm.Value < now
                && !x.DevolvidoEm.HasValue
                    ? "Em atraso"
                    : GetEnumLabel(x.Status);

            return new ImovelChaveMovimentoSummary(
                x.Id,
                x.ImovelId,
                string.IsNullOrWhiteSpace(x.ImovelRua) ? "-" : $"{x.ImovelRua}, {x.ImovelNumero}".Trim().Trim(','),
                GetImovelChaveMovimentoTipoLabel(x.Tipo),
                status,
                x.ChaveCodigo,
                x.RetiradoPorNome,
                FormatPhoneForDisplay(x.RetiradoPorTelefone),
                x.RetiradoPorDocumento,
                x.RetiradoPorRelacao,
                x.Motivo,
                x.RetiradoEm,
                x.PrevisaoDevolucaoEm,
                x.DevolvidoEm,
                x.DevolvidoParaNome,
                x.Observacoes);
        }).ToList();
    }
'@

if (-not $text.Contains('ImovelRua = x.Imovel != null ? x.Imovel.Rua : null')) {
    $text = Replace-MethodBySignature `
        -Text $text `
        -SignatureNeedle '    public async Task<IReadOnlyList<ImovelChaveMovimentoSummary>> GetImovelChaveMovimentosAsync(Guid? imovelId = null, CancellationToken cancellationToken = default)' `
        -Replacement $newMovimentos
}

if (-not $text.Contains('private static string GetImovelPublicacaoLabel(bool publicarNoSite, bool publicarNoApp, bool destaque)')) {
    $oldHelperStart = '    private static string GetImovelPublicacaoLabel(Imovel imovel)'
    $newHelper = @'
    private static string GetImovelPublicacaoLabel(Imovel imovel) =>
        GetImovelPublicacaoLabel(imovel.PublicarNoSite, imovel.PublicarNoApp, imovel.Destaque);

    private static string GetImovelPublicacaoLabel(bool publicarNoSite, bool publicarNoApp, bool destaque)
    {
        if (publicarNoSite && publicarNoApp)
        {
            return destaque ? "Site/App - destaque" : "Site/App";
        }

        if (publicarNoSite)
        {
            return destaque ? "Site - destaque" : "Site";
        }

        if (publicarNoApp)
        {
            return destaque ? "App - destaque" : "App";
        }

        return "Privado";
    }
'@
    $text = Replace-MethodBySignature -Text $text -SignatureNeedle $oldHelperStart -Replacement $newHelper
}

# Fix indentation left by previous IPTU patch while this file is being touched.
$text = $text.Replace("`r`nimovel.ColetaLixo = TrimOrNull(request.ColetaLixo);", "`r`n        imovel.ColetaLixo = TrimOrNull(request.ColetaLixo);")
$text = $text.Replace("`r`nimovel.ColetaLixo,", "`r`n            imovel.ColetaLixo,")
$text = $text.Replace("`r`nstring? ColetaLixo = null,", "`r`n    string? ColetaLixo = null,")

if ($text -eq $original) {
    Write-Host "No scanner query optimization changes were needed in $path"
} else {
    Set-Content -Path $path -Value $text -Encoding UTF8 -NoNewline
    Write-Host "Updated: $path"
}

Write-Host ''
Write-Host 'Scanner-based Chaves/list query optimization complete.'

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
