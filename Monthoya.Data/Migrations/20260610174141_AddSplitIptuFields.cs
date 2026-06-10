using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSplitIptuFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IptuMatricula",
                table: "imoveis",
                newName: "IptuInscricaoImobiliaria");

            migrationBuilder.AddColumn<string>(
                name: "IptuCadastroImovel",
                table: "imoveis",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IptuCadastroImovel",
                table: "imoveis");

            migrationBuilder.RenameColumn(
                name: "IptuInscricaoImobiliaria",
                table: "imoveis",
                newName: "IptuMatricula");
        }
    }
}
