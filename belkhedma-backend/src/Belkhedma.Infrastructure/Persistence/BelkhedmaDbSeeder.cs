using Belkhedma.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Belkhedma.Infrastructure.Persistence;

public static class BelkhedmaDbSeeder
{
    private sealed record CollectionCredentialsSeed(
        [property: JsonPropertyName("username")] string? Username,
        [property: JsonPropertyName("password")] string? Password);

    private sealed record ProviderSettingsSeed(
        [property: JsonPropertyName("collectionCredentials")] CollectionCredentialsSeed CollectionCredentials);

    private static readonly JsonSerializerOptions ProviderSettingsJsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static string BuildProviderSettingsJson(string providerCode)
    {
        var settings = new ProviderSettingsSeed(new CollectionCredentialsSeed(
            Username: $"{providerCode}_user",
            Password: $"change-me-{providerCode}"));
        return JsonSerializer.Serialize(settings, ProviderSettingsJsonOptions);
    }

    private sealed record DemoOfferSeed(
        string ProviderCode,
        string ProviderServiceId,
        ServiceMode ServiceMode,
        int DisplayOrder,
        string NameAr,
        string NameEn,
        IReadOnlyList<int> HourOptions,
        IReadOnlyList<string> NationalityOptions,
        decimal FinalPriceSar,
        decimal? OriginalPriceSar,
        DataSourceType SourceType);

    private sealed record EnayaHourlyPackagePoint(
        string Nationality,
        int Hours,
        int Workers,
        int WeeklyVisits,
        decimal FinalPriceSar,
        decimal? OriginalPriceSar);

    private sealed record LocalizedOptionSeed(
        [property: JsonPropertyName("value")] string Value,
        [property: JsonPropertyName("labelEn")] string LabelEn,
        [property: JsonPropertyName("labelAr")] string LabelAr);

    private sealed record ServiceAttributeSeed(
        string AttributeKey,
        string NameAr,
        string NameEn,
        ServiceAttributeType Type,
        IReadOnlyList<LocalizedOptionSeed>? OptionSet,
        bool IsMandatory,
        ServiceAttributeFilterScope FilterScope,
        int DisplayOrder);

    private sealed record DemoJsonDocumentSeed(
        string ProviderCode,
        ServiceMode ServiceMode,
        string DocumentSuffix,
        string SourceFileKey,
        string[] AttributeFields);

    private sealed record ProviderAttributeMapperSeed(
        string ProviderCode,
        string RawAttributeKey,
        string RawValue,
        string NormalizedAttributeKey,
        string NormalizedValue,
        string NormalizedTextEn,
        string NormalizedTextAr,
        ServiceMode? ServiceMode = null,
        string? RawTextEn = null,
        string? RawTextAr = null);

    private static readonly Dictionary<string, string> JsonSamples = new()
    {
        ["fawran_public_api_probe"] = "data/provider-json/fawran_public_api_probe.json",
        ["enaya_fawran_real_json_bundle"] = "data/provider-json/enaya_fawran_real_json_bundle.json",
        ["fawran_monthly_real_json_bundle"] = "data/provider-json/fawran_monthly_real_json_bundle.json"
    };

    private static readonly DemoOfferSeed[] BaseDemoOfferSeeds =
    [
        new(
            "tamkeen",
            "tamkeen-monthly-1m",
            ServiceMode.Monthly,
            40,
            "تمكين - باقة شهرية (شهر)",
            "Tamkeen - Monthly Package (1 Month)",
            [],
            [],
            2790.00m,
            3150.00m,
            DataSourceType.Scraper),
        new(
            "almutahidah",
            "almutahidah-monthly-3m",
            ServiceMode.Monthly,
            50,
            "الشركة المتحدة - باقة شهرية (3 أشهر)",
            "Almutahidah - Monthly Package (3 Months)",
            [],
            [],
            2650.00m,
            2990.00m,
            DataSourceType.Api),
        new(
            "esad-talents",
            "esad-talents-monthly-1m",
            ServiceMode.Monthly,
            60,
            "إسناد - باقة شهرية (شهر)",
            "Esad - Monthly Package (1 Month)",
            [],
            [],
            2390.00m,
            2710.00m,
            DataSourceType.Scraper)
    ];

    private static readonly string[] PreferredEnayaNationalities = ["Africa", "Philippines", "Indonesia"];
    private static readonly int[] EnayaHourlyWorkersOptions = [1, 2];
    private static readonly int[] EnayaHourlyWeeklyVisitsOptions = [1, 2];

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

    private static readonly ProviderAttributeMapperSeed[] ProviderAttributeMapperSeeds =
    [
        // Enaya hourly mapping from real provider payload values to our normalization values.
        new("enaya", "visitShiftName", "Morning", "shift", "morning", "Morning", "صباح", ServiceMode.Hourly),
        new("enaya", "visitShiftName", "Evening", "shift", "evening", "Evening", "مساء", ServiceMode.Hourly),
        new("enaya", "employeeNumber", "1", "workersCount", "1", "1 Worker", "عامل واحد", ServiceMode.Hourly),
        new("enaya", "employeeNumber", "2", "workersCount", "2", "2 Workers", "عاملان", ServiceMode.Hourly),
        new("enaya", "hoursNumber", "4", "hoursPerVisit", "4", "4 Hours", "4 ساعات", ServiceMode.Hourly),
        new("enaya", "hoursNumber", "6", "hoursPerVisit", "6", "6 Hours", "6 ساعات", ServiceMode.Hourly),
        new("enaya", "hoursNumber", "8", "hoursPerVisit", "8", "8 Hours", "8 ساعات", ServiceMode.Hourly),
        new("enaya", "weeklyVisits", "1", "weeklyVisits", "1", "1 Visit", "زيارة واحدة", ServiceMode.Hourly),
        new("enaya", "weeklyVisits", "2", "weeklyVisits", "2", "2 Visits", "زيارتان", ServiceMode.Hourly),
        new("enaya", "contractDurationName", "1 Week", "contractDuration", "1-week", "1 Week", "أسبوع واحد", ServiceMode.Hourly),
        new("enaya", "contractDurationName", "2 Weeks", "contractDuration", "2-weeks", "2 Weeks", "أسبوعين", ServiceMode.Hourly),

        // GUID examples from providers mapped to normalized categories.
        new(
            "enaya",
            "resourceGroupId",
            "f14b9b77-0a5a-ee11-a8a5-000d3a227ab4",
            "nationality",
            "africa",
            "Africa",
            "أفريقيا",
            ServiceMode.Hourly,
            RawTextEn: "afrca",
            RawTextAr: "أفريقيا"),
        new(
            "enaya",
            "selectedHourlyPricingId",
            "0f1b7e40-165a-ee11-a8a5-000d3a227ab4",
            "hoursPerVisit",
            "4",
            "4 Hours",
            "4 ساعات",
            ServiceMode.Hourly,
            RawTextEn: "hourly package id",
            RawTextAr: "معرف باقة بالساعة"),

        // Fawran/Tamkeen monthly examples.
        new("tamkeen", "contract_duration_months", "1", "contractDuration", "1-month", "1 Month", "شهر واحد", ServiceMode.Monthly),
        new("tamkeen", "contract_duration_months", "3", "contractDuration", "3-months", "3 Months", "3 أشهر", ServiceMode.Monthly),
        new("tamkeen", "contract_duration_months", "6", "contractDuration", "6-months", "6 Months", "6 أشهر", ServiceMode.Monthly),
        new("tamkeen", "employee_id", "1", "workersCount", "1", "1 Worker", "عامل واحد", ServiceMode.Monthly),
        new("tamkeen", "employee_id", "2", "workersCount", "2", "2 Workers", "عاملان", ServiceMode.Monthly),

        // Generic provider-level mapping fallback.
        new("emdad-hr", "employeeNumber", "1", "workersCount", "1", "1 Worker", "عامل واحد", ServiceMode.Hourly),
        new("emdad-hr", "weeklyVisits", "2", "weeklyVisits", "2", "2 Visits", "زيارتان", ServiceMode.Hourly)
    ];

    private static readonly ServiceAttributeSeed[] HourlyAttributeSeeds =
    [
        new(
            "providerSource",
            "مزود الخدمة",
            "Service Provider",
            ServiceAttributeType.OptionSet,
            null,
            true,
            ServiceAttributeFilterScope.Hourly,
            10),
        new(
            "nationality",
            "الجنسية",
            "Nationality",
            ServiceAttributeType.OptionSet,
            [
                new("africa", "Africa", "أفريقيا"),
                new("philippines", "Philippines", "الفلبين"),
                new("indonesia", "Indonesia", "إندونيسيا"),
                new("east-asia", "East Asia", "شرق آسيا"),
                new("african-countries", "African Countries", "الدول الأفريقية")
            ],
            true,
            ServiceAttributeFilterScope.Hourly,
            20),
        new(
            "shift",
            "الفترة",
            "Shift",
            ServiceAttributeType.OptionSet,
            [
                new("morning", "Morning", "صباح"),
                new("evening", "Evening", "مساء")
            ],
            true,
            ServiceAttributeFilterScope.Hourly,
            30),
        new(
            "contractDuration",
            "مدة التعاقد",
            "Contract Duration",
            ServiceAttributeType.OptionSet,
            [
                new("1-week", "1 Week", "أسبوع واحد"),
                new("2-weeks", "2 Weeks", "أسبوعين"),
                new("1-month", "1 Month", "شهر واحد"),
                new("3-months", "3 Months", "3 أشهر")
            ],
            true,
            ServiceAttributeFilterScope.Hourly,
            40),
        new(
            "weeklyVisits",
            "عدد الزيارات الأسبوعية",
            "Weekly Visits",
            ServiceAttributeType.Int,
            [
                new("1", "1 Visit", "زيارة واحدة"),
                new("2", "2 Visits", "زيارتان"),
                new("3", "3 Visits", "3 زيارات"),
                new("4", "4 Visits", "4 زيارات")
            ],
            true,
            ServiceAttributeFilterScope.Hourly,
            50),
        new(
            "workersCount",
            "عدد العمال",
            "Workers Count",
            ServiceAttributeType.Int,
            [
                new("1", "1 Worker", "عامل واحد"),
                new("2", "2 Workers", "عاملان"),
                new("3", "3 Workers", "3 عمال")
            ],
            true,
            ServiceAttributeFilterScope.Hourly,
            60),
        new(
            "hoursPerVisit",
            "ساعات الزيارة",
            "Hours per Visit",
            ServiceAttributeType.Int,
            [
                new("4", "4 Hours", "4 ساعات"),
                new("6", "6 Hours", "6 ساعات"),
                new("8", "8 Hours", "8 ساعات")
            ],
            true,
            ServiceAttributeFilterScope.Hourly,
            70),
        new(
            "serviceDate",
            "تاريخ الخدمة",
            "Service Date",
            ServiceAttributeType.Date,
            null,
            true,
            ServiceAttributeFilterScope.Hourly,
            80),
        new(
            "deliveryWindow",
            "نافذة التوصيل",
            "Delivery Window",
            ServiceAttributeType.OptionSet,
            [
                new("07:00-09:00", "07:00-09:00", "07:00-09:00"),
                new("15:00-17:00", "15:00-17:00", "15:00-17:00")
            ],
            false,
            ServiceAttributeFilterScope.Hourly,
            90)
    ];

    private static readonly ServiceAttributeSeed[] MonthlyAttributeSeeds =
    [
        new(
            "providerSource",
            "مزود الخدمة",
            "Service Provider",
            ServiceAttributeType.OptionSet,
            null,
            true,
            ServiceAttributeFilterScope.Monthly | ServiceAttributeFilterScope.Resident,
            10),
        new(
            "contractDuration",
            "مدة التعاقد",
            "Contract Duration",
            ServiceAttributeType.OptionSet,
            [
                new("1-month", "1 Month", "شهر واحد"),
                new("3-months", "3 Months", "3 أشهر"),
                new("6-months", "6 Months", "6 أشهر"),
                new("12-months", "12 Months", "12 شهر")
            ],
            true,
            ServiceAttributeFilterScope.Monthly | ServiceAttributeFilterScope.Resident,
            20),
        new(
            "workersCount",
            "عدد العمال",
            "Workers Count",
            ServiceAttributeType.Int,
            [
                new("1", "1 Worker", "عامل واحد"),
                new("2", "2 Workers", "عاملان")
            ],
            true,
            ServiceAttributeFilterScope.Monthly | ServiceAttributeFilterScope.Resident,
            30),
        new(
            "nationality",
            "الجنسية",
            "Nationality",
            ServiceAttributeType.OptionSet,
            [
                new("philippines", "Philippines", "الفلبين"),
                new("indonesia", "Indonesia", "إندونيسيا"),
                new("africa", "Africa", "أفريقيا")
            ],
            false,
            ServiceAttributeFilterScope.Monthly | ServiceAttributeFilterScope.Resident,
            40),
        new(
            "serviceDate",
            "تاريخ بداية الخدمة",
            "Service Start Date",
            ServiceAttributeType.Date,
            null,
            true,
            ServiceAttributeFilterScope.Monthly | ServiceAttributeFilterScope.Resident,
            50),
        new(
            "notes",
            "ملاحظات",
            "Notes",
            ServiceAttributeType.Input,
            null,
            false,
            ServiceAttributeFilterScope.Monthly | ServiceAttributeFilterScope.Resident,
            60)
    ];

    private static string SerializeIntList(IEnumerable<int> values)
    {
        var normalized = values
            .Where(x => x > 0)
            .Distinct()
            .OrderBy(x => x)
            .ToArray();
        return JsonSerializer.Serialize(normalized);
    }

    private static string SerializeStringList(IEnumerable<string> values)
    {
        var normalized = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();
        return JsonSerializer.Serialize(normalized);
    }

    private static IReadOnlyList<int> ParseIntList(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return [];
        }

        try
        {
            return (JsonSerializer.Deserialize<List<int>>(source) ?? [])
                .Where(x => x > 0)
                .Distinct()
                .OrderBy(x => x)
                .ToArray();
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
            return (JsonSerializer.Deserialize<List<string>>(source) ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x)
                .ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static IReadOnlyList<DemoOfferSeed> BuildDemoOfferSeeds()
    {
        var seeds = new List<DemoOfferSeed>(BaseDemoOfferSeeds);
        seeds.AddRange(BuildEnayaHourlyOfferSeedsFromJson());
        return seeds;
    }

    private static IReadOnlyList<DemoOfferSeed> BuildEnayaHourlyOfferSeedsFromJson()
    {
        var extractedPoints = ExtractEnayaHourlyPackagePointsFromJson();
        var sourcePoints = extractedPoints.Count > 0 ? extractedPoints : BuildFallbackEnayaHourlyPackagePoints();
        var expandedPoints = ExpandEnayaHourlyPackagePoints(sourcePoints);

        var seeds = new List<DemoOfferSeed>(expandedPoints.Count);
        var displayOrder = 10;
        foreach (var point in expandedPoints
            .OrderBy(x => x.Hours)
            .ThenBy(x => x.Workers)
            .ThenBy(x => x.WeeklyVisits)
            .ThenBy(x => x.Nationality, StringComparer.OrdinalIgnoreCase))
        {
            var nationalityEn = NormalizeNationalityLabel(point.Nationality);
            var nationalityAr = TranslateNationalityToArabic(nationalityEn);
            var providerServiceId = BuildHourlyProviderServiceId(point.Hours, point.Workers, point.WeeklyVisits, nationalityEn);
            var workersLabelEn = point.Workers == 1 ? "Worker" : "Workers";
            var visitsLabelEn = point.WeeklyVisits == 1 ? "Visit Weekly" : "Visits Weekly";

            seeds.Add(new DemoOfferSeed(
                ProviderCode: "enaya",
                ProviderServiceId: providerServiceId,
                ServiceMode: ServiceMode.Hourly,
                DisplayOrder: displayOrder,
                NameAr: $"عناية - {point.Hours} ساعات / {point.Workers} عامل / {point.WeeklyVisits} زيارة أسبوعياً / {nationalityAr}",
                NameEn: $"Enaya - {point.Hours} Hours / {point.Workers} {workersLabelEn} / {point.WeeklyVisits} {visitsLabelEn} / {nationalityEn}",
                HourOptions: [point.Hours],
                NationalityOptions: [nationalityEn],
                FinalPriceSar: point.FinalPriceSar,
                OriginalPriceSar: point.OriginalPriceSar,
                SourceType: DataSourceType.Api));

            displayOrder += 10;
        }

        return seeds;
    }

    private static IReadOnlyList<EnayaHourlyPackagePoint> BuildFallbackEnayaHourlyPackagePoints()
    {
        return
        [
            new("Africa", 4, 1, 1, 75.00m, 147.20m),
            new("Philippines", 4, 1, 1, 80.00m, 147.20m),
            new("Indonesia", 4, 1, 1, 85.00m, 147.20m)
        ];
    }

    private static IReadOnlyList<EnayaHourlyPackagePoint> ExpandEnayaHourlyPackagePoints(
        IReadOnlyList<EnayaHourlyPackagePoint> sourcePoints)
    {
        var basePoints = sourcePoints
            .Where(x =>
                x.Hours > 0 &&
                x.Workers > 0 &&
                x.WeeklyVisits > 0 &&
                !string.IsNullOrWhiteSpace(x.Nationality) &&
                x.FinalPriceSar > 0)
            .GroupBy(x => $"{NormalizeToken(x.Nationality)}|{x.Hours}")
            .Select(group =>
                group
                    .OrderBy(x => x.Workers)
                    .ThenBy(x => x.WeeklyVisits)
                    .ThenBy(x => x.FinalPriceSar)
                    .First())
            .ToList();

        var expanded = new List<EnayaHourlyPackagePoint>();
        foreach (var point in basePoints)
        {
            foreach (var workers in EnayaHourlyWorkersOptions)
            {
                foreach (var weeklyVisits in EnayaHourlyWeeklyVisitsOptions)
                {
                    var multiplier = workers * weeklyVisits;
                    var finalPrice = Math.Round(point.FinalPriceSar * multiplier, 2);
                    decimal? originalPrice = point.OriginalPriceSar.HasValue
                        ? Math.Round(point.OriginalPriceSar.Value * multiplier, 2)
                        : null;

                    expanded.Add(new EnayaHourlyPackagePoint(
                        Nationality: NormalizeNationalityLabel(point.Nationality),
                        Hours: point.Hours,
                        Workers: workers,
                        WeeklyVisits: weeklyVisits,
                        FinalPriceSar: finalPrice,
                        OriginalPriceSar: originalPrice));
                }
            }
        }

        return expanded
            .GroupBy(x => $"{NormalizeToken(x.Nationality)}|{x.Hours}|{x.Workers}|{x.WeeklyVisits}")
            .Select(group => group.First())
            .ToList();
    }

    private static IReadOnlyList<EnayaHourlyPackagePoint> ExtractEnayaHourlyPackagePointsFromJson()
    {
        if (!JsonSamples.TryGetValue("enaya_fawran_real_json_bundle", out var filePath))
        {
            return [];
        }

        var fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", filePath));
        if (!File.Exists(fullPath))
        {
            return [];
        }

        try
        {
            using var json = JsonDocument.Parse(File.ReadAllText(fullPath));
            var points = new List<EnayaHourlyPackagePoint>();
            CollectEnayaHourlyPackagePoints(json.RootElement, points);

            return points
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.Nationality) &&
                    x.Hours > 0 &&
                    x.Workers > 0 &&
                    x.WeeklyVisits > 0 &&
                    x.FinalPriceSar > 0)
                .GroupBy(x => $"{NormalizeToken(x.Nationality)}|{x.Hours}|{x.Workers}|{x.WeeklyVisits}")
                .Select(group => group.OrderBy(x => x.FinalPriceSar).First())
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private static void CollectEnayaHourlyPackagePoints(JsonElement element, ICollection<EnayaHourlyPackagePoint> points)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var nationality = TryReadStringValue(element, "nationality", "resourceGroupName");
            var hours = TryReadIntValue(element, "hours", "hoursNumber", "visitHours");
            var workers = TryReadIntValue(element, "workers", "employeeNumber");
            var weeklyVisits = TryReadIntValue(element, "visits", "weeklyVisits");
            var finalPrice = TryReadDecimalValue(element, "display_price_sar", "finalPrice");
            var originalPrice = TryReadDecimalValue(element, "original_price_sar", "oneVisitPrice");

            if (!string.IsNullOrWhiteSpace(nationality) &&
                hours > 0 &&
                workers > 0 &&
                weeklyVisits > 0 &&
                finalPrice.HasValue &&
                finalPrice.Value > 0)
            {
                points.Add(new EnayaHourlyPackagePoint(
                    Nationality: nationality,
                    Hours: hours,
                    Workers: workers,
                    WeeklyVisits: weeklyVisits,
                    FinalPriceSar: finalPrice.Value,
                    OriginalPriceSar: originalPrice));
            }

            foreach (var property in element.EnumerateObject())
            {
                CollectEnayaHourlyPackagePoints(property.Value, points);
            }
            return;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                CollectEnayaHourlyPackagePoints(item, points);
            }
        }
    }

    private static string BuildHourlyProviderServiceId(int hours, int workers, int weeklyVisits, string nationality)
    {
        var slug = NormalizeToken(nationality);
        var id = $"enaya-hourly-h{hours}-w{workers}-v{weeklyVisits}-{slug}";
        return id.Length <= 120 ? id : id[..120];
    }

    private static string NormalizeNationalityLabel(string value)
    {
        var normalized = NormalizeToken(value);
        return normalized switch
        {
            "afrca" => "Africa",
            "africa" => "Africa",
            "africancountries" => "African Countries",
            "philippines" => "Philippines",
            "indonesia" => "Indonesia",
            "eastasia" => "East Asia",
            _ => value.Trim()
        };
    }

    private static string TranslateNationalityToArabic(string value)
    {
        return NormalizeToken(value) switch
        {
            "afrca" => "أفريقيا",
            "africa" => "أفريقيا",
            "africancountries" => "الدول الأفريقية",
            "philippines" => "الفلبين",
            "indonesia" => "إندونيسيا",
            "eastasia" => "شرق آسيا",
            _ => value
        };
    }

    private static string NormalizeToken(string value)
    {
        var chars = value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray();
        return chars.Length == 0 ? "unknown" : new string(chars);
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static string? TryReadStringValue(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetPropertyIgnoreCase(element, propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                var result = value.GetString();
                if (!string.IsNullOrWhiteSpace(result))
                {
                    return result.Trim();
                }
            }
            else if (value.ValueKind == JsonValueKind.Number)
            {
                return value.ToString();
            }
        }

        return null;
    }

    private static int TryReadIntValue(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetPropertyIgnoreCase(element, propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var numberValue))
            {
                return numberValue;
            }

            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return 0;
    }

    private static decimal? TryReadDecimalValue(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetPropertyIgnoreCase(element, propertyName, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var decimalValue))
            {
                return decimalValue;
            }

            if (value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

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
                ContractDraftExpirationHours = 12,
                SettingsJson = BuildProviderSettingsJson("mueen")
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
                ContractDraftExpirationHours = 12,
                SettingsJson = BuildProviderSettingsJson("tamkeen")
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
                ContractDraftExpirationHours = 24,
                SettingsJson = BuildProviderSettingsJson("almutahidah")
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
                ContractDraftExpirationHours = 24,
                SettingsJson = BuildProviderSettingsJson("irc-saudi")
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
                ContractDraftExpirationHours = 24,
                SettingsJson = BuildProviderSettingsJson("enaya")
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
                ContractDraftExpirationHours = 24,
                SettingsJson = BuildProviderSettingsJson("esad-talents")
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
                ContractDraftExpirationHours = 24,
                SettingsJson = BuildProviderSettingsJson("emdad-hr")
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
                ContractDraftExpirationHours = 24,
                SettingsJson = BuildProviderSettingsJson("eitinaa")
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
            existing.SettingsJson = provider.SettingsJson;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await SeedDemoOffersAndPriceSnapshotsAsync(dbContext, cancellationToken);
        await SeedServiceAttributesAsync(dbContext, cancellationToken);
        await SeedProviderJsonDocumentsAsync(dbContext, cancellationToken);
        await SeedProviderAttributeValueMappersAsync(dbContext, cancellationToken);
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

        // Seed a deterministic refresh token for test/dev compatibility.
        var existingSession = await dbContext.CustomerAuthSessions
            .FirstOrDefaultAsync(x => x.RefreshToken == "demo-refresh-token-123456", cancellationToken);
        if (existingSession is null)
        {
            await dbContext.CustomerAuthSessions.AddAsync(new CustomerAuthSession
            {
                CustomerAccountId = customer.Id,
                RefreshToken = "demo-refresh-token-123456",
                CreatedAtUtc = now,
                LastUsedAtUtc = now,
                ExpiresAtUtc = now.AddDays(30),
                IsRevoked = false,
                RevokedAtUtc = null,
                ReplacedByRefreshToken = null
            }, cancellationToken);
        }
        else
        {
            existingSession.CustomerAccountId = customer.Id;
            existingSession.LastUsedAtUtc = now;
            existingSession.ExpiresAtUtc = now.AddDays(30);
            existingSession.IsRevoked = false;
            existingSession.RevokedAtUtc = null;
            existingSession.ReplacedByRefreshToken = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedDemoOffersAndPriceSnapshotsAsync(BelkhedmaDbContext dbContext, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var demoOfferSeeds = BuildDemoOfferSeeds();
        var providerByCode = await dbContext.Providers
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Code, x => x, cancellationToken);

        var offers = await dbContext.ServiceOffers.ToListAsync(cancellationToken);

        if (providerByCode.TryGetValue("enaya", out var enayaProvider))
        {
            var allowedEnayaHourlyServiceIds = demoOfferSeeds
                .Where(x => x.ProviderCode == "enaya" && x.ServiceMode == ServiceMode.Hourly)
                .Select(x => x.ProviderServiceId)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var staleEnayaHourlyOffers = offers
                .Where(x =>
                    x.ProviderId == enayaProvider.Id &&
                    x.ServiceMode == ServiceMode.Hourly &&
                    !allowedEnayaHourlyServiceIds.Contains(x.ProviderServiceId))
                .ToList();

            if (staleEnayaHourlyOffers.Count > 0)
            {
                var staleOfferIds = staleEnayaHourlyOffers
                    .Select(x => x.Id)
                    .ToHashSet();

                var stalePriceSnapshots = await dbContext.PriceSnapshots
                    .Where(x => staleOfferIds.Contains(x.ServiceOfferId))
                    .ToListAsync(cancellationToken);
                if (stalePriceSnapshots.Count > 0)
                {
                    dbContext.PriceSnapshots.RemoveRange(stalePriceSnapshots);
                }

                var staleAttributes = await dbContext.ServiceAttributes
                    .Where(x => staleOfferIds.Contains(x.ServiceOfferId))
                    .ToListAsync(cancellationToken);
                if (staleAttributes.Count > 0)
                {
                    dbContext.ServiceAttributes.RemoveRange(staleAttributes);
                }

                var staleDocs = await dbContext.ProviderJsonDocuments
                    .Where(x => staleOfferIds.Contains(x.ServiceOfferId))
                    .ToListAsync(cancellationToken);
                if (staleDocs.Count > 0)
                {
                    dbContext.ProviderJsonDocuments.RemoveRange(staleDocs);
                }

                var staleMappers = await dbContext.ProviderAttributeValueMappers
                    .Where(x => x.ServiceOfferId.HasValue && staleOfferIds.Contains(x.ServiceOfferId.Value))
                    .ToListAsync(cancellationToken);
                if (staleMappers.Count > 0)
                {
                    dbContext.ProviderAttributeValueMappers.RemoveRange(staleMappers);
                }

                dbContext.ServiceOffers.RemoveRange(staleEnayaHourlyOffers);
                offers.RemoveAll(x => staleOfferIds.Contains(x.Id));
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        foreach (var seed in demoOfferSeeds)
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
                    DisplayOrder = seed.DisplayOrder,
                    NameAr = seed.NameAr,
                    NameEn = seed.NameEn,
                    HourlyHoursJson = SerializeIntList(seed.HourOptions),
                    NationalityGroupsJson = SerializeStringList(seed.NationalityOptions),
                    IsAvailable = true,
                    UpdatedAtUtc = now
                };

                offers.Add(existingOffer);
                await dbContext.ServiceOffers.AddAsync(existingOffer, cancellationToken);
                continue;
            }

            existingOffer.ServiceMode = seed.ServiceMode;
            existingOffer.DisplayOrder = seed.DisplayOrder;
            existingOffer.NameAr = seed.NameAr;
            existingOffer.NameEn = seed.NameEn;
            existingOffer.HourlyHoursJson = SerializeIntList(seed.HourOptions);
            existingOffer.NationalityGroupsJson = SerializeStringList(seed.NationalityOptions);
            existingOffer.IsAvailable = true;
            existingOffer.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var priceRows = await dbContext.PriceSnapshots.ToListAsync(cancellationToken);
        foreach (var seed in demoOfferSeeds)
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

    private static async Task SeedServiceAttributesAsync(BelkhedmaDbContext dbContext, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var offers = await dbContext.ServiceOffers.ToListAsync(cancellationToken);
        var existingAttributes = await dbContext.ServiceAttributes.ToListAsync(cancellationToken);
        var providerCodeById = await dbContext.Providers
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Code, cancellationToken);

        foreach (var offer in offers)
        {
            var providerCode = providerCodeById.TryGetValue(offer.ProviderId, out var code) ? code : string.Empty;
            var attributeSeeds = offer.ServiceMode switch
            {
                ServiceMode.Hourly => HourlyAttributeSeeds,
                ServiceMode.Monthly or ServiceMode.Resident => MonthlyAttributeSeeds,
                _ => Array.Empty<ServiceAttributeSeed>()
            };

            foreach (var seed in attributeSeeds)
            {
                var existing = existingAttributes.FirstOrDefault(x =>
                    x.ServiceOfferId == offer.Id &&
                    x.AttributeKey == seed.AttributeKey);

                var optionSetJson = seed.OptionSet is { Count: > 0 }
                    ? JsonSerializer.Serialize(seed.OptionSet)
                    : null;

                if (seed.AttributeKey == "providerSource")
                {
                    var providerOption = new[]
                    {
                        new LocalizedOptionSeed(
                            providerCode,
                            string.IsNullOrWhiteSpace(providerCode) ? offer.NameEn : providerCode.ToUpperInvariant(),
                            offer.NameAr)
                    };
                    optionSetJson = JsonSerializer.Serialize(providerOption);
                }

                if (existing is null)
                {
                    existing = new ServiceAttribute
                    {
                        ServiceOfferId = offer.Id,
                        AttributeKey = seed.AttributeKey
                    };
                    existingAttributes.Add(existing);
                    await dbContext.ServiceAttributes.AddAsync(existing, cancellationToken);
                }

                existing.NameAr = seed.NameAr;
                existing.NameEn = seed.NameEn;
                existing.Type = seed.Type;
                existing.OptionSetJson = optionSetJson;
                existing.IsMandatory = seed.IsMandatory;
                existing.FilterScope = seed.FilterScope;
                existing.DisplayOrder = seed.DisplayOrder;
                existing.IsActive = true;
                existing.UpdatedAtUtc = now;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedProviderAttributeValueMappersAsync(BelkhedmaDbContext dbContext, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var providersByCode = await dbContext.Providers
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Code, x => x.Id, cancellationToken);
        var offers = await dbContext.ServiceOffers
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var existingMappers = await dbContext.ProviderAttributeValueMappers
            .ToListAsync(cancellationToken);

        foreach (var seed in ProviderAttributeMapperSeeds)
        {
            if (!providersByCode.TryGetValue(seed.ProviderCode, out var providerId))
            {
                continue;
            }

            var matchingOfferIds = offers
                .Where(x =>
                    x.ProviderId == providerId &&
                    (!seed.ServiceMode.HasValue || x.ServiceMode == seed.ServiceMode.Value))
                .OrderBy(x => x.DisplayOrder)
                .Select(x => x.Id)
                .ToList();

            if (matchingOfferIds.Count == 0)
            {
                matchingOfferIds.Add(Guid.Empty);
            }
            foreach (var candidateOfferId in matchingOfferIds)
            {
                Guid? serviceOfferId = candidateOfferId == Guid.Empty ? null : candidateOfferId;

                var existing = existingMappers.FirstOrDefault(x =>
                    x.ProviderId == providerId &&
                    x.ServiceOfferId == serviceOfferId &&
                    x.ServiceMode == seed.ServiceMode &&
                    string.Equals(x.RawAttributeKey, seed.RawAttributeKey, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(x.RawValue, seed.RawValue, StringComparison.OrdinalIgnoreCase));

                if (existing is null)
                {
                    existing = new ProviderAttributeValueMapper
                    {
                        ProviderId = providerId,
                        ServiceOfferId = serviceOfferId,
                        ServiceMode = seed.ServiceMode,
                        RawAttributeKey = seed.RawAttributeKey,
                        RawValue = seed.RawValue
                    };
                    existingMappers.Add(existing);
                    await dbContext.ProviderAttributeValueMappers.AddAsync(existing, cancellationToken);
                }

                existing.RawTextEn = seed.RawTextEn;
                existing.RawTextAr = seed.RawTextAr;
                existing.NormalizedAttributeKey = seed.NormalizedAttributeKey;
                existing.NormalizedValue = seed.NormalizedValue;
                existing.NormalizedTextEn = seed.NormalizedTextEn;
                existing.NormalizedTextAr = seed.NormalizedTextAr;
                existing.IsActive = true;
                existing.UpdatedAtUtc = now;
            }
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
                Code = "enaya-hourly-slider",
                CompanyNameAr = "عناية",
                CompanyNameEn = "Enaya",
                TitleAr = "عروض عناية بالساعة",
                TitleEn = "Enaya Hourly Offers",
                SubtitleAr = "خدمات منزلية فورية مع خيارات مرنة",
                SubtitleEn = "Instant home services with flexible options.",
                ImageUrl = "https://enaya.sa:8001/SiteImages/SliderWebImages/%7BC08A1D99-5786-F011-A912-000D3A227AB4%7Dhourlyworker.png",
                TargetUrl = "https://enaya.sa/home",
                DeepLink = "belkhedma://promotions/enaya-hourly",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "Hourly cleaning",
                    "Flexible shifts",
                    "Trusted provider"
                }),
                ProviderCode = "enaya",
                DisplayOrder = 1,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Code = "enaya-resident-slider",
                CompanyNameAr = "عناية",
                CompanyNameEn = "Enaya",
                TitleAr = "خدمات عناية المقيمة",
                TitleEn = "Enaya Resident Services",
                SubtitleAr = "باقات إقامة شهرية للعاملات المنزلية",
                SubtitleEn = "Monthly resident worker packages.",
                ImageUrl = "https://enaya.sa:8001/SiteImages/SliderWebImages/%7BDB45E476-5786-F011-A912-000D3A227AB4%7Dresidentworker.png",
                TargetUrl = "https://enaya.sa/home",
                DeepLink = "belkhedma://promotions/enaya-resident",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "Resident packages",
                    "Monthly contracts",
                    "Home support"
                }),
                ProviderCode = "enaya",
                DisplayOrder = 2,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Code = "enaya-mediation-slider",
                CompanyNameAr = "عناية",
                CompanyNameEn = "Enaya",
                TitleAr = "عروض التوسط من عناية",
                TitleEn = "Enaya Mediation Offers",
                SubtitleAr = "خدمات توسط واستقدام بأفضل الخيارات",
                SubtitleEn = "Mediation and recruitment with top options.",
                ImageUrl = "https://enaya.sa:8001/SiteImages/SliderWebImages/%7B5CAEE1B1-5786-F011-A912-000D3A227AB4%7Dmediation.png",
                TargetUrl = "https://enaya.sa/home",
                DeepLink = "belkhedma://promotions/enaya-mediation",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "Mediation support",
                    "Recruitment options",
                    "Fast response"
                }),
                ProviderCode = "enaya",
                DisplayOrder = 3,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Code = "tamkeen-hourly-slider",
                CompanyNameAr = "تمكين",
                CompanyNameEn = "Tamkeen",
                TitleAr = "خدمات بالساعة من تمكين",
                TitleEn = "Tamkeen Hourly Services",
                SubtitleAr = "كوادر منزلية مدربة وحجز سريع",
                SubtitleEn = "Trained domestic staff with fast booking.",
                ImageUrl = "https://www.tamkeenhr.sa/_astro/medium_Banner_hourly_7492672ab9_ZdSyNi.webp",
                TargetUrl = "https://www.tamkeenhr.sa/service-hour",
                DeepLink = "belkhedma://promotions/tamkeen-hourly",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "Hourly services",
                    "Fast booking",
                    "Licensed provider"
                }),
                ProviderCode = "tamkeen",
                DisplayOrder = 4,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Code = "tamkeen-resident-slider",
                CompanyNameAr = "تمكين",
                CompanyNameEn = "Tamkeen",
                TitleAr = "خدمات مقيمة من تمكين",
                TitleEn = "Tamkeen Resident Services",
                SubtitleAr = "حلول شهرية للإقامة وخدمات المنزل",
                SubtitleEn = "Monthly resident solutions for households.",
                ImageUrl = "https://www.tamkeenhr.sa/_astro/medium_Banner_Ass_cleaner_c12ce5c50c_Z38BLb.webp",
                TargetUrl = "https://www.tamkeenhr.sa/service-resident",
                DeepLink = "belkhedma://promotions/tamkeen-resident",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "Resident packages",
                    "Monthly contracts",
                    "Flexible service"
                }),
                ProviderCode = "tamkeen",
                DisplayOrder = 5,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Code = "tamkeen-b2c-slider",
                CompanyNameAr = "تمكين",
                CompanyNameEn = "Tamkeen",
                TitleAr = "عروض تمكين للأفراد",
                TitleEn = "Tamkeen B2C Offers",
                SubtitleAr = "خيارات مميزة لخدمات الأفراد",
                SubtitleEn = "Specialized packages for individuals.",
                ImageUrl = "https://backend.tamkeenhr.sa/uploads/medium_B2_C_4_703f2239f9.jpg",
                TargetUrl = "https://www.tamkeenhr.sa/",
                DeepLink = "belkhedma://promotions/tamkeen-b2c",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "B2C campaigns",
                    "Trusted staffing",
                    "Professional support"
                }),
                ProviderCode = "tamkeen",
                DisplayOrder = 6,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Code = "mueen-main-banner",
                CompanyNameAr = "معين",
                CompanyNameEn = "Mueen",
                TitleAr = "عروض معين للخدمات المنزلية",
                TitleEn = "Mueen Home Services Offers",
                SubtitleAr = "خدمات منزلية متنوعة للأفراد والشركات",
                SubtitleEn = "Diverse on-demand services for home and business.",
                ImageUrl = "https://www.mueen.com.sa/uploads/banners/Mueen_banner.png",
                TargetUrl = "https://www.mueen.com.sa/",
                DeepLink = "belkhedma://promotions/mueen-home",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "On-demand labor",
                    "Multiple sectors",
                    "Nationwide branches"
                }),
                ProviderCode = "mueen",
                DisplayOrder = 7,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Code = "mueen-featured-banner",
                CompanyNameAr = "معين",
                CompanyNameEn = "Mueen",
                TitleAr = "خدمة فورية من معين",
                TitleEn = "Featured Mueen Campaign",
                SubtitleAr = "احجز بسرعة عبر منصة معين",
                SubtitleEn = "Book quickly through Mueen platform.",
                ImageUrl = "https://www.mueen.com.sa/uploads/banners/Z62_3957_new.png",
                TargetUrl = "https://www.mueen.com.sa/en/on-demand-services",
                DeepLink = "belkhedma://promotions/mueen-featured",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "Quick booking",
                    "Reliable workers",
                    "Service flexibility"
                }),
                ProviderCode = "mueen",
                DisplayOrder = 8,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Code = "emdad-fawran-app",
                CompanyNameAr = "إمداد",
                CompanyNameEn = "Emdad",
                TitleAr = "فوراً من إمداد",
                TitleEn = "Emdad Fawran",
                SubtitleAr = "حلول مرنة وفورية للعمالة المنزلية",
                SubtitleEn = "Flexible and instant domestic labor solutions.",
                ImageUrl = "https://emdadhr.com:8020//media/0u5beaal/app2.png",
                TargetUrl = "https://emdadhr.com/#/fawran",
                DeepLink = "belkhedma://promotions/emdad-fawran",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "Instant services",
                    "Domestic workers",
                    "Digital booking journey"
                }),
                ProviderCode = "emdad-hr",
                DisplayOrder = 9,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Code = "almutahidah-slider-main",
                CompanyNameAr = "الشركة المتحدة",
                CompanyNameEn = "Almutahidah",
                TitleAr = "عروض الشركة المتحدة",
                TitleEn = "Almutahidah Promotions",
                SubtitleAr = "باقات منزلية متنوعة ومرنة",
                SubtitleEn = "Flexible domestic service campaigns.",
                ImageUrl = "https://crm.almutahidah.com:8000/SliderWebImages/%7BB033829A-9E78-EE11-8159-BCC9007DA0C2%7D4%20(4)%20(2).jpg",
                TargetUrl = "https://almutahidah.com/home",
                DeepLink = "belkhedma://promotions/almutahidah-main",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "Main slider campaign",
                    "Domestic workforce",
                    "Easy booking"
                }),
                ProviderCode = "almutahidah",
                DisplayOrder = 10,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Code = "almutahidah-slider-alt",
                CompanyNameAr = "الشركة المتحدة",
                CompanyNameEn = "Almutahidah",
                TitleAr = "حملة موسمية من المتحدة",
                TitleEn = "Almutahidah Seasonal Campaign",
                SubtitleAr = "عروض موسمية لخدمات المنازل",
                SubtitleEn = "Seasonal offers for household services.",
                ImageUrl = "https://crm.almutahidah.com:8000/SliderWebImages/%7B43122DC4-5476-EE11-8159-BCC9007DA0C2%7Dafc1%20-%20Copy.jpeg",
                TargetUrl = "https://almutahidah.com/home",
                DeepLink = "belkhedma://promotions/almutahidah-seasonal",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "Seasonal discount",
                    "Qualified workers",
                    "Reliable service"
                }),
                ProviderCode = "almutahidah",
                DisplayOrder = 11,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Code = "esad-home-main",
                CompanyNameAr = "إسناد",
                CompanyNameEn = "Esad Talents",
                TitleAr = "عروض إسناد للمواهب",
                TitleEn = "Esad Talents Offers",
                SubtitleAr = "حلول استقدام وخدمات موارد بشرية",
                SubtitleEn = "Recruitment and HR service solutions.",
                ImageUrl = "https://esadtalents.com/Content/imgs/home-img-1.jpg",
                TargetUrl = "https://esadtalents.com/",
                DeepLink = "belkhedma://promotions/esad-home",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "HR outsourcing",
                    "Recruitment services",
                    "Business support"
                }),
                ProviderCode = "esad-talents",
                DisplayOrder = 12,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new()
            {
                Code = "esad-home-alt",
                CompanyNameAr = "إسناد",
                CompanyNameEn = "Esad Talents",
                TitleAr = "حملة مميزة من إسناد",
                TitleEn = "Esad Featured Campaign",
                SubtitleAr = "كوادر مؤهلة بخيارات متعددة",
                SubtitleEn = "Qualified talents with multiple options.",
                ImageUrl = "https://esadtalents.com/Content/imgs/home-img-2.jpg",
                TargetUrl = "https://esadtalents.com/",
                DeepLink = "belkhedma://promotions/esad-featured",
                ItemsJson = JsonSerializer.Serialize(new[]
                {
                    "Qualified talents",
                    "Flexible contracts",
                    "Fast onboarding"
                }),
                ProviderCode = "esad-talents",
                DisplayOrder = 13,
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
