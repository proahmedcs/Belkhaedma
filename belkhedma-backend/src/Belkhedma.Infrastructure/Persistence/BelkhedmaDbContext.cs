using Belkhedma.Domain;
using Microsoft.EntityFrameworkCore;

namespace Belkhedma.Infrastructure.Persistence;

public sealed class BelkhedmaDbContext(DbContextOptions<BelkhedmaDbContext> options) : DbContext(options)
{
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<ServiceOffer> ServiceOffers => Set<ServiceOffer>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<CollectionJobRun> CollectionJobRuns => Set<CollectionJobRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Provider>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.NameAr).HasMaxLength(200).IsRequired();
            entity.Property(x => x.NameEn).HasMaxLength(200).IsRequired();
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
        });

        modelBuilder.Entity<CollectionJobRun>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Details).HasColumnType("nvarchar(max)");
            entity.HasIndex(x => new { x.ProviderId, x.StartedAtUtc });
        });
    }
}
