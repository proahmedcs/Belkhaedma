using Belkhedma.Domain;

namespace Belkhedma.Application;

public sealed record ProviderDto(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    string ProviderType,
    bool HasApiAccess,
    bool SupportsHourly,
    bool SupportsMonthly,
    bool SupportsB2B,
    bool SupportsRecruitment,
    string IntegrationModeKey,
    ProviderIntegrationWays IntegrationWays,
    ProviderCommunicationWays CommunicationWays,
    ProviderContractMode ContractMode,
    PaymentCollectionMode PaymentCollectionMode,
    bool RequirePaymentBeforeSubmission,
    string? ApiBaseUrl,
    string? WebsiteUrl,
    string? AppUrl,
    string? TinyUrl,
    string? LogoUrl,
    string? BookingEmail,
    string? OperationsEmail,
    string Notes,
    int PricingExpirationHours,
    int SessionExpirationHours,
    int ContractDraftExpirationHours,
    string SettingsJson,
    string? SettingsUsername,
    string? SettingsPassword,
    bool IsActive);

public sealed record ServiceOfferDto(
    Guid Id,
    Guid ProviderId,
    string ProviderServiceId,
    ServiceMode ServiceMode,
    string NameAr,
    string NameEn,
    IReadOnlyList<int> HourOptions,
    IReadOnlyList<string> NationalityOptions,
    bool IsAvailable,
    DateTime UpdatedAtUtc);

public sealed record PriceSnapshotDto(
    Guid Id,
    Guid ProviderId,
    Guid ServiceOfferId,
    decimal FinalPriceSar,
    decimal? OriginalPriceSar,
    decimal? VatAmountSar,
    DataSourceType SourceType,
    DateTime CollectedAtUtc,
    DateTime ExpiresAtUtc);

public sealed record ProviderJsonDocumentDto(
    Guid Id,
    Guid ProviderId,
    Guid ServiceOfferId,
    string DocumentKey,
    string FileName,
    string? ProviderCode,
    ServiceMode? ServiceMode,
    string JsonAttributes,
    string JsonData,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc)
{
    // Backward-compatible alias for existing mobile clients.
    public string JsonContent => JsonData;
}

public sealed record CustomerSavedLocationDto(
    Guid Id,
    string CustomerReference,
    string Label,
    string City,
    string District,
    decimal Latitude,
    decimal Longitude,
    string? GoogleMapsUrl,
    string? GooglePlaceId,
    DateTime UpdatedAtUtc);

public sealed record CustomerAuthRequest(
    string MobileNumber,
    string FullName);

public sealed record CustomerAuthResponse(
    Guid CustomerId,
    string CustomerReference,
    string FullName,
    string MobileNumber,
    string AuthToken,
    DateTime ExpiresAtUtc,
    bool IsNewAccount);

public sealed record CustomerProfileDto(
    Guid CustomerId,
    string CustomerReference,
    string FullName,
    string MobileNumber);

public sealed record HomePromotionDto(
    Guid Id,
    string Code,
    string CompanyNameAr,
    string CompanyNameEn,
    string TitleAr,
    string TitleEn,
    string SubtitleAr,
    string SubtitleEn,
    string ImageUrl,
    string? TargetUrl,
    string? DeepLink,
    IReadOnlyList<string> Items,
    string? ProviderCode,
    int DisplayOrder,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CreateOrUpdateHomePromotionRequest(
    string? Code,
    string CompanyNameAr,
    string CompanyNameEn,
    string TitleAr,
    string TitleEn,
    string SubtitleAr,
    string SubtitleEn,
    string ImageUrl,
    string? TargetUrl,
    string? DeepLink,
    IReadOnlyList<string>? Items,
    string? ProviderCode,
    int DisplayOrder,
    bool IsActive);

public interface IMarketplaceQueryService
{
    Task<IReadOnlyList<ProviderDto>> GetProvidersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceOfferDto>> GetOffersAsync(string? providerCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceSnapshotDto>> GetLatestPricesAsync(string? providerCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceSnapshotDto>> GetAllPricesAsync(
        string? providerCode,
        bool includeExpired = true,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProviderJsonDocumentDto>> GetProviderJsonDocumentsAsync(
        string? providerCode,
        bool includeExpired = false,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CustomerSavedLocationDto>> GetCustomerSavedLocationsAsync(
        string customerReference,
        CancellationToken cancellationToken = default);
    Task<CustomerProfileDto?> GetCustomerProfileByTokenAsync(
        string authToken,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HomePromotionDto>> GetHomePromotionsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
}

public interface ICustomerAuthService
{
    Task<CustomerAuthResponse> RegisterOrLoginAsync(
        CustomerAuthRequest request,
        CancellationToken cancellationToken = default);
}

public interface IDataCollectionService
{
    Task CollectProviderDataAsync(string providerCode, CancellationToken cancellationToken = default);
    Task CollectAllProvidersDataAsync(CancellationToken cancellationToken = default);
}

public interface IMarketplaceAdminService
{
    Task<int> SetPricesExpirationBulkAsync(string? providerCode, DateTime? expiresAtUtc, CancellationToken cancellationToken = default);
    Task<int> SetPriceExpirationAsync(Guid priceSnapshotId, DateTime? expiresAtUtc, CancellationToken cancellationToken = default);
    Task<int> SetJsonDocumentsExpirationBulkAsync(string? providerCode, DateTime? expiresAtUtc, CancellationToken cancellationToken = default);
    Task<int> SetJsonDocumentExpirationAsync(Guid documentId, DateTime? expiresAtUtc, CancellationToken cancellationToken = default);
    Task<HomePromotionDto> CreateHomePromotionAsync(
        CreateOrUpdateHomePromotionRequest request,
        CancellationToken cancellationToken = default);
    Task<HomePromotionDto?> UpdateHomePromotionAsync(
        Guid promotionId,
        CreateOrUpdateHomePromotionRequest request,
        CancellationToken cancellationToken = default);
    Task<bool> DeleteHomePromotionAsync(Guid promotionId, CancellationToken cancellationToken = default);
}
