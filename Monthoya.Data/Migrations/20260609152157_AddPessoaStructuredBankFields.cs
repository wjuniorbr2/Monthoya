using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPessoaStructuredBankFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResponsavelAgenciaDigito",
                table: "pessoa_juridica",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelAgenciaNumero",
                table: "pessoa_juridica",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelBancoCodigo",
                table: "pessoa_juridica",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelBancoNome",
                table: "pessoa_juridica",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelContaDigito",
                table: "pessoa_juridica",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelContaNumero",
                table: "pessoa_juridica",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponsavelContaTipo",
                table: "pessoa_juridica",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelPixChave",
                table: "pessoa_juridica",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponsavelPixTipo",
                table: "pessoa_juridica",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponsavelRepassePreferencial",
                table: "pessoa_juridica",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelTitularDocumento",
                table: "pessoa_juridica",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelTitularNome",
                table: "pessoa_juridica",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgenciaDigito",
                table: "pessoa_fisica",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AgenciaNumero",
                table: "pessoa_fisica",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BancoCodigo",
                table: "pessoa_fisica",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BancoNome",
                table: "pessoa_fisica",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContaDigito",
                table: "pessoa_fisica",
                type: "character varying(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContaNumero",
                table: "pessoa_fisica",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContaTipo",
                table: "pessoa_fisica",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PixChave",
                table: "pessoa_fisica",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PixTipo",
                table: "pessoa_fisica",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RepassePreferencial",
                table: "pessoa_fisica",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitularDocumento",
                table: "pessoa_fisica",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitularNome",
                table: "pessoa_fisica",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponsavelAgenciaDigito",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "ResponsavelAgenciaNumero",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "ResponsavelBancoCodigo",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "ResponsavelBancoNome",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "ResponsavelContaDigito",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "ResponsavelContaNumero",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "ResponsavelContaTipo",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "ResponsavelPixChave",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "ResponsavelPixTipo",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "ResponsavelRepassePreferencial",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "ResponsavelTitularDocumento",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "ResponsavelTitularNome",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "AgenciaDigito",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "AgenciaNumero",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "BancoCodigo",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "BancoNome",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ContaDigito",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ContaNumero",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ContaTipo",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "PixChave",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "PixTipo",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "RepassePreferencial",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "TitularDocumento",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "TitularNome",
                table: "pessoa_fisica");
        }
    }
}
