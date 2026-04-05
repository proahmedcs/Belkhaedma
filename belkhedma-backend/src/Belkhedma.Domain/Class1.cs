namespace Belkhedma.Domain;

public enum ServiceMode
{
    Hourly = 1,
    Monthly = 2,
    Resident = 3,
    Business = 4
}

public enum DataSourceType
{
    Api = 1,
    Scraper = 2
}

[Flags]
public enum ProviderIntegrationWays
{
    None = 0,
    Api = 1,
    Website = 2,
    ManualScrape = 4
}

[Flags]
public enum ProviderCommunicationWays
{
    None = 0,
    Api = 1,
    Email = 2,
    WebsitePortal = 4,
    ManualOperations = 8
}

public enum ProviderContractMode
{
    LeadOnly = 1,
    FullContract = 2,
    Hybrid = 3
}

public enum PaymentCollectionMode
{
    NoPayment = 1,
    CustomerPaysProvider = 2,
    CustomerPaysBelkhedma = 3,
    Configurable = 4
}

public enum JobRunStatus
{
    Succeeded = 1,
    Failed = 2
}

public sealed class Provider
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public bool HasApiAccess { get; set; }
    public bool SupportsHourly { get; set; }
    public bool SupportsMonthly { get; set; }
    public bool SupportsB2B { get; set; }
    public bool SupportsRecruitment { get; set; }
    public string IntegrationModeKey { get; set; } = "manual_rfq";
    public ProviderIntegrationWays IntegrationWays { get; set; } = ProviderIntegrationWays.ManualScrape;
    public ProviderCommunicationWays CommunicationWays { get; set; } = ProviderCommunicationWays.ManualOperations;
    public ProviderContractMode ContractMode { get; set; } = ProviderContractMode.LeadOnly;
    public PaymentCollectionMode PaymentCollectionMode { get; set; } = PaymentCollectionMode.Configurable;
    public bool RequirePaymentBeforeSubmission { get; set; }
    public string? ApiBaseUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? AppUrl { get; set; }
    public string? TinyUrl { get; set; }
    public string? LogoUrl { get; set; }
    public string? BookingEmail { get; set; }
    public string? OperationsEmail { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int PricingExpirationHours { get; set; } = 6;
    public int SessionExpirationHours { get; set; } = 2;
    public int ContractDraftExpirationHours { get; set; } = 24;
    public string SettingsJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class ServiceOffer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProviderId { get; set; }
    public string ProviderServiceId { get; set; } = string.Empty;
    public ServiceMode ServiceMode { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PriceSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProviderId { get; set; }
    public Guid ServiceOfferId { get; set; }
    public decimal FinalPriceSar { get; set; }
    public decimal? OriginalPriceSar { get; set; }
    public decimal? VatAmountSar { get; set; }
    public string Currency { get; set; } = "SAR";
    public DataSourceType SourceType { get; set; }
    public string RawPayload { get; set; } = "{}";
    public DateTime CollectedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddHours(6);
}

public sealed class ProviderJsonDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProviderId { get; set; }
    public Guid ServiceOfferId { get; set; }
    public string DocumentKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public ServiceMode? ServiceMode { get; set; }
    public string JsonAttributes { get; set; } = "{}";
    public string JsonData { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMonths(6);
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class HomePromotion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string CompanyNameAr { get; set; } = string.Empty;
    public string CompanyNameEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string SubtitleAr { get; set; } = string.Empty;
    public string SubtitleEn { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string? TargetUrl { get; set; }
    public string? DeepLink { get; set; }
    public string ItemsJson { get; set; } = "[]";
    public string? ProviderCode { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class CollectionJobRun
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProviderId { get; set; }
    public DataSourceType SourceType { get; set; }
    public JobRunStatus Status { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime FinishedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class CustomerSavedLocation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerReference { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? GoogleMapsUrl { get; set; }
    public string? GooglePlaceId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class CustomerAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerReference { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public string NormalizedMobileNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public sealed class CustomerAuthSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerAccountId { get; set; }
    public string AuthToken { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddDays(30);
    public DateTime LastUsedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }
}
