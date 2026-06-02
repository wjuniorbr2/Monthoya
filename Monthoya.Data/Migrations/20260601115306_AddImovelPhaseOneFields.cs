using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImovelPhaseOneFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AceitaPets",
                table: "imoveis",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AreaConstruida",
                table: "imoveis",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AreaTerreno",
                table: "imoveis",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Banheiros",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ChaveAutorizacaoNecessaria",
                table: "imoveis",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ChaveCodigo",
                table: "imoveis",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveContatoDocumento",
                table: "imoveis",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveContatoNome",
                table: "imoveis",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveLocalRetirada",
                table: "imoveis",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveMelhorHorario",
                table: "imoveis",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveObservacoes",
                table: "imoveis",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ChavePosse",
                table: "imoveis",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ChaveQuemTem",
                table: "imoveis",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChaveTelefone",
                table: "imoveis",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescricaoInterna",
                table: "imoveis",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescricaoPublica",
                table: "imoveis",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Destaque",
                table: "imoveis",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Mobiliado",
                table: "imoveis",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ModoExibicaoEnderecoPublico",
                table: "imoveis",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "MostrarEnderecoCompletoPublicamente",
                table: "imoveis",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PublicarNoApp",
                table: "imoveis",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PublicarNoSite",
                table: "imoveis",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Quartos",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Suites",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VagasGaragem",
                table: "imoveis",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorCondominio",
                table: "imoveis",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorIptu",
                table: "imoveis",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE imoveis
                SET "DescricaoInterna" = "Descricao"
                WHERE "DescricaoInterna" IS NULL
                  AND "Descricao" IS NOT NULL
                  AND btrim("Descricao") <> ''
                """);

            migrationBuilder.CreateTable(
                name: "imovel_chave_movimentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImovelId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChaveCodigo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    RetiradoPorNome = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    RetiradoPorTelefone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RetiradoPorDocumento = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RetiradoPorRelacao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Motivo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RetiradoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PrevisaoDevolucaoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DevolvidoEm = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DevolvidoParaNome = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imovel_chave_movimentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_imovel_chave_movimentos_imoveis_ImovelId",
                        column: x => x.ImovelId,
                        principalTable: "imoveis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_imovel_chave_movimentos_ImovelId_Status",
                table: "imovel_chave_movimentos",
                columns: new[] { "ImovelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_imovel_chave_movimentos_PrevisaoDevolucaoEm",
                table: "imovel_chave_movimentos",
                column: "PrevisaoDevolucaoEm");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "imovel_chave_movimentos");

            migrationBuilder.DropColumn(
                name: "AceitaPets",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "AreaConstruida",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "AreaTerreno",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Banheiros",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "ChaveAutorizacaoNecessaria",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "ChaveCodigo",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "ChaveContatoDocumento",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "ChaveContatoNome",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "ChaveLocalRetirada",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "ChaveMelhorHorario",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "ChaveObservacoes",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "ChavePosse",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "ChaveQuemTem",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "ChaveTelefone",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "DescricaoInterna",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "DescricaoPublica",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Destaque",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Mobiliado",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "ModoExibicaoEnderecoPublico",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "MostrarEnderecoCompletoPublicamente",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "PublicarNoApp",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "PublicarNoSite",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Quartos",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "Suites",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "VagasGaragem",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "ValorCondominio",
                table: "imoveis");

            migrationBuilder.DropColumn(
                name: "ValorIptu",
                table: "imoveis");
        }
    }
}
