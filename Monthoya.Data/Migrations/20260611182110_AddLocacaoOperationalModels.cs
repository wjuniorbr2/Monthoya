using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocacaoOperationalModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CalculoProporcionalPrimeiroMes",
                table: "locacoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CobrarTaxaContratoInicio",
                table: "locacoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CobrarTaxaContratoReajuste",
                table: "locacoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CobrarTaxaContratoRenovacao",
                table: "locacoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Codigo",
                table: "locacoes",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CorrecaoMonetariaAtraso",
                table: "locacoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataAssinaturaContrato",
                table: "locacoes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataBaseReajuste",
                table: "locacoes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataCadastro",
                table: "locacoes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataDesocupacao",
                table: "locacoes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataEncerramento",
                table: "locacoes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataEntregaChaves",
                table: "locacoes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataFimPrevista",
                table: "locacoes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataInicioCobranca",
                table: "locacoes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DataInicioLocacao",
                table: "locacoes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DescontoValidoAteVencimento",
                table: "locacoes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "DestinoRepasse",
                table: "locacoes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DiaRepasseProprietario",
                table: "locacoes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiaVencimentoLocatario",
                table: "locacoes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiasTolerancia",
                table: "locacoes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IndiceCorrecaoAtraso",
                table: "locacoes",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "JurosMoraPercentualMes",
                table: "locacoes",
                type: "numeric(8,4)",
                precision: 8,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MetaComissaoPrimeiroAluguelPercentual",
                table: "locacoes",
                type: "numeric(8,4)",
                precision: 8,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MetodoCalculoProporcional",
                table: "locacoes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ModoCobrancaTaxaContrato",
                table: "locacoes",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "ModoReajuste",
                table: "locacoes",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<string>(
                name: "MotivoEncerramento",
                table: "locacoes",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MultaAtrasoTipo",
                table: "locacoes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MultaAtrasoValor",
                table: "locacoes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservacoesInternas",
                table: "locacoes",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PeriodicidadeReajusteMeses",
                table: "locacoes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PrazoMeses",
                table: "locacoes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ProximaDataReajuste",
                table: "locacoes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReajusteRequerAprovacao",
                table: "locacoes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsavelNome",
                table: "locacoes",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResponsavelUsuarioId",
                table: "locacoes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TaxaContratoManualOverride",
                table: "locacoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxaContratoPercentual",
                table: "locacoes",
                type: "numeric(8,4)",
                precision: 8,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TemDescontoPontualidade",
                table: "locacoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TemReajuste",
                table: "locacoes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TipoDescontoPontualidade",
                table: "locacoes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoLocacao",
                table: "locacoes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorAluguelAtual",
                table: "locacoes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorAluguelInicial",
                table: "locacoes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorDescontoPontualidade",
                table: "locacoes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "locacao_cobrancas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoCobranca = table.Column<int>(type: "integer", nullable: false),
                    Competencia = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodoInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodoFim = table.Column<DateOnly>(type: "date", nullable: false),
                    DataVencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ValorAluguel = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorDescontos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorEncargos = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorMulta = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorJuros = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EnviadoEmUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PagoEmUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CanceladoEmUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locacao_cobrancas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_locacao_cobrancas_locacoes_LocacaoId",
                        column: x => x.LocacaoId,
                        principalTable: "locacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "locacao_encargos_recorrentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoEncargo = table.Column<int>(type: "integer", nullable: false),
                    ControladoPelaImobiliaria = table.Column<bool>(type: "boolean", nullable: false),
                    CobradoComAluguel = table.Column<bool>(type: "boolean", nullable: false),
                    PagoDiretoPeloLocatario = table.Column<bool>(type: "boolean", nullable: false),
                    PagoPeloProprietario = table.Column<bool>(type: "boolean", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Fixo = table.Column<bool>(type: "boolean", nullable: false),
                    NumeroParcelas = table.Column<int>(type: "integer", nullable: true),
                    DiaVencimento = table.Column<int>(type: "integer", nullable: true),
                    RequerAtualizacao = table.Column<bool>(type: "boolean", nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locacao_encargos_recorrentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_locacao_encargos_recorrentes_locacoes_LocacaoId",
                        column: x => x.LocacaoId,
                        principalTable: "locacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "locacao_garantias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoGarantia = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DataValidade = table.Column<DateOnly>(type: "date", nullable: true),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ObservacoesDocumento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locacao_garantias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_locacao_garantias_locacoes_LocacaoId",
                        column: x => x.LocacaoId,
                        principalTable: "locacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "locacao_historicos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataHoraUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Usuario = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    Acao = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Campo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ValorAnterior = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ValorNovo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Motivo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locacao_historicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_locacao_historicos_locacoes_LocacaoId",
                        column: x => x.LocacaoId,
                        principalTable: "locacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "locacao_lancamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoLancamento = table.Column<int>(type: "integer", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Competencia = table.Column<DateOnly>(type: "date", nullable: true),
                    DataVencimento = table.Column<DateOnly>(type: "date", nullable: true),
                    AfetaCobrancaLocatario = table.Column<bool>(type: "boolean", nullable: false),
                    AfetaRepasseProprietario = table.Column<bool>(type: "boolean", nullable: false),
                    RequerAprovacao = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AprovadoEmUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CanceladoEmUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locacao_lancamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_locacao_lancamentos_locacoes_LocacaoId",
                        column: x => x.LocacaoId,
                        principalTable: "locacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "locacao_notificacao_regras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoNotificacao = table.Column<int>(type: "integer", nullable: false),
                    Modo = table.Column<int>(type: "integer", nullable: false),
                    DestinatarioTipo = table.Column<int>(type: "integer", nullable: false),
                    DestinatarioUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinatarioRole = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    DiasAntes = table.Column<int>(type: "integer", nullable: true),
                    DiasDepois = table.Column<int>(type: "integer", nullable: true),
                    RepetirAteResolver = table.Column<bool>(type: "boolean", nullable: false),
                    Canal = table.Column<int>(type: "integer", nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false),
                    Observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locacao_notificacao_regras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_locacao_notificacao_regras_locacoes_LocacaoId",
                        column: x => x.LocacaoId,
                        principalTable: "locacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_locacao_notificacao_regras_users_DestinatarioUsuarioId",
                        column: x => x.DestinatarioUsuarioId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "locacao_partes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoParte = table.Column<int>(type: "integer", nullable: false),
                    IsPrincipal = table.Column<bool>(type: "boolean", nullable: false),
                    PercentualParticipacao = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    RecebeCobranca = table.Column<bool>(type: "boolean", nullable: false),
                    RecebeRepasse = table.Column<bool>(type: "boolean", nullable: false),
                    RecebeNotificacao = table.Column<bool>(type: "boolean", nullable: false),
                    PercentualRepasse = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locacao_partes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_locacao_partes_locacoes_LocacaoId",
                        column: x => x.LocacaoId,
                        principalTable: "locacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_locacao_partes_pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "locacao_valores_historicos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataVigencia = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorAnterior = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ValorNovo = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Motivo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IndiceReajuste = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    PercentualAplicado = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    Usuario = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: true),
                    CriadoEmUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locacao_valores_historicos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_locacao_valores_historicos_locacoes_LocacaoId",
                        column: x => x.LocacaoId,
                        principalTable: "locacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "locacao_cobranca_itens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoCobrancaId = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoItem = table.Column<int>(type: "integer", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ReferenciaId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locacao_cobranca_itens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_locacao_cobranca_itens_locacao_cobrancas_LocacaoCobrancaId",
                        column: x => x.LocacaoCobrancaId,
                        principalTable: "locacao_cobrancas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_locacoes_Codigo",
                table: "locacoes",
                column: "Codigo");

            migrationBuilder.CreateIndex(
                name: "IX_locacoes_ImovelId_Status",
                table: "locacoes",
                columns: new[] { "ImovelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_locacoes_ResponsavelUsuarioId",
                table: "locacoes",
                column: "ResponsavelUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_locacao_cobranca_itens_LocacaoCobrancaId",
                table: "locacao_cobranca_itens",
                column: "LocacaoCobrancaId");

            migrationBuilder.CreateIndex(
                name: "IX_locacao_cobrancas_LocacaoId_Competencia",
                table: "locacao_cobrancas",
                columns: new[] { "LocacaoId", "Competencia" });

            migrationBuilder.CreateIndex(
                name: "IX_locacao_cobrancas_Status_DataVencimento",
                table: "locacao_cobrancas",
                columns: new[] { "Status", "DataVencimento" });

            migrationBuilder.CreateIndex(
                name: "IX_locacao_encargos_recorrentes_LocacaoId_TipoEncargo_Ativo",
                table: "locacao_encargos_recorrentes",
                columns: new[] { "LocacaoId", "TipoEncargo", "Ativo" });

            migrationBuilder.CreateIndex(
                name: "IX_locacao_garantias_LocacaoId_Ativa",
                table: "locacao_garantias",
                columns: new[] { "LocacaoId", "Ativa" },
                unique: true,
                filter: "\"Ativa\" = TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_locacao_historicos_LocacaoId_DataHoraUtc",
                table: "locacao_historicos",
                columns: new[] { "LocacaoId", "DataHoraUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_locacao_lancamentos_DataVencimento",
                table: "locacao_lancamentos",
                column: "DataVencimento");

            migrationBuilder.CreateIndex(
                name: "IX_locacao_lancamentos_LocacaoId_Status",
                table: "locacao_lancamentos",
                columns: new[] { "LocacaoId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_locacao_notificacao_regras_DestinatarioUsuarioId",
                table: "locacao_notificacao_regras",
                column: "DestinatarioUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_locacao_notificacao_regras_LocacaoId_TipoNotificacao_Ativa",
                table: "locacao_notificacao_regras",
                columns: new[] { "LocacaoId", "TipoNotificacao", "Ativa" });

            migrationBuilder.CreateIndex(
                name: "IX_locacao_partes_LocacaoId_PessoaId_TipoParte",
                table: "locacao_partes",
                columns: new[] { "LocacaoId", "PessoaId", "TipoParte" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_locacao_partes_LocacaoId_TipoParte_IsPrincipal",
                table: "locacao_partes",
                columns: new[] { "LocacaoId", "TipoParte", "IsPrincipal" });

            migrationBuilder.CreateIndex(
                name: "IX_locacao_partes_PessoaId",
                table: "locacao_partes",
                column: "PessoaId");

            migrationBuilder.CreateIndex(
                name: "IX_locacao_valores_historicos_LocacaoId_DataVigencia",
                table: "locacao_valores_historicos",
                columns: new[] { "LocacaoId", "DataVigencia" });

            migrationBuilder.AddForeignKey(
                name: "FK_locacoes_users_ResponsavelUsuarioId",
                table: "locacoes",
                column: "ResponsavelUsuarioId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_locacoes_users_ResponsavelUsuarioId",
                table: "locacoes");

            migrationBuilder.DropTable(
                name: "locacao_cobranca_itens");

            migrationBuilder.DropTable(
                name: "locacao_encargos_recorrentes");

            migrationBuilder.DropTable(
                name: "locacao_garantias");

            migrationBuilder.DropTable(
                name: "locacao_historicos");

            migrationBuilder.DropTable(
                name: "locacao_lancamentos");

            migrationBuilder.DropTable(
                name: "locacao_notificacao_regras");

            migrationBuilder.DropTable(
                name: "locacao_partes");

            migrationBuilder.DropTable(
                name: "locacao_valores_historicos");

            migrationBuilder.DropTable(
                name: "locacao_cobrancas");

            migrationBuilder.DropIndex(
                name: "IX_locacoes_Codigo",
                table: "locacoes");

            migrationBuilder.DropIndex(
                name: "IX_locacoes_ImovelId_Status",
                table: "locacoes");

            migrationBuilder.DropIndex(
                name: "IX_locacoes_ResponsavelUsuarioId",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "CalculoProporcionalPrimeiroMes",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "CobrarTaxaContratoInicio",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "CobrarTaxaContratoReajuste",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "CobrarTaxaContratoRenovacao",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "Codigo",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "CorrecaoMonetariaAtraso",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "DataAssinaturaContrato",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "DataBaseReajuste",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "DataCadastro",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "DataDesocupacao",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "DataEncerramento",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "DataEntregaChaves",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "DataFimPrevista",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "DataInicioCobranca",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "DataInicioLocacao",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "DescontoValidoAteVencimento",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "DestinoRepasse",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "DiaRepasseProprietario",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "DiaVencimentoLocatario",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "DiasTolerancia",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "IndiceCorrecaoAtraso",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "JurosMoraPercentualMes",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "MetaComissaoPrimeiroAluguelPercentual",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "MetodoCalculoProporcional",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "ModoCobrancaTaxaContrato",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "ModoReajuste",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "MotivoEncerramento",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "MultaAtrasoTipo",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "MultaAtrasoValor",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "ObservacoesInternas",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "PeriodicidadeReajusteMeses",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "PrazoMeses",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "ProximaDataReajuste",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "ReajusteRequerAprovacao",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "ResponsavelNome",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "ResponsavelUsuarioId",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "TaxaContratoManualOverride",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "TaxaContratoPercentual",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "TemDescontoPontualidade",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "TemReajuste",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "TipoDescontoPontualidade",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "TipoLocacao",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "ValorAluguelAtual",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "ValorAluguelInicial",
                table: "locacoes");

            migrationBuilder.DropColumn(
                name: "ValorDescontoPontualidade",
                table: "locacoes");
        }
    }
}
