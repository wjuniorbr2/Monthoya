using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRentalManagementFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "boletos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    LancamentoFinanceiroId = table.Column<Guid>(type: "uuid", nullable: true),
                    PessoaPagadoraId = table.Column<Guid>(type: "uuid", nullable: true),
                    BancoProvider = table.Column<string>(type: "text", nullable: true),
                    NossoNumero = table.Column<string>(type: "text", nullable: true),
                    LinhaDigitavel = table.Column<string>(type: "text", nullable: true),
                    CodigoBarras = table.Column<string>(type: "text", nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DataVencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    DataEmissao = table.Column<DateOnly>(type: "date", nullable: true),
                    DataPagamento = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    UrlPdf = table.Column<string>(type: "text", nullable: true),
                    ExternalId = table.Column<string>(type: "text", nullable: true),
                    PayloadRequest = table.Column<string>(type: "text", nullable: true),
                    PayloadResponse = table.Column<string>(type: "text", nullable: true),
                    ErroMensagem = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_boletos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "certificados_digitais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PessoaJuridicaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Nome = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    ValidadeInicio = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidadeFim = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificados_digitais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "contas_pagar_receber",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Escopo = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Categoria = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DataVencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    DataPagamento = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImovelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contas_pagar_receber", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dimob_declaracoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnoCalendario = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DataGeracao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ArquivoUrl = table.Column<string>(type: "text", nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dimob_declaracoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "dimob_itens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DimobDeclaracaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImovelId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProprietarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocatarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    AnoCalendario = table.Column<int>(type: "integer", nullable: false),
                    Mes = table.Column<int>(type: "integer", nullable: false),
                    ValorAluguel = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorComissao = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorImpostoRetido = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorPagoProprietario = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dimob_itens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "documentos_gerados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModeloId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImovelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    Titulo = table.Column<string>(type: "text", nullable: false),
                    PdfUrl = table.Column<string>(type: "text", nullable: true),
                    ConteudoFinal = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentos_gerados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "documentos_modelos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Nome = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    ConteudoTemplate = table.Column<string>(type: "text", nullable: false),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    StatusRevisao = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documentos_modelos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "indices_reajuste",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Percentual = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    DataReferencia = table.Column<DateOnly>(type: "date", nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_indices_reajuste", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "lancamentos_financeiros",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImovelId = table.Column<Guid>(type: "uuid", nullable: true),
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    Categoria = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DataCompetencia = table.Column<DateOnly>(type: "date", nullable: true),
                    DataVencimento = table.Column<DateOnly>(type: "date", nullable: false),
                    DataPagamento = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Origem = table.Column<string>(type: "text", nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lancamentos_financeiros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "manutencoes_imovel",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImovelId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    PessoaResponsavelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Categoria = table.Column<string>(type: "text", nullable: true),
                    Valor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DataSolicitacao = table.Column<DateOnly>(type: "date", nullable: false),
                    DataExecucao = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_manutencoes_imovel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notas_fiscais",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    LancamentoFinanceiroId = table.Column<Guid>(type: "uuid", nullable: true),
                    PessoaTomadorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Municipio = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Numero = table.Column<string>(type: "text", nullable: true),
                    CodigoVerificacao = table.Column<string>(type: "text", nullable: true),
                    ValorServico = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Aliquota = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    IssValor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DataEmissao = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    XmlUrl = table.Column<string>(type: "text", nullable: true),
                    PdfUrl = table.Column<string>(type: "text", nullable: true),
                    XmlConteudo = table.Column<string>(type: "text", nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    ExternalId = table.Column<string>(type: "text", nullable: true),
                    PayloadRequest = table.Column<string>(type: "text", nullable: true),
                    PayloadResponse = table.Column<string>(type: "text", nullable: true),
                    ErroMensagem = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas_fiscais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pessoas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TipoPessoa = table.Column<int>(type: "integer", nullable: false),
                    NomeDisplay = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Telefone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Observacoes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pessoas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rescisoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataSolicitacao = table.Column<DateOnly>(type: "date", nullable: true),
                    DataRescisao = table.Column<DateOnly>(type: "date", nullable: true),
                    Motivo = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    DebitosTotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    VistoriaSaidaId = table.Column<Guid>(type: "uuid", nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rescisoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vistorias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImovelId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Tipo = table.Column<int>(type: "integer", nullable: false),
                    DataVistoria = table.Column<DateOnly>(type: "date", nullable: false),
                    Responsavel = table.Column<string>(type: "text", nullable: true),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vistorias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "imoveis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProprietarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rua = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Numero = table.Column<string>(type: "text", nullable: true),
                    Complemento = table.Column<string>(type: "text", nullable: true),
                    Bairro = table.Column<string>(type: "text", nullable: true),
                    Cidade = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Estado = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Cep = table.Column<string>(type: "text", nullable: true),
                    SaneparMatricula = table.Column<string>(type: "text", nullable: true),
                    CopelMatricula = table.Column<string>(type: "text", nullable: true),
                    IptuMatricula = table.Column<string>(type: "text", nullable: true),
                    TipoImovel = table.Column<string>(type: "text", nullable: true),
                    Descricao = table.Column<string>(type: "text", nullable: true),
                    ValorAluguel = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ValorVenda = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Finalidade = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Longitude = table.Column<decimal>(type: "numeric(9,6)", precision: 9, scale: 6, nullable: true),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_imoveis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_imoveis_pessoas_ProprietarioId",
                        column: x => x.ProprietarioId,
                        principalTable: "pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pessoa_fisica",
                columns: table => new
                {
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nome = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Endereco = table.Column<string>(type: "text", nullable: true),
                    EstadoCivil = table.Column<string>(type: "text", nullable: true),
                    Nacionalidade = table.Column<string>(type: "text", nullable: true),
                    DataNascimento = table.Column<DateOnly>(type: "date", nullable: true),
                    Telefone = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Rg = table.Column<string>(type: "text", nullable: true),
                    Cpf = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Profissao = table.Column<string>(type: "text", nullable: true),
                    OndeTrabalha = table.Column<string>(type: "text", nullable: true),
                    EnderecoTrabalho = table.Column<string>(type: "text", nullable: true),
                    NomeEmpresaTrabalho = table.Column<string>(type: "text", nullable: true),
                    TelefoneEmpresaTrabalho = table.Column<string>(type: "text", nullable: true),
                    DadosBancarios = table.Column<string>(type: "text", nullable: true),
                    ConjugeNome = table.Column<string>(type: "text", nullable: true),
                    ConjugeRg = table.Column<string>(type: "text", nullable: true),
                    ConjugeCpf = table.Column<string>(type: "text", nullable: true),
                    ConjugeDataNascimento = table.Column<DateOnly>(type: "date", nullable: true),
                    ConjugeProfissao = table.Column<string>(type: "text", nullable: true),
                    ConjugeNacionalidade = table.Column<string>(type: "text", nullable: true),
                    ConjugeTelefone = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pessoa_fisica", x => x.PessoaId);
                    table.ForeignKey(
                        name: "FK_pessoa_fisica_pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pessoa_juridica",
                columns: table => new
                {
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeEmpresa = table.Column<string>(type: "character varying(220)", maxLength: 220, nullable: false),
                    Cnpj = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    EnderecoEmpresa = table.Column<string>(type: "text", nullable: true),
                    ResponsavelNome = table.Column<string>(type: "text", nullable: true),
                    ResponsavelEndereco = table.Column<string>(type: "text", nullable: true),
                    ResponsavelEstadoCivil = table.Column<string>(type: "text", nullable: true),
                    ResponsavelNacionalidade = table.Column<string>(type: "text", nullable: true),
                    ResponsavelDataNascimento = table.Column<DateOnly>(type: "date", nullable: true),
                    ResponsavelTelefone = table.Column<string>(type: "text", nullable: true),
                    ResponsavelEmail = table.Column<string>(type: "text", nullable: true),
                    ResponsavelRg = table.Column<string>(type: "text", nullable: true),
                    ResponsavelCpf = table.Column<string>(type: "text", nullable: true),
                    ResponsavelProfissao = table.Column<string>(type: "text", nullable: true),
                    ResponsavelOndeTrabalha = table.Column<string>(type: "text", nullable: true),
                    ResponsavelEnderecoTrabalho = table.Column<string>(type: "text", nullable: true),
                    ResponsavelNomeEmpresaTrabalho = table.Column<string>(type: "text", nullable: true),
                    ResponsavelTelefoneEmpresaTrabalho = table.Column<string>(type: "text", nullable: true),
                    ResponsavelDadosBancarios = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pessoa_juridica", x => x.PessoaId);
                    table.ForeignKey(
                        name: "FK_pessoa_juridica_pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pessoa_roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PessoaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pessoa_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pessoa_roles_pessoas_PessoaId",
                        column: x => x.PessoaId,
                        principalTable: "pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "locacoes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImovelId = table.Column<Guid>(type: "uuid", nullable: false),
                    LocatarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProprietarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    DataInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFim = table.Column<DateOnly>(type: "date", nullable: true),
                    PeriodoMeses = table.Column<int>(type: "integer", nullable: true),
                    DiaBase = table.Column<int>(type: "integer", nullable: false),
                    VencimentoLocatarioDia = table.Column<int>(type: "integer", nullable: false),
                    VencimentoProprietarioDia = table.Column<int>(type: "integer", nullable: true),
                    ValorAluguel = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    AluguelAntecipado = table.Column<bool>(type: "boolean", nullable: false),
                    MultaPercentual = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    JurosPercentual = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    DescontoAteVencimentoAtivo = table.Column<bool>(type: "boolean", nullable: false),
                    DescontoAteVencimentoValor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    DescontoAteVencimentoPercentual = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    IndiceReajusteId = table.Column<Guid>(type: "uuid", nullable: true),
                    DataProximoReajuste = table.Column<DateOnly>(type: "date", nullable: true),
                    ModeloTaxaAdministracao = table.Column<int>(type: "integer", nullable: false),
                    TaxaAdministracaoValor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    TaxaAdministracaoPercentual = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: true),
                    TaxaContratoValor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    TaxaRenovacaoValor = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Observacoes = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_locacoes_imoveis_ImovelId",
                        column: x => x.ImovelId,
                        principalTable: "imoveis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_locacoes_indices_reajuste_IndiceReajusteId",
                        column: x => x.IndiceReajusteId,
                        principalTable: "indices_reajuste",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_locacoes_pessoas_LocatarioId",
                        column: x => x.LocatarioId,
                        principalTable: "pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_locacoes_pessoas_ProprietarioId",
                        column: x => x.ProprietarioId,
                        principalTable: "pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "locacao_fiadores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocacaoId = table.Column<Guid>(type: "uuid", nullable: false),
                    FiadorId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locacao_fiadores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_locacao_fiadores_locacoes_LocacaoId",
                        column: x => x.LocacaoId,
                        principalTable: "locacoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_locacao_fiadores_pessoas_FiadorId",
                        column: x => x.FiadorId,
                        principalTable: "pessoas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "documentos_modelos",
                columns: new[] { "Id", "Ativo", "ConteudoTemplate", "CreatedAtUtc", "Nome", "StatusRevisao", "Tipo", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), true, "Modelo inicial pendente de revisão jurídica do cliente.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Contrato residencial - modelo inicial", 1, "contrato_residencial", null },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), true, "Modelo inicial pendente de revisão.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Recibo do locatário - modelo inicial", 1, "recibo_locatario", null },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), true, "Estrutura inicial. Confirmar layout oficial vigente antes de exportar TXT.", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "DIMOB - relatório de conferência", 0, "dimob", null }
                });

            migrationBuilder.InsertData(
                table: "indices_reajuste",
                columns: new[] { "Id", "Ativo", "Codigo", "CreatedAtUtc", "DataReferencia", "Nome", "Observacoes", "Percentual", "Tipo", "UpdatedAtUtc" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), true, "IGPM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "IGP-M", null, null, 0, null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), true, "IPCA", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "IPCA", null, null, 0, null },
                    { new Guid("33333333-3333-3333-3333-333333333333"), true, "INPC", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "INPC", null, null, 0, null },
                    { new Guid("44444444-4444-4444-4444-444444444444"), true, "IVAR", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "IVAR", null, null, 0, null },
                    { new Guid("55555555-5555-5555-5555-555555555555"), true, "CUSTOM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Custom/manual", null, null, 1, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_imoveis_ProprietarioId",
                table: "imoveis",
                column: "ProprietarioId");

            migrationBuilder.CreateIndex(
                name: "IX_indices_reajuste_Codigo",
                table: "indices_reajuste",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_locacao_fiadores_FiadorId",
                table: "locacao_fiadores",
                column: "FiadorId");

            migrationBuilder.CreateIndex(
                name: "IX_locacao_fiadores_LocacaoId_FiadorId",
                table: "locacao_fiadores",
                columns: new[] { "LocacaoId", "FiadorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_locacoes_ImovelId",
                table: "locacoes",
                column: "ImovelId");

            migrationBuilder.CreateIndex(
                name: "IX_locacoes_IndiceReajusteId",
                table: "locacoes",
                column: "IndiceReajusteId");

            migrationBuilder.CreateIndex(
                name: "IX_locacoes_LocatarioId",
                table: "locacoes",
                column: "LocatarioId");

            migrationBuilder.CreateIndex(
                name: "IX_locacoes_ProprietarioId",
                table: "locacoes",
                column: "ProprietarioId");

            migrationBuilder.CreateIndex(
                name: "IX_pessoa_fisica_Cpf",
                table: "pessoa_fisica",
                column: "Cpf",
                unique: true,
                filter: "\"Cpf\" IS NOT NULL AND \"Cpf\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_pessoa_juridica_Cnpj",
                table: "pessoa_juridica",
                column: "Cnpj",
                unique: true,
                filter: "\"Cnpj\" IS NOT NULL AND \"Cnpj\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_pessoa_roles_PessoaId_Role",
                table: "pessoa_roles",
                columns: new[] { "PessoaId", "Role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "boletos");

            migrationBuilder.DropTable(
                name: "certificados_digitais");

            migrationBuilder.DropTable(
                name: "contas_pagar_receber");

            migrationBuilder.DropTable(
                name: "dimob_declaracoes");

            migrationBuilder.DropTable(
                name: "dimob_itens");

            migrationBuilder.DropTable(
                name: "documentos_gerados");

            migrationBuilder.DropTable(
                name: "documentos_modelos");

            migrationBuilder.DropTable(
                name: "lancamentos_financeiros");

            migrationBuilder.DropTable(
                name: "locacao_fiadores");

            migrationBuilder.DropTable(
                name: "manutencoes_imovel");

            migrationBuilder.DropTable(
                name: "notas_fiscais");

            migrationBuilder.DropTable(
                name: "pessoa_fisica");

            migrationBuilder.DropTable(
                name: "pessoa_juridica");

            migrationBuilder.DropTable(
                name: "pessoa_roles");

            migrationBuilder.DropTable(
                name: "rescisoes");

            migrationBuilder.DropTable(
                name: "vistorias");

            migrationBuilder.DropTable(
                name: "locacoes");

            migrationBuilder.DropTable(
                name: "imoveis");

            migrationBuilder.DropTable(
                name: "indices_reajuste");

            migrationBuilder.DropTable(
                name: "pessoas");
        }
    }
}
