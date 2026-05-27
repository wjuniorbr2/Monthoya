using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePessoaGenericAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "EnderecoEmpresa";
                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "ResponsavelEndereco";
                ALTER TABLE pessoa_fisica DROP COLUMN IF EXISTS "Endereco";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "EnderecoEmpresa" text;
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "ResponsavelEndereco" text;
                ALTER TABLE pessoa_fisica ADD COLUMN IF NOT EXISTS "Endereco" text;
                """);
        }
    }
}
