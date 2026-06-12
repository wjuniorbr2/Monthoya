using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImovelAmbienteCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AreasServico",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Churrasqueiras",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Copas",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Cozinhas",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Despensas",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Estendais",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HallsEntrada",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Lavabos",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Lavanderias",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Piscinas",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Quintais",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sacadas",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Salas",
                table: "imoveis",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreasServico",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Churrasqueiras",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Copas",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Cozinhas",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Despensas",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Estendais",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "HallsEntrada",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Lavabos",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Lavanderias",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Piscinas",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Quintais",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Sacadas",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Salas",
                table: "imoveis");
        }
    }
}
