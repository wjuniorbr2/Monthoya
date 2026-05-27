using Microsoft.EntityFrameworkCore;
using Monthoya.Core.Entities;

namespace Monthoya.Data;

public sealed class MonthoyaDbContext(DbContextOptions<MonthoyaDbContext> options) : DbContext(options)
{
    public DbSet<Person> People => Set<Person>();

    public DbSet<Property> Properties => Set<Property>();

    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();

    public DbSet<Contract> Contracts => Set<Contract>();

    public DbSet<RentInstallment> RentInstallments => Set<RentInstallment>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<AppUser> Users => Set<AppUser>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<BoletoRecord> BoletoRecords => Set<BoletoRecord>();

    public DbSet<NfseRecord> NfseRecords => Set<NfseRecord>();

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
        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("people");
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.DocumentNumber).HasMaxLength(50);
            entity.Property(x => x.Email).HasMaxLength(320);
            entity.Property(x => x.Phone).HasMaxLength(50);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => x.DocumentNumber);
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.ToTable("properties");
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.AddressLine).HasMaxLength(300).IsRequired();
            entity.Property(x => x.City).HasMaxLength(120).IsRequired();
            entity.Property(x => x.State).HasMaxLength(2).IsRequired();
            entity.Property(x => x.PostalCode).HasMaxLength(20);
            entity.Property(x => x.ListingPrice).HasPrecision(18, 2);
            entity.Property(x => x.RentalPrice).HasPrecision(18, 2);
            entity.Property(x => x.Latitude).HasPrecision(9, 6);
            entity.Property(x => x.Longitude).HasPrecision(9, 6);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PropertyImage>(entity =>
        {
            entity.ToTable("property_images");
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(100);
            entity.HasOne(x => x.Property).WithMany(x => x.Images).HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Contract>(entity =>
        {
            entity.ToTable("contracts");
            entity.Property(x => x.MonthlyRent).HasPrecision(18, 2);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasOne(x => x.Property).WithMany().HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RentInstallment>(entity =>
        {
            entity.ToTable("rent_installments");
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasOne(x => x.Contract).WithMany(x => x.RentInstallments).HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Method).HasMaxLength(80);
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasOne(x => x.RentInstallment).WithMany(x => x.Payments).HasForeignKey(x => x.RentInstallmentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.Property(x => x.RelatedEntityType).HasMaxLength(100).IsRequired();
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(100);
        });

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

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EntityName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(4000);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<BoletoRecord>(entity =>
        {
            entity.ToTable("boleto_records");
            entity.Property(x => x.ExternalId).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Barcode).HasMaxLength(200);
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasOne(x => x.RentInstallment).WithMany().HasForeignKey(x => x.RentInstallmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<NfseRecord>(entity =>
        {
            entity.ToTable("nfse_records");
            entity.Property(x => x.ExternalId).HasMaxLength(120);
            entity.Property(x => x.Status).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ServiceDescription).HasMaxLength(500).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.HasOne(x => x.Contract).WithMany().HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.SetNull);
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
            entity.HasIndex(x => x.Cpf).IsUnique().HasFilter("\"Cpf\" IS NOT NULL AND \"Cpf\" <> ''");
            entity.HasOne(x => x.Pessoa).WithOne(x => x.PessoaFisica).HasForeignKey<PessoaFisica>(x => x.PessoaId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PessoaJuridica>(entity =>
        {
            entity.ToTable("pessoa_juridica");
            entity.HasKey(x => x.PessoaId);
            entity.Property(x => x.NomeEmpresa).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Cnpj).HasMaxLength(24);
            entity.Property(x => x.EmpresaRua).HasMaxLength(220);
            entity.Property(x => x.EmpresaNumero).HasMaxLength(40);
            entity.Property(x => x.EmpresaComplemento).HasMaxLength(120);
            entity.Property(x => x.EmpresaBairro).HasMaxLength(120);
            entity.Property(x => x.EmpresaCidade).HasMaxLength(120);
            entity.Property(x => x.EmpresaEstado).HasMaxLength(2);
            entity.Property(x => x.EmpresaCep).HasMaxLength(20);
            entity.Property(x => x.ResponsavelRua).HasMaxLength(220);
            entity.Property(x => x.ResponsavelNumero).HasMaxLength(40);
            entity.Property(x => x.ResponsavelComplemento).HasMaxLength(120);
            entity.Property(x => x.ResponsavelBairro).HasMaxLength(120);
            entity.Property(x => x.ResponsavelCidade).HasMaxLength(120);
            entity.Property(x => x.ResponsavelEstado).HasMaxLength(2);
            entity.Property(x => x.ResponsavelCep).HasMaxLength(20);
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
