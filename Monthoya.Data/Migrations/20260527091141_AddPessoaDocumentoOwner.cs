using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPessoaDocumentoOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE pessoa_documentos
                ADD COLUMN IF NOT EXISTS "DocumentoDe" character varying(40) NOT NULL DEFAULT 'pessoa';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE pessoa_documentos DROP COLUMN IF EXISTS "DocumentoDe";
                """);
        }
    }
}
