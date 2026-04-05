using Belkhedma.Application;
using Belkhedma.Domain;
using Belkhedma.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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

        return services;
    }
}

internal sealed class MarketplaceQueryService(BelkhedmaDbContext dbContext) : IMarketplaceQueryService, IMarketplaceAdminService
{
    private const int MaxPromotionCodeLength = 80;
    private static readonly JsonSerializerOptions ProviderSettingsJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<CustomerProfileDto?> GetCustomerProfileByTokenAsync(
        string authToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(authToken))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var session = await dbContext.CustomerAuthSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.AuthToken == authToken &&
                !x.IsRevoked &&
                x.ExpiresAtUtc > now, cancellationToken);

        if (session is null)
        {
            return null;
        }

        var customer = await dbContext.CustomerAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == session.CustomerAccountId && x.IsActive, cancellationToken);

        if (customer is null)
        {
            return null;
        }

        return new CustomerProfileDto(
            customer.Id,
            customer.CustomerReference,
            customer.FullName,
            customer.MobileNumber);
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
                x.IsAvailable,
                x.UpdatedAtUtc))
            .ToList();
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

internal sealed class CustomerAuthService(BelkhedmaDbContext dbContext) : ICustomerAuthService
{
    public async Task<CustomerAuthResponse> RegisterOrLoginAsync(
        CustomerAuthRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedMobile = NormalizeMobile(request.MobileNumber);
        if (string.IsNullOrWhiteSpace(normalizedMobile))
        {
            throw new InvalidOperationException("Valid mobile number is required.");
        }

        var fullName = (request.FullName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("Full name is required.");
        }

        var now = DateTime.UtcNow;
        var customer = await dbContext.CustomerAccounts
            .FirstOrDefaultAsync(x => x.NormalizedMobileNumber == normalizedMobile, cancellationToken);

        var isNewAccount = false;
        if (customer is null)
        {
            isNewAccount = true;
            customer = new CustomerAccount
            {
                CustomerReference = $"cust-{Guid.NewGuid():N}"[..18],
                FullName = fullName,
                MobileNumber = request.MobileNumber.Trim(),
                NormalizedMobileNumber = normalizedMobile,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await dbContext.CustomerAccounts.AddAsync(customer, cancellationToken);
        }
        else
        {
            customer.FullName = fullName;
            customer.MobileNumber = request.MobileNumber.Trim();
            customer.UpdatedAtUtc = now;
            customer.IsActive = true;
        }

        var token = GenerateAuthToken();
        var expiresAtUtc = now.AddDays(30);
        await dbContext.CustomerAuthSessions.AddAsync(new CustomerAuthSession
        {
            CustomerAccountId = customer.Id,
            AuthToken = token,
            CreatedAtUtc = now,
            LastUsedAtUtc = now,
            ExpiresAtUtc = expiresAtUtc,
            IsRevoked = false
        }, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CustomerAuthResponse(
            customer.Id,
            customer.CustomerReference,
            customer.FullName,
            customer.MobileNumber,
            token,
            expiresAtUtc,
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

    private static string GenerateAuthToken()
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
