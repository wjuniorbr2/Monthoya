param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'

function Read-Text([string]$Path) {
    if (-not (Test-Path $Path)) { throw "File not found: $Path" }
    return Get-Content -Path $Path -Raw -Encoding UTF8
}

function Save-IfChanged([string]$Path, [string]$Original, [string]$Updated) {
    if ($Original -eq $Updated) {
        Write-Host "No changes needed: $Path"
    } else {
        Set-Content -Path $Path -Value $Updated -Encoding UTF8 -NoNewline
        Write-Host "Updated: $Path"
    }
}

function Replace-LineContaining([string]$Text, [string]$Needle, [string]$Replacement) {
    $lines = $Text -split "`r?`n"
    $changed = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Contains($Needle)) {
            $lines[$i] = $Replacement
            $changed = $true
        }
    }
    if ($changed) { return ($lines -join "`r`n") }
    return $Text
}

function Remove-LineContaining([string]$Text, [string]$Needle) {
    $lines = $Text -split "`r?`n"
    $newLines = @()
    foreach ($line in $lines) {
        if (-not $line.Contains($Needle)) { $newLines += $line }
    }
    return ($newLines -join "`r`n")
}

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
Set-Location $root

# 1) Entity: remove old IPTU registration field and keep the two real registry fields.
$path = 'Monthoya.Core/Entities/RentalManagementEntities.cs'
$text = Read-Text $path
$original = $text
$text = $text -replace '(?m)^\s*public\s+string\?\s+IptuMatricula\s*\{\s*get;\s*set;\s*\}\s*\r?\n?', ''
if (-not $text.Contains('IptuInscricaoImobiliaria')) {
    $text = $text -replace '(?m)^(\s*public\s+string\?\s+CopelMatricula\s*\{\s*get;\s*set;\s*\}\s*)$', "`$1`r`n    public string? IptuInscricaoImobiliaria { get; set; }`r`n    public string? IptuCadastroImovel { get; set; }"
} elseif (-not $text.Contains('IptuCadastroImovel')) {
    $text = $text -replace '(?m)^(\s*public\s+string\?\s+IptuInscricaoImobiliaria\s*\{\s*get;\s*set;\s*\}\s*)$', "`$1`r`n    public string? IptuCadastroImovel { get; set; }"
}
Save-IfChanged $path $original $text

# 2) Request contract: remove old IptuMatricula and expose only the two new fields.
$path = 'Monthoya.Core/Services/RentalManagementServices.cs'
$text = Read-Text $path
$original = $text
$text = $text -replace '(?m)^\s*string\?\s+IptuMatricula\s*=\s*null,\s*\r?\n?', ''
if (-not $text.Contains('IptuInscricaoImobiliaria')) {
    $text = $text -replace '(?m)^(\s*string\?\s+CopelMatricula\s*=\s*null,\s*)$', "`$1`r`n    string? IptuInscricaoImobiliaria = null,`r`n    string? IptuCadastroImovel = null,"
} elseif (-not $text.Contains('IptuCadastroImovel')) {
    $text = $text -replace '(?m)^(\s*string\?\s+IptuInscricaoImobiliaria\s*=\s*null,\s*)$', "`$1`r`n    string? IptuCadastroImovel = null,"
}
Save-IfChanged $path $original $text

# 3) Data service: persist/read only the new fields.
$path = 'Monthoya.Data/RentalManagement/RentalManagementService.cs'
$text = Read-Text $path
$original = $text
$text = $text -replace '(?m)^\s*imovel\.IptuMatricula\s*=\s*TrimOrNull\(request\.IptuMatricula\);\s*\r?\n?', ''
if (-not $text.Contains('imovel.IptuInscricaoImobiliaria = TrimOrNull(request.IptuInscricaoImobiliaria);')) {
    $text = $text -replace '(?m)^(\s*imovel\.CopelMatricula\s*=\s*TrimOrNull\(request\.CopelMatricula\);\s*)$', "`$1`r`n        imovel.IptuInscricaoImobiliaria = TrimOrNull(request.IptuInscricaoImobiliaria);`r`n        imovel.IptuCadastroImovel = TrimOrNull(request.IptuCadastroImovel);"
}
$text = $text -replace '(?m)^\s*imovel\.IptuMatricula,\s*\r?\n?', ''
if ($text.Contains('imovel.CopelMatricula,') -and -not $text.Contains('imovel.IptuInscricaoImobiliaria,')) {
    $text = $text -replace '(?m)^(\s*imovel\.CopelMatricula,\s*)$', "`$1`r`n            imovel.IptuInscricaoImobiliaria,`r`n            imovel.IptuCadastroImovel,"
}
Save-IfChanged $path $original $text

# 4) Desktop code-behind: make sure it references the new fields consistently.
$path = 'Monthoya.Desktop/Views/ShellWindow.Imoveis.cs'
$text = Read-Text $path
$original = $text
$text = $text -replace 'IptuMatricula:\s*ImovelIptuBox\.Text,', "IptuInscricaoImobiliaria: ImovelIptuInscricaoBox.Text,`r`n            IptuCadastroImovel: ImovelIptuCadastroBox.Text,"
$text = $text -replace '(?m)^\s*ImovelIptuBox\.Text,\s*$', "            ImovelIptuInscricaoBox.Text,`r`n            ImovelIptuCadastroBox.Text,"
$text = $text -replace 'ImovelIptuBox\.Text\s*=\s*state\.Iptu;', "ImovelIptuInscricaoBox.Text = state.IptuInscricaoImobiliaria;`r`n        ImovelIptuCadastroBox.Text = state.IptuCadastroImovel;"
$text = $text -replace 'ImovelIptuBox\.Text\s*=\s*dados\.IptuMatricula\s*\?\?\s*string\.Empty;', "ImovelIptuInscricaoBox.Text = dados.IptuInscricaoImobiliaria ?? string.Empty;`r`n        ImovelIptuCadastroBox.Text = dados.IptuCadastroImovel ?? string.Empty;"
$text = $text -replace '(?m)^\s*string\s+Iptu,\s*$', "    string IptuInscricaoImobiliaria,`r`n    string IptuCadastroImovel,"
Save-IfChanged $path $original $text

# 5) XAML: split utility IPTU registration and move Coleta de lixo to values, after IPTU.
$path = 'Monthoya.Desktop/Views/ShellWindow.xaml'
$text = Read-Text $path
$original = $text
$text = Remove-LineContaining $text 'ImovelColetaLixoBox'

$iptuRegistrationReplacement = @'
                                                    <StackPanel Width="190" Margin="0,0,14,12"><TextBlock Text="Inscrição imobiliária" FontWeight="SemiBold" /><TextBox x:Name="ImovelIptuInscricaoBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="190" Margin="0,0,14,12"><TextBlock Text="Cadastro do imóvel" FontWeight="SemiBold" /><TextBox x:Name="ImovelIptuCadastroBox" Margin="0,6,0,0" /></StackPanel>
'@
$iptuRegistrationReplacement = $iptuRegistrationReplacement.Replace('\"', '"').TrimEnd()

if ($text.Contains('ImovelIptuBox')) {
    $text = Replace-LineContaining $text 'ImovelIptuBox' $iptuRegistrationReplacement
} elseif (-not $text.Contains('ImovelIptuInscricaoBox')) {
    throw 'Could not find old IPTU utility field or new IPTU fields in ShellWindow.xaml.'
}

if (-not $text.Contains('ImovelColetaLixoBox')) {
    $iptuValueLine = '                                                    <StackPanel Width="130" Margin="0,0,14,12"><TextBlock Text="IPTU" FontWeight="SemiBold" /><TextBox x:Name="ImovelValorIptuBox" Margin="0,6,0,0" /></StackPanel>'.Replace('\"', '"')
    $valueReplacement = @'
                                                    <StackPanel Width="130" Margin="0,0,14,12"><TextBlock Text="IPTU" FontWeight="SemiBold" /><TextBox x:Name="ImovelValorIptuBox" Margin="0,6,0,0" /></StackPanel>
                                                    <StackPanel Width="130" Margin="0,0,14,12"><TextBlock Text="Coleta de lixo" FontWeight="SemiBold" /><TextBox x:Name="ImovelColetaLixoBox" Margin="0,6,0,0" /></StackPanel>
                                                </WrapPanel>
                                                <WrapPanel>
'@
    $valueReplacement = $valueReplacement.Replace('\"', '"').TrimEnd()
    if ($text.Contains($iptuValueLine)) {
        $text = $text.Replace($iptuValueLine, $valueReplacement)
    } else {
        throw 'Could not find IPTU value field in ShellWindow.xaml.'
    }
}
Save-IfChanged $path $original $text

# 6) Migration: add the two new columns and drop the old one. Snapshot is intentionally left for EF tooling to refresh later.
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
'@.Replace('\"', '"') | Set-Content -Path $migrationPath -Encoding UTF8 -NoNewline
Write-Host "Updated: $migrationPath"

Write-Host ''
Write-Host 'Repair patch finished. Building solution now...'

if (-not $SkipBuild) {
    dotnet build Monthoya.sln
}
