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
    int DisplayOrder,
    IReadOnlyList<int> HourOptions,
    IReadOnlyList<string> NationalityOptions,
    IReadOnlyList<ServiceAttributeDto> ServiceAttributes,
    bool IsAvailable,
    DateTime UpdatedAtUtc);

public sealed record ServiceAttributeDto(
    Guid Id,
    Guid ServiceOfferId,
    string AttributeKey,
    string NameAr,
    string NameEn,
    ServiceAttributeType Type,
    string? OptionSetJson,
    bool IsMandatory,
    ServiceAttributeFilterScope FilterScope,
    int DisplayOrder,
    bool IsActive,
    DateTime UpdatedAtUtc);

public sealed record ProviderAttributeValueMapperDto(
    Guid Id,
    Guid ProviderId,
    Guid? ServiceOfferId,
    ServiceMode? ServiceMode,
    string RawAttributeKey,
    string RawValue,
    string? RawTextEn,
    string? RawTextAr,
    string NormalizedAttributeKey,
    string NormalizedValue,
    string NormalizedTextEn,
    string NormalizedTextAr,
    bool IsActive,
    DateTime UpdatedAtUtc);

public sealed record JsonAttributeValueDto(
    string AttributeKey,
    string RawValue,
    string? TextEn,
    string? TextAr);

public sealed record NormalizedAttributeValueDto(
    string AttributeKey,
    string Value,
    string TextEn,
    string TextAr,
    string RawAttributeKey,
    string RawValue,
    string? RawTextEn,
    string? RawTextAr);

public sealed record NormalizedPriceSnapshotDto(
    Guid PriceSnapshotId,
    Guid ProviderId,
    string ProviderCode,
    string ProviderNameAr,
    string ProviderNameEn,
    Guid ServiceOfferId,
    string ProviderServiceId,
    ServiceMode ServiceMode,
    string ServiceNameAr,
    string ServiceNameEn,
    decimal Price,
    decimal Vat,
    decimal PriceWithVat,
    DataSourceType SourceType,
    DateTime CollectedAtUtc,
    DateTime ExpiresAtUtc,
    IReadOnlyList<JsonAttributeValueDto> JsonAttributes,
    IReadOnlyList<NormalizedAttributeValueDto> NormalizedAttributes);

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

public sealed record CustomerRequestAttributeValueDto(
    string AttributeKey,
    string AttributeNameAr,
    string AttributeNameEn,
    string Value,
    string ValueAr,
    string ValueEn);

public sealed record CreateCustomerServiceRequestPayload(
    Guid ServiceOfferId,
    Guid ProviderId,
    Guid? PriceSnapshotId,
    Guid? LocationId,
    string? LocationLabel,
    string? LocationCity,
    string? LocationDistrict,
    decimal? LocationLatitude,
    decimal? LocationLongitude,
    string? LocationGoogleMapsUrl,
    string? LocationGooglePlaceId,
    string? ServiceDate,
    string? Shift,
    string? Nationality,
    string? ContractDuration,
    int? WorkersCount,
    int? HoursPerVisit,
    int? WeeklyVisits,
    string? DeliveryWindow,
    string? ProviderSource,
    string? Notes,
    IReadOnlyList<CustomerRequestAttributeValueDto>? PackageAttributes);

public sealed record CustomerServiceRequestDto(
    Guid Id,
    Guid CustomerId,
    string CustomerReference,
    Guid? CustomerSavedLocationId,
    string LocationLabel,
    string LocationCity,
    string LocationDistrict,
    decimal? LocationLatitude,
    decimal? LocationLongitude,
    string? LocationGoogleMapsUrl,
    string? LocationGooglePlaceId,
    Guid ProviderId,
    Guid ServiceOfferId,
    Guid? PriceSnapshotId,
    ServiceMode ServiceMode,
    string PackageNameAr,
    string PackageNameEn,
    decimal FinalPriceSar,
    decimal? OriginalPriceSar,
    decimal? VatAmountSar,
    string Currency,
    string? ServiceDate,
    string? SelectedShift,
    string? SelectedNationality,
    string? SelectedContractDuration,
    int? SelectedWorkersCount,
    int? SelectedHoursPerVisit,
    int? SelectedWeeklyVisits,
    string? SelectedDeliveryWindow,
    string? SelectedProviderSource,
    string Notes,
    IReadOnlyList<CustomerRequestAttributeValueDto> PackageAttributes,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record CustomerAuthRequest(
    string? Email,
    string? MobileNumber,
    string? FullName,
    string Password,
    string? UserNameOrEmail);

public sealed record CustomerRegisterRequest(
    string Email,
    string MobileNumber,
    string FullName,
    string Password);

public sealed record CustomerLoginRequest(
    string UserNameOrEmail,
    string Password);

public sealed record CustomerTokenRefreshRequest(
    string RefreshToken);

public sealed record CustomerTokenRevokeRequest(
    string RefreshToken);

public sealed record CustomerAuthResponse(
    Guid CustomerId,
    string CustomerReference,
    string FullName,
    string MobileNumber,
    string AuthToken,
    string RefreshToken,
    string Email,
    DateTime ExpiresAtUtc,
    bool IsNewAccount);

public sealed record CustomerProfileDto(
    Guid CustomerId,
    string CustomerReference,
    string FullName,
    string MobileNumber,
    string? Email = null);

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
    Task<IReadOnlyList<ServiceAttributeDto>> GetServiceAttributesAsync(
        string? providerCode,
        Guid? serviceOfferId,
        ServiceMode? serviceMode,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProviderAttributeValueMapperDto>> GetProviderAttributeValueMappersAsync(
        string? providerCode,
        Guid? serviceOfferId,
        ServiceMode? serviceMode,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NormalizedPriceSnapshotDto>> GetNormalizedPriceSnapshotsAsync(
        string? providerCode,
        bool includeExpired = true,
        CancellationToken cancellationToken = default);
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
    Task<CustomerProfileDto?> GetCustomerProfileByIdAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);
    Task<CustomerProfileDto?> GetCustomerProfileByTokenAsync(
        string authToken,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HomePromotionDto>> GetHomePromotionsAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
    Task<CustomerServiceRequestDto> CreateCustomerServiceRequestAsync(
        Guid customerId,
        CreateCustomerServiceRequestPayload request,
        CancellationToken cancellationToken = default);
}

public interface ICustomerAuthService
{
    Task<CustomerAuthResponse> RegisterAsync(
        CustomerRegisterRequest request,
        CancellationToken cancellationToken = default);
    Task<CustomerAuthResponse> LoginAsync(
        CustomerLoginRequest request,
        CancellationToken cancellationToken = default);
    Task<CustomerAuthResponse> RegisterOrLoginAsync(
        CustomerAuthRequest request,
        CancellationToken cancellationToken = default);
    Task<CustomerAuthResponse> RefreshTokenAsync(
        CustomerTokenRefreshRequest request,
        CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(
        CustomerTokenRevokeRequest request,
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
