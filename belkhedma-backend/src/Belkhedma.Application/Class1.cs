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
    bool IsActive);

public sealed record ServiceOfferDto(
    Guid Id,
    Guid ProviderId,
    string ProviderServiceId,
    ServiceMode ServiceMode,
    string NameAr,
    string NameEn,
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

public interface IMarketplaceQueryService
{
    Task<IReadOnlyList<ProviderDto>> GetProvidersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceOfferDto>> GetOffersAsync(string? providerCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriceSnapshotDto>> GetLatestPricesAsync(string? providerCode, CancellationToken cancellationToken = default);
}

public interface IDataCollectionService
{
    Task CollectProviderDataAsync(string providerCode, CancellationToken cancellationToken = default);
    Task CollectAllProvidersDataAsync(CancellationToken cancellationToken = default);
}
