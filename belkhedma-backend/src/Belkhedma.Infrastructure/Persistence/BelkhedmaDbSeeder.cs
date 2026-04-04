using Belkhedma.Domain;
using Microsoft.EntityFrameworkCore;

namespace Belkhedma.Infrastructure.Persistence;

public static class BelkhedmaDbSeeder
{
    private static readonly Dictionary<string, string> JsonSamples = new()
    {
        ["fawran_public_api_probe"] = "data/provider-json/fawran_public_api_probe.json",
        ["enaya_fawran_real_json_bundle"] = "data/provider-json/enaya_fawran_real_json_bundle.json",
        ["fawran_monthly_real_json_bundle"] = "data/provider-json/fawran_monthly_real_json_bundle.json"
    };

    public static async Task SeedAsync(BelkhedmaDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var providers = new List<Provider>
        {
            new()
            {
                Code = "mueen",
                NameAr = "معين",
                NameEn = "Mueen Human Resources",
                ProviderType = "Household Services",
                HasApiAccess = false,
                SupportsHourly = true,
                SupportsMonthly = false,
                SupportsB2B = false,
                SupportsRecruitment = false,
                IntegrationModeKey = "hybrid_simulated",
                IntegrationWays = ProviderIntegrationWays.Website | ProviderIntegrationWays.ManualScrape,
                CommunicationWays = ProviderCommunicationWays.Email | ProviderCommunicationWays.ManualOperations,
                ContractMode = ProviderContractMode.LeadOnly,
                PaymentCollectionMode = PaymentCollectionMode.Configurable,
                RequirePaymentBeforeSubmission = false,
                WebsiteUrl = "https://www.mueen.com.sa/",
                AppUrl = "https://www.mueen.com.sa/",
                TinyUrl = "https://tinyurl.com/belkhedma-mueen",
                LogoUrl = "https://www.mueen.com.sa/assets/fav-icon/corporate/android-icon-192x192.png",
                Notes = "Strong hourly services (cleaning, nanny)",
                PricingExpirationHours = 4,
                SessionExpirationHours = 2,
                ContractDraftExpirationHours = 12
            },
            new()
            {
                Code = "tamkeen",
                NameAr = "تمكين",
                NameEn = "Tamkeen Human Resources",
                ProviderType = "HR + Staffing",
                HasApiAccess = false,
                SupportsHourly = true,
                SupportsMonthly = true,
                SupportsB2B = true,
                SupportsRecruitment = true,
                IntegrationModeKey = "hybrid_simulated",
                IntegrationWays = ProviderIntegrationWays.Website | ProviderIntegrationWays.ManualScrape,
                CommunicationWays = ProviderCommunicationWays.Email | ProviderCommunicationWays.WebsitePortal | ProviderCommunicationWays.ManualOperations,
                ContractMode = ProviderContractMode.Hybrid,
                PaymentCollectionMode = PaymentCollectionMode.Configurable,
                RequirePaymentBeforeSubmission = false,
                WebsiteUrl = "https://tamkeenhr.sa/",
                AppUrl = "https://tamkeenhr.sa/",
                TinyUrl = "https://tinyurl.com/belkhedma-tamkeen",
                LogoUrl = "https://tamkeenhr.sa/favicon.svg",
                Notes = "Covers B2C + B2B + recruitment",
                PricingExpirationHours = 4,
                SessionExpirationHours = 2,
                ContractDraftExpirationHours = 12
            },
            new()
            {
                Code = "almutahidah",
                NameAr = "الشركة المتحدة",
                NameEn = "Almutahidah Company",
                ProviderType = "Domestic Services",
                HasApiAccess = true,
                SupportsHourly = true,
                SupportsMonthly = true,
                SupportsB2B = false,
                SupportsRecruitment = true,
                IntegrationModeKey = "price_only_api",
                IntegrationWays = ProviderIntegrationWays.Api | ProviderIntegrationWays.Website,
                CommunicationWays = ProviderCommunicationWays.Api | ProviderCommunicationWays.Email | ProviderCommunicationWays.WebsitePortal,
                ContractMode = ProviderContractMode.Hybrid,
                PaymentCollectionMode = PaymentCollectionMode.Configurable,
                RequirePaymentBeforeSubmission = false,
                WebsiteUrl = "https://almutahidah.com/home",
                AppUrl = "https://almutahidah.com/home",
                TinyUrl = "https://tinyurl.com/belkhedma-almutahidah",
                LogoUrl = "https://almutahidah.com/favicon.png",
                Notes = "Cleaning + hospitality + residential",
                PricingExpirationHours = 6,
                SessionExpirationHours = 2,
                ContractDraftExpirationHours = 24
            },
            new()
            {
                Code = "irc-saudi",
                NameAr = "IRC السعودية",
                NameEn = "IRC",
                ProviderType = "Recruitment",
                HasApiAccess = false,
                SupportsHourly = false,
                SupportsMonthly = true,
                SupportsB2B = true,
                SupportsRecruitment = true,
                IntegrationModeKey = "manual_rfq",
                IntegrationWays = ProviderIntegrationWays.ManualScrape,
                CommunicationWays = ProviderCommunicationWays.Email | ProviderCommunicationWays.ManualOperations,
                ContractMode = ProviderContractMode.LeadOnly,
                PaymentCollectionMode = PaymentCollectionMode.NoPayment,
                RequirePaymentBeforeSubmission = false,
                WebsiteUrl = "https://own.irc.sa/home",
                AppUrl = "https://own.irc.sa/home",
                TinyUrl = "https://tinyurl.com/belkhedma-irc",
                LogoUrl = "https://own.irc.sa/favicon.png",
                Notes = "Strong recruitment & manpower",
                PricingExpirationHours = 8,
                SessionExpirationHours = 2,
                ContractDraftExpirationHours = 24
            },
            new()
            {
                Code = "enaya",
                NameAr = "عناية للاستقدام",
                NameEn = "Enaya Recruitment",
                ProviderType = "Recruitment + Domestic",
                HasApiAccess = true,
                SupportsHourly = true,
                SupportsMonthly = true,
                SupportsB2B = true,
                SupportsRecruitment = true,
                IntegrationModeKey = "hybrid_simulated",
                IntegrationWays = ProviderIntegrationWays.Api | ProviderIntegrationWays.Website,
                CommunicationWays = ProviderCommunicationWays.Api | ProviderCommunicationWays.Email,
                ContractMode = ProviderContractMode.FullContract,
                PaymentCollectionMode = PaymentCollectionMode.Configurable,
                RequirePaymentBeforeSubmission = false,
                ApiBaseUrl = "https://enaya.sa/api",
                WebsiteUrl = "https://enaya.sa/home",
                AppUrl = "https://enaya.sa/home",
                TinyUrl = "https://tinyurl.com/belkhedma-enaya",
                LogoUrl = "https://enaya.sa/favicon.ico",
                Notes = "Domestic + business + recruitment",
                PricingExpirationHours = 6,
                SessionExpirationHours = 2,
                ContractDraftExpirationHours = 24
            },
            new()
            {
                Code = "esad-talents",
                NameAr = "إسناد تالنتس",
                NameEn = "Esad",
                ProviderType = "Staffing + Recruitment",
                HasApiAccess = false,
                SupportsHourly = true,
                SupportsMonthly = true,
                SupportsB2B = true,
                SupportsRecruitment = true,
                IntegrationModeKey = "hybrid_simulated",
                IntegrationWays = ProviderIntegrationWays.Website | ProviderIntegrationWays.ManualScrape,
                CommunicationWays = ProviderCommunicationWays.Email | ProviderCommunicationWays.WebsitePortal | ProviderCommunicationWays.ManualOperations,
                ContractMode = ProviderContractMode.Hybrid,
                PaymentCollectionMode = PaymentCollectionMode.Configurable,
                RequirePaymentBeforeSubmission = false,
                WebsiteUrl = "https://esadtalents.com/",
                AppUrl = "https://esadtalents.com/",
                TinyUrl = "https://tinyurl.com/belkhedma-esad",
                LogoUrl = "https://esadtalents.com/Content/img/favicon.ico",
                Notes = "Hader visits + long-term contracts",
                PricingExpirationHours = 6,
                SessionExpirationHours = 2,
                ContractDraftExpirationHours = 24
            },
            new()
            {
                Code = "emdad-hr",
                NameAr = "إمداد للموارد البشرية",
                NameEn = "Emdad Human Resources",
                ProviderType = "HR + Outsourcing",
                HasApiAccess = true,
                SupportsHourly = true,
                SupportsMonthly = false,
                SupportsB2B = true,
                SupportsRecruitment = false,
                IntegrationModeKey = "hybrid_simulated",
                IntegrationWays = ProviderIntegrationWays.Api | ProviderIntegrationWays.Website,
                CommunicationWays = ProviderCommunicationWays.Api | ProviderCommunicationWays.Email | ProviderCommunicationWays.WebsitePortal,
                ContractMode = ProviderContractMode.Hybrid,
                PaymentCollectionMode = PaymentCollectionMode.Configurable,
                RequirePaymentBeforeSubmission = false,
                ApiBaseUrl = "https://emdadhr.com:8005/en/api",
                WebsiteUrl = "https://emdadhr.com/#/fawran",
                AppUrl = "https://emdadhr.com/#/fawran",
                TinyUrl = "https://tinyurl.com/belkhedma-emdad",
                LogoUrl = "https://emdadhr.com/assets/images/favicon.png",
                Notes = "Fawran instant services + business",
                PricingExpirationHours = 6,
                SessionExpirationHours = 2,
                ContractDraftExpirationHours = 24
            },
            new()
            {
                Code = "eitinaa",
                NameAr = "اعتناء",
                NameEn = "Eitinaa Human Resources",
                ProviderType = "Mediation + Domestic",
                HasApiAccess = false,
                SupportsHourly = false,
                SupportsMonthly = true,
                SupportsB2B = false,
                SupportsRecruitment = true,
                IntegrationModeKey = "manual_rfq",
                IntegrationWays = ProviderIntegrationWays.ManualScrape,
                CommunicationWays = ProviderCommunicationWays.Email | ProviderCommunicationWays.ManualOperations,
                ContractMode = ProviderContractMode.LeadOnly,
                PaymentCollectionMode = PaymentCollectionMode.NoPayment,
                RequirePaymentBeforeSubmission = false,
                WebsiteUrl = "https://eitinaa.com/ar",
                AppUrl = "https://eitinaa.com/ar",
                TinyUrl = "https://tinyurl.com/belkhedma-eitinaa",
                LogoUrl = "https://eitinaa.com/assets/images/favicon-96x96.png",
                Notes = "Recruitment mediation + monthly services",
                PricingExpirationHours = 8,
                SessionExpirationHours = 2,
                ContractDraftExpirationHours = 24
            }
        };

        var existingProviders = await dbContext.Providers.ToListAsync(cancellationToken);
        foreach (var provider in providers)
        {
            var existing = existingProviders.FirstOrDefault(x => x.Code == provider.Code);
            if (existing is null)
            {
                await dbContext.Providers.AddAsync(provider, cancellationToken);
                continue;
            }

            existing.NameAr = provider.NameAr;
            existing.NameEn = provider.NameEn;
            existing.ProviderType = provider.ProviderType;
            existing.HasApiAccess = provider.HasApiAccess;
            existing.SupportsHourly = provider.SupportsHourly;
            existing.SupportsMonthly = provider.SupportsMonthly;
            existing.SupportsB2B = provider.SupportsB2B;
            existing.SupportsRecruitment = provider.SupportsRecruitment;
            existing.IntegrationModeKey = provider.IntegrationModeKey;
            existing.IntegrationWays = provider.IntegrationWays;
            existing.CommunicationWays = provider.CommunicationWays;
            existing.ContractMode = provider.ContractMode;
            existing.PaymentCollectionMode = provider.PaymentCollectionMode;
            existing.RequirePaymentBeforeSubmission = provider.RequirePaymentBeforeSubmission;
            existing.ApiBaseUrl = provider.ApiBaseUrl;
            existing.WebsiteUrl = provider.WebsiteUrl;
            existing.AppUrl = provider.AppUrl;
            existing.TinyUrl = provider.TinyUrl;
            existing.LogoUrl = provider.LogoUrl;
            existing.BookingEmail = provider.BookingEmail;
            existing.OperationsEmail = provider.OperationsEmail;
            existing.Notes = provider.Notes;
            existing.PricingExpirationHours = provider.PricingExpirationHours;
            existing.SessionExpirationHours = provider.SessionExpirationHours;
            existing.ContractDraftExpirationHours = provider.ContractDraftExpirationHours;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await SeedProviderJsonDocumentsAsync(dbContext, cancellationToken);
        await SeedCustomerSavedLocationsAsync(dbContext, cancellationToken);
    }

    private static async Task SeedProviderJsonDocumentsAsync(BelkhedmaDbContext dbContext, CancellationToken cancellationToken)
    {
        foreach (var (documentKey, filePath) in JsonSamples)
        {
            var fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", filePath));
            if (!File.Exists(fullPath))
            {
                continue;
            }

            var existing = await dbContext.ProviderJsonDocuments
                .FirstOrDefaultAsync(x => x.DocumentKey == documentKey, cancellationToken);

            var json = await File.ReadAllTextAsync(fullPath, cancellationToken);
            var fileName = Path.GetFileName(fullPath);
            var mode = documentKey.Contains("monthly", StringComparison.OrdinalIgnoreCase)
                ? ServiceMode.Monthly
                : ServiceMode.Hourly;
            var providerCode = documentKey.Contains("enaya", StringComparison.OrdinalIgnoreCase)
                ? "enaya"
                : "emdad-hr";

            if (existing is null)
            {
                await dbContext.ProviderJsonDocuments.AddAsync(new ProviderJsonDocument
                {
                    DocumentKey = documentKey,
                    FileName = fileName,
                    ProviderCode = providerCode,
                    ServiceMode = mode,
                    JsonContent = json,
                    ExpiresAtUtc = DateTime.UtcNow.AddMonths(6)
                }, cancellationToken);
                continue;
            }

            existing.FileName = fileName;
            existing.ProviderCode = providerCode;
            existing.ServiceMode = mode;
            existing.JsonContent = json;
            existing.ExpiresAtUtc = DateTime.UtcNow.AddMonths(6);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedCustomerSavedLocationsAsync(BelkhedmaDbContext dbContext, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var seededLocations = new List<CustomerSavedLocation>
        {
            new()
            {
                CustomerReference = "demo-customer",
                Label = "Home",
                City = "Riyadh",
                District = "Al Yasmin",
                Latitude = 24.826112m,
                Longitude = 46.623093m,
                GoogleMapsUrl = "https://maps.google.com/?q=24.826112,46.623093",
                GooglePlaceId = "ChIJe0c7fX4LLz4R9jN8sQYf9f8",
                UpdatedAtUtc = now
            },
            new()
            {
                CustomerReference = "demo-customer",
                Label = "Office",
                City = "Riyadh",
                District = "Al Olaya",
                Latitude = 24.707707m,
                Longitude = 46.675296m,
                GoogleMapsUrl = "https://maps.google.com/?q=24.707707,46.675296",
                GooglePlaceId = "ChIJw2h6G4YLLz4RW5h6qH9GM8w",
                UpdatedAtUtc = now
            }
        };

        foreach (var location in seededLocations)
        {
            var existing = await dbContext.CustomerSavedLocations
                .FirstOrDefaultAsync(x =>
                    x.CustomerReference == location.CustomerReference &&
                    x.Label == location.Label, cancellationToken);

            if (existing is null)
            {
                await dbContext.CustomerSavedLocations.AddAsync(location, cancellationToken);
                continue;
            }

            existing.City = location.City;
            existing.District = location.District;
            existing.Latitude = location.Latitude;
            existing.Longitude = location.Longitude;
            existing.GoogleMapsUrl = location.GoogleMapsUrl;
            existing.GooglePlaceId = location.GooglePlaceId;
            existing.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
