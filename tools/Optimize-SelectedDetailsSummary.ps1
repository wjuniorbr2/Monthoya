param([switch]$SkipBuild, [switch]$SkipTests)

$ErrorActionPreference = 'Stop'

function Find-MatchingBrace {
    param([string]$Text, [int]$OpenIndex)
    $depth = 0
    for ($i = $OpenIndex; $i -lt $Text.Length; $i++) {
        if ($Text[$i] -eq '{') { $depth++ }
        elseif ($Text[$i] -eq '}') {
            $depth--
            if ($depth -eq 0) { return $i }
        }
    }
    throw 'No matching closing brace found.'
}

function Replace-MethodBySignature {
    param([string]$Text, [string]$SignatureNeedle, [string]$Replacement)
    $start = $Text.IndexOf($SignatureNeedle)
    if ($start -lt 0) { throw "Could not find method signature: $SignatureNeedle" }
    $open = $Text.IndexOf('{', $start)
    if ($open -lt 0) { throw "Could not find method body start: $SignatureNeedle" }
    $close = Find-MatchingBrace -Text $Text -OpenIndex $open
    return $Text.Substring(0, $start) + $Replacement.TrimEnd() + $Text.Substring($close + 1)
}

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

$path = 'Monthoya.Data/RentalManagement/RentalManagementService.cs'
$text = Get-Content -Path $path -Raw -Encoding UTF8
$original = $text

$newGetPessoa = @'
    public async Task<PessoaDetails?> GetPessoaAsync(Guid pessoaId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var pessoa = await dbContext.Pessoas
            .AsNoTracking()
            .Include(x => x.PessoaFisica)
            .Include(x => x.PessoaJuridica)
            .SingleOrDefaultAsync(x => x.Id == pessoaId, cancellationToken);

        if (pessoa is null)
        {
            return null;
        }

        var isProprietario = await dbContext.Imoveis
            .AsNoTracking()
            .AnyAsync(x => x.Status != ImovelStatus.Inativo && x.ProprietarioId == pessoa.Id, cancellationToken);
        var isLocatario = await dbContext.Locacoes
            .AsNoTracking()
            .AnyAsync(x => x.Status == LocacaoStatus.Ativa && x.LocatarioId == pessoa.Id, cancellationToken);
        var isFiador = await dbContext.LocacaoFiadores
            .AsNoTracking()
            .AnyAsync(x => x.FiadorId == pessoa.Id && x.Locacao != null && x.Locacao.Status == LocacaoStatus.Ativa, cancellationToken);

        var documento = pessoa.TipoPessoa == TipoPessoa.Fisica
            ? pessoa.PessoaFisica?.Cpf
            : pessoa.PessoaJuridica?.Cnpj;

        var summary = new PessoaSummary(
            pessoa.Id,
            pessoa.NomeDisplay,
            pessoa.TipoPessoa == TipoPessoa.Fisica ? "Física" : "Jurídica",
            GetPessoaRolesLabel(isProprietario, isLocatario, isFiador),
            FormatCpfCnpjForDisplay(pessoa.TipoPessoa, documento),
            FormatPhoneForDisplay(pessoa.Telefone),
            pessoa.Email,
            pessoa.Status == RegistroStatus.Ativo ? "Ativo" : "Inativo",
            isProprietario,
            isLocatario,
            isFiador);

        return new PessoaDetails(summary, ToPessoaRequest(pessoa));
    }
'@

if ($text.Contains('var summary = (await GetPessoasCoreAsync(cancellationToken)).Single(x => x.Id == pessoa.Id);')) {
    $text = Replace-MethodBySignature -Text $text -SignatureNeedle '    public async Task<PessoaDetails?> GetPessoaAsync(Guid pessoaId, CancellationToken cancellationToken = default)' -Replacement $newGetPessoa
}

$newGetImovel = @'
    public async Task<ImovelDetails?> GetImovelAsync(Guid imovelId, CancellationToken cancellationToken = default)
    {
        await using var operation = await DbContextOperationGate.EnterAsync(cancellationToken);
        var imovel = await dbContext.Imoveis
            .AsNoTracking()
            .Include(x => x.Proprietario)
            .SingleOrDefaultAsync(x => x.Id == imovelId, cancellationToken);

        if (imovel is null)
        {
            return null;
        }

        var summary = new ImovelSummary(
            imovel.Id,
            $"{imovel.Rua}, {imovel.Numero}".Trim().Trim(','),
            imovel.Bairro,
            imovel.Proprietario?.NomeDisplay ?? "-",
            imovel.TipoImovel,
            GetImovelFinalidadeLabel(imovel.Finalidade),
            GetImovelStatusLabel(imovel.Status),
            GetImovelChavePosseLabel(imovel.ChavePosse),
            GetImovelPublicacaoLabel(imovel),
            imovel.ValorAluguel,
            imovel.ValorVenda,
            imovel.ChaveCodigo);

        return new ImovelDetails(summary, ToImovelRequest(imovel));
    }
'@

if ($text.Contains('var summary = (await GetImoveisCoreAsync(cancellationToken)).Single(x => x.Id == imovel.Id);')) {
    $text = Replace-MethodBySignature -Text $text -SignatureNeedle '    public async Task<ImovelDetails?> GetImovelAsync(Guid imovelId, CancellationToken cancellationToken = default)' -Replacement $newGetImovel
}

if ($text -ne $original) {
    Set-Content -Path $path -Value $text -Encoding UTF8 -NoNewline
    Write-Host "Updated: $path"
} else {
    Write-Host "No changes needed: $path"
}

if (-not $SkipBuild) { dotnet build Monthoya.sln }
if (-not $SkipTests) { dotnet test Monthoya.Tests }
