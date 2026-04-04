using Belkhedma.Domain;
using Microsoft.EntityFrameworkCore;

namespace Belkhedma.Infrastructure.Persistence;

public sealed class BelkhedmaDbContext(DbContextOptions<BelkhedmaDbContext> options) : DbContext(options)
{
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<ServiceOffer> ServiceOffers => Set<ServiceOffer>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<ProviderJsonDocument> ProviderJsonDocuments => Set<ProviderJsonDocument>();
    public DbSet<CollectionJobRun> CollectionJobRuns => Set<CollectionJobRun>();
    public DbSet<CustomerSavedLocation> CustomerSavedLocations => Set<CustomerSavedLocation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
            entity.Property(x => x.ProviderType).HasMaxLength(200).IsRequired();
            entity.Property(x => x.IntegrationModeKey).HasMaxLength(80).IsRequired();
            entity.Property(x => x.ApiBaseUrl).HasMaxLength(500);
            entity.Property(x => x.WebsiteUrl).HasMaxLength(500);
            entity.Property(x => x.AppUrl).HasMaxLength(500);
            entity.Property(x => x.TinyUrl).HasMaxLength(200);
            entity.Property(x => x.LogoUrl).HasMaxLength(500);
            entity.Property(x => x.BookingEmail).HasMaxLength(320);
            entity.Property(x => x.OperationsEmail).HasMaxLength(320);
            entity.Property(x => x.Notes).HasColumnType("nvarchar(max)");
            entity.Property(x => x.SettingsJson).HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<ServiceOffer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ProviderId, x.ProviderServiceId }).IsUnique();
            entity.Property(x => x.ProviderServiceId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.NameAr).HasMaxLength(300).IsRequired();
            entity.Property(x => x.NameEn).HasMaxLength(300).IsRequired();
        });

        modelBuilder.Entity<PriceSnapshot>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Currency).HasMaxLength(8).IsRequired();
            entity.Property(x => x.FinalPriceSar).HasPrecision(18, 2);
            entity.Property(x => x.OriginalPriceSar).HasPrecision(18, 2);
            entity.Property(x => x.VatAmountSar).HasPrecision(18, 2);
            entity.Property(x => x.RawPayload).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.ProviderId, x.CollectedAtUtc });
            entity.HasIndex(x => x.ExpiresAtUtc);
        });

        modelBuilder.Entity<ProviderJsonDocument>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.DocumentKey).IsUnique();
            entity.Property(x => x.DocumentKey).HasMaxLength(120).IsRequired();
            entity.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.ProviderCode).HasMaxLength(50);
            entity.Property(x => x.JsonContent).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasIndex(x => x.ExpiresAtUtc);
        });

        modelBuilder.Entity<CollectionJobRun>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Details).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.ProviderId, x.StartedAtUtc });
        });

        modelBuilder.Entity<CustomerSavedLocation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerReference).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Label).HasMaxLength(200).IsRequired();
            entity.Property(x => x.City).HasMaxLength(120).IsRequired();
            entity.Property(x => x.District).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Latitude).HasPrecision(9, 6);
            entity.Property(x => x.Longitude).HasPrecision(9, 6);
            entity.Property(x => x.GoogleMapsUrl).HasMaxLength(500);
            entity.Property(x => x.GooglePlaceId).HasMaxLength(120);
            entity.HasIndex(x => new { x.CustomerReference, x.UpdatedAtUtc });
        });
    }
}
