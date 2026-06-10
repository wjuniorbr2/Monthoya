param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

function Update-FileText {
    param(
        [Parameter(Mandatory=$true)][string]$Path,
        [Parameter(Mandatory=$true)][scriptblock]$Updater
    )

    if (-not (Test-Path $Path)) {
        throw "File not found: $Path"
    }

    $original = Get-Content -Path $Path -Raw -Encoding UTF8
    $updated = & $Updater $original

    if ($updated -eq $original) {
        Write-Host "No changes needed: $Path"
        return
    }

    Set-Content -Path $Path -Value $updated -Encoding UTF8 -NoNewline
    Write-Host "Updated: $Path"
}

function Replace-Required {
    param(
        [Parameter(Mandatory=$true)][string]$Text,
        [Parameter(Mandatory=$true)][string]$Old,
        [Parameter(Mandatory=$true)][string]$New,
        [Parameter(Mandatory=$true)][string]$Description
    )

    if ($Text.Contains($New)) {
        return $Text
    }

    if (-not $Text.Contains($Old)) {
        throw "Could not find expected text for: $Description"
    }

    return $Text.Replace($Old, $New)
}

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

# 1) Domain entity: keep IptuMatricula as legacy/backward-compatible field and add the two new IPTU identifiers.
Update-FileText 'Monthoya.Core/Entities/RentalManagementEntities.cs' {
    param($text)
    Replace-Required $text `
        '    public string? IptuMatricula { get; set; }
    public string? ColetaLixo { get; set; }' `
        '    public string? IptuMatricula { get; set; }
    public string? IptuInscricaoImobiliaria { get; set; }
    public string? IptuCadastroImovel { get; set; }
    public string? ColetaLixo { get; set; }' `
        'Imovel IPTU entity fields'
}

# 2) Service contract/request: add the new request fields while preserving the old parameter for compatibility.
Update-FileText 'Monthoya.Core/Services/RentalManagementServices.cs' {
    param($text)
    Replace-Required $text `
        '    string? IptuMatricula = null,
    string? ColetaLixo = null,' `
        '    string? IptuMatricula = null,
    string? IptuInscricaoImobiliaria = null,
    string? IptuCadastroImovel = null,
    string? ColetaLixo = null,' `
        'CreateImovelRequest IPTU fields'
}

# 3) Data service: persist the new fields, using the old IPTU value as fallback for inscrição imobiliária.
Update-FileText 'Monthoya.Data/RentalManagement/RentalManagementService.cs' {
    param($text)
    $text = Replace-Required $text `
        '        imovel.IptuMatricula = TrimOrNull(request.IptuMatricula);
        imovel.ColetaLixo = TrimOrNull(request.ColetaLixo);' `
        '        imovel.IptuMatricula = TrimOrNull(request.IptuMatricula);
        imovel.IptuInscricaoImobiliaria = TrimOrNull(request.IptuInscricaoImobiliaria) ?? TrimOrNull(request.IptuMatricula);
        imovel.IptuCadastroImovel = TrimOrNull(request.IptuCadastroImovel);
        imovel.ColetaLixo = TrimOrNull(request.ColetaLixo);' `
        'ApplyImovelRequest IPTU persistence'

    $text = Replace-Required $text `
        '            imovel.CopelMatricula,
            imovel.IptuMatricula,
            imovel.ColetaLixo,' `
        '            imovel.CopelMatricula,
            imovel.IptuMatricula,
            imovel.IptuInscricaoImobiliaria,
            imovel.IptuCadastroImovel,
            imovel.ColetaLixo,' `
        'ToImovelRequest IPTU values'

    return $text
}

# 4) Desktop form code: replace one visible IPTU box with two fields.
Update-FileText 'Monthoya.Desktop/Views/ShellWindow.Imoveis.cs' {
    param($text)
    $text = Replace-Required $text `
        '            IptuMatricula: ImovelIptuBox.Text,
            ColetaLixo: ImovelColetaLixoBox.Text,' `
        '            IptuInscricaoImobiliaria: ImovelIptuInscricaoBox.Text,
            IptuCadastroImovel: ImovelIptuCadastroBox.Text,
            ColetaLixo: ImovelColetaLixoBox.Text,' `
        'BuildImovelRequestFromForm IPTU values'

    $text = Replace-Required $text `
        '            ImovelIptuBox.Text,
            ImovelColetaLixoBox.Text,' `
        '            ImovelIptuInscricaoBox.Text,
            ImovelIptuCadastroBox.Text,
            ImovelColetaLixoBox.Text,' `
        'CaptureImoveisPageState IPTU values'

    $text = Replace-Required $text `
        '        ImovelIptuBox.Text = state.Iptu;
        ImovelColetaLixoBox.Text = state.ColetaLixo;' `
        '        ImovelIptuInscricaoBox.Text = state.IptuInscricaoImobiliaria;
        ImovelIptuCadastroBox.Text = state.IptuCadastroImovel;
        ImovelColetaLixoBox.Text = state.ColetaLixo;' `
        'RestoreImoveisPageStateAsync IPTU values'

    $text = Replace-Required $text `
        '        ImovelIptuBox.Text = dados.IptuMatricula ?? string.Empty;
        ImovelColetaLixoBox.Text = dados.ColetaLixo ?? string.Empty;' `
        '        ImovelIptuInscricaoBox.Text = dados.IptuInscricaoImobiliaria ?? dados.IptuMatricula ?? string.Empty;
        ImovelIptuCadastroBox.Text = dados.IptuCadastroImovel ?? string.Empty;
        ImovelColetaLixoBox.Text = dados.ColetaLixo ?? string.Empty;' `
        'SetImovelForm IPTU values'

    $text = Replace-Required $text `
        '    string Iptu,
    string ColetaLixo,' `
        '    string IptuInscricaoImobiliaria,
    string IptuCadastroImovel,
    string ColetaLixo,' `
        'ImoveisPageState IPTU fields'

    return $text
}

# 5) Desktop XAML: show the two IPTU fields under the utility registrations area.
Update-FileText 'Monthoya.Desktop/Views/ShellWindow.xaml' {
    param($text)
    Replace-Required $text `
        '<StackPanel Width="190" Margin="0,0,14,12"><TextBlock Text="Matrícula IPTU" FontWeight="SemiBold" /><TextBox x:Name="ImovelIptuBox" Margin="0,6,0,0" /></StackPanel>' `
        '<StackPanel Width="190" Margin="0,0,14,12"><TextBlock Text="Inscrição imobiliária" FontWeight="SemiBold" /><TextBox x:Name="ImovelIptuInscricaoBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="190" Margin="0,0,14,12"><TextBlock Text="Cadastro do imóvel" FontWeight="SemiBold" /><TextBox x:Name="ImovelIptuCadastroBox" Margin="0,6,0,0" /></StackPanel>' `
        'XAML IPTU fields'
}

# 6) Add a lightweight EF migration that adds the two new columns and copies the old IPTU value to the new inscrição field.
$migrationPath = 'Monthoya.Data/Migrations/20260610143000_AddSplitIptuFields.cs'
if (-not (Test-Path $migrationPath)) {
    @'
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations;

public partial class AddSplitIptuFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IptuInscricaoImobiliaria",
            table: "Imoveis",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "IptuCadastroImovel",
            table: "Imoveis",
            type: "TEXT",
            nullable: true);

        migrationBuilder.Sql("UPDATE \"Imoveis\" SET \"IptuInscricaoImobiliaria\" = \"IptuMatricula\" WHERE \"IptuInscricaoImobiliaria\" IS NULL AND \"IptuMatricula\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "IptuCadastroImovel",
            table: "Imoveis");

        migrationBuilder.DropColumn(
            name: "IptuInscricaoImobiliaria",
            table: "Imoveis");
    }
}
'@ | Set-Content -Path $migrationPath -Encoding UTF8 -NoNewline
    Write-Host "Created: $migrationPath"
} else {
    Write-Host "Migration already exists: $migrationPath"
}

Write-Host ''
Write-Host 'Patch applied locally. Review the diff, then run the app/database migration as usual.'

if (-not $SkipBuild) {
    dotnet build Monthoya.sln
}
