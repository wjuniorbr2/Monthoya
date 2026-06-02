using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRichInspectionWorkflowModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AiErrorMessage",
                table: "vistorias",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AiProcessedAt",
                table: "vistorias",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiStatus",
                table: "vistorias",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiSummary",
                table: "vistorias",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescricaoGeral",
                table: "vistorias",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfPath",
                table: "vistorias",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StableId",
                table: "vistorias",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStatus",
                table: "vistorias",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "vistoria_ambientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StableId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    VistoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TipoAmbiente = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    CondicaoGeral = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vistoria_ambientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vistoria_ambientes_vistorias_VistoriaId",
                        column: x => x.VistoriaId,
                        principalTable: "vistorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vistoria_itens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StableId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    VistoriaAmbienteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Categoria = table.Column<int>(type: "integer", nullable: false),
                    Condicao = table.Column<int>(type: "integer", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    ResponsabilidadeSugerida = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AiDetectedDamage = table.Column<bool>(type: "boolean", nullable: true),
                    AiSuggestedDescription = table.Column<string>(type: "text", nullable: true),
                    AiConfidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    AiStatus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    AiProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AiErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vistoria_itens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vistoria_itens_vistoria_ambientes_VistoriaAmbienteId",
                        column: x => x.VistoriaAmbienteId,
                        principalTable: "vistoria_ambientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vistoria_fotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StableId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    VistoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    VistoriaAmbienteId = table.Column<Guid>(type: "uuid", nullable: true),
                    VistoriaItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImovelId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    FileName = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    LocalDevicePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    StoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    Caption = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TakenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UploadStatus = table.Column<int>(type: "integer", nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    IsPublicWebsite = table.Column<bool>(type: "boolean", nullable: false),
                    VisibleToClientApp = table.Column<bool>(type: "boolean", nullable: true),
                    AiDescription = table.Column<string>(type: "text", nullable: true),
                    AiDetectedDamage = table.Column<bool>(type: "boolean", nullable: true),
                    AiSuggestedCaption = table.Column<string>(type: "text", nullable: true),
                    AiConfidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    AiStatus = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    AiProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AiErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vistoria_fotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vistoria_fotos_imoveis_ImovelId",
                        column: x => x.ImovelId,
                        principalTable: "imoveis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vistoria_fotos_vistoria_ambientes_VistoriaAmbienteId",
                        column: x => x.VistoriaAmbienteId,
                        principalTable: "vistoria_ambientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_vistoria_fotos_vistoria_itens_VistoriaItemId",
                        column: x => x.VistoriaItemId,
                        principalTable: "vistoria_itens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_vistoria_fotos_vistorias_VistoriaId",
                        column: x => x.VistoriaId,
                        principalTable: "vistorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vistorias_ImovelId_DataVistoria",
                table: "vistorias",
                columns: new[] { "ImovelId", "DataVistoria" });

            migrationBuilder.CreateIndex(
                name: "IX_vistorias_StableId",
                table: "vistorias",
                column: "StableId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vistoria_ambientes_StableId",
                table: "vistoria_ambientes",
                column: "StableId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vistoria_ambientes_VistoriaId_DisplayOrder",
                table: "vistoria_ambientes",
                columns: new[] { "VistoriaId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_vistoria_fotos_ImovelId_UploadStatus",
                table: "vistoria_fotos",
                columns: new[] { "ImovelId", "UploadStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_vistoria_fotos_StableId",
                table: "vistoria_fotos",
                column: "StableId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vistoria_fotos_VistoriaAmbienteId",
                table: "vistoria_fotos",
                column: "VistoriaAmbienteId");

            migrationBuilder.CreateIndex(
                name: "IX_vistoria_fotos_VistoriaId_DisplayOrder",
                table: "vistoria_fotos",
                columns: new[] { "VistoriaId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_vistoria_fotos_VistoriaItemId",
                table: "vistoria_fotos",
                column: "VistoriaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_vistoria_itens_StableId",
                table: "vistoria_itens",
                column: "StableId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vistoria_itens_VistoriaAmbienteId",
                table: "vistoria_itens",
                column: "VistoriaAmbienteId");

            migrationBuilder.AddForeignKey(
                name: "FK_vistorias_imoveis_ImovelId",
                table: "vistorias",
                column: "ImovelId",
                principalTable: "imoveis",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_vistorias_imoveis_ImovelId",
                table: "vistorias");

            migrationBuilder.DropTable(
                name: "vistoria_fotos");

            migrationBuilder.DropTable(
                name: "vistoria_itens");

            migrationBuilder.DropTable(
                name: "vistoria_ambientes");

            migrationBuilder.DropIndex(
                name: "IX_vistorias_ImovelId_DataVistoria",
                table: "vistorias");

            migrationBuilder.DropIndex(
                name: "IX_vistorias_StableId",
                table: "vistorias");

            migrationBuilder.DropColumn(
                name: "AiErrorMessage",
                table: "vistorias");

            migrationBuilder.DropColumn(
                name: "AiProcessedAt",
                table: "vistorias");

            migrationBuilder.DropColumn(
                name: "AiStatus",
                table: "vistorias");

            migrationBuilder.DropColumn(
                name: "AiSummary",
                table: "vistorias");

            migrationBuilder.DropColumn(
                name: "DescricaoGeral",
                table: "vistorias");

            migrationBuilder.DropColumn(
                name: "PdfPath",
                table: "vistorias");

            migrationBuilder.DropColumn(
                name: "StableId",
                table: "vistorias");

            migrationBuilder.DropColumn(
                name: "WorkflowStatus",
                table: "vistorias");
        }
    }
}
