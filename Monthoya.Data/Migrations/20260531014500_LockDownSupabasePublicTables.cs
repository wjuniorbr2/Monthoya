using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Monthoya.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(MonthoyaDbContext))]
    [Migration("20260531014500_LockDownSupabasePublicTables")]
    public partial class LockDownSupabasePublicTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    table_name text;
                    table_names text[] := ARRAY[
                        'boletos',
                        'certificados_digitais',
                        'contas_pagar_receber',
                        'dimob_declaracoes',
                        'dimob_itens',
                        'documentos_gerados',
                        'documentos_modelos',
                        'imovel_imagens',
                        'imoveis',
                        'indices_reajuste',
                        'lancamentos_financeiros',
                        'locacao_fiadores',
                        'locacoes',
                        'manutencoes_imovel',
                        'notas_fiscais',
                        'pessoa_documentos',
                        'pessoa_fisica',
                        'pessoa_juridica',
                        'pessoa_roles',
                        'pessoas',
                        'rescisoes',
                        'users',
                        'vistorias'
                    ];
                BEGIN
                    FOREACH table_name IN ARRAY table_names
                    LOOP
                        IF to_regclass(format('public.%I', table_name)) IS NOT NULL THEN
                            EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', table_name);
                            EXECUTE format('REVOKE ALL PRIVILEGES ON TABLE public.%I FROM anon, authenticated', table_name);
                        END IF;
                    END LOOP;
                END $$;

                REVOKE USAGE ON SCHEMA public FROM anon, authenticated;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    table_name text;
                    table_names text[] := ARRAY[
                        'boletos',
                        'certificados_digitais',
                        'contas_pagar_receber',
                        'dimob_declaracoes',
                        'dimob_itens',
                        'documentos_gerados',
                        'documentos_modelos',
                        'imovel_imagens',
                        'imoveis',
                        'indices_reajuste',
                        'lancamentos_financeiros',
                        'locacao_fiadores',
                        'locacoes',
                        'manutencoes_imovel',
                        'notas_fiscais',
                        'pessoa_documentos',
                        'pessoa_fisica',
                        'pessoa_juridica',
                        'pessoa_roles',
                        'pessoas',
                        'rescisoes',
                        'users',
                        'vistorias'
                    ];
                BEGIN
                    FOREACH table_name IN ARRAY table_names
                    LOOP
                        IF to_regclass(format('public.%I', table_name)) IS NOT NULL THEN
                            EXECUTE format('ALTER TABLE public.%I DISABLE ROW LEVEL SECURITY', table_name);
                        END IF;
                    END LOOP;
                END $$;

                GRANT USAGE ON SCHEMA public TO anon, authenticated;
                """);
        }
    }
}
