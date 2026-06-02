using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImovelMediaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Caption",
                table: "imovel_imagens",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCover",
                table: "imovel_imagens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "imovel_imagens",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MediaCategory",
                table: "imovel_imagens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "imovel_imagens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_imovel_imagens_ImovelId_IsCover",
                table: "imovel_imagens",
                columns: new[] { "ImovelId", "IsCover" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_imovel_imagens_ImovelId_IsCover",
                table: "imovel_imagens");

            migrationBuilder.DropColumn(
                name: "Caption",
                table: "imovel_imagens");

            migrationBuilder.DropColumn(
                name: "IsCover",
                table: "imovel_imagens");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "imovel_imagens");

            migrationBuilder.DropColumn(
                name: "MediaCategory",
                table: "imovel_imagens");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "imovel_imagens");
        }
    }
}
