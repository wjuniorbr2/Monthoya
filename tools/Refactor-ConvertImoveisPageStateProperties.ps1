param(
    [switch]$SkipBuild,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

$statePath = 'Monthoya.Desktop/Views/ShellWindow.ImoveisPageState.cs'
$imoveisPath = 'Monthoya.Desktop/Views/ShellWindow.Imoveis.cs'

if (-not (Test-Path $statePath)) { throw "File not found: $statePath" }
if (-not (Test-Path $imoveisPath)) { throw "File not found: $imoveisPath" }

$stateContent = @'
using Monthoya.Core.Entities;

namespace Monthoya.Desktop.Views;

public partial class ShellWindow
{
    private sealed class ImoveisPageState : IShellPageState
    {
        public string SearchText { get; init; } = string.Empty;
        public Guid? SelectedImovelId { get; init; }
        public Guid? ProprietarioId { get; init; }
        public ImovelFinalidade Finalidade { get; init; } = ImovelFinalidade.Locacao;
        public string Rua { get; init; } = string.Empty;
        public string Numero { get; init; } = string.Empty;
        public string Complemento { get; init; } = string.Empty;
        public string Bairro { get; init; } = string.Empty;
        public string Cidade { get; init; } = "Paranava\u00ED";
        public string Estado { get; init; } = "PR";
        public string Cep { get; init; } = string.Empty;
        public string TipoImovel { get; init; } = string.Empty;
        public string Sanepar { get; init; } = string.Empty;
        public string Copel { get; init; } = string.Empty;
        public string IptuInscricaoImobiliaria { get; init; } = string.Empty;
        public string IptuCadastroImovel { get; init; } = string.Empty;
        public string ColetaLixo { get; init; } = string.Empty;
        public string ValorAluguel { get; init; } = string.Empty;
        public string ValorVenda { get; init; } = string.Empty;
        public string ValorCondominio { get; init; } = string.Empty;
        public string ValorIptu { get; init; } = string.Empty;
        public string Latitude { get; init; } = string.Empty;
        public string Longitude { get; init; } = string.Empty;
        public ImovelStatus Status { get; init; } = ImovelStatus.Disponivel;
        public string Quartos { get; init; } = string.Empty;
        public string Suites { get; init; } = string.Empty;
        public string Banheiros { get; init; } = string.Empty;
        public string Vagas { get; init; } = string.Empty;
        public string AreaConstruida { get; init; } = string.Empty;
        public string AreaTerreno { get; init; } = string.Empty;
        public bool? Mobiliado { get; init; } = false;
        public bool? AceitaPets { get; init; } = false;
        public string Descricao { get; init; } = string.Empty;
        public string DescricaoPublica { get; init; } = string.Empty;
        public string Observacoes { get; init; } = string.Empty;
        public bool PublicarSite { get; init; }
        public bool PublicarApp { get; init; }
        public bool Destaque { get; init; }
        public bool MostrarEnderecoCompleto { get; init; }
        public ImovelEnderecoPublicoModo ModoEnderecoPublico { get; init; } = ImovelEnderecoPublicoModo.BairroCidade;
        public ImovelChavePosse ChavePosse { get; init; } = ImovelChavePosse.NaoCadastrada;
        public string ChaveCodigo { get; init; } = string.Empty;
        public string ChaveQuemTem { get; init; } = string.Empty;
        public string ChaveTelefone { get; init; } = string.Empty;
        public string ChaveContatoNome { get; init; } = string.Empty;
        public string ChaveContatoDocumento { get; init; } = string.Empty;
        public string ChaveLocal { get; init; } = string.Empty;
        public string ChaveHorario { get; init; } = string.Empty;
        public bool ChaveAutorizacao { get; init; }
        public string ChaveObservacoes { get; init; } = string.Empty;

        public static ImoveisPageState Default { get; } = new();
    }
}
'@
Set-Content -Path $statePath -Value $stateContent -Encoding UTF8 -NoNewline
Write-Host "Converted $statePath to property-based state."

$imoveis = Get-Content -Path $imoveisPath -Raw -Encoding UTF8
$originalImoveis = $imoveis

$newCapture = @'
    private ImoveisPageState CaptureImoveisPageState() =>
        new()
        {
            SearchText = ImoveisSearchBox.Text,
            SelectedImovelId = TryGetItemId(ImoveisGrid.SelectedItem),
            ProprietarioId = ImovelProprietarioBox.SelectedValue as Guid?,
            Finalidade = ImovelFinalidadeBox.SelectedValue is ImovelFinalidade finalidade ? finalidade : ImovelFinalidade.Locacao,
            Rua = ImovelRuaBox.Text,
            Numero = ImovelNumeroBox.Text,
            Complemento = ImovelComplementoBox.Text,
            Bairro = ImovelBairroBox.Text,
            Cidade = ImovelCidadeBox.Text,
            Estado = ImovelEstadoBox.Text,
            Cep = ImovelCepBox.Text,
            TipoImovel = ImovelTipoBox.Text,
            Sanepar = ImovelSaneparBox.Text,
            Copel = ImovelCopelBox.Text,
            IptuInscricaoImobiliaria = ImovelIptuInscricaoBox.Text,
            IptuCadastroImovel = ImovelIptuCadastroBox.Text,
            ColetaLixo = ImovelColetaLixoBox.Text,
            ValorAluguel = ImovelValorAluguelBox.Text,
            ValorVenda = ImovelValorVendaBox.Text,
            ValorCondominio = ImovelValorCondominioBox.Text,
            ValorIptu = ImovelValorIptuBox.Text,
            Latitude = ImovelLatitudeBox.Text,
            Longitude = ImovelLongitudeBox.Text,
            Status = ImovelStatusBox.SelectedValue is ImovelStatus status ? status : ImovelStatus.Disponivel,
            Quartos = ImovelQuartosBox.Text,
            Suites = ImovelSuitesBox.Text,
            Banheiros = ImovelBanheirosBox.Text,
            Vagas = ImovelVagasBox.Text,
            AreaConstruida = ImovelAreaConstruidaBox.Text,
            AreaTerreno = ImovelAreaTerrenoBox.Text,
            Mobiliado = ImovelMobiliadoBox.IsChecked,
            AceitaPets = ImovelAceitaPetsBox.IsChecked,
            Descricao = ImovelDescricaoBox.Text,
            DescricaoPublica = ImovelDescricaoPublicaBox.Text,
            Observacoes = ImovelObservacoesBox.Text,
            PublicarSite = ImovelPublicarSiteBox.IsChecked == true,
            PublicarApp = ImovelPublicarAppBox.IsChecked == true,
            Destaque = ImovelDestaqueBox.IsChecked == true,
            MostrarEnderecoCompleto = ImovelMostrarEnderecoCompletoBox.IsChecked == true,
            ModoEnderecoPublico = ImovelEnderecoPublicoModoBox.SelectedValue is ImovelEnderecoPublicoModo modo ? modo : ImovelEnderecoPublicoModo.BairroCidade,
            ChavePosse = ImovelChavePosseBox.SelectedValue is ImovelChavePosse posse ? posse : ImovelChavePosse.NaoCadastrada,
            ChaveCodigo = ImovelChaveCodigoBox.Text,
            ChaveQuemTem = ImovelChaveQuemTemBox.Text,
            ChaveTelefone = ImovelChaveTelefoneBox.Text,
            ChaveContatoNome = ImovelChaveContatoNomeBox.Text,
            ChaveContatoDocumento = ImovelChaveContatoDocumentoBox.Text,
            ChaveLocal = ImovelChaveLocalBox.Text,
            ChaveHorario = ImovelChaveHorarioBox.Text,
            ChaveAutorizacao = ImovelChaveAutorizacaoBox.IsChecked == true,
            ChaveObservacoes = ImovelChaveObservacoesBox.Text
        };
'@

$pattern = '(?s)    private ImoveisPageState CaptureImoveisPageState\(\) =>\s*        new\(.*?\);\s*(?=\r?\n    private Task RestoreImoveisPageStateAsync)'
$updatedImoveis = [regex]::Replace($imoveis, $pattern, $newCapture, 1)

if ($updatedImoveis -eq $originalImoveis) {
    throw 'Could not replace CaptureImoveisPageState. The source format may have changed.'
}

Set-Content -Path $imoveisPath -Value $updatedImoveis -Encoding UTF8 -NoNewline
Write-Host "Converted CaptureImoveisPageState to object initializer."

Write-Host ''
Write-Host 'Property-based ImoveisPageState refactor complete.'

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
