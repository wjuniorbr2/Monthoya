param(
    [switch]$SkipBuild,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

$path = 'Monthoya.Data/RentalManagement/RentalManagementService.cs'
if (-not (Test-Path $path)) {
    throw "File not found: $path"
}

$text = Get-Content -Path $path -Raw -Encoding UTF8
$original = $text

$oldImoveisCore = @'
    private async Task<IReadOnlyList<ImovelSummary>> GetImoveisCoreAsync(CancellationToken cancellationToken)
    {
        var imoveis = await dbContext.Imoveis
            .AsNoTracking()
            .Include(x => x.Proprietario)
            .OrderBy(x => x.Rua)
            .ToListAsync(cancellationToken);

        return imoveis.Select(x => new ImovelSummary(
            x.Id,
            $"{x.Rua}, {x.Numero}".Trim().Trim(','),
            x.Bairro,
            x.Proprietario?.NomeDisplay ?? "-",
            x.TipoImovel,
            GetImovelFinalidadeLabel(x.Finalidade),
            GetImovelStatusLabel(x.Status),
            GetImovelChavePosseLabel(x.ChavePosse),
            GetImovelPublicacaoLabel(x),
            x.ValorAluguel,
            x.ValorVenda)).ToList();
    }
'@

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

if ($text.Contains($oldImoveisCore)) {
    $text = $text.Replace($oldImoveisCore, $newImoveisCore)
} elseif (-not $text.Contains('GetImovelPublicacaoLabel(x.PublicarNoSite, x.PublicarNoApp, x.Destaque)')) {
    throw 'Could not replace GetImoveisCoreAsync. Source format may have changed.'
}

$oldMovimentos = @'
    public async Task<IReadOnlyList<ImovelChaveMovimentoSummary>> GetImovelChaveMovimentosAsync(Guid? imovelId = null, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var query = dbContext.ImovelChaveMovimentos
            .AsNoTracking()
            .Include(x => x.Imovel)
            .AsQueryable();

        if (imovelId.HasValue)
        {
            query = query.Where(x => x.ImovelId == imovelId.Value);
        }

        var now = DateTimeOffset.UtcNow;
        var movimentos = await query
            .OrderByDescending(x => x.CreatedAtUtc)
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
                x.Imovel is null ? "-" : $"{x.Imovel.Rua}, {x.Imovel.Numero}".Trim().Trim(','),
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

if ($text.Contains($oldMovimentos)) {
    $text = $text.Replace($oldMovimentos, $newMovimentos)
} elseif (-not $text.Contains('ImovelRua = x.Imovel != null ? x.Imovel.Rua : null')) {
    throw 'Could not replace GetImovelChaveMovimentosAsync. Source format may have changed.'
}

# Add overload so list projection does not need a full Imovel entity.
if (-not $text.Contains('private static string GetImovelPublicacaoLabel(bool publicarNoSite, bool publicarNoApp, bool destaque)')) {
    $oldHelper = @'
    private static string GetImovelPublicacaoLabel(Imovel imovel)
    {
        if (imovel.Destaque)
        {
            return "Destaque";
        }

        if (imovel.PublicarNoSite && imovel.PublicarNoApp)
        {
            return "Site/App";
        }

        if (imovel.PublicarNoSite)
        {
            return "Site";
        }

        if (imovel.PublicarNoApp)
        {
            return "App";
        }

        return "Interno";
    }
'@

    $newHelper = @'
    private static string GetImovelPublicacaoLabel(Imovel imovel) =>
        GetImovelPublicacaoLabel(imovel.PublicarNoSite, imovel.PublicarNoApp, imovel.Destaque);

    private static string GetImovelPublicacaoLabel(bool publicarNoSite, bool publicarNoApp, bool destaque)
    {
        if (destaque)
        {
            return "Destaque";
        }

        if (publicarNoSite && publicarNoApp)
        {
            return "Site/App";
        }

        if (publicarNoSite)
        {
            return "Site";
        }

        if (publicarNoApp)
        {
            return "App";
        }

        return "Interno";
    }
'@

    if ($text.Contains($oldHelper)) {
        $text = $text.Replace($oldHelper, $newHelper)
    } else {
        throw 'Could not add GetImovelPublicacaoLabel overload. Source format may have changed.'
    }
}

if ($text -eq $original) {
    Write-Host "No query optimization changes were needed in $path"
} else {
    Set-Content -Path $path -Value $text -Encoding UTF8 -NoNewline
    Write-Host "Updated: $path"
}

Write-Host ''
Write-Host 'Chaves/list query optimization complete.'

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
