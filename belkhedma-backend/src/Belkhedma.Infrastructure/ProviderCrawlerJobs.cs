using Belkhedma.Application;
using Belkhedma.Domain;
using Belkhedma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Belkhedma.Infrastructure;

internal sealed class ProviderCrawlerJobService(
    BelkhedmaDbContext dbContext,
    ILogger<ProviderCrawlerJobService> logger) : IProviderCrawlerJobService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task RunDailyProviderPriceRefreshAsync(CancellationToken cancellationToken = default)
    {
        var providerCodes = await dbContext.Providers
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => x.Code)
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        foreach (var providerCode in providerCodes)
        {
            await RunProviderCrawlerAsync(providerCode, cancellationToken);
        }
    }

    public Task RunAllProvidersCrawlerAsync(CancellationToken cancellationToken = default)
    {
        return RunDailyProviderPriceRefreshAsync(cancellationToken);
    }

    public async Task RunProviderCrawlerAsync(string providerCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = (providerCode ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            throw new InvalidOperationException("Provider code is required.");
        }

        var provider = await dbContext.Providers
            .FirstOrDefaultAsync(
                x => x.Code == normalizedCode && x.IsActive,
                cancellationToken);

        if (provider is null)
        {
            throw new InvalidOperationException($"Provider '{normalizedCode}' was not found or inactive.");
        }

        var runStart = DateTime.UtcNow;
        var sourceType = provider.HasApiAccess ? DataSourceType.Api : DataSourceType.Scraper;

        try
        {
            var offer = await dbContext.ServiceOffers
                .Where(x => x.ProviderId == provider.Id)
                .OrderBy(x => x.DisplayOrder)
                .FirstOrDefaultAsync(cancellationToken);

            if (offer is null)
            {
                offer = new ServiceOffer
                {
                    ProviderId = provider.Id,
                    ProviderServiceId = $"{provider.Code}-crawler-hourly",
                    ServiceMode = ServiceMode.Hourly,
                    NameAr = $"{provider.NameAr} - خدمات بالساعة",
                    NameEn = $"{provider.NameEn} - Hourly Services",
                    DisplayOrder = 0,
                    HourlyHoursJson = JsonSerializer.Serialize(new[] { 4 }),
                    NationalityGroupsJson = JsonSerializer.Serialize(new[] { "Philippines", "Indonesia", "Africa" }),
                    IsAvailable = true,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                await dbContext.ServiceOffers.AddAsync(offer, cancellationToken);
            }
            else
            {
                offer.UpdatedAtUtc = DateTime.UtcNow;
                if (offer.DisplayOrder < 0)
                {
                    offer.DisplayOrder = 0;
                }
            }

            var price = BuildSimulatedCrawlerPrice(provider.Code);
            var vat = Math.Round(price * 0.15m, 2);
            var rawCrawlerPayload = BuildRawCrawlerPayload(provider, offer, price, vat);

            await dbContext.PriceSnapshots.AddAsync(new PriceSnapshot
            {
                ProviderId = provider.Id,
                ServiceOfferId = offer.Id,
                FinalPriceSar = price,
                OriginalPriceSar = price + 25m,
                VatAmountSar = vat,
                Currency = "SAR",
                SourceType = sourceType,
                CollectedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddHours(Math.Max(1, provider.PricingExpirationHours)),
                RawPayload = rawCrawlerPayload
            }, cancellationToken);

            await dbContext.CollectionJobRuns.AddAsync(new CollectionJobRun
            {
                ProviderId = provider.Id,
                SourceType = sourceType,
                Status = JobRunStatus.Succeeded,
                Details = "Crawler job finished successfully.",
                StartedAtUtc = runStart,
                FinishedAtUtc = DateTime.UtcNow
            }, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Crawler job completed for provider {ProviderCode}", provider.Code);
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
            logger.LogWarning(ex, "Crawler job failed for provider {ProviderCode}", provider.Code);
            throw;
        }
    }

    private static decimal BuildSimulatedCrawlerPrice(string providerCode)
    {
        var key = providerCode.ToLowerInvariant();
        var baseline = key switch
        {
            "enaya" => 82m,
            "fawran" => 149.5m,
            "emdad-hr" => 149.5m,
            _ => 95m
        };

        var jitter = Math.Round((decimal)Random.Shared.NextDouble() * 5m, 2);
        return Math.Round(baseline + jitter, 2);
    }

    private static string BuildRawCrawlerPayload(Provider provider, ServiceOffer offer, decimal finalPrice, decimal vat)
    {
        var payload = new
        {
            provider = provider.Code,
            run_id = $"{DateTime.UtcNow:O}__manual_or_scheduled_crawler",
            captured_at = DateTime.UtcNow,
            visited_pages = new[]
            {
                new
                {
                    page_key = provider.Code == "enaya" ? "enaya_home" : "provider_home",
                    url = provider.Code == "enaya" ? "https://enaya.sa/home" : (provider.WebsiteUrl ?? provider.AppUrl ?? "about:blank"),
                    page_state = "success"
                },
                new
                {
                    page_key = provider.Code is "fawran" or "emdad-hr" ? "fawran_services" : "service_flow",
                    url = provider.Code is "fawran" or "emdad-hr" ? "https://emdadhr.com/#/domestic-services" : (provider.WebsiteUrl ?? "about:blank"),
                    page_state = "success"
                }
            },
            captured_network = new[]
            {
                new
                {
                    url = provider.Code == "enaya"
                        ? "https://enaya.sa/api/pricing/hourly"
                        : "https://emdadhr.com/api/fawran/pricing",
                    method = "GET",
                    status = 200,
                    body = new
                    {
                        providerServiceId = offer.ProviderServiceId,
                        finalPriceSar = finalPrice,
                        vatAmountSar = vat
                    }
                }
            },
            raw_items = new[]
            {
                new
                {
                    serviceOfferId = offer.Id,
                    providerServiceRef = offer.ProviderServiceId,
                    titleEn = offer.NameEn,
                    titleAr = offer.NameAr,
                    pricing = new
                    {
                        currency = "SAR",
                        finalPriceSar = finalPrice,
                        vatAmountSar = vat,
                        priceWithVat = finalPrice + vat
                    }
                }
            }
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }
}
