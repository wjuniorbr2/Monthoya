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
    public DbSet<ImovelChaveMovimento> ImovelChaveMovimentos => Set<ImovelChaveMovimento>();
    public DbSet<Locacao> Locacoes => Set<Locacao>();
    public DbSet<LocacaoFiador> LocacaoFiadores => Set<LocacaoFiador>();
    public DbSet<LocacaoParte> LocacaoPartes => Set<LocacaoParte>();
    public DbSet<LocacaoGarantia> LocacaoGarantias => Set<LocacaoGarantia>();
    public DbSet<LocacaoValorHistorico> LocacaoValoresHistoricos => Set<LocacaoValorHistorico>();
    public DbSet<LocacaoEncargoRecorrente> LocacaoEncargosRecorrentes => Set<LocacaoEncargoRecorrente>();
    public DbSet<LocacaoLancamento> LocacaoLancamentos => Set<LocacaoLancamento>();
    public DbSet<LocacaoCobranca> LocacaoCobrancas => Set<LocacaoCobranca>();
    public DbSet<LocacaoCobrancaItem> LocacaoCobrancaItens => Set<LocacaoCobrancaItem>();
    public DbSet<LocacaoNotificacaoRegra> LocacaoNotificacaoRegras => Set<LocacaoNotificacaoRegra>();
    public DbSet<LocacaoHistorico> LocacaoHistoricos => Set<LocacaoHistorico>();
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
    public DbSet<VistoriaAmbiente> VistoriaAmbientes => Set<VistoriaAmbiente>();
    public DbSet<VistoriaItem> VistoriaItens => Set<VistoriaItem>();
    public DbSet<VistoriaFoto> VistoriaFotos => Set<VistoriaFoto>();
    public DbSet<Rescisao> Rescisoes => Set<Rescisao>();
    public DbSet<NotificationMessage> NotificationMessages => Set<NotificationMessage>();
    public DbSet<NotificationRecipient> NotificationRecipients => Set<NotificationRecipient>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<NotificationEmailSettings> NotificationEmailSettings => Set<NotificationEmailSettings>();

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
        ConfigureNotifications(modelBuilder);
    }

    private static void ConfigureNotifications(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationMessage>(entity =>
        {
            entity.ToTable("notification_messages");
            entity.Property(x => x.Title).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Body).HasMaxLength(8000).IsRequired();
            entity.Property(x => x.Category).HasConversion<int>();
            entity.Property(x => x.Priority).HasConversion<int>();
            entity.Property(x => x.RelatedEntityType).HasMaxLength(120);
            entity.Property(x => x.ActionLabel).HasMaxLength(120);
            entity.Property(x => x.ActionTarget).HasMaxLength(500);
            entity.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.Category, x.RelatedEntityType, x.RelatedEntityId });
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasIndex(x => x.ScheduledForUtc);
        });

        modelBuilder.Entity<NotificationRecipient>(entity =>
        {
            entity.ToTable("notification_recipients");
            entity.HasOne(x => x.NotificationMessage).WithMany(x => x.Recipients).HasForeignKey(x => x.NotificationMessageId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.NotificationMessageId, x.UserId }).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.ReadAtUtc });
            entity.HasIndex(x => new { x.UserId, x.AcknowledgedAtUtc });
        });

        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.ToTable("notification_deliveries");
            entity.Property(x => x.Channel).HasConversion<int>();
            entity.Property(x => x.Destination).HasMaxLength(500);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.ErrorMessage).HasMaxLength(2000);
            entity.HasOne(x => x.NotificationMessage).WithMany(x => x.Deliveries).HasForeignKey(x => x.NotificationMessageId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.RecipientUser).WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.NotificationMessageId, x.Channel });
            entity.HasIndex(x => new { x.RecipientUserId, x.Status });
        });

        modelBuilder.Entity<NotificationEmailSettings>(entity =>
        {
            entity.ToTable("notification_email_settings");
            entity.Property(x => x.SenderDisplayName).HasMaxLength(220);
            entity.Property(x => x.SenderEmail).HasMaxLength(320);
            entity.Property(x => x.SmtpHost).HasMaxLength(220);
            entity.Property(x => x.SmtpUsername).HasMaxLength(320);
            entity.Property(x => x.SmtpPasswordSecret).HasMaxLength(1000);
            entity.Property(x => x.ReplyToEmail).HasMaxLength(320);
        });
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
            entity.Property(x => x.BancoCodigo).HasMaxLength(20);
            entity.Property(x => x.BancoNome).HasMaxLength(120);
            entity.Property(x => x.AgenciaNumero).HasMaxLength(20);
            entity.Property(x => x.AgenciaDigito).HasMaxLength(5);
            entity.Property(x => x.ContaNumero).HasMaxLength(30);
            entity.Property(x => x.ContaDigito).HasMaxLength(5);
            entity.Property(x => x.TitularNome).HasMaxLength(220);
            entity.Property(x => x.TitularDocumento).HasMaxLength(20);
            entity.Property(x => x.PixChave).HasMaxLength(320);
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
            entity.Property(x => x.ResponsavelBancoCodigo).HasMaxLength(20);
            entity.Property(x => x.ResponsavelBancoNome).HasMaxLength(120);
            entity.Property(x => x.ResponsavelAgenciaNumero).HasMaxLength(20);
            entity.Property(x => x.ResponsavelAgenciaDigito).HasMaxLength(5);
            entity.Property(x => x.ResponsavelContaNumero).HasMaxLength(30);
            entity.Property(x => x.ResponsavelContaDigito).HasMaxLength(5);
            entity.Property(x => x.ResponsavelTitularNome).HasMaxLength(220);
            entity.Property(x => x.ResponsavelTitularDocumento).HasMaxLength(20);
            entity.Property(x => x.ResponsavelPixChave).HasMaxLength(320);
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
            entity.Property(x => x.ValorCondominio).HasPrecision(18, 2);
            entity.Property(x => x.ValorIptu).HasPrecision(18, 2);
            entity.Property(x => x.Latitude).HasPrecision(9, 6);
            entity.Property(x => x.Longitude).HasPrecision(9, 6);
            entity.Property(x => x.AreaConstruida).HasPrecision(12, 2);
            entity.Property(x => x.AreaTerreno).HasPrecision(12, 2);
            entity.Property(x => x.ColetaLixo).HasMaxLength(200);
            entity.Property(x => x.DescricaoInterna).HasMaxLength(4000);
            entity.Property(x => x.DescricaoPublica).HasMaxLength(4000);
            entity.Property(x => x.ChaveCodigo).HasMaxLength(80);
            entity.Property(x => x.ChaveQuemTem).HasMaxLength(220);
            entity.Property(x => x.ChaveTelefone).HasMaxLength(40);
            entity.Property(x => x.ChaveContatoNome).HasMaxLength(220);
            entity.Property(x => x.ChaveContatoDocumento).HasMaxLength(40);
            entity.Property(x => x.ChaveLocalRetirada).HasMaxLength(500);
            entity.Property(x => x.ChaveMelhorHorario).HasMaxLength(120);
            entity.Property(x => x.ChaveObservacoes).HasMaxLength(2000);
            entity.HasOne(x => x.Proprietario).WithMany().HasForeignKey(x => x.ProprietarioId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ImovelChaveMovimento>(entity =>
        {
            entity.ToTable("imovel_chave_movimentos");
            entity.Property(x => x.ChaveCodigo).HasMaxLength(80);
            entity.Property(x => x.RetiradoPorNome).HasMaxLength(220);
            entity.Property(x => x.RetiradoPorTelefone).HasMaxLength(40);
            entity.Property(x => x.RetiradoPorDocumento).HasMaxLength(40);
            entity.Property(x => x.RetiradoPorRelacao).HasMaxLength(120);
            entity.Property(x => x.Motivo).HasMaxLength(120);
            entity.Property(x => x.DevolvidoParaNome).HasMaxLength(220);
            entity.Property(x => x.Observacoes).HasMaxLength(2000);
            entity.HasOne(x => x.Imovel).WithMany(x => x.ChaveMovimentos).HasForeignKey(x => x.ImovelId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.ImovelId, x.Status });
            entity.HasIndex(x => x.PrevisaoDevolucaoEm);
        });

        modelBuilder.Entity<ImovelImagem>(entity =>
        {
            entity.ToTable("imovel_imagens");
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.StoragePath).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(100);
            entity.Property(x => x.Caption).HasMaxLength(500);
            entity.Property(x => x.MediaCategory).HasConversion<int>();
            entity.Property(x => x.Source).HasConversion<int>();
            entity.HasOne(x => x.Imovel).WithMany(x => x.Imagens).HasForeignKey(x => x.ImovelId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.ImovelId, x.DisplayOrder });
            entity.HasIndex(x => new { x.ImovelId, x.IsCover });
        });

        modelBuilder.Entity<Locacao>(entity =>
        {
            entity.ToTable("locacoes");
            entity.Property(x => x.Codigo).HasMaxLength(80);
            entity.Property(x => x.TipoLocacao).HasConversion<int>();
            entity.Property(x => x.ResponsavelNome).HasMaxLength(220);
            entity.Property(x => x.MotivoEncerramento).HasMaxLength(2000);
            entity.Property(x => x.ValorAluguel).HasPrecision(18, 2);
            entity.Property(x => x.ValorAluguelInicial).HasPrecision(18, 2);
            entity.Property(x => x.ValorAluguelAtual).HasPrecision(18, 2);
            entity.Property(x => x.MetodoCalculoProporcional).HasConversion<int>();
            entity.Property(x => x.TipoDescontoPontualidade).HasConversion<int>();
            entity.Property(x => x.ValorDescontoPontualidade).HasPrecision(18, 2);
            entity.Property(x => x.MultaAtrasoTipo).HasConversion<int>();
            entity.Property(x => x.MultaAtrasoValor).HasPrecision(18, 2);
            entity.Property(x => x.JurosMoraPercentualMes).HasPrecision(8, 4);
            entity.Property(x => x.IndiceCorrecaoAtraso).HasMaxLength(80);
            entity.Property(x => x.MultaPercentual).HasPrecision(8, 4);
            entity.Property(x => x.JurosPercentual).HasPrecision(8, 4);
            entity.Property(x => x.DescontoAteVencimentoValor).HasPrecision(18, 2);
            entity.Property(x => x.DescontoAteVencimentoPercentual).HasPrecision(8, 4);
            entity.Property(x => x.DescontoValidoAteVencimento).HasDefaultValue(true);
            entity.Property(x => x.ModoReajuste).HasConversion<int>().HasDefaultValue(ModoReajusteLocacao.Manual);
            entity.Property(x => x.ReajusteRequerAprovacao).HasDefaultValue(true);
            entity.Property(x => x.TaxaAdministracaoValor).HasPrecision(18, 2);
            entity.Property(x => x.TaxaAdministracaoPercentual).HasPrecision(8, 4);
            entity.Property(x => x.MetaComissaoPrimeiroAluguelPercentual).HasPrecision(8, 4);
            entity.Property(x => x.TaxaContratoValor).HasPrecision(18, 2);
            entity.Property(x => x.TaxaContratoPercentual).HasPrecision(8, 4);
            entity.Property(x => x.ModoCobrancaTaxaContrato).HasConversion<int>().HasDefaultValue(ModoCobrancaTaxaContratoLocacao.Manual);
            entity.Property(x => x.DestinoRepasse).HasConversion<int>();
            entity.Property(x => x.TaxaRenovacaoValor).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.ObservacoesInternas).HasMaxLength(4000);
            entity.HasOne(x => x.Imovel).WithMany().HasForeignKey(x => x.ImovelId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Locatario).WithMany().HasForeignKey(x => x.LocatarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Proprietario).WithMany().HasForeignKey(x => x.ProprietarioId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.IndiceReajuste).WithMany().HasForeignKey(x => x.IndiceReajusteId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.ResponsavelUsuario).WithMany().HasForeignKey(x => x.ResponsavelUsuarioId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => x.Codigo);
            entity.HasIndex(x => x.ImovelId);
            entity.HasIndex(x => new { x.ImovelId, x.Status });
        });

        modelBuilder.Entity<LocacaoFiador>(entity =>
        {
            entity.ToTable("locacao_fiadores");
            entity.HasIndex(x => new { x.LocacaoId, x.FiadorId }).IsUnique();
            entity.HasOne(x => x.Locacao).WithMany(x => x.Fiadores).HasForeignKey(x => x.LocacaoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Fiador).WithMany().HasForeignKey(x => x.FiadorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LocacaoParte>(entity =>
        {
            entity.ToTable("locacao_partes");
            entity.Property(x => x.TipoParte).HasConversion<int>();
            entity.Property(x => x.PercentualParticipacao).HasPrecision(8, 4);
            entity.Property(x => x.PercentualRepasse).HasPrecision(8, 4);
            entity.Property(x => x.Observacoes).HasMaxLength(2000);
            entity.HasOne(x => x.Locacao).WithMany(x => x.Partes).HasForeignKey(x => x.LocacaoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Pessoa).WithMany().HasForeignKey(x => x.PessoaId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.LocacaoId, x.PessoaId, x.TipoParte }).IsUnique();
            entity.HasIndex(x => new { x.LocacaoId, x.TipoParte, x.IsPrincipal });
        });

        modelBuilder.Entity<LocacaoGarantia>(entity =>
        {
            entity.ToTable("locacao_garantias");
            entity.Property(x => x.TipoGarantia).HasConversion<int>();
            entity.Property(x => x.Valor).HasPrecision(18, 2);
            entity.Property(x => x.Observacoes).HasMaxLength(2000);
            entity.Property(x => x.ObservacoesDocumento).HasMaxLength(2000);
            entity.HasOne(x => x.Locacao).WithMany(x => x.Garantias).HasForeignKey(x => x.LocacaoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.LocacaoId, x.Ativa }).IsUnique().HasFilter("\"Ativa\" = TRUE");
        });

        modelBuilder.Entity<LocacaoValorHistorico>(entity =>
        {
            entity.ToTable("locacao_valores_historicos");
            entity.Property(x => x.ValorAnterior).HasPrecision(18, 2);
            entity.Property(x => x.ValorNovo).HasPrecision(18, 2);
            entity.Property(x => x.Motivo).HasMaxLength(2000);
            entity.Property(x => x.IndiceReajuste).HasMaxLength(80);
            entity.Property(x => x.PercentualAplicado).HasPrecision(8, 4);
            entity.Property(x => x.Usuario).HasMaxLength(220);
            entity.HasOne(x => x.Locacao).WithMany(x => x.ValoresHistoricos).HasForeignKey(x => x.LocacaoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.LocacaoId, x.DataVigencia });
        });

        modelBuilder.Entity<LocacaoEncargoRecorrente>(entity =>
        {
            entity.ToTable("locacao_encargos_recorrentes");
            entity.Property(x => x.TipoEncargo).HasConversion<int>();
            entity.Property(x => x.Valor).HasPrecision(18, 2);
            entity.Property(x => x.Observacoes).HasMaxLength(2000);
            entity.HasOne(x => x.Locacao).WithMany(x => x.EncargosRecorrentes).HasForeignKey(x => x.LocacaoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.LocacaoId, x.TipoEncargo, x.Ativo });
        });

        modelBuilder.Entity<LocacaoLancamento>(entity =>
        {
            entity.ToTable("locacao_lancamentos");
            entity.Property(x => x.TipoLancamento).HasConversion<int>();
            entity.Property(x => x.Descricao).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Valor).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.Observacoes).HasMaxLength(2000);
            entity.HasOne(x => x.Locacao).WithMany(x => x.Lancamentos).HasForeignKey(x => x.LocacaoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.LocacaoId, x.Status });
            entity.HasIndex(x => x.DataVencimento);
        });

        modelBuilder.Entity<LocacaoCobranca>(entity =>
        {
            entity.ToTable("locacao_cobrancas");
            entity.Property(x => x.TipoCobranca).HasConversion<int>();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.ValorAluguel).HasPrecision(18, 2);
            entity.Property(x => x.ValorDescontos).HasPrecision(18, 2);
            entity.Property(x => x.ValorEncargos).HasPrecision(18, 2);
            entity.Property(x => x.ValorMulta).HasPrecision(18, 2);
            entity.Property(x => x.ValorJuros).HasPrecision(18, 2);
            entity.Property(x => x.ValorTotal).HasPrecision(18, 2);
            entity.HasOne(x => x.Locacao).WithMany(x => x.Cobrancas).HasForeignKey(x => x.LocacaoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.LocacaoId, x.Competencia });
            entity.HasIndex(x => new { x.Status, x.DataVencimento });
        });

        modelBuilder.Entity<LocacaoCobrancaItem>(entity =>
        {
            entity.ToTable("locacao_cobranca_itens");
            entity.Property(x => x.TipoItem).HasConversion<int>();
            entity.Property(x => x.Descricao).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Valor).HasPrecision(18, 2);
            entity.Property(x => x.ReferenciaId).HasMaxLength(120);
            entity.Property(x => x.Observacoes).HasMaxLength(2000);
            entity.HasOne(x => x.LocacaoCobranca).WithMany(x => x.Itens).HasForeignKey(x => x.LocacaoCobrancaId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.LocacaoCobrancaId);
        });

        modelBuilder.Entity<LocacaoNotificacaoRegra>(entity =>
        {
            entity.ToTable("locacao_notificacao_regras");
            entity.Property(x => x.TipoNotificacao).HasConversion<int>();
            entity.Property(x => x.Modo).HasConversion<int>();
            entity.Property(x => x.DestinatarioTipo).HasConversion<int>();
            entity.Property(x => x.DestinatarioRole).HasMaxLength(120);
            entity.Property(x => x.Canal).HasConversion<int>();
            entity.Property(x => x.Observacoes).HasMaxLength(2000);
            entity.HasOne(x => x.Locacao).WithMany(x => x.NotificacaoRegras).HasForeignKey(x => x.LocacaoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.DestinatarioUsuario).WithMany().HasForeignKey(x => x.DestinatarioUsuarioId).OnDelete(DeleteBehavior.SetNull);
            entity.HasIndex(x => new { x.LocacaoId, x.TipoNotificacao, x.Ativa });
        });

        modelBuilder.Entity<LocacaoHistorico>(entity =>
        {
            entity.ToTable("locacao_historicos");
            entity.Property(x => x.Usuario).HasMaxLength(220);
            entity.Property(x => x.Acao).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Campo).HasMaxLength(120);
            entity.Property(x => x.ValorAnterior).HasMaxLength(2000);
            entity.Property(x => x.ValorNovo).HasMaxLength(2000);
            entity.Property(x => x.Motivo).HasMaxLength(2000);
            entity.HasOne(x => x.Locacao).WithMany(x => x.Historicos).HasForeignKey(x => x.LocacaoId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => new { x.LocacaoId, x.DataHoraUtc });
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
        modelBuilder.Entity<Vistoria>(entity =>
        {
            entity.ToTable("vistorias");
            entity.Property(x => x.StableId).HasDefaultValueSql("gen_random_uuid()").IsRequired();
            entity.Property(x => x.WorkflowStatus).HasConversion<int>();
            entity.Property(x => x.PdfPath).HasMaxLength(1000);
            entity.Property(x => x.AiStatus).HasMaxLength(80);
            entity.Property(x => x.AiErrorMessage).HasMaxLength(2000);
            entity.HasOne(x => x.Imovel).WithMany().HasForeignKey(x => x.ImovelId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.StableId).IsUnique();
            entity.HasIndex(x => new { x.ImovelId, x.DataVistoria });
        });
        modelBuilder.Entity<VistoriaAmbiente>(entity =>
        {
            entity.ToTable("vistoria_ambientes");
            entity.Property(x => x.StableId).HasDefaultValueSql("gen_random_uuid()").IsRequired();
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.TipoAmbiente).HasConversion<int>();
            entity.Property(x => x.CondicaoGeral).HasMaxLength(120);
            entity.Property(x => x.Observacoes).HasMaxLength(4000);
            entity.HasOne(x => x.Vistoria).WithMany(x => x.Ambientes).HasForeignKey(x => x.VistoriaId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.StableId).IsUnique();
            entity.HasIndex(x => new { x.VistoriaId, x.DisplayOrder });
        });
        modelBuilder.Entity<VistoriaItem>(entity =>
        {
            entity.ToTable("vistoria_itens");
            entity.Property(x => x.StableId).HasDefaultValueSql("gen_random_uuid()").IsRequired();
            entity.Property(x => x.Nome).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Categoria).HasConversion<int>();
            entity.Property(x => x.Condicao).HasConversion<int>();
            entity.Property(x => x.ResponsabilidadeSugerida).HasMaxLength(120);
            entity.Property(x => x.AiConfidence).HasPrecision(5, 4);
            entity.Property(x => x.AiStatus).HasMaxLength(80);
            entity.Property(x => x.AiErrorMessage).HasMaxLength(2000);
            entity.HasOne(x => x.Ambiente).WithMany(x => x.Itens).HasForeignKey(x => x.VistoriaAmbienteId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.StableId).IsUnique();
            entity.HasIndex(x => x.VistoriaAmbienteId);
        });
        modelBuilder.Entity<VistoriaFoto>(entity =>
        {
            entity.ToTable("vistoria_fotos");
            entity.Property(x => x.StableId).HasDefaultValueSql("gen_random_uuid()").IsRequired();
            entity.Property(x => x.FileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.LocalDevicePath).HasMaxLength(1000);
            entity.Property(x => x.StoragePath).HasMaxLength(1000);
            entity.Property(x => x.ContentType).HasMaxLength(100);
            entity.Property(x => x.Caption).HasMaxLength(500);
            entity.Property(x => x.UploadStatus).HasConversion<int>();
            entity.Property(x => x.Source).HasConversion<int>();
            entity.Property(x => x.AiConfidence).HasPrecision(5, 4);
            entity.Property(x => x.AiStatus).HasMaxLength(80);
            entity.Property(x => x.AiErrorMessage).HasMaxLength(2000);
            entity.HasOne(x => x.Vistoria).WithMany(x => x.Fotos).HasForeignKey(x => x.VistoriaId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Ambiente).WithMany(x => x.Fotos).HasForeignKey(x => x.VistoriaAmbienteId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Item).WithMany(x => x.Fotos).HasForeignKey(x => x.VistoriaItemId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Imovel).WithMany().HasForeignKey(x => x.ImovelId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(x => x.StableId).IsUnique();
            entity.HasIndex(x => new { x.VistoriaId, x.DisplayOrder });
            entity.HasIndex(x => new { x.ImovelId, x.UploadStatus });
        });
        modelBuilder.Entity<Rescisao>(entity =>
        {
            entity.ToTable("rescisoes");
            entity.Property(x => x.DebitosTotal).HasPrecision(18, 2);
        });
    }
}
