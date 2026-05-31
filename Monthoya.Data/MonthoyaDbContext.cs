using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;

namespace Monthoya.Data;

public sealed class MonthoyaDbContext(DbContextOptions<MonthoyaDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<Pessoa> Pessoas => Set<Pessoa>();
    public DbSet<PessoaRole> PessoaRoles => Set<PessoaRole>();
    public DbSet<PessoaDocumento> PessoaDocumentos => Set<PessoaDocumento>();
    public DbSet<PessoaFisica> PessoasFisicas => Set<PessoaFisica>();
    public DbSet<PessoaJuridica> PessoasJuridicas => Set<PessoaJuridica>();
    public DbSet<Imovel> Imoveis => Set<Imovel>();
    public DbSet<ImovelImagem> ImovelImagens => Set<ImovelImagem>();
    public DbSet<Locacao> Locacoes => Set<Locacao>();
    public DbSet<LocacaoFiador> LocacaoFiadores => Set<LocacaoFiador>();
    public DbSet<IndiceReajuste> IndicesReajuste => Set<IndiceReajuste>();
    public DbSet<LancamentoFinanceiro> LancamentosFinanceiros => Set<LancamentoFinanceiro>();
    public DbSet<ContaPagarReceber> ContasPagarReceber => Set<ContaPagarReceber>();
    public DbSet<Boleto> Boletos => Set<Boleto>();
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();
    public DbSet<CertificadoDigital> CertificadosDigitais => Set<CertificadoDigital>();
    public DbSet<DocumentoModelo> DocumentosModelos => Set<DocumentoModelo>();
    public DbSet<DocumentoGerado> DocumentosGerados => Set<DocumentoGerado>();
    public DbSet<DimobDeclaracao> DimobDeclaracoes => Set<DimobDeclaracao>();
    public DbSet<DimobItem> DimobItens => Set<DimobItem>();
    public DbSet<ManutencaoImovel> ManutencoesImovel => Set<ManutencaoImovel>();
    public DbSet<Vistoria> Vistorias => Set<Vistoria>();
    public DbSet<Rescisao> Rescisoes => Set<Rescisao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("users");
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.LoginName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.NormalizedLoginName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.NormalizedEmail).HasMaxLength(320).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Access).HasConversion<int>().IsRequired();
            entity.HasIndex(x => x.NormalizedLoginName).IsUnique();
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();
        });

        ConfigureRentalManagement(modelBuilder);
    }

    private static void ConfigureRentalManagement(ModelBuilder modelBuilder)
    {
        var seedCreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        modelBuilder.Entity<Pessoa>(entity =>
        {
            entity.ToTable("pessoas");
            entity.Property(x => x.NomeDisplay).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Telefone).HasMaxLength(50);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Observacoes).HasMaxLength(4000);
        });

        modelBuilder.Entity<PessoaRole>(entity =>
        {
            entity.ToTable("pessoa_roles");
            entity.HasIndex(x => new { x.PessoaId, x.Role }).IsUnique();
            entity.HasOne(x => x.Pessoa).WithMany(x => x.Roles).HasForeignKey(x => x.PessoaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PessoaDocumento>(entity =>
        {
            entity.ToTable("pessoa_documentos");
            entity.Property(x => x.Tipo).HasMaxLength(80).IsRequired();
            entity.Property(x => x.DocumentoDe).HasMaxLength(40).HasDefaultValue("pessoa");
            entity.Property(x => x.Nome).HasMaxLength(220).IsRequired();
            entity.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(100);
            entity.Property(x => x.Observacoes).HasMaxLength(2000);
            entity.Property(x => x.OcrTextoExtraido);
            entity.Property(x => x.OcrErroMensagem).HasMaxLength(2000);
            entity.Property(x => x.OcrCamposAplicados).HasMaxLength(1000);
            entity.Ignore(x => x.SkipOcrAutofill);
            entity.HasOne(x => x.Pessoa).WithMany(x => x.Documentos).HasForeignKey(x => x.PessoaId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.PessoaId, x.Tipo });
        });

        modelBuilder.Entity<PessoaFisica>(entity =>
        {
            entity.ToTable("pessoa_fisica");
            entity.HasKey(x => x.PessoaId);
            entity.Property(x => x.Nome).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Rua).HasMaxLength(220);
            entity.Property(x => x.Numero).HasMaxLength(40);
            entity.Property(x => x.Complemento).HasMaxLength(120);
            entity.Property(x => x.Bairro).HasMaxLength(120);
            entity.Property(x => x.Cidade).HasMaxLength(120);
            entity.Property(x => x.Estado).HasMaxLength(2);
            entity.Property(x => x.Cep).HasMaxLength(20);
            entity.Property(x => x.Cpf).HasMaxLength(20);
            entity.Property(x => x.PetQual).HasMaxLength(120);
            entity.Property(x => x.CnpjEmpresaTrabalho).HasMaxLength(20);
            entity.Property(x => x.EmailEmpresaTrabalho).HasMaxLength(320);
            entity.Property(x => x.CargoTrabalho).HasMaxLength(120);
            entity.Property(x => x.RendaTrabalho).HasPrecision(18, 2);
            entity.Property(x => x.TempoEmprego).HasMaxLength(80);
            entity.Property(x => x.TipoComprovanteRenda).HasMaxLength(120);
            entity.Property(x => x.OutrasInformacoes).HasMaxLength(4000);
            entity.Property(x => x.TrabalhoOutrasInformacoes).HasMaxLength(4000);
            entity.Property(x => x.EmpresaRua).HasMaxLength(220);
            entity.Property(x => x.EmpresaNumero).HasMaxLength(40);
            entity.Property(x => x.EmpresaComplemento).HasMaxLength(120);
            entity.Property(x => x.EmpresaBairro).HasMaxLength(120);
            entity.Property(x => x.EmpresaCidade).HasMaxLength(120);
            entity.Property(x => x.EmpresaEstado).HasMaxLength(2);
            entity.Property(x => x.EmpresaCep).HasMaxLength(20);
            entity.Property(x => x.ConjugeEmail).HasMaxLength(320);
            entity.Property(x => x.ConjugeDadosBancarios).HasMaxLength(2000);
            entity.Property(x => x.ConjugeObservacoes).HasMaxLength(4000);
            entity.Property(x => x.ConjugeOutrasInformacoes).HasMaxLength(4000);
            entity.Property(x => x.ConjugeNomeEmpresaTrabalho).HasMaxLength(220);
            entity.Property(x => x.ConjugeCnpjEmpresaTrabalho).HasMaxLength(20);
            entity.Property(x => x.ConjugeTelefoneEmpresaTrabalho).HasMaxLength(50);
            entity.Property(x => x.ConjugeEmailEmpresaTrabalho).HasMaxLength(320);
            entity.Property(x => x.ConjugeCargoTrabalho).HasMaxLength(120);
            entity.Property(x => x.ConjugeRendaTrabalho).HasPrecision(18, 2);
            entity.Property(x => x.ConjugeTempoEmprego).HasMaxLength(80);
            entity.Property(x => x.ConjugeTipoComprovanteRenda).HasMaxLength(120);
            entity.Property(x => x.ConjugeTrabalhoOutrasInformacoes).HasMaxLength(4000);
            entity.Property(x => x.ConjugeEmpresaRua).HasMaxLength(220);
            entity.Property(x => x.ConjugeEmpresaNumero).HasMaxLength(40);
            entity.Property(x => x.ConjugeEmpresaComplemento).HasMaxLength(120);
            entity.Property(x => x.ConjugeEmpresaBairro).HasMaxLength(120);
            entity.Property(x => x.ConjugeEmpresaCidade).HasMaxLength(120);
            entity.Property(x => x.ConjugeEmpresaEstado).HasMaxLength(2);
            entity.Property(x => x.ConjugeEmpresaCep).HasMaxLength(20);
            entity.HasIndex(x => x.Cpf).IsUnique().HasFilter("\"Cpf\" IS NOT NULL AND \"Cpf\" <> ''");
            entity.HasOne(x => x.Pessoa).WithOne(x => x.PessoaFisica).HasForeignKey<PessoaFisica>(x => x.PessoaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PessoaJuridica>(entity =>
        {
            entity.ToTable("pessoa_juridica");
            entity.HasKey(x => x.PessoaId);
            entity.Property(x => x.NomeEmpresa).HasMaxLength(220).IsRequired();
            entity.Property(x => x.NomeFantasia).HasMaxLength(220);
            entity.Property(x => x.Atividade).HasMaxLength(220);
            entity.Property(x => x.ReceitaMensal).HasPrecision(18, 2);
            entity.Property(x => x.Cnpj).HasMaxLength(24);
            entity.Property(x => x.InscricaoEstadual).HasMaxLength(40);
            entity.Property(x => x.InscricaoMunicipal).HasMaxLength(40);
            entity.Property(x => x.EmpresaRua).HasMaxLength(220);
            entity.Property(x => x.EmpresaNumero).HasMaxLength(40);
            entity.Property(x => x.EmpresaComplemento).HasMaxLength(120);
            entity.Property(x => x.EmpresaBairro).HasMaxLength(120);
            entity.Property(x => x.EmpresaCidade).HasMaxLength(120);
            entity.Property(x => x.EmpresaEstado).HasMaxLength(2);
            entity.Property(x => x.EmpresaCep).HasMaxLength(20);
            entity.Property(x => x.ResponsavelCargo).HasMaxLength(120);
            entity.Property(x => x.ResponsavelRua).HasMaxLength(220);
            entity.Property(x => x.ResponsavelNumero).HasMaxLength(40);
            entity.Property(x => x.ResponsavelComplemento).HasMaxLength(120);
            entity.Property(x => x.ResponsavelBairro).HasMaxLength(120);
            entity.Property(x => x.ResponsavelCidade).HasMaxLength(120);
            entity.Property(x => x.ResponsavelEstado).HasMaxLength(2);
            entity.Property(x => x.ResponsavelCep).HasMaxLength(20);
            entity.Property(x => x.ResponsavelObservacoes).HasMaxLength(4000);
            entity.HasIndex(x => x.Cnpj).IsUnique().HasFilter("\"Cnpj\" IS NOT NULL AND \"Cnpj\" <> ''");
            entity.HasOne(x => x.Pessoa).WithOne(x => x.PessoaJuridica).HasForeignKey<PessoaJuridica>(x => x.PessoaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Imovel>(entity =>
        {
            entity.ToTable("imoveis");
            entity.Property(x => x.Rua).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Cidade).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Estado).HasMaxLength(2).IsRequired();
            entity.Property(x => x.ValorAluguel).HasPrecision(18, 2);
            entity.Property(x => x.ValorVenda).HasPrecision(18, 2);
            entity.Property(x => x.Latitude).HasPrecision(9, 6);
            entity.Property(x => x.Longitude).HasPrecision(9, 6);
            entity.HasOne(x => x.Proprietario).WithMany().HasForeignKey(x => x.ProprietarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ImovelImagem>(entity =>
        {
            entity.ToTable("imovel_imagens");
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(100);
            entity.HasOne(x => x.Imovel).WithMany(x => x.Imagens).HasForeignKey(x => x.ImovelId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.ImovelId, x.DisplayOrder });
        });

        modelBuilder.Entity<Locacao>(entity =>
        {
            entity.ToTable("locacoes");
            entity.Property(x => x.ValorAluguel).HasPrecision(18, 2);
            entity.Property(x => x.MultaPercentual).HasPrecision(8, 4);
            entity.Property(x => x.JurosPercentual).HasPrecision(8, 4);
            entity.Property(x => x.DescontoAteVencimentoValor).HasPrecision(18, 2);
            entity.Property(x => x.DescontoAteVencimentoPercentual).HasPrecision(8, 4);
            entity.Property(x => x.TaxaAdministracaoValor).HasPrecision(18, 2);
            entity.Property(x => x.TaxaAdministracaoPercentual).HasPrecision(8, 4);
            entity.Property(x => x.TaxaContratoValor).HasPrecision(18, 2);
            entity.Property(x => x.TaxaRenovacaoValor).HasPrecision(18, 2);
            entity.HasOne(x => x.Imovel).WithMany().HasForeignKey(x => x.ImovelId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Locatario).WithMany().HasForeignKey(x => x.LocatarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Proprietario).WithMany().HasForeignKey(x => x.ProprietarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.IndiceReajuste).WithMany().HasForeignKey(x => x.IndiceReajusteId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<LocacaoFiador>(entity =>
        {
            entity.ToTable("locacao_fiadores");
            entity.HasIndex(x => new { x.LocacaoId, x.FiadorId }).IsUnique();
            entity.HasOne(x => x.Locacao).WithMany(x => x.Fiadores).HasForeignKey(x => x.LocacaoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Fiador).WithMany().HasForeignKey(x => x.FiadorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<IndiceReajuste>(entity =>
        {
            entity.ToTable("indices_reajuste");
            entity.Property(x => x.Nome).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Codigo).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Percentual).HasPrecision(8, 4);
            entity.HasIndex(x => x.Codigo).IsUnique();
            entity.HasData(
                new IndiceReajuste { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Nome = "IGP-M", Codigo = "IGPM", Tipo = ReajusteTipo.Oficial, Ativo = true, CreatedAtUtc = seedCreatedAt },
                new IndiceReajuste { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Nome = "IPCA", Codigo = "IPCA", Tipo = ReajusteTipo.Oficial, Ativo = true, CreatedAtUtc = seedCreatedAt },
                new IndiceReajuste { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Nome = "INPC", Codigo = "INPC", Tipo = ReajusteTipo.Oficial, Ativo = true, CreatedAtUtc = seedCreatedAt },
                new IndiceReajuste { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Nome = "IVAR", Codigo = "IVAR", Tipo = ReajusteTipo.Oficial, Ativo = true, CreatedAtUtc = seedCreatedAt },
                new IndiceReajuste { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Nome = "Custom/manual", Codigo = "CUSTOM", Tipo = ReajusteTipo.Custom, Ativo = true, CreatedAtUtc = seedCreatedAt });
        });

        modelBuilder.Entity<LancamentoFinanceiro>(entity =>
        {
            entity.ToTable("lancamentos_financeiros");
            entity.Property(x => x.Valor).HasPrecision(18, 2);
            entity.Property(x => x.Descricao).HasMaxLength(300).IsRequired();
        });

        modelBuilder.Entity<ContaPagarReceber>(entity =>
        {
            entity.ToTable("contas_pagar_receber");
            entity.Property(x => x.Valor).HasPrecision(18, 2);
            entity.Property(x => x.Descricao).HasMaxLength(300).IsRequired();
        });

        modelBuilder.Entity<Boleto>(entity =>
        {
            entity.ToTable("boletos");
            entity.Property(x => x.Valor).HasPrecision(18, 2);
        });

        modelBuilder.Entity<NotaFiscal>(entity =>
        {
            entity.ToTable("notas_fiscais");
            entity.Property(x => x.ValorServico).HasPrecision(18, 2);
            entity.Property(x => x.Aliquota).HasPrecision(8, 4);
            entity.Property(x => x.IssValor).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CertificadoDigital>(entity =>
        {
            entity.ToTable("certificados_digitais");
            entity.Property(x => x.Nome).HasMaxLength(220).IsRequired();
        });

        modelBuilder.Entity<DocumentoModelo>(entity =>
        {
            entity.ToTable("documentos_modelos");
            entity.Property(x => x.Tipo).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Nome).HasMaxLength(220).IsRequired();
            entity.HasData(
                new DocumentoModelo { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), Tipo = "contrato_residencial", Nome = "Contrato residencial - modelo inicial", ConteudoTemplate = "Modelo inicial pendente de revisão jurídica do cliente.", StatusRevisao = DocumentoModeloStatusRevisao.PendenteRevisao, Ativo = true, CreatedAtUtc = seedCreatedAt },
                new DocumentoModelo { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), Tipo = "recibo_locatario", Nome = "Recibo do locatário - modelo inicial", ConteudoTemplate = "Modelo inicial pendente de revisão.", StatusRevisao = DocumentoModeloStatusRevisao.PendenteRevisao, Ativo = true, CreatedAtUtc = seedCreatedAt },
                new DocumentoModelo { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), Tipo = "dimob", Nome = "DIMOB - relatório de conferência", ConteudoTemplate = "Estrutura inicial. Confirmar layout oficial vigente antes de exportar TXT.", StatusRevisao = DocumentoModeloStatusRevisao.Inicial, Ativo = true, CreatedAtUtc = seedCreatedAt });
        });

        modelBuilder.Entity<DocumentoGerado>(entity => entity.ToTable("documentos_gerados"));
        modelBuilder.Entity<DimobDeclaracao>(entity => entity.ToTable("dimob_declaracoes"));
        modelBuilder.Entity<DimobItem>(entity =>
        {
            entity.ToTable("dimob_itens");
            entity.Property(x => x.ValorAluguel).HasPrecision(18, 2);
            entity.Property(x => x.ValorComissao).HasPrecision(18, 2);
            entity.Property(x => x.ValorImpostoRetido).HasPrecision(18, 2);
            entity.Property(x => x.ValorPagoProprietario).HasPrecision(18, 2);
        });
        modelBuilder.Entity<ManutencaoImovel>(entity =>
        {
            entity.ToTable("manutencoes_imovel");
            entity.Property(x => x.Valor).HasPrecision(18, 2);
        });
        modelBuilder.Entity<Vistoria>(entity => entity.ToTable("vistorias"));
        modelBuilder.Entity<Rescisao>(entity =>
        {
            entity.ToTable("rescisoes");
            entity.Property(x => x.DebitosTotal).HasPrecision(18, 2);
        });
    }
}
