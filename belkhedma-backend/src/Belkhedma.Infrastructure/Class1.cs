using Belkhedma.Application;
using Belkhedma.Domain;
using Belkhedma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Belkhedma.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string sqlConnectionString)
    {
        services.AddDbContext<BelkhedmaDbContext>(options =>
            options.UseSqlServer(sqlConnectionString, sql =>
                sql.MigrationsAssembly(typeof(BelkhedmaDbContext).Assembly.FullName)));

        services.AddScoped<IMarketplaceQueryService, MarketplaceQueryService>();
        services.AddScoped<IDataCollectionService, DataCollectionService>();

        return services;
    }
}

internal sealed class MarketplaceQueryService(BelkhedmaDbContext dbContext) : IMarketplaceQueryService
{
    public async Task<IReadOnlyList<ProviderDto>> GetProvidersAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Providers
            .AsNoTracking()
            .OrderBy(x => x.NameAr)
            .Select(x => new ProviderDto(x.Id, x.Code, x.NameAr, x.NameEn, x.HasApiAccess, x.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceOfferDto>> GetOffersAsync(string? providerCode, CancellationToken cancellationToken = default)
    {
        var providerIds = dbContext.Providers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(providerCode))
        {
            providerIds = providerIds.Where(x => x.Code == providerCode);
        }

        var ids = await providerIds.Select(x => x.Id).ToListAsync(cancellationToken);

        return await dbContext.ServiceOffers
            .AsNoTracking()
            .Where(x => ids.Contains(x.ProviderId))
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Select(x => new ServiceOfferDto(
                x.Id,
                x.ProviderId,
                x.ProviderServiceId,
                x.ServiceMode,
                x.NameAr,
                x.NameEn,
                x.IsAvailable,
                x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PriceSnapshotDto>> GetLatestPricesAsync(string? providerCode, CancellationToken cancellationToken = default)
    {
        var providers = dbContext.Providers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(providerCode))
        {
            providers = providers.Where(x => x.Code == providerCode);
        }

        var providerIds = await providers.Select(x => x.Id).ToListAsync(cancellationToken);

        return await dbContext.PriceSnapshots
            .AsNoTracking()
            .Where(x => providerIds.Contains(x.ProviderId))
            .OrderByDescending(x => x.CollectedAtUtc)
            .Take(100)
            .Select(x => new PriceSnapshotDto(
                x.Id,
                x.ProviderId,
                x.ServiceOfferId,
                x.FinalPriceSar,
                x.OriginalPriceSar,
                x.VatAmountSar,
                x.SourceType,
                x.CollectedAtUtc))
            .ToListAsync(cancellationToken);
    }
}

internal sealed class DataCollectionService(BelkhedmaDbContext dbContext) : IDataCollectionService
{
    public async Task CollectProviderDataAsync(string providerCode, CancellationToken cancellationToken = default)
    {
        var provider = await dbContext.Providers.FirstOrDefaultAsync(x => x.Code == providerCode, cancellationToken);
        if (provider is null || !provider.IsActive)
        {
            return;
        }

        var runStart = DateTime.UtcNow;
        var sourceType = provider.HasApiAccess ? DataSourceType.Api : DataSourceType.Scraper;

        try
        {
            var offer = await dbContext.ServiceOffers
                .FirstOrDefaultAsync(x => x.ProviderId == provider.Id, cancellationToken);

            if (offer is null)
            {
                offer = new ServiceOffer
                {
                    ProviderId = provider.Id,
                    ProviderServiceId = provider.Code == "enaya"
                        ? "a5fbc0b6-3b59-ee11-a8a4-000d3a227ab4"
                        : "c97fdb23-4687-ec11-a837-000d3abe20f8",
                    ServiceMode = ServiceMode.Hourly,
                    NameAr = provider.NameAr + " - خدمة بالساعة",
                    NameEn = provider.NameEn + " - Hourly Service"
                };

                await dbContext.ServiceOffers.AddAsync(offer, cancellationToken);
            }
            else
            {
                offer.UpdatedAtUtc = DateTime.UtcNow;
            }

            var random = Random.Shared.NextDouble();
            var price = provider.Code == "enaya"
                ? Math.Round(75m + (decimal)random * 6m, 2)
                : Math.Round(90m + (decimal)random * 10m, 2);

            await dbContext.PriceSnapshots.AddAsync(new PriceSnapshot
            {
                ProviderId = provider.Id,
                ServiceOfferId = offer.Id,
                FinalPriceSar = price,
                OriginalPriceSar = price + 35,
                VatAmountSar = Math.Round(price * 0.15m, 2),
                SourceType = sourceType,
                RawPayload = $$"""
                {
                  "providerCode": "{{provider.Code}}",
                  "integrationMode": "{{(provider.HasApiAccess ? "api" : "scraper")}}",
                  "note": "Seeded collection sample - replace with real provider adapter payload."
                }
                """
            }, cancellationToken);

            await dbContext.CollectionJobRuns.AddAsync(new CollectionJobRun
            {
                ProviderId = provider.Id,
                SourceType = sourceType,
                Status = JobRunStatus.Succeeded,
                Details = "Collection finished successfully.",
                StartedAtUtc = runStart,
                FinishedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await dbContext.CollectionJobRuns.AddAsync(new CollectionJobRun
            {
                ProviderId = provider.Id,
                SourceType = sourceType,
                Status = JobRunStatus.Failed,
                Details = ex.Message,
                StartedAtUtc = runStart,
                FinishedAtUtc = DateTime.UtcNow
            }, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task CollectAllProvidersDataAsync(CancellationToken cancellationToken = default)
    {
        var providerCodes = await dbContext.Providers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.Code)
            .ToListAsync(cancellationToken);

        foreach (var providerCode in providerCodes)
        {
            await CollectProviderDataAsync(providerCode, cancellationToken);
        }
    }
}
