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
        services.AddScoped<IMarketplaceAdminService, MarketplaceQueryService>();
        services.AddScoped<IDataCollectionService, DataCollectionService>();

        return services;
    }
}

internal sealed class MarketplaceQueryService(BelkhedmaDbContext dbContext) : IMarketplaceQueryService, IMarketplaceAdminService
{
    public async Task<IReadOnlyList<ProviderDto>> GetProvidersAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Providers
            .AsNoTracking()
            .OrderBy(x => x.NameAr)
            .Select(x => new ProviderDto(
                x.Id,
                x.Code,
                x.NameAr,
                x.NameEn,
                x.ProviderType,
                x.HasApiAccess,
                x.SupportsHourly,
                x.SupportsMonthly,
                x.SupportsB2B,
                x.SupportsRecruitment,
                x.IntegrationModeKey,
                x.IntegrationWays,
                x.CommunicationWays,
                x.ContractMode,
                x.PaymentCollectionMode,
                x.RequirePaymentBeforeSubmission,
                x.ApiBaseUrl,
                x.WebsiteUrl,
                x.AppUrl,
                x.TinyUrl,
                x.LogoUrl,
                x.BookingEmail,
                x.OperationsEmail,
                x.Notes,
                x.PricingExpirationHours,
                x.SessionExpirationHours,
                x.ContractDraftExpirationHours,
                x.SettingsJson,
                x.IsActive))
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
            .Where(x => x.ExpiresAtUtc > DateTime.UtcNow)
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
                x.CollectedAtUtc,
                x.ExpiresAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderJsonDocumentDto>> GetProviderJsonDocumentsAsync(
        string? providerCode,
        bool includeExpired,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ProviderJsonDocuments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(providerCode))
        {
            var providerId = await dbContext.Providers
                .AsNoTracking()
                .Where(x => x.Code == providerCode)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (!providerId.HasValue)
            {
                return [];
            }

            query = query.Where(x => x.ProviderId == providerId.Value);
        }

        if (!includeExpired)
        {
            query = query.Where(x => x.ExpiresAtUtc > DateTime.UtcNow && x.IsActive);
        }

        return await query
            .Join(
                dbContext.Providers.AsNoTracking(),
                doc => doc.ProviderId,
                provider => provider.Id,
                (doc, provider) => new { doc, provider.Code })
            .OrderByDescending(x => x.doc.CreatedAtUtc)
            .Select(x => new ProviderJsonDocumentDto(
                x.doc.Id,
                x.doc.ProviderId,
                x.doc.ServiceOfferId,
                x.doc.DocumentKey,
                x.doc.FileName,
                x.Code,
                x.doc.ServiceMode,
                x.doc.JsonAttributes,
                x.doc.JsonData,
                x.doc.IsActive,
                x.doc.CreatedAtUtc,
                x.doc.ExpiresAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerSavedLocationDto>> GetCustomerSavedLocationsAsync(
        string customerReference,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(customerReference))
        {
            return [];
        }

        return await dbContext.CustomerSavedLocations
            .AsNoTracking()
            .Where(x => x.CustomerReference == customerReference)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Select(x => new CustomerSavedLocationDto(
                x.Id,
                x.CustomerReference,
                x.Label,
                x.City,
                x.District,
                x.Latitude,
                x.Longitude,
                x.GoogleMapsUrl,
                x.GooglePlaceId,
                x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PriceSnapshotDto>> GetAllPricesAsync(string? providerCode, bool includeExpired, CancellationToken cancellationToken = default)
    {
        var providers = dbContext.Providers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(providerCode))
        {
            providers = providers.Where(x => x.Code == providerCode);
        }

        var providerIds = await providers.Select(x => x.Id).ToListAsync(cancellationToken);
        var query = dbContext.PriceSnapshots.AsNoTracking().Where(x => providerIds.Contains(x.ProviderId));

        if (!includeExpired)
        {
            query = query.Where(x => x.ExpiresAtUtc > DateTime.UtcNow);
        }

        return await query
            .OrderByDescending(x => x.CollectedAtUtc)
            .Take(500)
            .Select(x => new PriceSnapshotDto(
                x.Id,
                x.ProviderId,
                x.ServiceOfferId,
                x.FinalPriceSar,
                x.OriginalPriceSar,
                x.VatAmountSar,
                x.SourceType,
                x.CollectedAtUtc,
                x.ExpiresAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SetPriceExpirationAsync(Guid priceSnapshotId, DateTime? expiresAtUtc, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.PriceSnapshots.FirstOrDefaultAsync(x => x.Id == priceSnapshotId, cancellationToken);
        if (entity is null)
        {
            return 0;
        }

        entity.ExpiresAtUtc = expiresAtUtc ?? DateTime.UtcNow.AddYears(50);
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> SetPricesExpirationBulkAsync(string? providerCode, DateTime? expiresAtUtc, CancellationToken cancellationToken = default)
    {
        var providers = dbContext.Providers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(providerCode))
        {
            providers = providers.Where(x => x.Code == providerCode);
        }

        var providerIds = await providers.Select(x => x.Id).ToListAsync(cancellationToken);
        var rows = await dbContext.PriceSnapshots
            .Where(x => providerIds.Contains(x.ProviderId))
            .ToListAsync(cancellationToken);

        var newExpiry = expiresAtUtc ?? DateTime.UtcNow.AddYears(50);
        foreach (var row in rows)
        {
            row.ExpiresAtUtc = newExpiry;
        }

        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> SetJsonDocumentExpirationAsync(Guid documentId, DateTime? expiresAtUtc, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ProviderJsonDocuments.FirstOrDefaultAsync(x => x.Id == documentId, cancellationToken);
        if (entity is null)
        {
            return 0;
        }

        entity.ExpiresAtUtc = expiresAtUtc ?? DateTime.UtcNow.AddYears(50);
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> SetJsonDocumentsExpirationBulkAsync(string? providerCode, DateTime? expiresAtUtc, CancellationToken cancellationToken = default)
    {
        var query = dbContext.ProviderJsonDocuments.AsQueryable();
        if (!string.IsNullOrWhiteSpace(providerCode))
        {
            var providerId = await dbContext.Providers
                .AsNoTracking()
                .Where(x => x.Code == providerCode)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (!providerId.HasValue)
            {
                return 0;
            }

            query = query.Where(x => x.ProviderId == providerId.Value);
        }

        var docs = await query.ToListAsync(cancellationToken);
        var newExpiry = expiresAtUtc ?? DateTime.UtcNow.AddYears(50);
        foreach (var doc in docs)
        {
            doc.ExpiresAtUtc = newExpiry;
        }

        return await dbContext.SaveChangesAsync(cancellationToken);
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
                ExpiresAtUtc = DateTime.UtcNow.AddHours(provider.PricingExpirationHours),
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
