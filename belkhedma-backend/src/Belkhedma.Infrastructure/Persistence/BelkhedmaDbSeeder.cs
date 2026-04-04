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
                NameEn = "Mueen",
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
                LogoUrl = "https://logo.clearbit.com/mueen.com.sa",
                Notes = "Strong hourly services (cleaning, nanny)",
                PricingExpirationHours = 4,
                SessionExpirationHours = 2,
                ContractDraftExpirationHours = 12
            },
            new()
            {
                Code = "tamkeen",
                NameAr = "تمكين",
                NameEn = "Tamkeen HR",
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
                LogoUrl = "https://logo.clearbit.com/tamkeenhr.sa",
                Notes = "Covers B2C + B2B + recruitment",
                PricingExpirationHours = 4,
                SessionExpirationHours = 2,
                ContractDraftExpirationHours = 12
            },
            new()
            {
                Code = "almutahidah",
                NameAr = "المتحدة",
                NameEn = "Almutahidah",
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
                LogoUrl = "https://logo.clearbit.com/almutahidah.com",
                Notes = "Cleaning + hospitality + residential",
                PricingExpirationHours = 6,
                SessionExpirationHours = 2,
                ContractDraftExpirationHours = 24
            },
            new()
            {
                Code = "irc-saudi",
                NameAr = "IRC السعودية",
                NameEn = "IRC Saudi",
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
                LogoUrl = "https://logo.clearbit.com/irc.sa",
                Notes = "Strong recruitment & manpower",
                PricingExpirationHours = 8,
                SessionExpirationHours = 2,
                ContractDraftExpirationHours = 24
            },
            new()
            {
                Code = "enaya",
                NameAr = "شركة عناية",
                NameEn = "Enaya",
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
                LogoUrl = "https://logo.clearbit.com/enaya.sa",
                Notes = "Domestic + business + recruitment",
                PricingExpirationHours = 6,
                SessionExpirationHours = 2,
                ContractDraftExpirationHours = 24
            },
            new()
            {
                Code = "esad-talents",
                NameAr = "إسناد تالنتس",
                NameEn = "Esad Talents",
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
                LogoUrl = "https://logo.clearbit.com/esadtalents.com",
                Notes = "Hader visits + long-term contracts",
                PricingExpirationHours = 6,
                SessionExpirationHours = 2,
                ContractDraftExpirationHours = 24
            },
            new()
            {
                Code = "emdad-hr",
                NameAr = "إمداد HR",
                NameEn = "Emdad HR",
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
                LogoUrl = "https://logo.clearbit.com/emdadhr.com",
                Notes = "Fawran instant services + business",
                PricingExpirationHours = 6,
                SessionExpirationHours = 2,
                ContractDraftExpirationHours = 24
            },
            new()
            {
                Code = "eitinaa",
                NameAr = "اعتناء",
                NameEn = "Eitinaa",
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
                LogoUrl = "https://logo.clearbit.com/eitinaa.com",
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
}
