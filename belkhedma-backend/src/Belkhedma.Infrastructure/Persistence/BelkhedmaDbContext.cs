using Belkhedma.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Belkhedma.Infrastructure.Persistence;

public sealed class BelkhedmaDbContext(DbContextOptions<BelkhedmaDbContext> options) : IdentityDbContext<IdentityUser, IdentityRole, string>(options)
{
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<ServiceOffer> ServiceOffers => Set<ServiceOffer>();
    public DbSet<ServiceAttribute> ServiceAttributes => Set<ServiceAttribute>();
    public DbSet<ProviderAttributeValueMapper> ProviderAttributeValueMappers => Set<ProviderAttributeValueMapper>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<ProviderJsonDocument> ProviderJsonDocuments => Set<ProviderJsonDocument>();
    public DbSet<HomePromotion> HomePromotions => Set<HomePromotion>();
    public DbSet<CollectionJobRun> CollectionJobRuns => Set<CollectionJobRun>();
    public DbSet<CustomerSavedLocation> CustomerSavedLocations => Set<CustomerSavedLocation>();
    public DbSet<CustomerAccount> CustomerAccounts => Set<CustomerAccount>();
    public DbSet<CustomerAuthSession> CustomerAuthSessions => Set<CustomerAuthSession>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
            entity.HasIndex(x => new { x.ProviderId, x.DisplayOrder, x.NameEn });
            entity.Property(x => x.ProviderServiceId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.NameAr).HasMaxLength(300).IsRequired();
            entity.Property(x => x.NameEn).HasMaxLength(300).IsRequired();
            entity.Property(x => x.HourlyHoursJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(x => x.NationalityGroupsJson).HasColumnType("nvarchar(max)").IsRequired();
        });

        modelBuilder.Entity<ServiceAttribute>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ServiceOfferId, x.AttributeKey }).IsUnique();
            entity.HasIndex(x => new { x.ServiceOfferId, x.DisplayOrder, x.NameEn });
            entity.Property(x => x.AttributeKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
            entity.Property(x => x.OptionSetJson).HasColumnType("nvarchar(max)");
        });

        modelBuilder.Entity<ProviderAttributeValueMapper>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new
            {
                x.ProviderId,
                x.ServiceOfferId,
                x.ServiceMode,
                x.RawAttributeKey,
                x.RawValue
            }).IsUnique();
            entity.HasIndex(x => new { x.ProviderId, x.RawAttributeKey, x.RawValue, x.IsActive });
            entity.Property(x => x.RawAttributeKey).HasMaxLength(120).IsRequired();
            entity.Property(x => x.RawValue).HasMaxLength(300).IsRequired();
            entity.Property(x => x.RawTextEn).HasMaxLength(400);
            entity.Property(x => x.RawTextAr).HasMaxLength(400);
            entity.Property(x => x.NormalizedAttributeKey).HasMaxLength(120).IsRequired();
            entity.Property(x => x.NormalizedValue).HasMaxLength(300).IsRequired();
            entity.Property(x => x.NormalizedTextEn).HasMaxLength(400).IsRequired();
            entity.Property(x => x.NormalizedTextAr).HasMaxLength(400).IsRequired();
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
            entity.HasIndex(x => new { x.ProviderId, x.ServiceOfferId, x.IsActive });
            entity.Property(x => x.DocumentKey).HasMaxLength(120).IsRequired();
            entity.Property(x => x.FileName).HasMaxLength(255).IsRequired();
            entity.Property(x => x.JsonAttributes).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(x => x.JsonData).HasColumnType("nvarchar(max)").IsRequired();
            entity.HasIndex(x => x.ExpiresAtUtc);
        });

        modelBuilder.Entity<HomePromotion>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => new { x.IsActive, x.DisplayOrder, x.CreatedAtUtc });
            entity.Property(x => x.Code).HasMaxLength(80).IsRequired();
            entity.Property(x => x.CompanyNameAr).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CompanyNameEn).HasMaxLength(200).IsRequired();
            entity.Property(x => x.TitleAr).HasMaxLength(250).IsRequired();
            entity.Property(x => x.TitleEn).HasMaxLength(250).IsRequired();
            entity.Property(x => x.SubtitleAr).HasMaxLength(500).IsRequired();
            entity.Property(x => x.SubtitleEn).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ImageUrl).HasMaxLength(1500).IsRequired();
            entity.Property(x => x.TargetUrl).HasMaxLength(1500);
            entity.Property(x => x.DeepLink).HasMaxLength(500);
            entity.Property(x => x.ItemsJson).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(x => x.ProviderCode).HasMaxLength(80);
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

        modelBuilder.Entity<CustomerAccount>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CustomerReference).HasMaxLength(120).IsRequired();
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.MobileNumber).HasMaxLength(30).IsRequired();
            entity.Property(x => x.NormalizedMobileNumber).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.CustomerReference).IsUnique();
            entity.HasIndex(x => x.NormalizedMobileNumber).IsUnique();
        });

        modelBuilder.Entity<CustomerAuthSession>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AuthToken).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.AuthToken).IsUnique();
            entity.HasIndex(x => new { x.CustomerAccountId, x.ExpiresAtUtc });
        });
    }
}
