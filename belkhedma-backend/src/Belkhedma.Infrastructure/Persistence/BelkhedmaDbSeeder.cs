using Belkhedma.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Belkhedma.Infrastructure.Persistence;

public static class BelkhedmaDbSeeder
{
    private sealed record DemoOfferSeed(
        string ProviderCode,
        string ProviderServiceId,
        ServiceMode ServiceMode,
        string NameAr,
        string NameEn,
        decimal FinalPriceSar,
        decimal? OriginalPriceSar,
        DataSourceType SourceType);

    private sealed record DemoJsonDocumentSeed(
        string ProviderCode,
        ServiceMode ServiceMode,
        string DocumentSuffix,
        string SourceFileKey,
        string[] AttributeFields);

    private static readonly Dictionary<string, string> JsonSamples = new()
    {
        ["fawran_public_api_probe"] = "data/provider-json/fawran_public_api_probe.json",
        ["enaya_fawran_real_json_bundle"] = "data/provider-json/enaya_fawran_real_json_bundle.json",
        ["fawran_monthly_real_json_bundle"] = "data/provider-json/fawran_monthly_real_json_bundle.json"
    };

    private static readonly DemoOfferSeed[] DemoOfferSeeds =
    [
        new(
            "enaya",
            "a5fbc0b6-3b59-ee11-a8a4-000d3a227ab4",
            ServiceMode.Hourly,
            "عناية - زيارة تنظيف 4 ساعات",
            "Enaya - Cleaning Visit 4 Hours",
            75.00m,
            147.20m,
            DataSourceType.Api),
        new(
            "emdad-hr",
            "c97fdb23-4687-ec11-a837-000d3abe20f8",
            ServiceMode.Hourly,
            "إمداد - فوراً 4 ساعات",
            "Emdad - Fawran 4 Hours",
            90.00m,
            140.00m,
            DataSourceType.Api),
        new(
            "mueen",
            "mueen-hourly-4h",
            ServiceMode.Hourly,
            "معين - تنظيف بالساعة 4 ساعات",
            "Mueen - Hourly Cleaning 4 Hours",
            94.00m,
            129.00m,
            DataSourceType.Scraper),
        new(
            "tamkeen",
            "tamkeen-monthly-1m",
            ServiceMode.Monthly,
            "تمكين - باقة شهرية (شهر)",
            "Tamkeen - Monthly Package (1 Month)",
            2790.00m,
            3150.00m,
            DataSourceType.Scraper),
        new(
            "almutahidah",
            "almutahidah-monthly-3m",
            ServiceMode.Monthly,
            "الشركة المتحدة - باقة شهرية (3 أشهر)",
            "Almutahidah - Monthly Package (3 Months)",
            2650.00m,
            2990.00m,
            DataSourceType.Api),
        new(
            "esad-talents",
            "esad-talents-monthly-1m",
            ServiceMode.Monthly,
            "إسناد - باقة شهرية (شهر)",
            "Esad - Monthly Package (1 Month)",
            2390.00m,
            2710.00m,
            DataSourceType.Scraper)
    ];

    private static readonly DemoJsonDocumentSeed[] DemoJsonDocumentSeeds =
    [
        new(
            "enaya",
            ServiceMode.Hourly,
            "hourly-package",
            "enaya_fawran_real_json_bundle",
            ["visitShiftName", "resourceGroupName", "contractDurationName", "employeeNumber", "hoursNumber", "weeklyVisits", "visitHours", "deliveryWindow"]),
        new(
            "emdad-hr",
            ServiceMode.Hourly,
            "hourly-package",
            "fawran_public_api_probe",
            ["period_tabs", "resourceGroupName", "contractDurationName", "employeeNumber", "hoursNumber", "weeklyVisits"]),
        new(
            "mueen",
            ServiceMode.Hourly,
            "hourly-package",
            "fawran_public_api_probe",
            ["period_tabs", "resourceGroupName", "contractDurationName", "employeeNumber", "hoursNumber", "weeklyVisits"]),
        new(
            "tamkeen",
            ServiceMode.Monthly,
            "monthly-package",
            "fawran_monthly_real_json_bundle",
            ["contract_duration_months", "delivery_method", "employee_id", "contract_details", "payment"]),
        new(
            "almutahidah",
            ServiceMode.Monthly,
            "monthly-package",
            "fawran_monthly_real_json_bundle",
            ["contract_duration_months", "delivery_method", "employee_id", "contract_details", "payment"]),
        new(
            "esad-talents",
            ServiceMode.Monthly,
            "monthly-package",
            "fawran_monthly_real_json_bundle",
            ["contract_duration_months", "delivery_method", "employee_id", "contract_details", "payment"])
    ];

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

        await SeedDemoOffersAndPriceSnapshotsAsync(dbContext, cancellationToken);
        await SeedProviderJsonDocumentsAsync(dbContext, cancellationToken);
        await SeedDemoCustomerAccountsAsync(dbContext, cancellationToken);
        await SeedCustomerSavedLocationsAsync(dbContext, cancellationToken);
        await SeedHomePromotionsAsync(dbContext, cancellationToken);
    }

    private static async Task SeedDemoCustomerAccountsAsync(BelkhedmaDbContext dbContext, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        const string demoCustomerReference = "demo-customer";
        const string demoMobile = "+966500000000";
        const string normalizedDemoMobile = "+966500000000";

        var customer = await dbContext.CustomerAccounts
            .FirstOrDefaultAsync(x => x.CustomerReference == demoCustomerReference, cancellationToken);

        if (customer is null)
        {
            customer = new CustomerAccount
            {
                CustomerReference = demoCustomerReference,
                FullName = "Demo Customer",
                MobileNumber = demoMobile,
                NormalizedMobileNumber = normalizedDemoMobile,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await dbContext.CustomerAccounts.AddAsync(customer, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        else
        {
            customer.FullName = "Demo Customer";
            customer.MobileNumber = demoMobile;
            customer.NormalizedMobileNumber = normalizedDemoMobile;
            customer.IsActive = true;
            customer.UpdatedAtUtc = now;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedDemoOffersAndPriceSnapshotsAsync(BelkhedmaDbContext dbContext, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var providerByCode = await dbContext.Providers
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Code, x => x, cancellationToken);

        var offers = await dbContext.ServiceOffers.ToListAsync(cancellationToken);

        foreach (var seed in DemoOfferSeeds)
        {
            if (!providerByCode.TryGetValue(seed.ProviderCode, out var provider))
            {
                continue;
            }

            var existingOffer = offers.FirstOrDefault(x =>
                x.ProviderId == provider.Id &&
                x.ProviderServiceId == seed.ProviderServiceId);

            if (existingOffer is null)
            {
                existingOffer = new ServiceOffer
                {
                    ProviderId = provider.Id,
                    ProviderServiceId = seed.ProviderServiceId,
                    ServiceMode = seed.ServiceMode,
                    NameAr = seed.NameAr,
                    NameEn = seed.NameEn,
                    IsAvailable = true,
                    UpdatedAtUtc = now
                };

                offers.Add(existingOffer);
                await dbContext.ServiceOffers.AddAsync(existingOffer, cancellationToken);
                continue;
            }

            existingOffer.ServiceMode = seed.ServiceMode;
            existingOffer.NameAr = seed.NameAr;
            existingOffer.NameEn = seed.NameEn;
            existingOffer.IsAvailable = true;
            existingOffer.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var priceRows = await dbContext.PriceSnapshots.ToListAsync(cancellationToken);
        foreach (var seed in DemoOfferSeeds)
        {
            if (!providerByCode.TryGetValue(seed.ProviderCode, out var provider))
            {
                continue;
            }

            var offer = offers.FirstOrDefault(x =>
                x.ProviderId == provider.Id &&
                x.ProviderServiceId == seed.ProviderServiceId);

            if (offer is null)
            {
                continue;
            }

            var priceSeedKey = $"{seed.ProviderCode}:{seed.ProviderServiceId}";
            var existingPrice = priceRows.FirstOrDefault(x =>
                x.ProviderId == provider.Id &&
                x.ServiceOfferId == offer.Id &&
                x.RawPayload.Contains($"\"priceSeedKey\":\"{priceSeedKey}\"", StringComparison.OrdinalIgnoreCase));

            var payload = JsonSerializer.Serialize(new
            {
                priceSeedKey,
                providerCode = seed.ProviderCode,
                providerServiceId = seed.ProviderServiceId,
                serviceMode = seed.ServiceMode.ToString(),
                source = "demo-seed"
            });

            if (existingPrice is null)
            {
                await dbContext.PriceSnapshots.AddAsync(new PriceSnapshot
                {
                    ProviderId = provider.Id,
                    ServiceOfferId = offer.Id,
                    FinalPriceSar = seed.FinalPriceSar,
                    OriginalPriceSar = seed.OriginalPriceSar,
                    VatAmountSar = Math.Round(seed.FinalPriceSar * 0.15m, 2),
                    SourceType = seed.SourceType,
                    RawPayload = payload,
                    CollectedAtUtc = now,
                    ExpiresAtUtc = now.AddHours(provider.PricingExpirationHours)
                }, cancellationToken);
            }
            else
            {
                existingPrice.FinalPriceSar = seed.FinalPriceSar;
                existingPrice.OriginalPriceSar = seed.OriginalPriceSar;
                existingPrice.VatAmountSar = Math.Round(seed.FinalPriceSar * 0.15m, 2);
                existingPrice.SourceType = seed.SourceType;
                existingPrice.RawPayload = payload;
                existingPrice.CollectedAtUtc = now;
                existingPrice.ExpiresAtUtc = now.AddHours(provider.PricingExpirationHours);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedProviderJsonDocumentsAsync(BelkhedmaDbContext dbContext, CancellationToken cancellationToken)
    {
        var providers = await dbContext.Providers
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Code, x => x.Id, cancellationToken);

        var offers = await dbContext.ServiceOffers
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var seed in DemoJsonDocumentSeeds)
        {
            if (!providers.TryGetValue(seed.ProviderCode, out var providerId))
            {
                continue;
            }

            var serviceOffer = offers
                .Where(x => x.ProviderId == providerId && x.ServiceMode == seed.ServiceMode)
                .OrderByDescending(x => x.UpdatedAtUtc)
                .FirstOrDefault();

            if (serviceOffer is null)
            {
                continue;
            }

            if (!JsonSamples.TryGetValue(seed.SourceFileKey, out var filePath))
            {
                continue;
            }

            var fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", filePath));
            if (!File.Exists(fullPath))
            {
                continue;
            }

            var documentKey = $"{seed.ProviderCode}_{seed.DocumentSuffix}";
            var existing = await dbContext.ProviderJsonDocuments
                .FirstOrDefaultAsync(x => x.DocumentKey == documentKey, cancellationToken);

            var json = await File.ReadAllTextAsync(fullPath, cancellationToken);
            var fileName = Path.GetFileName(fullPath);
            var attributesJson = JsonSerializer.Serialize(new
            {
                providerCode = seed.ProviderCode,
                serviceMode = seed.ServiceMode.ToString(),
                attributeFields = seed.AttributeFields
            });

            if (existing is null)
            {
                await dbContext.ProviderJsonDocuments.AddAsync(new ProviderJsonDocument
                {
                    ProviderId = providerId,
                    ServiceOfferId = serviceOffer.Id,
                    DocumentKey = documentKey,
                    FileName = fileName,
                    ServiceMode = seed.ServiceMode,
                    JsonAttributes = attributesJson,
                    JsonData = json,
                    IsActive = true,
                    ExpiresAtUtc = DateTime.UtcNow.AddMonths(6)
                }, cancellationToken);
                continue;
            }

            existing.ProviderId = providerId;
            existing.ServiceOfferId = serviceOffer.Id;
            existing.FileName = fileName;
            existing.ServiceMode = seed.ServiceMode;
            existing.JsonAttributes = attributesJson;
            existing.JsonData = json;
            existing.IsActive = true;
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

    private static async Task SeedHomePromotionsAsync(BelkhedmaDbContext dbContext, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var seeds = new List<HomePromotion>
        {
            new()
            {
                Code = "mediation-campaign",
                CompanyNameAr = "بالخدمة",
                CompanyNameEn = "Belkhedma",
                TitleAr = "خدمة التوسط",
                TitleEn = "Mediation Service",
                SubtitleAr = "جسر ثقة.. يوصلك بالكفاءات",
                SubtitleEn = "Bridge trust and connect with top professionals.",
                ImageUrl = "https://images.unsplash.com/photo-1521791136064-7986c2920216?auto=format&fit=crop&w=1280&q=80",
                TargetUrl = "https://belkhedma.example.com/promotions/mediation",
                DeepLink = "belkhedma://promotions/mediation",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "Trusted providers",
                    "Fast approvals",
                    "Lead + contract support"
                }),
                ProviderCode = "wasata",
                DisplayOrder = 1,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Code = "medical-home-visit",
                CompanyNameAr = "عناية",
                CompanyNameEn = "Enaya",
                TitleAr = "خصم الزيارة الطبية المنزلية",
                TitleEn = "Medical Home Visit Discount",
                SubtitleAr = "عروض موسمية على باقات التمريض المنزلي",
                SubtitleEn = "Get seasonal offers on home nursing packages.",
                ImageUrl = "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=1280&q=80",
                TargetUrl = "https://belkhedma.example.com/promotions/medical",
                DeepLink = "belkhedma://promotions/medical",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "Nursing at home",
                    "Doctor follow-up",
                    "Discounted seasonal prices"
                }),
                ProviderCode = "enaya",
                DisplayOrder = 2,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Code = "monthly-package-campaign",
                CompanyNameAr = "تمكين",
                CompanyNameEn = "Tamkeen",
                TitleAr = "حملة الباقات الشهرية",
                TitleEn = "Monthly Package Campaign",
                SubtitleAr = "أفضل الخطط الشهرية من عدة مزودين",
                SubtitleEn = "Best monthly plans from multiple providers.",
                ImageUrl = "https://images.unsplash.com/photo-1484154218962-a197022b5858?auto=format&fit=crop&w=1280&q=80",
                TargetUrl = "https://belkhedma.example.com/promotions/monthly",
                DeepLink = "belkhedma://promotions/monthly",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "1/3/6 months plans",
                    "Compare providers",
                    "Transparent contract terms"
                }),
                ProviderCode = "tamkeen",
                DisplayOrder = 3,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }
        };

        var existingRows = await dbContext.HomePromotions.ToListAsync(cancellationToken);
        foreach (var seed in seeds)
        {
            var existing = existingRows.FirstOrDefault(x => x.Code == seed.Code);
            if (existing is null)
            {
                await dbContext.HomePromotions.AddAsync(seed, cancellationToken);
                continue;
            }

            existing.CompanyNameAr = seed.CompanyNameAr;
            existing.CompanyNameEn = seed.CompanyNameEn;
            existing.TitleAr = seed.TitleAr;
            existing.TitleEn = seed.TitleEn;
            existing.SubtitleAr = seed.SubtitleAr;
            existing.SubtitleEn = seed.SubtitleEn;
            existing.ImageUrl = seed.ImageUrl;
            existing.TargetUrl = seed.TargetUrl;
            existing.DeepLink = seed.DeepLink;
            existing.ItemsJson = seed.ItemsJson;
            existing.ProviderCode = seed.ProviderCode;
            existing.DisplayOrder = seed.DisplayOrder;
            existing.IsActive = seed.IsActive;
            existing.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
