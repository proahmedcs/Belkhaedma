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
    public bool HasApiAccess { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
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
