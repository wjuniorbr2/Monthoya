using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPessoaDetailedRegistryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Atividade",
                table: "pessoa_juridica",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataAbertura",
                table: "pessoa_juridica",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InscricaoEstadual",
                table: "pessoa_juridica",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InscricaoMunicipal",
                table: "pessoa_juridica",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NomeFantasia",
                table: "pessoa_juridica",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReceitaMensal",
                table: "pessoa_juridica",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelCargo",
                table: "pessoa_juridica",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelObservacoes",
                table: "pessoa_juridica",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CargoTrabalho",
                table: "pessoa_fisica",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CnpjEmpresaTrabalho",
                table: "pessoa_fisica",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeCargoTrabalho",
                table: "pessoa_fisica",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeCnpjEmpresaTrabalho",
                table: "pessoa_fisica",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeDadosBancarios",
                table: "pessoa_fisica",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeEmail",
                table: "pessoa_fisica",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeEmailEmpresaTrabalho",
                table: "pessoa_fisica",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeEmpresaBairro",
                table: "pessoa_fisica",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeEmpresaCep",
                table: "pessoa_fisica",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeEmpresaCidade",
                table: "pessoa_fisica",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeEmpresaComplemento",
                table: "pessoa_fisica",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeEmpresaEstado",
                table: "pessoa_fisica",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeEmpresaNumero",
                table: "pessoa_fisica",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeEmpresaRua",
                table: "pessoa_fisica",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeNomeEmpresaTrabalho",
                table: "pessoa_fisica",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeObservacoes",
                table: "pessoa_fisica",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ConjugePossuiTrabalho",
                table: "pessoa_fisica",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ConjugeRendaTrabalho",
                table: "pessoa_fisica",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeTelefoneEmpresaTrabalho",
                table: "pessoa_fisica",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeTempoEmprego",
                table: "pessoa_fisica",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConjugeTipoComprovanteRenda",
                table: "pessoa_fisica",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailEmpresaTrabalho",
                table: "pessoa_fisica",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmpresaBairro",
                table: "pessoa_fisica",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmpresaCep",
                table: "pessoa_fisica",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmpresaCidade",
                table: "pessoa_fisica",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmpresaComplemento",
                table: "pessoa_fisica",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmpresaEstado",
                table: "pessoa_fisica",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmpresaNumero",
                table: "pessoa_fisica",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmpresaRua",
                table: "pessoa_fisica",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PetQual",
                table: "pessoa_fisica",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PossuiPet",
                table: "pessoa_fisica",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PossuiTrabalho",
                table: "pessoa_fisica",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RendaTrabalho",
                table: "pessoa_fisica",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TempoEmprego",
                table: "pessoa_fisica",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoComprovanteRenda",
                table: "pessoa_fisica",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Atividade",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "DataAbertura",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "InscricaoEstadual",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "InscricaoMunicipal",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "NomeFantasia",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "ReceitaMensal",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "ResponsavelCargo",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "ResponsavelObservacoes",
                table: "pessoa_juridica");

            migrationBuilder.DropColumn(
                name: "CargoTrabalho",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "CnpjEmpresaTrabalho",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeCargoTrabalho",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeCnpjEmpresaTrabalho",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeDadosBancarios",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeEmail",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeEmailEmpresaTrabalho",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeEmpresaBairro",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeEmpresaCep",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeEmpresaCidade",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeEmpresaComplemento",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeEmpresaEstado",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeEmpresaNumero",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeEmpresaRua",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeNomeEmpresaTrabalho",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeObservacoes",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugePossuiTrabalho",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeRendaTrabalho",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeTelefoneEmpresaTrabalho",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeTempoEmprego",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "ConjugeTipoComprovanteRenda",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "EmailEmpresaTrabalho",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "EmpresaBairro",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "EmpresaCep",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "EmpresaCidade",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "EmpresaComplemento",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "EmpresaEstado",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "EmpresaNumero",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "EmpresaRua",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "PetQual",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "PossuiPet",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "PossuiTrabalho",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "RendaTrabalho",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "TempoEmprego",
                table: "pessoa_fisica");

            migrationBuilder.DropColumn(
                name: "TipoComprovanteRenda",
                table: "pessoa_fisica");
        }
    }
}
