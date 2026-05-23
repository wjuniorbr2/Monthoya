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
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.HasIndex(x => x.Email).IsUnique();
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
    }
}
