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
        Write-Host "Already applied: $Description"
        return $Text
    }

    if (-not $Text.Contains($Old)) {
        Write-Warning "Skipping: $Description. Expected old text was not found; this part may already be changed locally."
        return $Text
    }

    Write-Host "Applying: $Description"
    return $Text.Replace($Old, $New)
}

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

# 1) Entity: replace the single IPTU field with the two real fields.
Update-FileText 'Monthoya.Core/Entities/RentalManagementEntities.cs' {
    param($text)
    Replace-Required $text `
        '    public string? IptuMatricula { get; set; }
    public string? ColetaLixo { get; set; }' `
        '    public string? IptuInscricaoImobiliaria { get; set; }
    public string? IptuCadastroImovel { get; set; }
    public string? ColetaLixo { get; set; }' `
        'Imovel IPTU entity fields'
}

# 2) Request contract: remove old IptuMatricula and expose only the two new fields.
Update-FileText 'Monthoya.Core/Services/RentalManagementServices.cs' {
    param($text)
    Replace-Required $text `
        '    string? IptuMatricula = null,
    string? ColetaLixo = null,' `
        '    string? IptuInscricaoImobiliaria = null,
    string? IptuCadastroImovel = null,
    string? ColetaLixo = null,' `
        'CreateImovelRequest IPTU fields'
}

# 3) Data service: persist/read only the new fields.
Update-FileText 'Monthoya.Data/RentalManagement/RentalManagementService.cs' {
    param($text)
    $text = Replace-Required $text `
        '        imovel.IptuMatricula = TrimOrNull(request.IptuMatricula);
        imovel.ColetaLixo = TrimOrNull(request.ColetaLixo);' `
        '        imovel.IptuInscricaoImobiliaria = TrimOrNull(request.IptuInscricaoImobiliaria);
        imovel.IptuCadastroImovel = TrimOrNull(request.IptuCadastroImovel);
        imovel.ColetaLixo = TrimOrNull(request.ColetaLixo);' `
        'ApplyImovelRequest IPTU persistence'

    $text = Replace-Required $text `
        '            imovel.CopelMatricula,
            imovel.IptuMatricula,
            imovel.ColetaLixo,' `
        '            imovel.CopelMatricula,
            imovel.IptuInscricaoImobiliaria,
            imovel.IptuCadastroImovel,
            imovel.ColetaLixo,' `
        'ToImovelRequest IPTU values'

    return $text
}

# 4) Desktop form code: replace the one IPTU box with two fields.
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
        '        ImovelIptuInscricaoBox.Text = dados.IptuInscricaoImobiliaria ?? string.Empty;
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

# 5) Desktop XAML: split the IPTU registry fields and move Coleta de lixo to values after IPTU.
Update-FileText 'Monthoya.Desktop/Views/ShellWindow.xaml' {
    param($text)

    $text = Replace-Required $text `
        '<WrapPanel>
                                                    <StackPanel Width="130" Margin="0,0,14,12"><TextBlock Text="Aluguel" FontWeight="SemiBold" /><TextBox x:Name="ImovelValorAluguelBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="130" Margin="0,0,14,12"><TextBlock Text="Venda" FontWeight="SemiBold" /><TextBox x:Name="ImovelValorVendaBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="130" Margin="0,0,14,12"><TextBlock Text="Condomínio" FontWeight="SemiBold" /><TextBox x:Name="ImovelValorCondominioBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="130" Margin="0,0,14,12"><TextBlock Text="IPTU" FontWeight="SemiBold" /><TextBox x:Name="ImovelValorIptuBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="90" Margin="0,0,14,12"><TextBlock Text="Quartos" FontWeight="SemiBold" /><TextBox x:Name="ImovelQuartosBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="90" Margin="0,0,14,12"><TextBlock Text="Suítes" FontWeight="SemiBold" /><TextBox x:Name="ImovelSuitesBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="90" Margin="0,0,14,12"><TextBlock Text="Banheiros" FontWeight="SemiBold" /><TextBox x:Name="ImovelBanheirosBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="90" Margin="0,0,14,12"><TextBlock Text="Garagens" FontWeight="SemiBold" /><TextBox x:Name="ImovelVagasBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="120" Margin="0,0,14,12"><TextBlock Text="Área construída" FontWeight="SemiBold" /><TextBox x:Name="ImovelAreaConstruidaBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="120" Margin="0,0,14,12"><TextBlock Text="Área terreno" FontWeight="SemiBold" /><TextBox x:Name="ImovelAreaTerrenoBox" Margin="0,6,0,0" /></StackPanel>
                                                    <CheckBox x:Name="ImovelMobiliadoBox" Content="Mobiliado" Margin="0,24,18,12" />
                                                    <CheckBox x:Name="ImovelAceitaPetsBox" Content="Aceita pets" Margin="0,24,18,12" />
                                                </WrapPanel>' `
        '<WrapPanel>
                                                    <StackPanel Width="130" Margin="0,0,14,12"><TextBlock Text="Aluguel" FontWeight="SemiBold" /><TextBox x:Name="ImovelValorAluguelBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="130" Margin="0,0,14,12"><TextBlock Text="Venda" FontWeight="SemiBold" /><TextBox x:Name="ImovelValorVendaBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="130" Margin="0,0,14,12"><TextBlock Text="Condomínio" FontWeight="SemiBold" /><TextBox x:Name="ImovelValorCondominioBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="130" Margin="0,0,14,12"><TextBlock Text="IPTU" FontWeight="SemiBold" /><TextBox x:Name="ImovelValorIptuBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="130" Margin="0,0,14,12"><TextBlock Text="Coleta de lixo" FontWeight="SemiBold" /><TextBox x:Name="ImovelColetaLixoBox" Margin="0,6,0,0" /></StackPanel>
                                                </WrapPanel>
                                                <WrapPanel>
                                                    <StackPanel Width="90" Margin="0,0,14,12"><TextBlock Text="Quartos" FontWeight="SemiBold" /><TextBox x:Name="ImovelQuartosBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="90" Margin="0,0,14,12"><TextBlock Text="Suítes" FontWeight="SemiBold" /><TextBox x:Name="ImovelSuitesBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="90" Margin="0,0,14,12"><TextBlock Text="Banheiros" FontWeight="SemiBold" /><TextBox x:Name="ImovelBanheirosBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="90" Margin="0,0,14,12"><TextBlock Text="Garagens" FontWeight="SemiBold" /><TextBox x:Name="ImovelVagasBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="120" Margin="0,0,14,12"><TextBlock Text="Área construída" FontWeight="SemiBold" /><TextBox x:Name="ImovelAreaConstruidaBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="120" Margin="0,0,14,12"><TextBlock Text="Área terreno" FontWeight="SemiBold" /><TextBox x:Name="ImovelAreaTerrenoBox" Margin="0,6,0,0" /></StackPanel>
                                                    <CheckBox x:Name="ImovelMobiliadoBox" Content="Mobiliado" Margin="0,24,18,12" />
                                                    <CheckBox x:Name="ImovelAceitaPetsBox" Content="Aceita pets" Margin="0,24,18,12" />
                                                </WrapPanel>' `
        'values and characteristics layout'

    $text = Replace-Required $text `
        '<StackPanel Width="190" Margin="0,0,14,12"><TextBlock Text="Matrícula IPTU" FontWeight="SemiBold" /><TextBox x:Name="ImovelIptuBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="220" Margin="0,0,14,12"><TextBlock Text="Coleta de lixo" FontWeight="SemiBold" /><TextBox x:Name="ImovelColetaLixoBox" Margin="0,6,0,0" /></StackPanel>' `
        '<StackPanel Width="190" Margin="0,0,14,12"><TextBlock Text="Inscrição imobiliária" FontWeight="SemiBold" /><TextBox x:Name="ImovelIptuInscricaoBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="190" Margin="0,0,14,12"><TextBlock Text="Cadastro do imóvel" FontWeight="SemiBold" /><TextBox x:Name="ImovelIptuCadastroBox" Margin="0,6,0,0" /></StackPanel>' `
        'utility registry IPTU fields'

    return $text
}

# 6) EF snapshot: update the current snapshot so future migrations do not keep the old column as a shadow property.
Update-FileText 'Monthoya.Data/Migrations/MonthoyaDbContextModelSnapshot.cs' {
    param($text)
    Replace-Required $text `
        '                    b.Property<string>("IptuMatricula")
                        .HasColumnType("TEXT");' `
        '                    b.Property<string>("IptuCadastroImovel")
                        .HasColumnType("TEXT");

                    b.Property<string>("IptuInscricaoImobiliaria")
                        .HasColumnType("TEXT");' `
        'Model snapshot IPTU properties'
}

# 7) Migration: add the two new columns and drop the old one.
$migrationPath = 'Monthoya.Data/Migrations/20260610143000_AddSplitIptuFields.cs'
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

        migrationBuilder.DropColumn(
            name: "IptuMatricula",
            table: "Imoveis");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "IptuMatricula",
            table: "Imoveis",
            type: "TEXT",
            nullable: true);

        migrationBuilder.DropColumn(
            name: "IptuCadastroImovel",
            table: "Imoveis");

        migrationBuilder.DropColumn(
            name: "IptuInscricaoImobiliaria",
            table: "Imoveis");
    }
}
'@ | Set-Content -Path $migrationPath -Encoding UTF8 -NoNewline
Write-Host "Created/updated: $migrationPath"

Write-Host ''
Write-Host 'Clean IPTU split patch applied locally. Review the diff, then run the app/database migration as usual.'

if (-not $SkipBuild) {
    dotnet build Monthoya.sln
}
