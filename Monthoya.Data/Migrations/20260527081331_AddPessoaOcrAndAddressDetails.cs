using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPessoaOcrAndAddressDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "EmpresaBairro" character varying(120);
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "EmpresaCep" character varying(20);
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "EmpresaCidade" character varying(120);
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "EmpresaComplemento" character varying(120);
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "EmpresaEstado" character varying(2);
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "EmpresaNumero" character varying(40);
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "EmpresaRua" character varying(220);
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "ResponsavelBairro" character varying(120);
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "ResponsavelCep" character varying(20);
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "ResponsavelCidade" character varying(120);
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "ResponsavelComplemento" character varying(120);
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "ResponsavelEstado" character varying(2);
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "ResponsavelNumero" character varying(40);
                ALTER TABLE pessoa_juridica ADD COLUMN IF NOT EXISTS "ResponsavelRua" character varying(220);

                ALTER TABLE pessoa_fisica ADD COLUMN IF NOT EXISTS "Bairro" character varying(120);
                ALTER TABLE pessoa_fisica ADD COLUMN IF NOT EXISTS "Cep" character varying(20);
                ALTER TABLE pessoa_fisica ADD COLUMN IF NOT EXISTS "Cidade" character varying(120);
                ALTER TABLE pessoa_fisica ADD COLUMN IF NOT EXISTS "Complemento" character varying(120);
                ALTER TABLE pessoa_fisica ADD COLUMN IF NOT EXISTS "Estado" character varying(2);
                ALTER TABLE pessoa_fisica ADD COLUMN IF NOT EXISTS "Numero" character varying(40);
                ALTER TABLE pessoa_fisica ADD COLUMN IF NOT EXISTS "Rua" character varying(220);

                ALTER TABLE pessoa_documentos ADD COLUMN IF NOT EXISTS "OcrCamposAplicados" character varying(1000);
                ALTER TABLE pessoa_documentos ADD COLUMN IF NOT EXISTS "OcrErroMensagem" character varying(2000);
                ALTER TABLE pessoa_documentos ADD COLUMN IF NOT EXISTS "OcrProcessadoEmUtc" timestamp with time zone;
                ALTER TABLE pessoa_documentos ADD COLUMN IF NOT EXISTS "OcrStatus" integer NOT NULL DEFAULT 0;
                ALTER TABLE pessoa_documentos ADD COLUMN IF NOT EXISTS "OcrTextoExtraido" text;

                CREATE TABLE IF NOT EXISTS imovel_imagens (
                    "Id" uuid NOT NULL,
                    "ImovelId" uuid NOT NULL,
                    "FileName" character varying(260) NOT NULL,
                    "StoragePath" character varying(1000) NOT NULL,
                    "ContentType" character varying(100),
                    "DisplayOrder" integer NOT NULL,
                    "Status" integer NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone,
                    CONSTRAINT "PK_imovel_imagens" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_imovel_imagens_imoveis_ImovelId" FOREIGN KEY ("ImovelId") REFERENCES imoveis ("Id") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS "IX_imovel_imagens_ImovelId_DisplayOrder" ON imovel_imagens ("ImovelId", "DisplayOrder");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TABLE IF EXISTS imovel_imagens;

                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "EmpresaBairro";
                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "EmpresaCep";
                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "EmpresaCidade";
                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "EmpresaComplemento";
                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "EmpresaEstado";
                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "EmpresaNumero";
                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "EmpresaRua";
                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "ResponsavelBairro";
                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "ResponsavelCep";
                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "ResponsavelCidade";
                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "ResponsavelComplemento";
                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "ResponsavelEstado";
                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "ResponsavelNumero";
                ALTER TABLE pessoa_juridica DROP COLUMN IF EXISTS "ResponsavelRua";

                ALTER TABLE pessoa_fisica DROP COLUMN IF EXISTS "Bairro";
                ALTER TABLE pessoa_fisica DROP COLUMN IF EXISTS "Cep";
                ALTER TABLE pessoa_fisica DROP COLUMN IF EXISTS "Cidade";
                ALTER TABLE pessoa_fisica DROP COLUMN IF EXISTS "Complemento";
                ALTER TABLE pessoa_fisica DROP COLUMN IF EXISTS "Estado";
                ALTER TABLE pessoa_fisica DROP COLUMN IF EXISTS "Numero";
                ALTER TABLE pessoa_fisica DROP COLUMN IF EXISTS "Rua";

                ALTER TABLE pessoa_documentos DROP COLUMN IF EXISTS "OcrCamposAplicados";
                ALTER TABLE pessoa_documentos DROP COLUMN IF EXISTS "OcrErroMensagem";
                ALTER TABLE pessoa_documentos DROP COLUMN IF EXISTS "OcrProcessadoEmUtc";
                ALTER TABLE pessoa_documentos DROP COLUMN IF EXISTS "OcrStatus";
                ALTER TABLE pessoa_documentos DROP COLUMN IF EXISTS "OcrTextoExtraido";
                """);
        }
    }
}
