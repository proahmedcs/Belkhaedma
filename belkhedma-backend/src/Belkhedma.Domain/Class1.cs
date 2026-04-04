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
    public string DocumentKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? ProviderCode { get; set; }
    public ServiceMode? ServiceMode { get; set; }
    public string JsonContent { get; set; } = "{}";
    public DateTime ExpiresAtUtc { get; set; } = DateTime.UtcNow.AddMonths(6);
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
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
