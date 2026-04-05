using Belkhedma.Application;
using Belkhedma.Domain;
using Belkhedma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        services.AddScoped<ICustomerAuthService, CustomerAuthService>();
        services.AddScoped<IDataCollectionService, DataCollectionService>();
        services.AddScoped<IProviderCrawlerJobService, ProviderCrawlerJobService>();

        return services;
    }
}

internal sealed class MarketplaceQueryService(
    BelkhedmaDbContext dbContext,
    UserManager<IdentityUser> userManager) : IMarketplaceQueryService, IMarketplaceAdminService
{
    private const int MaxPromotionCodeLength = 80;
    private static readonly JsonSerializerOptions ProviderSettingsJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<CustomerProfileDto?> GetCustomerProfileByIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty)
        {
            return null;
        }

        var customer = await dbContext.CustomerAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == customerId && x.IsActive, cancellationToken);

        if (customer is null)
        {
            return null;
        }

        IdentityUser? identityUser = null;
        if (!string.IsNullOrWhiteSpace(customer.CustomerReference) &&
            customer.CustomerReference.StartsWith("idn-", StringComparison.OrdinalIgnoreCase))
        {
            var identityUserId = customer.CustomerReference["idn-".Length..];
            if (!string.IsNullOrWhiteSpace(identityUserId))
            {
                identityUser = await userManager.FindByIdAsync(identityUserId);
            }
        }

        if (identityUser is null && !string.IsNullOrWhiteSpace(customer.NormalizedMobileNumber))
        {
            identityUser = await userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.PhoneNumber == customer.NormalizedMobileNumber,
                    cancellationToken);
        }

        return new CustomerProfileDto(
            customer.Id,
            customer.CustomerReference,
            customer.FullName,
            customer.MobileNumber,
            identityUser?.Email);
    }

    public async Task<CustomerProfileDto?> GetCustomerProfileByTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        // Legacy token lookup for backward compatibility.
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var session = await dbContext.CustomerAuthSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.RefreshToken == refreshToken &&
                !x.IsRevoked &&
                x.ExpiresAtUtc > now, cancellationToken);

        if (session is null)
        {
            return null;
        }

        return await GetCustomerProfileByIdAsync(session.CustomerAccountId, cancellationToken);
    }

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
                ExtractProviderCredentials(x.SettingsJson).Username,
                ExtractProviderCredentials(x.SettingsJson).Password,
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

        var rows = await dbContext.ServiceOffers
            .AsNoTracking()
            .Where(x => ids.Contains(x.ProviderId))
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        var offerIds = rows.Select(x => x.Id).ToList();
        var attributesByOfferId = await dbContext.ServiceAttributes
            .AsNoTracking()
            .Where(x => offerIds.Contains(x.ServiceOfferId) && x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.NameEn)
            .ToListAsync(cancellationToken);
        var groupedAttributes = attributesByOfferId
            .GroupBy(x => x.ServiceOfferId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<ServiceAttributeDto>)x.Select(MapServiceAttribute).ToList());

        return rows
            .Select(x => new ServiceOfferDto(
                x.Id,
                x.ProviderId,
                x.ProviderServiceId,
                x.ServiceMode,
                x.NameAr,
                x.NameEn,
                x.DisplayOrder,
                ParseHourOptions(x.HourlyHoursJson),
                ParseStringList(x.NationalityGroupsJson),
                groupedAttributes.TryGetValue(x.Id, out var attributes) ? attributes : [],
                x.IsAvailable,
                x.UpdatedAtUtc))
            .ToList();
    }

    public async Task<IReadOnlyList<ServiceAttributeDto>> GetServiceAttributesAsync(
        string? providerCode,
        Guid? serviceOfferId,
        ServiceMode? serviceMode,
        CancellationToken cancellationToken = default)
    {
        var query = from attribute in dbContext.ServiceAttributes.AsNoTracking()
                    join offer in dbContext.ServiceOffers.AsNoTracking() on attribute.ServiceOfferId equals offer.Id
                    join provider in dbContext.Providers.AsNoTracking() on offer.ProviderId equals provider.Id
                    where attribute.IsActive
                    select new
                    {
                        Attribute = attribute,
                        OfferServiceMode = offer.ServiceMode,
                        ProviderCode = provider.Code
                    };

        if (!string.IsNullOrWhiteSpace(providerCode))
        {
            query = query.Where(x => x.ProviderCode == providerCode);
        }

        if (serviceOfferId.HasValue)
        {
            query = query.Where(x => x.Attribute.ServiceOfferId == serviceOfferId.Value);
        }

        if (serviceMode.HasValue)
        {
            query = query.Where(x => x.OfferServiceMode == serviceMode.Value);
        }

        return await query
            .OrderBy(x => x.Attribute.DisplayOrder)
            .ThenBy(x => x.Attribute.NameEn)
            .Select(x => MapServiceAttribute(x.Attribute))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProviderAttributeValueMapperDto>> GetProviderAttributeValueMappersAsync(
        string? providerCode,
        Guid? serviceOfferId,
        ServiceMode? serviceMode,
        CancellationToken cancellationToken = default)
    {
        var query = from mapper in dbContext.ProviderAttributeValueMappers.AsNoTracking()
                    join provider in dbContext.Providers.AsNoTracking() on mapper.ProviderId equals provider.Id
                    where mapper.IsActive
                    select new
                    {
                        Mapper = mapper,
                        ProviderCode = provider.Code
                    };

        if (!string.IsNullOrWhiteSpace(providerCode))
        {
            query = query.Where(x => x.ProviderCode == providerCode);
        }

        if (serviceOfferId.HasValue)
        {
            query = query.Where(x => x.Mapper.ServiceOfferId == serviceOfferId.Value || x.Mapper.ServiceOfferId == null);
        }

        if (serviceMode.HasValue)
        {
            query = query.Where(x => x.Mapper.ServiceMode == serviceMode.Value || x.Mapper.ServiceMode == null);
        }

        return await query
            .OrderBy(x => x.Mapper.ProviderId)
            .ThenByDescending(x => x.Mapper.ServiceOfferId.HasValue)
            .ThenByDescending(x => x.Mapper.ServiceMode.HasValue)
            .ThenBy(x => x.Mapper.RawAttributeKey)
            .ThenBy(x => x.Mapper.RawValue)
            .Select(x => MapProviderAttributeValueMapper(x.Mapper))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NormalizedPriceSnapshotDto>> GetNormalizedPriceSnapshotsAsync(
        string? providerCode,
        bool includeExpired = true,
        CancellationToken cancellationToken = default)
    {
        var providersQuery = dbContext.Providers.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(providerCode))
        {
            providersQuery = providersQuery.Where(x => x.Code == providerCode);
        }

        var providers = await providersQuery.ToListAsync(cancellationToken);
        if (providers.Count == 0)
        {
            return [];
        }

        var providerById = providers.ToDictionary(x => x.Id);
        var providerIds = providerById.Keys.ToList();

        var offers = await dbContext.ServiceOffers
            .AsNoTracking()
            .Where(x => providerIds.Contains(x.ProviderId))
            .ToListAsync(cancellationToken);
        var offerById = offers.ToDictionary(x => x.Id);
        var offerIds = offerById.Keys.ToList();

        var pricesQuery = dbContext.PriceSnapshots
            .AsNoTracking()
            .Where(x => providerIds.Contains(x.ProviderId));
        if (!includeExpired)
        {
            pricesQuery = pricesQuery.Where(x => x.ExpiresAtUtc > DateTime.UtcNow);
        }

        var prices = await pricesQuery
            .OrderByDescending(x => x.CollectedAtUtc)
            .Take(500)
            .ToListAsync(cancellationToken);

        if (prices.Count == 0)
        {
            return [];
        }

        var docsQuery = dbContext.ProviderJsonDocuments
            .AsNoTracking()
            .Where(x => providerIds.Contains(x.ProviderId) && offerIds.Contains(x.ServiceOfferId) && x.IsActive);
        if (!includeExpired)
        {
            docsQuery = docsQuery.Where(x => x.ExpiresAtUtc > DateTime.UtcNow);
        }

        var docs = await docsQuery
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var docsByOffer = docs
            .GroupBy(x => x.ServiceOfferId)
            .ToDictionary(x => x.Key, x => x.ToList());

        var mappers = await dbContext.ProviderAttributeValueMappers
            .AsNoTracking()
            .Where(x => providerIds.Contains(x.ProviderId) && x.IsActive)
            .ToListAsync(cancellationToken);

        var result = new List<NormalizedPriceSnapshotDto>(prices.Count);
        foreach (var price in prices)
        {
            if (!providerById.TryGetValue(price.ProviderId, out var provider))
            {
                continue;
            }

            if (!offerById.TryGetValue(price.ServiceOfferId, out var offer))
            {
                continue;
            }

            docsByOffer.TryGetValue(offer.Id, out var providerDocsForOffer);
            var rawJsonAttributes = ExtractJsonAttributeValues(providerDocsForOffer);
            var normalizedAttributes = NormalizeUsingMapperRules(rawJsonAttributes, mappers, provider.Id, offer);

            var vat = price.VatAmountSar ?? Math.Round(price.FinalPriceSar * 0.15m, 2);
            var priceWithVat = price.FinalPriceSar;
            var netPrice = Math.Max(0m, Math.Round(priceWithVat - vat, 2));

            result.Add(new NormalizedPriceSnapshotDto(
                price.Id,
                provider.Id,
                provider.Code,
                provider.NameAr,
                provider.NameEn,
                offer.Id,
                offer.ProviderServiceId,
                offer.ServiceMode,
                offer.NameAr,
                offer.NameEn,
                netPrice,
                vat,
                priceWithVat,
                price.SourceType,
                price.CollectedAtUtc,
                price.ExpiresAtUtc,
                rawJsonAttributes,
                normalizedAttributes));
        }

        return result;
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

    public async Task<IReadOnlyList<HomePromotionDto>> GetHomePromotionsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.HomePromotions.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        var rows = await query
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        return rows.Select(MapHomePromotion).ToList();
    }

    public async Task<CustomerServiceRequestDto> CreateCustomerServiceRequestAsync(
        Guid customerId,
        CreateCustomerServiceRequestPayload request,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty)
        {
            throw new InvalidOperationException("Customer is required.");
        }

        if (request.ServiceOfferId == Guid.Empty)
        {
            throw new InvalidOperationException("Service offer is required.");
        }

        if (request.ProviderId == Guid.Empty)
        {
            throw new InvalidOperationException("Provider is required.");
        }

        var customer = await dbContext.CustomerAccounts
            .FirstOrDefaultAsync(x => x.Id == customerId && x.IsActive, cancellationToken);
        if (customer is null)
        {
            throw new InvalidOperationException("Customer not found or inactive.");
        }

        var offer = await dbContext.ServiceOffers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.ServiceOfferId && x.ProviderId == request.ProviderId, cancellationToken);
        if (offer is null)
        {
            throw new InvalidOperationException("Selected package was not found for this provider.");
        }

        PriceSnapshot? priceSnapshot = null;
        if (request.PriceSnapshotId.HasValue)
        {
            priceSnapshot = await dbContext.PriceSnapshots
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == request.PriceSnapshotId.Value &&
                    x.ProviderId == request.ProviderId &&
                    x.ServiceOfferId == request.ServiceOfferId, cancellationToken);
        }

        if (priceSnapshot is null)
        {
            priceSnapshot = await dbContext.PriceSnapshots
                .AsNoTracking()
                .Where(x => x.ProviderId == request.ProviderId && x.ServiceOfferId == request.ServiceOfferId)
                .OrderByDescending(x => x.CollectedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var location = await ResolveLocationAsync(customer, request, cancellationToken);
        var normalizedAttributes = NormalizeRequestAttributes(request.PackageAttributes);

        var now = DateTime.UtcNow;
        var entity = new CustomerServiceRequest
        {
            CustomerAccountId = customer.Id,
            CustomerReference = customer.CustomerReference,
            CustomerSavedLocationId = location.Id == Guid.Empty ? null : location.Id,
            LocationLabel = location.Label,
            LocationCity = location.City,
            LocationDistrict = location.District,
            LocationLatitude = location.Latitude,
            LocationLongitude = location.Longitude,
            LocationGoogleMapsUrl = location.GoogleMapsUrl,
            LocationGooglePlaceId = location.GooglePlaceId,
            ProviderId = request.ProviderId,
            ServiceOfferId = request.ServiceOfferId,
            PriceSnapshotId = priceSnapshot?.Id,
            ServiceMode = offer.ServiceMode,
            PackageNameAr = offer.NameAr,
            PackageNameEn = offer.NameEn,
            FinalPriceSar = priceSnapshot?.FinalPriceSar ?? 0m,
            OriginalPriceSar = priceSnapshot?.OriginalPriceSar,
            VatAmountSar = priceSnapshot?.VatAmountSar,
            Currency = priceSnapshot?.Currency ?? "SAR",
            ServiceDate = NormalizeNullable(request.ServiceDate),
            SelectedShift = NormalizeNullable(request.Shift),
            SelectedNationality = NormalizeNullable(request.Nationality),
            SelectedContractDuration = NormalizeNullable(request.ContractDuration),
            SelectedWorkersCount = request.WorkersCount,
            SelectedHoursPerVisit = request.HoursPerVisit,
            SelectedWeeklyVisits = request.WeeklyVisits,
            SelectedDeliveryWindow = NormalizeNullable(request.DeliveryWindow),
            SelectedProviderSource = NormalizeNullable(request.ProviderSource),
            Notes = NormalizeNullable(request.Notes) ?? string.Empty,
            PackageAttributesJson = JsonSerializer.Serialize(normalizedAttributes),
            Status = "submitted",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await dbContext.CustomerServiceRequests.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapCustomerServiceRequest(entity);
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

    public async Task<HomePromotionDto> CreateHomePromotionAsync(
        CreateOrUpdateHomePromotionRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateHomePromotionRequest(request);

        var now = DateTime.UtcNow;
        var normalizedCode = NormalizePromotionCode(request.Code);
        var uniqueCode = await EnsureUniquePromotionCodeAsync(normalizedCode, null, cancellationToken);

        var entity = new HomePromotion
        {
            Code = uniqueCode,
            CompanyNameAr = request.CompanyNameAr.Trim(),
            CompanyNameEn = request.CompanyNameEn.Trim(),
            TitleAr = request.TitleAr.Trim(),
            TitleEn = request.TitleEn.Trim(),
            SubtitleAr = request.SubtitleAr.Trim(),
            SubtitleEn = request.SubtitleEn.Trim(),
            ImageUrl = request.ImageUrl.Trim(),
            TargetUrl = NormalizeNullable(request.TargetUrl),
            DeepLink = NormalizeNullable(request.DeepLink),
            ItemsJson = SerializePromotionItems(request.Items),
            ProviderCode = NormalizeNullable(request.ProviderCode),
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await dbContext.HomePromotions.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapHomePromotion(entity);
    }

    public async Task<HomePromotionDto?> UpdateHomePromotionAsync(
        Guid promotionId,
        CreateOrUpdateHomePromotionRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateHomePromotionRequest(request);

        var entity = await dbContext.HomePromotions
            .FirstOrDefaultAsync(x => x.Id == promotionId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var normalizedCode = string.IsNullOrWhiteSpace(request.Code)
            ? entity.Code
            : NormalizePromotionCode(request.Code);
        var uniqueCode = await EnsureUniquePromotionCodeAsync(normalizedCode, promotionId, cancellationToken);

        entity.Code = uniqueCode;
        entity.CompanyNameAr = request.CompanyNameAr.Trim();
        entity.CompanyNameEn = request.CompanyNameEn.Trim();
        entity.TitleAr = request.TitleAr.Trim();
        entity.TitleEn = request.TitleEn.Trim();
        entity.SubtitleAr = request.SubtitleAr.Trim();
        entity.SubtitleEn = request.SubtitleEn.Trim();
        entity.ImageUrl = request.ImageUrl.Trim();
        entity.TargetUrl = NormalizeNullable(request.TargetUrl);
        entity.DeepLink = NormalizeNullable(request.DeepLink);
        entity.ItemsJson = SerializePromotionItems(request.Items);
        entity.ProviderCode = NormalizeNullable(request.ProviderCode);
        entity.DisplayOrder = request.DisplayOrder;
        entity.IsActive = request.IsActive;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return MapHomePromotion(entity);
    }

    public async Task<bool> DeleteHomePromotionAsync(Guid promotionId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.HomePromotions
            .FirstOrDefaultAsync(x => x.Id == promotionId, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        dbContext.HomePromotions.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static HomePromotionDto MapHomePromotion(HomePromotion entity)
    {
        return new HomePromotionDto(
            entity.Id,
            entity.Code,
            entity.CompanyNameAr,
            entity.CompanyNameEn,
            entity.TitleAr,
            entity.TitleEn,
            entity.SubtitleAr,
            entity.SubtitleEn,
            entity.ImageUrl,
            entity.TargetUrl,
            entity.DeepLink,
            ParsePromotionItems(entity.ItemsJson),
            entity.ProviderCode,
            entity.DisplayOrder,
            entity.IsActive,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
    }

    private static CustomerServiceRequestDto MapCustomerServiceRequest(CustomerServiceRequest entity)
    {
        IReadOnlyList<CustomerRequestAttributeValueDto> packageAttributes;
        try
        {
            packageAttributes = JsonSerializer.Deserialize<List<CustomerRequestAttributeValueDto>>(entity.PackageAttributesJson) ?? [];
        }
        catch
        {
            packageAttributes = [];
        }

        return new CustomerServiceRequestDto(
            entity.Id,
            entity.CustomerAccountId,
            entity.CustomerReference,
            entity.CustomerSavedLocationId,
            entity.LocationLabel,
            entity.LocationCity,
            entity.LocationDistrict,
            entity.LocationLatitude,
            entity.LocationLongitude,
            entity.LocationGoogleMapsUrl,
            entity.LocationGooglePlaceId,
            entity.ProviderId,
            entity.ServiceOfferId,
            entity.PriceSnapshotId,
            entity.ServiceMode,
            entity.PackageNameAr,
            entity.PackageNameEn,
            entity.FinalPriceSar,
            entity.OriginalPriceSar,
            entity.VatAmountSar,
            entity.Currency,
            entity.ServiceDate,
            entity.SelectedShift,
            entity.SelectedNationality,
            entity.SelectedContractDuration,
            entity.SelectedWorkersCount,
            entity.SelectedHoursPerVisit,
            entity.SelectedWeeklyVisits,
            entity.SelectedDeliveryWindow,
            entity.SelectedProviderSource,
            entity.Notes,
            packageAttributes,
            entity.Status,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);
    }

    private static IReadOnlyList<CustomerRequestAttributeValueDto> NormalizeRequestAttributes(
        IReadOnlyList<CustomerRequestAttributeValueDto>? source)
    {
        return (source ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x.AttributeKey) && !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => new CustomerRequestAttributeValueDto(
                x.AttributeKey.Trim(),
                (x.AttributeNameAr ?? string.Empty).Trim(),
                (x.AttributeNameEn ?? string.Empty).Trim(),
                x.Value.Trim(),
                (x.ValueAr ?? x.Value).Trim(),
                (x.ValueEn ?? x.Value).Trim()))
            .GroupBy(x => $"{x.AttributeKey}::{x.Value}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .Take(100)
            .ToList();
    }

    private async Task<CustomerSavedLocation> ResolveLocationAsync(
        CustomerAccount customer,
        CreateCustomerServiceRequestPayload request,
        CancellationToken cancellationToken)
    {
        if (request.LocationId.HasValue)
        {
            var byId = await dbContext.CustomerSavedLocations
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.Id == request.LocationId.Value &&
                    x.CustomerReference == customer.CustomerReference, cancellationToken);
            if (byId is not null)
            {
                return byId;
            }
        }

        var payloadLabel = NormalizeNullable(request.LocationLabel);
        var payloadCity = NormalizeNullable(request.LocationCity);
        var payloadDistrict = NormalizeNullable(request.LocationDistrict);
        if (!string.IsNullOrWhiteSpace(payloadLabel) &&
            !string.IsNullOrWhiteSpace(payloadCity) &&
            !string.IsNullOrWhiteSpace(payloadDistrict))
        {
            var safeLatitude = request.LocationLatitude is > -90m and < 90m ? request.LocationLatitude : null;
            var safeLongitude = request.LocationLongitude is > -180m and < 180m ? request.LocationLongitude : null;
            return new CustomerSavedLocation
            {
                Id = request.LocationId ?? Guid.Empty,
                CustomerReference = customer.CustomerReference,
                Label = payloadLabel,
                City = payloadCity,
                District = payloadDistrict,
                Latitude = safeLatitude ?? 24.7136m,
                Longitude = safeLongitude ?? 46.6753m,
                GoogleMapsUrl = NormalizeNullable(request.LocationGoogleMapsUrl),
                GooglePlaceId = NormalizeNullable(request.LocationGooglePlaceId),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        var latest = await dbContext.CustomerSavedLocations
            .AsNoTracking()
            .Where(x => x.CustomerReference == customer.CustomerReference)
            .OrderByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (latest is not null)
        {
            return latest;
        }

        var normalizedMobile = customer.NormalizedMobileNumber.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedMobile))
        {
            var suffixChars = normalizedMobile.Where(char.IsDigit).TakeLast(4).ToArray();
            var suffix = suffixChars.Length > 0 ? new string(suffixChars) : "0000";
            return new CustomerSavedLocation
            {
                Id = Guid.Empty,
                CustomerReference = customer.CustomerReference,
                Label = $"Auto Address {suffix}",
                City = "Riyadh",
                District = "Not Specified",
                Latitude = 24.7136m,
                Longitude = 46.6753m,
                GoogleMapsUrl = null,
                GooglePlaceId = null,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        return new CustomerSavedLocation
        {
            Id = Guid.Empty,
            CustomerReference = customer.CustomerReference,
            Label = "Auto Address",
            City = "Riyadh",
            District = "Not Specified",
            Latitude = 24.7136m,
            Longitude = 46.6753m,
            GoogleMapsUrl = null,
            GooglePlaceId = null,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    private static ServiceAttributeDto MapServiceAttribute(ServiceAttribute entity)
    {
        return new ServiceAttributeDto(
            entity.Id,
            entity.ServiceOfferId,
            entity.AttributeKey,
            entity.NameAr,
            entity.NameEn,
            entity.Type,
            entity.OptionSetJson,
            entity.IsMandatory,
            entity.FilterScope,
            entity.DisplayOrder,
            entity.IsActive,
            entity.UpdatedAtUtc);
    }

    private static ProviderAttributeValueMapperDto MapProviderAttributeValueMapper(ProviderAttributeValueMapper entity)
    {
        return new ProviderAttributeValueMapperDto(
            entity.Id,
            entity.ProviderId,
            entity.ServiceOfferId,
            entity.ServiceMode,
            entity.RawAttributeKey,
            entity.RawValue,
            entity.RawTextEn,
            entity.RawTextAr,
            entity.NormalizedAttributeKey,
            entity.NormalizedValue,
            entity.NormalizedTextEn,
            entity.NormalizedTextAr,
            entity.IsActive,
            entity.UpdatedAtUtc);
    }

    private static IReadOnlyList<JsonAttributeValueDto> ExtractJsonAttributeValues(IReadOnlyList<ProviderJsonDocument>? providerDocs)
    {
        if (providerDocs is null || providerDocs.Count == 0)
        {
            return [];
        }

        var values = new List<JsonAttributeValueDto>(capacity: 256);
        foreach (var providerDoc in providerDocs)
        {
            if (providerDoc is null || string.IsNullOrWhiteSpace(providerDoc.JsonData))
            {
                continue;
            }

            try
            {
                using var parsed = JsonDocument.Parse(providerDoc.JsonData);
                CollectJsonLeafValues(parsed.RootElement, null, values);
            }
            catch
            {
                // Ignore malformed provider payload and continue with other docs.
            }
        }

        return values
            .Where(x => !string.IsNullOrWhiteSpace(x.AttributeKey) && !string.IsNullOrWhiteSpace(x.RawValue))
            .GroupBy(x => $"{x.AttributeKey}||{x.RawValue}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderByDescending(x => x.AttributeKey.EndsWith("id", StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => x.AttributeKey)
            .ThenBy(x => x.RawValue)
            .Take(500)
            .ToList();
    }

    private static IReadOnlyList<NormalizedAttributeValueDto> NormalizeUsingMapperRules(
        IReadOnlyList<JsonAttributeValueDto> rawAttributes,
        IReadOnlyList<ProviderAttributeValueMapper> allMappers,
        Guid providerId,
        ServiceOffer offer)
    {
        if (rawAttributes.Count == 0)
        {
            return [];
        }

        var providerMappers = allMappers
            .Where(x => x.ProviderId == providerId && x.IsActive)
            .ToList();
        if (providerMappers.Count == 0)
        {
            return [];
        }

        var normalized = new List<NormalizedAttributeValueDto>();
        foreach (var raw in rawAttributes)
        {
            var matched = providerMappers
                .Where(x =>
                    string.Equals(x.RawAttributeKey, raw.AttributeKey, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(NormalizeComparisonToken(x.RawValue), NormalizeComparisonToken(raw.RawValue), StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.ServiceOfferId == offer.Id)
                .ThenByDescending(x => x.ServiceMode == offer.ServiceMode)
                .ThenByDescending(x => x.ServiceOfferId.HasValue)
                .ThenByDescending(x => x.ServiceMode.HasValue)
                .FirstOrDefault();

            if (matched is null)
            {
                continue;
            }

            normalized.Add(new NormalizedAttributeValueDto(
                matched.NormalizedAttributeKey,
                matched.NormalizedValue,
                matched.NormalizedTextEn,
                matched.NormalizedTextAr,
                raw.AttributeKey,
                raw.RawValue,
                string.IsNullOrWhiteSpace(matched.RawTextEn) ? raw.TextEn : matched.RawTextEn,
                string.IsNullOrWhiteSpace(matched.RawTextAr) ? raw.TextAr : matched.RawTextAr));
        }

        return normalized
            .GroupBy(x => $"{x.AttributeKey}||{x.Value}||{x.RawAttributeKey}||{x.RawValue}", StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.AttributeKey)
            .ThenBy(x => x.Value)
            .ToList();
    }

    private static void CollectJsonLeafValues(
        JsonElement element,
        string? currentPath,
        List<JsonAttributeValueDto> sink)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var nextPath = string.IsNullOrWhiteSpace(currentPath)
                        ? property.Name
                        : $"{currentPath}.{property.Name}";
                    CollectJsonLeafValues(property.Value, nextPath, sink);
                }
                break;
            case JsonValueKind.Array:
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                    {
                        continue;
                    }

                    var stringValue = ParseJsonNodePrimitiveToString(item);
                    if (string.IsNullOrWhiteSpace(stringValue) || string.IsNullOrWhiteSpace(currentPath))
                    {
                        continue;
                    }

                    var rawKey = currentPath;
                    if (!LooksLikeRawAttributeKey(rawKey))
                    {
                        continue;
                    }

                    sink.Add(new JsonAttributeValueDto(
                        rawKey,
                        stringValue,
                        stringValue,
                        null));
                }
                break;
            }
            default:
            {
                var stringValue = ParseJsonNodePrimitiveToString(element);
                if (string.IsNullOrWhiteSpace(stringValue) || string.IsNullOrWhiteSpace(currentPath))
                {
                    break;
                }

                var rawKey = currentPath;
                if (!LooksLikeRawAttributeKey(rawKey))
                {
                    break;
                }

                sink.Add(new JsonAttributeValueDto(
                    rawKey,
                    stringValue,
                    stringValue,
                    null));
                break;
            }
        }
    }

    private static bool LooksLikeRawAttributeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var key = value.ToLowerInvariant();
        return key.Contains("shift", StringComparison.Ordinal) ||
               key.Contains("resourcegroup", StringComparison.Ordinal) ||
               key.Contains("nationality", StringComparison.Ordinal) ||
               key.Contains("duration", StringComparison.Ordinal) ||
               key.Contains("hours", StringComparison.Ordinal) ||
               key.Contains("visit", StringComparison.Ordinal) ||
               key.Contains("employee", StringComparison.Ordinal) ||
               key.Contains("worker", StringComparison.Ordinal) ||
               key.Contains("delivery", StringComparison.Ordinal) ||
               key.Contains("method", StringComparison.Ordinal) ||
               key.Contains("date", StringComparison.Ordinal) ||
               key.Contains("id", StringComparison.Ordinal);
    }

    private static string? ParseJsonNodePrimitiveToString(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()?.Trim(),
            JsonValueKind.Number => value.TryGetDecimal(out var n)
                ? n.ToString(CultureInfo.InvariantCulture)
                : value.ToString(),
            JsonValueKind.True => bool.TrueString.ToLowerInvariant(),
            JsonValueKind.False => bool.FalseString.ToLowerInvariant(),
            _ => null
        };
    }

    private static string NormalizeComparisonToken(string? value)
    {
        var token = (value ?? string.Empty).Trim();
        if (decimal.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
        {
            token = number.ToString("0.################", CultureInfo.InvariantCulture);
        }

        return token.ToLowerInvariant();
    }

    private static IReadOnlyList<string> ParsePromotionItems(string itemsJson)
    {
        if (string.IsNullOrWhiteSpace(itemsJson))
        {
            return [];
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(itemsJson) ?? [];
            return parsed
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string SerializePromotionItems(IReadOnlyList<string>? items)
    {
        var normalized = (items ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
        return JsonSerializer.Serialize(normalized);
    }

    private static string? NormalizeNullable(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static ProviderCollectionCredentials ExtractProviderCredentials(string? settingsJson)
    {
        return ProviderSettingsHelper.ExtractProviderCredentials(settingsJson);
    }

    private static IReadOnlyList<int> ParseHourOptions(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return [];
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<int>>(source) ?? [];
            return parsed
                .Where(x => x > 0 && x <= 24)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<string> ParseStringList(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return [];
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<List<string>>(source) ?? [];
            return parsed
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string SerializeIntList(IEnumerable<int> values)
    {
        var normalized = values
            .Where(x => x > 0 && x <= 24)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        return JsonSerializer.Serialize(normalized);
    }

    private static string SerializeStringList(IEnumerable<string> values)
    {
        var normalized = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return JsonSerializer.Serialize(normalized);
    }

    private static string NormalizePromotionCode(string? code)
    {
        var raw = (code ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = $"promo-{Guid.NewGuid():N}"[..14];
        }

        var normalizedChars = raw.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
        var normalized = new string(normalizedChars).Trim('-');
        while (normalized.Contains("--", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        }

        if (normalized.Length > MaxPromotionCodeLength)
        {
            normalized = normalized[..MaxPromotionCodeLength];
        }

        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = $"promo-{Guid.NewGuid():N}"[..14];
        }

        return normalized;
    }

    private async Task<string> EnsureUniquePromotionCodeAsync(
        string requestedCode,
        Guid? excludingPromotionId,
        CancellationToken cancellationToken)
    {
        var code = requestedCode;
        var suffix = 2;
        while (await dbContext.HomePromotions.AnyAsync(
            x => x.Code == code && (!excludingPromotionId.HasValue || x.Id != excludingPromotionId.Value),
            cancellationToken))
        {
            var suffixText = $"-{suffix}";
            var maxBaseLength = Math.Max(1, MaxPromotionCodeLength - suffixText.Length);
            var baseCode = requestedCode.Length > maxBaseLength ? requestedCode[..maxBaseLength] : requestedCode;
            code = $"{baseCode}{suffixText}";
            suffix++;
        }

        return code;
    }

    private static void ValidateHomePromotionRequest(CreateOrUpdateHomePromotionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TitleAr) || string.IsNullOrWhiteSpace(request.TitleEn))
        {
            throw new InvalidOperationException("Arabic and English title are required.");
        }

        if (string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            throw new InvalidOperationException("Image URL is required.");
        }
    }
}

internal sealed class CustomerAuthService(
    BelkhedmaDbContext dbContext,
    UserManager<IdentityUser> userManager) : ICustomerAuthService
{
    private const string CustomerReferenceClaimType = "customer_reference";
    private const string CustomerIdClaimType = "customer_id";
    private const string CustomerMobileClaimType = "customer_mobile";
    private const string CustomerFullNameClaimType = "customer_full_name";
    private const int AccessTokenLifetimeMinutes = 30;
    private const int RefreshTokenLifetimeDays = 30;

    public async Task<CustomerAuthResponse> RegisterAsync(
        CustomerRegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        return await RegisterCoreAsync(
            request.Email,
            request.MobileNumber,
            request.FullName,
            request.Password,
            cancellationToken);
    }

    public async Task<CustomerAuthResponse> LoginAsync(
        CustomerLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var loginIdentity = (request.UserNameOrEmail ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(loginIdentity))
        {
            throw new InvalidOperationException("Username/email is required.");
        }

        var password = (request.Password ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Password is required.");
        }

        var identityUser = await userManager.FindByEmailAsync(loginIdentity);
        if (identityUser is null)
        {
            identityUser = await userManager.FindByNameAsync(loginIdentity);
        }

        if (identityUser is null)
        {
            throw new InvalidOperationException("Invalid username/email or password.");
        }

        var validPassword = await userManager.CheckPasswordAsync(identityUser, password);
        if (!validPassword)
        {
            throw new InvalidOperationException("Invalid username/email or password.");
        }

        var customer = await GetOrCreateCustomerAccountForIdentityUserAsync(identityUser, cancellationToken);
        customer.IsActive = true;
        var now = DateTime.UtcNow;
        customer.UpdatedAtUtc = now;

        var refreshToken = GenerateRefreshToken();
        await dbContext.CustomerAuthSessions.AddAsync(new CustomerAuthSession
        {
            CustomerAccountId = customer.Id,
            RefreshToken = refreshToken,
            CreatedAtUtc = now,
            LastUsedAtUtc = now,
            ExpiresAtUtc = now.AddDays(RefreshTokenLifetimeDays),
            IsRevoked = false
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildCustomerAuthResponse(customer, identityUser, isNewAccount: false, refreshToken, now);
    }

    public async Task<CustomerAuthResponse> RegisterOrLoginAsync(
        CustomerAuthRequest request,
        CancellationToken cancellationToken = default)
    {
        var userNameOrEmail = (request.UserNameOrEmail ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(userNameOrEmail))
        {
            return await LoginAsync(new CustomerLoginRequest(userNameOrEmail, request.Password), cancellationToken);
        }

        var email = (request.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            var normalizedMobile = NormalizeMobile(request.MobileNumber);
            if (string.IsNullOrWhiteSpace(normalizedMobile))
            {
                throw new InvalidOperationException("Email or mobile number is required.");
            }

            var emailLocal = normalizedMobile.TrimStart('+').Replace(" ", string.Empty, StringComparison.Ordinal);
            email = $"m-{emailLocal}@belkhedma.local";
        }

        var mobile = request.MobileNumber ?? string.Empty;
        var fullName = request.FullName ?? string.Empty;
        return await RegisterCoreAsync(email, mobile, fullName, request.Password, cancellationToken);
    }

    public async Task<CustomerAuthResponse> RefreshTokenAsync(
        CustomerTokenRefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = (request.RefreshToken ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidOperationException("Refresh token is required.");
        }

        var now = DateTime.UtcNow;
        var session = await dbContext.CustomerAuthSessions
            .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken, cancellationToken);
        if (session is null ||
            session.IsRevoked ||
            session.RevokedAtUtc.HasValue ||
            session.ExpiresAtUtc <= now)
        {
            throw new InvalidOperationException("Invalid or expired refresh token.");
        }

        var customer = await dbContext.CustomerAccounts
            .FirstOrDefaultAsync(x => x.Id == session.CustomerAccountId && x.IsActive, cancellationToken);
        if (customer is null)
        {
            throw new InvalidOperationException("Customer account not found or inactive.");
        }

        var identityUser = await FindIdentityUserForCustomerAsync(customer, cancellationToken);
        if (identityUser is null)
        {
            throw new InvalidOperationException("Identity user not found for customer.");
        }

        var newRefreshToken = GenerateRefreshToken();
        var newSession = new CustomerAuthSession
        {
            CustomerAccountId = customer.Id,
            RefreshToken = newRefreshToken,
            CreatedAtUtc = now,
            LastUsedAtUtc = now,
            ExpiresAtUtc = now.AddDays(RefreshTokenLifetimeDays),
            IsRevoked = false
        };

        session.IsRevoked = true;
        session.RevokedAtUtc = now;
        session.ReplacedByRefreshToken = newRefreshToken;
        session.LastUsedAtUtc = now;

        await dbContext.CustomerAuthSessions.AddAsync(newSession, cancellationToken);
        customer.UpdatedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildCustomerAuthResponse(customer, identityUser, isNewAccount: false, refreshToken: newRefreshToken, issuedAtUtc: now);
    }

    public async Task RevokeRefreshTokenAsync(
        CustomerTokenRevokeRequest request,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = (request.RefreshToken ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var session = await dbContext.CustomerAuthSessions
            .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken, cancellationToken);
        if (session is null || session.IsRevoked)
        {
            return;
        }

        var now = DateTime.UtcNow;
        session.IsRevoked = true;
        session.RevokedAtUtc = now;
        session.LastUsedAtUtc = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<CustomerAuthResponse> RegisterCoreAsync(
        string email,
        string mobileNumber,
        string fullName,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw new InvalidOperationException("Email is required.");
        }

        var normalizedMobile = NormalizeMobile(mobileNumber);
        if (string.IsNullOrWhiteSpace(normalizedMobile))
        {
            throw new InvalidOperationException("Valid mobile number is required.");
        }

        var normalizedFullName = (fullName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedFullName))
        {
            throw new InvalidOperationException("Full name is required.");
        }

        var normalizedPassword = (password ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedPassword))
        {
            throw new InvalidOperationException("Password is required.");
        }

        using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var existingUser = await userManager.FindByEmailAsync(normalizedEmail);
        var isNewAccount = false;
        IdentityUser identityUser;
        if (existingUser is null)
        {
            isNewAccount = true;
            identityUser = new IdentityUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                EmailConfirmed = true,
                PhoneNumber = normalizedMobile,
                PhoneNumberConfirmed = true
            };

            var createResult = await userManager.CreateAsync(identityUser, normalizedPassword);
            if (!createResult.Succeeded)
            {
                var message = createResult.Errors.FirstOrDefault()?.Description ?? "Failed to create customer account.";
                throw new InvalidOperationException(message);
            }
        }
        else
        {
            identityUser = existingUser;

            var validPassword = await userManager.CheckPasswordAsync(identityUser, normalizedPassword);
            if (!validPassword)
            {
                throw new InvalidOperationException("Invalid password for existing account.");
            }
        }

        var customerAccount = await GetOrCreateCustomerAccountForIdentityUserAsync(identityUser, cancellationToken);
        customerAccount.FullName = normalizedFullName;
        customerAccount.MobileNumber = mobileNumber.Trim();
        customerAccount.NormalizedMobileNumber = normalizedMobile;
        customerAccount.IsActive = true;
        customerAccount.UpdatedAtUtc = DateTime.UtcNow;

        await UpsertIdentityClaimsAsync(identityUser, customerAccount, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var refreshToken = GenerateRefreshToken();
        await dbContext.CustomerAuthSessions.AddAsync(new CustomerAuthSession
        {
            CustomerAccountId = customerAccount.Id,
            RefreshToken = refreshToken,
            CreatedAtUtc = DateTime.UtcNow,
            LastUsedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays),
            IsRevoked = false
        }, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildCustomerAuthResponse(customerAccount, identityUser, isNewAccount, refreshToken);
    }

    private async Task<CustomerAccount> GetOrCreateCustomerAccountForIdentityUserAsync(
        IdentityUser identityUser,
        CancellationToken cancellationToken)
    {
        var customerIdClaim = await userManager.GetClaimsAsync(identityUser);
        var linkedCustomerId = customerIdClaim
            .FirstOrDefault(x => x.Type == CustomerIdClaimType)
            ?.Value;
        if (Guid.TryParse(linkedCustomerId, out var parsedCustomerId))
        {
            var linkedAccount = await dbContext.CustomerAccounts
                .FirstOrDefaultAsync(x => x.Id == parsedCustomerId, cancellationToken);
            if (linkedAccount is not null)
            {
                return linkedAccount;
            }
        }

        var byEmailReference = await dbContext.CustomerAccounts
            .FirstOrDefaultAsync(x => x.CustomerReference == $"idn-{identityUser.Id}".ToLowerInvariant(), cancellationToken);
        if (byEmailReference is not null)
        {
            return byEmailReference;
        }

        var normalizedPhone = NormalizeMobile(identityUser.PhoneNumber);
        if (!string.IsNullOrWhiteSpace(normalizedPhone))
        {
            var byPhone = await dbContext.CustomerAccounts
                .FirstOrDefaultAsync(x => x.NormalizedMobileNumber == normalizedPhone, cancellationToken);
            if (byPhone is not null)
            {
                byPhone.CustomerReference = $"idn-{identityUser.Id}".ToLowerInvariant();
                byPhone.UpdatedAtUtc = DateTime.UtcNow;
                return byPhone;
            }
        }

        var now = DateTime.UtcNow;
        var fallbackMobile = normalizedPhone;
        if (string.IsNullOrWhiteSpace(fallbackMobile))
        {
            fallbackMobile = $"+9665{Random.Shared.Next(10000000, 99999999)}";
        }

        var account = new CustomerAccount
        {
            CustomerReference = $"idn-{identityUser.Id}".ToLowerInvariant(),
            FullName = identityUser.UserName ?? identityUser.Email ?? "Customer",
            MobileNumber = identityUser.PhoneNumber ?? fallbackMobile,
            NormalizedMobileNumber = fallbackMobile,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        await dbContext.CustomerAccounts.AddAsync(account, cancellationToken);
        return account;
    }

    private async Task UpsertIdentityClaimsAsync(
        IdentityUser identityUser,
        CustomerAccount customerAccount,
        CancellationToken cancellationToken)
    {
        var claims = await userManager.GetClaimsAsync(identityUser);
        await UpsertIdentityClaimAsync(identityUser, claims, CustomerIdClaimType, customerAccount.Id.ToString(), cancellationToken);
        await UpsertIdentityClaimAsync(identityUser, claims, CustomerReferenceClaimType, customerAccount.CustomerReference, cancellationToken);
        await UpsertIdentityClaimAsync(identityUser, claims, CustomerMobileClaimType, customerAccount.MobileNumber, cancellationToken);
        await UpsertIdentityClaimAsync(identityUser, claims, CustomerFullNameClaimType, customerAccount.FullName, cancellationToken);
    }

    private async Task UpsertIdentityClaimAsync(
        IdentityUser identityUser,
        IList<System.Security.Claims.Claim> claims,
        string claimType,
        string claimValue,
        CancellationToken cancellationToken)
    {
        var existing = claims.FirstOrDefault(x => x.Type == claimType);
        if (existing is null)
        {
            var addResult = await userManager.AddClaimAsync(identityUser, new System.Security.Claims.Claim(claimType, claimValue));
            if (!addResult.Succeeded)
            {
                var message = addResult.Errors.FirstOrDefault()?.Description ?? $"Failed to add claim '{claimType}'.";
                throw new InvalidOperationException(message);
            }
            return;
        }

        if (string.Equals(existing.Value, claimValue, StringComparison.Ordinal))
        {
            return;
        }

        var replaceResult = await userManager.ReplaceClaimAsync(identityUser, existing, new System.Security.Claims.Claim(claimType, claimValue));
        if (!replaceResult.Succeeded)
        {
            var message = replaceResult.Errors.FirstOrDefault()?.Description ?? $"Failed to update claim '{claimType}'.";
            throw new InvalidOperationException(message);
        }
    }

    private async Task<IdentityUser?> FindIdentityUserForCustomerAsync(
        CustomerAccount customer,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(customer.CustomerReference) &&
            customer.CustomerReference.StartsWith("idn-", StringComparison.OrdinalIgnoreCase))
        {
            var identityUserId = customer.CustomerReference["idn-".Length..];
            if (!string.IsNullOrWhiteSpace(identityUserId))
            {
                return await userManager.FindByIdAsync(identityUserId);
            }
        }

        if (!string.IsNullOrWhiteSpace(customer.NormalizedMobileNumber))
        {
            var byPhone = await userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PhoneNumber == customer.NormalizedMobileNumber, cancellationToken);
            if (byPhone is not null)
            {
                return byPhone;
            }
        }

        return null;
    }

    private static CustomerAuthResponse BuildCustomerAuthResponse(
        CustomerAccount customer,
        IdentityUser identityUser,
        bool isNewAccount,
        string refreshToken,
        DateTime? issuedAtUtc = null)
    {
        var now = issuedAtUtc ?? DateTime.UtcNow;
        return new CustomerAuthResponse(
            customer.Id,
            customer.CustomerReference,
            customer.FullName,
            customer.MobileNumber,
            identityUser.Id,
            refreshToken,
            identityUser.Email ?? string.Empty,
            now.AddMinutes(AccessTokenLifetimeMinutes),
            isNewAccount);
    }

    private static string NormalizeMobile(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile))
        {
            return string.Empty;
        }

        var chars = mobile.Trim()
            .Where(ch => char.IsDigit(ch) || ch == '+')
            .ToArray();

        return new string(chars);
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", string.Empty, StringComparison.Ordinal)
            .Replace("/", string.Empty, StringComparison.Ordinal)
            .Replace("=", string.Empty, StringComparison.Ordinal)
            + Guid.NewGuid().ToString("N");
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

        // Provider credentials are loaded from settings and can be used by
        // real provider adapters when fetching fresh JSON/prices.
        var providerCredentials = ProviderSettingsHelper.ExtractProviderCredentials(provider.SettingsJson);
        var providerUsername = providerCredentials.Username;
        var providerPassword = providerCredentials.Password;

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
                    NameEn = provider.NameEn + " - Hourly Service",
                    DisplayOrder = 0,
                    HourlyHoursJson = SerializeIntList([4]),
                    NationalityGroupsJson = SerializeStringList(["Africa", "Philippines", "Indonesia"])
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
                if (string.IsNullOrWhiteSpace(offer.HourlyHoursJson))
                {
                    offer.HourlyHoursJson = SerializeIntList([4]);
                }
                if (string.IsNullOrWhiteSpace(offer.NationalityGroupsJson))
                {
                    offer.NationalityGroupsJson = SerializeStringList(["Africa", "Philippines", "Indonesia"]);
                }
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
                  "hasCollectionCredentials": {{(string.IsNullOrWhiteSpace(providerUsername) || string.IsNullOrWhiteSpace(providerPassword) ? "false" : "true")}},
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

    private static string SerializeIntList(IEnumerable<int> values)
    {
        var normalized = values
            .Where(x => x > 0 && x <= 24)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        return JsonSerializer.Serialize(normalized);
    }

    private static string SerializeStringList(IEnumerable<string> values)
    {
        var normalized = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return JsonSerializer.Serialize(normalized);
    }
}

internal static class ProviderSettingsHelper
{
    private static readonly JsonSerializerOptions ProviderSettingsJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ProviderCollectionCredentials ExtractProviderCredentials(string? settingsJson)
    {
        if (string.IsNullOrWhiteSpace(settingsJson))
        {
            return new ProviderCollectionCredentials(null, null);
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<ProviderSettingsEnvelope>(settingsJson, ProviderSettingsJsonOptions);
            if (envelope?.CollectionCredentials is not null)
            {
                return new ProviderCollectionCredentials(
                    Normalize(envelope.CollectionCredentials.Username),
                    Normalize(envelope.CollectionCredentials.Password));
            }

            var direct = JsonSerializer.Deserialize<ProviderCollectionCredentials>(settingsJson, ProviderSettingsJsonOptions);
            if (direct is null)
            {
                return new ProviderCollectionCredentials(null, null);
            }

            return new ProviderCollectionCredentials(
                Normalize(direct.Username),
                Normalize(direct.Password));
        }
        catch
        {
            return new ProviderCollectionCredentials(null, null);
        }
    }

    private static string? Normalize(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}

internal sealed record ProviderCollectionCredentials(
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("password")] string? Password);

internal sealed record ProviderSettingsEnvelope(
    [property: JsonPropertyName("collectionCredentials")] ProviderCollectionCredentials? CollectionCredentials = null);
