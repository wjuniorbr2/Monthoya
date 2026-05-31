using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPessoaFisicaOtherInformationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConjugeOutrasInformacoes",
                table: "pessoa_fisica",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeTrabalhoOutrasInformacoes",
                table: "pessoa_fisica",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OutrasInformacoes",
                table: "pessoa_fisica",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrabalhoOutrasInformacoes",
                table: "pessoa_fisica",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE pessoa_fisica
                SET "ConjugeOutrasInformacoes" = "ConjugeObservacoes"
                WHERE "ConjugeOutrasInformacoes" IS NULL
                  AND "ConjugeObservacoes" IS NOT NULL
                  AND btrim("ConjugeObservacoes") <> ''
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConjugeOutrasInformacoes",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeTrabalhoOutrasInformacoes",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "OutrasInformacoes",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "TrabalhoOutrasInformacoes",
                table: "pessoa_fisica");
        }
    }
}
