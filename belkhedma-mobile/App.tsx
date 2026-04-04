import { StatusBar } from "expo-status-bar";
import { useEffect, useMemo, useState } from "react";
import {
  ActivityIndicator,
  FlatList,
  Image,
  Linking,
  SafeAreaView,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from "react-native";
import {
  getCustomerSavedLocations,
  getLatestPrices,
  getProviderJsonDocuments,
  getProviders,
  getServiceOffers,
} from "./src/services/marketplaceApi";
import { Brand } from "./src/theme/brand";
import {
  CustomerSavedLocation,
  PriceSnapshot,
  Provider,
  ProviderJsonDocument,
  ServiceOffer,
} from "./src/types/marketplace";

type LanguageMode = "ar" | "en";
type ServiceGroup = "hourly-cleaning" | "monthly" | "medical-services" | "mediation-services";
type WizardStep = 0 | 1 | 2 | 3 | 4;

function normalizeText(value: string): string {
  return value.toLowerCase().trim();
}

function inferServiceGroup(offer: ServiceOffer, provider?: Provider): ServiceGroup {
  const source = normalizeText(`${offer.nameEn} ${offer.nameAr} ${provider?.providerType ?? ""}`);

  if (
    source.includes("medical") ||
    source.includes("طبي") ||
    source.includes("maintenance") ||
    source.includes("صيانة")
  ) {
    return "medical-services";
  }

  if (
    source.includes("mediation") ||
    source.includes("recruit") ||
    source.includes("وساطة") ||
    source.includes("استقدام") ||
    provider?.supportsRecruitment
  ) {
    return "mediation-services";
  }

  if (offer.serviceMode === 2 || offer.serviceMode === 3 || source.includes("month") || source.includes("شهري")) {
    return "monthly";
  }

  return "hourly-cleaning";
}

function extractNumbersByRegex(value: string, regex: RegExp): number[] {
  const matches = value.match(regex) ?? [];
  return matches
    .map((m) => Number(m.replace(/\D/g, "")))
    .filter((n) => Number.isFinite(n) && n > 0);
}

export default function App() {
  const [providers, setProviders] = useState<Provider[]>([]);
  const [serviceOffers, setServiceOffers] = useState<ServiceOffer[]>([]);
  const [prices, setPrices] = useState<PriceSnapshot[]>([]);
  const [jsonDocuments, setJsonDocuments] = useState<ProviderJsonDocument[]>([]);
  const [savedLocations, setSavedLocations] = useState<CustomerSavedLocation[]>([]);

  const [selectedProvider, setSelectedProvider] = useState<string | null>(null);
  const [selectedGroup, setSelectedGroup] = useState<ServiceGroup | null>(null);
  const [selectedSubServiceId, setSelectedSubServiceId] = useState<string | null>(null);
  const [hourlyHours, setHourlyHours] = useState<number>(4);
  const [monthlyDurationMonths, setMonthlyDurationMonths] = useState<number>(1);
  const [serviceDate, setServiceDate] = useState<string>("");
  const [customerReference, setCustomerReference] = useState<string>("demo-customer");
  const [selectedLocationId, setSelectedLocationId] = useState<string | null>(null);
  const [searchText, setSearchText] = useState<string>("");

  const [wizardStep, setWizardStep] = useState<WizardStep>(0);
  const [languageMode, setLanguageMode] = useState<LanguageMode>("en");
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void loadData();
  }, []);

  const providerById = useMemo(() => {
    return providers.reduce<Record<string, Provider>>((acc, provider) => {
      acc[provider.id] = provider;
      return acc;
    }, {});
  }, [providers]);

  const offerById = useMemo(() => {
    return serviceOffers.reduce<Record<string, ServiceOffer>>((acc, offer) => {
      acc[offer.id] = offer;
      return acc;
    }, {});
  }, [serviceOffers]);

  const filteredProviders = useMemo(() => {
    if (!selectedProvider) return providers;
    return providers.filter((p) => p.code === selectedProvider);
  }, [providers, selectedProvider]);

  const visibleOffers = useMemo(() => {
    const visibleProviderIds = new Set(filteredProviders.map((p) => p.id));
    return serviceOffers.filter((offer) => visibleProviderIds.has(offer.providerId));
  }, [filteredProviders, serviceOffers]);

  const availableGroups = useMemo(() => {
    const groups = new Set<ServiceGroup>();
    for (const offer of visibleOffers) {
      groups.add(inferServiceGroup(offer, providerById[offer.providerId]));
    }
    return Array.from(groups);
  }, [providerById, visibleOffers]);

  useEffect(() => {
    if (availableGroups.length === 0) {
      setSelectedGroup(null);
      return;
    }
    if (!selectedGroup || !availableGroups.includes(selectedGroup)) {
      setSelectedGroup(availableGroups[0]);
    }
  }, [availableGroups, selectedGroup]);

  const groupFilteredOffers = useMemo(() => {
    if (!selectedGroup) return [];
    return visibleOffers.filter((offer) => inferServiceGroup(offer, providerById[offer.providerId]) === selectedGroup);
  }, [providerById, selectedGroup, visibleOffers]);

  useEffect(() => {
    if (groupFilteredOffers.length === 0) {
      setSelectedSubServiceId(null);
      return;
    }
    if (!selectedSubServiceId || !groupFilteredOffers.some((x) => x.id === selectedSubServiceId)) {
      setSelectedSubServiceId(groupFilteredOffers[0].id);
    }
  }, [groupFilteredOffers, selectedSubServiceId]);

  const selectedSubService = useMemo(
    () => groupFilteredOffers.find((x) => x.id === selectedSubServiceId) ?? null,
    [groupFilteredOffers, selectedSubServiceId]
  );

  const hourlyOptions = useMemo(() => {
    const values = new Set<number>();
    for (const offer of groupFilteredOffers) {
      const source = `${offer.nameEn} ${offer.nameAr}`;
      extractNumbersByRegex(source, /\d+\s*(hour|hours|ساعة|ساعات)/gi).forEach((n) => values.add(n));
    }
    if (values.size === 0) {
      values.add(4);
      values.add(8);
    }
    return Array.from(values).sort((a, b) => a - b);
  }, [groupFilteredOffers]);

  const monthlyDurationOptions = useMemo(() => {
    const values = new Set<number>();
    for (const offer of groupFilteredOffers) {
      const source = `${offer.nameEn} ${offer.nameAr}`;
      extractNumbersByRegex(source, /\d+\s*(month|months|شهر|أشهر)/gi).forEach((n) => values.add(n));
      extractNumbersByRegex(source, /\d+\s*(week|weeks|اسبوع|أسبوع)/gi).forEach((weeks) =>
        values.add(Math.max(1, Math.ceil(weeks / 4)))
      );
    }
    if (values.size === 0) {
      [1, 3, 6, 12].forEach((n) => values.add(n));
    }
    return Array.from(values).sort((a, b) => a - b);
  }, [groupFilteredOffers]);

  useEffect(() => {
    if (!hourlyOptions.includes(hourlyHours)) {
      setHourlyHours(hourlyOptions[0] ?? 4);
    }
  }, [hourlyHours, hourlyOptions]);

  useEffect(() => {
    if (!monthlyDurationOptions.includes(monthlyDurationMonths)) {
      setMonthlyDurationMonths(monthlyDurationOptions[0] ?? 1);
    }
  }, [monthlyDurationMonths, monthlyDurationOptions]);

  const selectedLocation = useMemo(
    () => savedLocations.find((x) => x.id === selectedLocationId) ?? null,
    [savedLocations, selectedLocationId]
  );

  const searchedPrices = useMemo(() => {
    const q = searchText.trim().toLowerCase();
    return prices.filter((price) => {
      const provider = providerById[price.providerId];
      if (!provider) return false;

      if (selectedProvider && provider.code !== selectedProvider) return false;
      if (selectedSubServiceId && price.serviceOfferId !== selectedSubServiceId) return false;

      if (!q) return true;
      const haystack = [provider.nameAr, provider.nameEn, provider.code, provider.providerType]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      return haystack.includes(q);
    });
  }, [prices, providerById, searchText, selectedProvider, selectedSubServiceId]);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);

      const [providersData, offersData, pricesData, docsData, locationsData] = await Promise.all([
        getProviders(),
        getServiceOffers(),
        getLatestPrices(),
        getProviderJsonDocuments(undefined, false),
        getCustomerSavedLocations(customerReference),
      ]);

      setProviders(providersData);
      setServiceOffers(offersData);
      setPrices(pricesData);
      setJsonDocuments(docsData);
      setSavedLocations(locationsData);
      setSelectedLocationId((current) => current ?? locationsData[0]?.id ?? null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unexpected error.");
    } finally {
      setLoading(false);
    }
  };

  const loadSavedLocations = async () => {
    try {
      const locationsData = await getCustomerSavedLocations(customerReference);
      setSavedLocations(locationsData);
      setSelectedLocationId((current) => {
        if (current && locationsData.some((x) => x.id === current)) {
          return current;
        }
        return locationsData[0]?.id ?? null;
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unexpected error while loading locations.");
    }
  };

  const groupLabel = (group: ServiceGroup) => {
    if (languageMode === "ar") {
      if (group === "hourly-cleaning") return "خدمات تنظيف بالساعة";
      if (group === "monthly") return "خدمات شهرية";
      if (group === "medical-services") return "خدمات طبية";
      return "خدمات وساطة";
    }
    if (group === "hourly-cleaning") return "Hourly Cleaning";
    if (group === "monthly") return "Monthly";
    if (group === "medical-services") return "Medical Services";
    return "Mediation Services";
  };

  const monthlyDurationLabel = (months: number) => {
    if (languageMode === "ar") {
      return months === 1 ? "1 شهر" : `${months} أشهر`;
    }
    return months === 1 ? "1 Month" : `${months} Months`;
  };

  const wizardSteps = [
    languageMode === "ar" ? "المجموعة" : "Group",
    languageMode === "ar" ? "الخدمة الفرعية" : "Sub Service",
    languageMode === "ar" ? "التفاصيل" : "Details",
    languageMode === "ar" ? "الموقع" : "Location",
    languageMode === "ar" ? "النتائج" : "Results",
  ];

  const canGoNext = useMemo(() => {
    if (wizardStep === 0) return !!selectedGroup;
    if (wizardStep === 1) return !!selectedSubServiceId;
    if (wizardStep === 2) return !!serviceDate;
    if (wizardStep === 3) return savedLocations.length === 0 || !!selectedLocationId;
    return true;
  }, [savedLocations.length, selectedGroup, selectedLocationId, selectedSubServiceId, serviceDate, wizardStep]);

  const goNext = () => {
    if (!canGoNext) return;
    setWizardStep((prev) => Math.min(4, prev + 1) as WizardStep);
  };

  const goBack = () => {
    setWizardStep((prev) => Math.max(0, prev - 1) as WizardStep);
  };

  return (
    <SafeAreaView style={styles.safeArea}>
      <StatusBar style="light" />
      <ScrollView contentContainerStyle={styles.page}>
        <View style={styles.header}>
          <Text style={styles.logo}>Belkhedma</Text>
          <Text style={styles.subtitle}>Dynamic wizard with service group and sub service</Text>
          <View style={styles.langSwitchRow}>
            <TouchableOpacity
              style={[styles.langButton, languageMode === "en" && styles.langButtonActive]}
              onPress={() => setLanguageMode("en")}
            >
              <Text style={[styles.langText, languageMode === "en" && styles.langTextActive]}>EN</Text>
            </TouchableOpacity>
            <TouchableOpacity
              style={[styles.langButton, languageMode === "ar" && styles.langButtonActive]}
              onPress={() => setLanguageMode("ar")}
            >
              <Text style={[styles.langText, languageMode === "ar" && styles.langTextActive]}>AR</Text>
            </TouchableOpacity>
          </View>
        </View>

        <View style={styles.filterCard}>
          <Text style={styles.sectionTitle}>{languageMode === "ar" ? "معالج الخدمة" : "Service Wizard"}</Text>
          <View style={styles.wizardStepsRow}>
            {wizardSteps.map((label, index) => {
              const active = wizardStep === index;
              const complete = wizardStep > index;
              return (
                <View key={label} style={styles.wizardStepItem}>
                  <View style={[styles.wizardBullet, (active || complete) && styles.wizardBulletActive]}>
                    <Text style={styles.wizardBulletText}>{index + 1}</Text>
                  </View>
                  <Text style={[styles.wizardStepLabel, active && styles.wizardStepLabelActive]}>{label}</Text>
                </View>
              );
            })}
          </View>

          {wizardStep === 0 ? (
            <>
              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "المزود (اختياري)" : "Provider (optional)"}</Text>
              <FlatList
                horizontal
                data={[
                  {
                    code: "",
                    nameAr: languageMode === "ar" ? "كل المزودين" : "All Providers",
                    nameEn: "All Providers",
                    id: "",
                    hasApiAccess: false,
                    supportsHourly: false,
                    supportsMonthly: false,
                    supportsB2B: false,
                    supportsRecruitment: false,
                    integrationModeKey: "",
                    isActive: true,
                  } as Provider,
                  ...providers,
                ]}
                keyExtractor={(item) => item.code || "all"}
                showsHorizontalScrollIndicator={false}
                renderItem={({ item }) => {
                  const active = item.code === "" ? selectedProvider === null : selectedProvider === item.code;
                  return (
                    <TouchableOpacity
                      style={[styles.chip, active && styles.chipActive]}
                      onPress={() => setSelectedProvider(item.code === "" ? null : item.code)}
                    >
                      <Text style={[styles.chipText, active && styles.chipTextActive]}>
                        {languageMode === "ar" ? item.nameAr : item.nameEn}
                      </Text>
                    </TouchableOpacity>
                  );
                }}
              />

              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "مجموعة الخدمة" : "Service Group"}</Text>
              <FlatList
                horizontal
                data={availableGroups}
                keyExtractor={(item) => item}
                showsHorizontalScrollIndicator={false}
                renderItem={({ item }) => {
                  const active = selectedGroup === item;
                  return (
                    <TouchableOpacity style={[styles.chip, active && styles.chipActive]} onPress={() => setSelectedGroup(item)}>
                      <Text style={[styles.chipText, active && styles.chipTextActive]}>{groupLabel(item)}</Text>
                    </TouchableOpacity>
                  );
                }}
                ListEmptyComponent={<Text style={styles.meta}>No service groups available for this provider.</Text>}
              />
            </>
          ) : null}

          {wizardStep === 1 ? (
            <>
              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "الخدمة الفرعية" : "Sub Service"}</Text>
              <FlatList
                horizontal
                data={groupFilteredOffers}
                keyExtractor={(item) => item.id}
                showsHorizontalScrollIndicator={false}
                renderItem={({ item }) => {
                  const active = selectedSubServiceId === item.id;
                  return (
                    <TouchableOpacity
                      style={[styles.chip, active && styles.chipActive]}
                      onPress={() => setSelectedSubServiceId(item.id)}
                    >
                      <Text style={[styles.chipText, active && styles.chipTextActive]}>
                        {languageMode === "ar" ? item.nameAr : item.nameEn}
                      </Text>
                    </TouchableOpacity>
                  );
                }}
                ListEmptyComponent={<Text style={styles.meta}>No sub services found in this group.</Text>}
              />
            </>
          ) : null}

          {wizardStep === 2 ? (
            <>
              {selectedGroup === "hourly-cleaning" ? (
                <>
                  <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "كم عدد الساعات؟" : "How many hours?"}</Text>
                  <Text style={styles.fieldHint}>JSON: hoursNumber / visitHours</Text>
                  <FlatList
                    horizontal
                    data={hourlyOptions}
                    keyExtractor={(item) => item.toString()}
                    showsHorizontalScrollIndicator={false}
                    renderItem={({ item }) => {
                      const active = hourlyHours === item;
                      return (
                        <TouchableOpacity style={[styles.chip, active && styles.chipActive]} onPress={() => setHourlyHours(item)}>
                          <Text style={[styles.chipText, active && styles.chipTextActive]}>
                            {item} {languageMode === "ar" ? "ساعات" : "Hours"}
                          </Text>
                        </TouchableOpacity>
                      );
                    }}
                  />
                </>
              ) : null}

              {selectedGroup === "monthly" ? (
                <>
                  <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "مدة العقد" : "Contract Duration"}</Text>
                  <Text style={styles.fieldHint}>JSON: contractDuration / contract_duration_months</Text>
                  <FlatList
                    horizontal
                    data={monthlyDurationOptions}
                    keyExtractor={(item) => item.toString()}
                    showsHorizontalScrollIndicator={false}
                    renderItem={({ item }) => {
                      const active = monthlyDurationMonths === item;
                      return (
                        <TouchableOpacity
                          style={[styles.chip, active && styles.chipActive]}
                          onPress={() => setMonthlyDurationMonths(item)}
                        >
                          <Text style={[styles.chipText, active && styles.chipTextActive]}>{monthlyDurationLabel(item)}</Text>
                        </TouchableOpacity>
                      );
                    }}
                  />
                </>
              ) : null}

              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "تاريخ الخدمة" : "Service Date"}</Text>
              <TextInput
                placeholder="YYYY-MM-DD"
                value={serviceDate}
                onChangeText={setServiceDate}
                style={styles.searchInput}
                placeholderTextColor={Brand.colors.textSecondary}
              />
            </>
          ) : null}

          {wizardStep === 3 ? (
            <>
              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "مرجع العميل" : "Customer Reference"}</Text>
              <View style={styles.rowControls}>
                <TextInput
                  placeholder={languageMode === "ar" ? "مثال: demo-customer" : "e.g. demo-customer"}
                  value={customerReference}
                  onChangeText={setCustomerReference}
                  style={[styles.searchInput, styles.customerInput]}
                  placeholderTextColor={Brand.colors.textSecondary}
                />
                <TouchableOpacity style={styles.refreshButton} onPress={loadSavedLocations}>
                  <Text style={styles.refreshText}>Load</Text>
                </TouchableOpacity>
              </View>

              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "الموقع المحفوظ" : "Saved Location"}</Text>
              {savedLocations.length === 0 ? (
                <Text style={styles.meta}>No saved locations for this customer reference.</Text>
              ) : (
                <FlatList
                  horizontal
                  data={savedLocations}
                  keyExtractor={(item) => item.id}
                  showsHorizontalScrollIndicator={false}
                  renderItem={({ item }) => {
                    const active = selectedLocationId === item.id;
                    return (
                      <TouchableOpacity
                        style={[styles.chip, active && styles.chipActive]}
                        onPress={() => setSelectedLocationId(item.id)}
                      >
                        <Text style={[styles.chipText, active && styles.chipTextActive]}>
                          {item.label} - {item.city}
                        </Text>
                      </TouchableOpacity>
                    );
                  }}
                />
              )}

              {selectedLocation ? (
                <View style={styles.locationCard}>
                  <Text style={styles.meta}>
                    {selectedLocation.city}, {selectedLocation.district}
                  </Text>
                  <Text style={styles.meta}>
                    Lat: {selectedLocation.latitude} | Long: {selectedLocation.longitude}
                  </Text>
                  <TouchableOpacity
                    onPress={() => {
                      if (!selectedLocation.googleMapsUrl) return;
                      void Linking.openURL(selectedLocation.googleMapsUrl);
                    }}
                  >
                    <Text style={styles.mapLink}>
                      {selectedLocation.googleMapsUrl ? "Open in Google Maps" : "No Google Maps link"}
                    </Text>
                  </TouchableOpacity>
                </View>
              ) : null}
            </>
          ) : null}

          {wizardStep === 4 ? (
            <>
              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "بحث المزود" : "Provider Search"}</Text>
              <TextInput
                placeholder={languageMode === "ar" ? "ابحث عن مزود..." : "Search providers..."}
                value={searchText}
                onChangeText={setSearchText}
                style={styles.searchInput}
                placeholderTextColor={Brand.colors.textSecondary}
              />
              <View style={styles.summaryBar}>
                <Text style={styles.summaryText}>
                  {selectedGroup ? groupLabel(selectedGroup) : "No group"} | {selectedSubService ? (languageMode === "ar" ? selectedSubService.nameAr : selectedSubService.nameEn) : "No sub service"}
                </Text>
                <Text style={styles.summaryText}>{serviceDate || "No date selected"}</Text>
              </View>
            </>
          ) : null}

          <View style={styles.wizardActions}>
            <TouchableOpacity style={[styles.navButton, wizardStep === 0 && styles.navButtonDisabled]} onPress={goBack}>
              <Text style={styles.navText}>{languageMode === "ar" ? "السابق" : "Back"}</Text>
            </TouchableOpacity>
            {wizardStep < 4 ? (
              <TouchableOpacity style={[styles.navButton, !canGoNext && styles.navButtonDisabled]} onPress={goNext}>
                <Text style={styles.navText}>{languageMode === "ar" ? "التالي" : "Next"}</Text>
              </TouchableOpacity>
            ) : (
              <TouchableOpacity style={styles.navButton} onPress={loadData}>
                <Text style={styles.navText}>{languageMode === "ar" ? "تحديث" : "Refresh"}</Text>
              </TouchableOpacity>
            )}
          </View>
        </View>

        <View style={styles.listCard}>
          <View style={styles.listHeader}>
            <Text style={styles.sectionTitle}>Latest Prices</Text>
          </View>

          {loading ? (
            <View style={styles.stateBlock}>
              <ActivityIndicator size="large" color={Brand.colors.primary} />
              <Text style={styles.stateText}>Loading data from backend...</Text>
            </View>
          ) : error ? (
            <View style={styles.stateBlock}>
              <Text style={styles.errorText}>{error}</Text>
              <Text style={styles.stateText}>
                Ensure backend is running and EXPO_PUBLIC_API_BASE_URL is reachable.
              </Text>
            </View>
          ) : (
            searchedPrices.map((price) => {
              const provider = providerById[price.providerId];
              return (
                <View style={styles.priceCard} key={price.id}>
                  <View style={styles.providerRow}>
                    {provider?.logoUrl ? (
                      <Image source={{ uri: provider.logoUrl }} style={styles.providerLogo} />
                    ) : (
                      <View style={styles.providerLogoPlaceholder}>
                        <Text style={styles.providerLogoPlaceholderText}>
                          {(provider?.nameEn ?? "P").substring(0, 1).toUpperCase()}
                        </Text>
                      </View>
                    )}
                    <View style={styles.providerMetaCol}>
                      <Text style={styles.providerName}>
                        {provider ? (languageMode === "ar" ? provider.nameAr : provider.nameEn) : "Unknown Provider"}
                      </Text>
                      {provider?.tinyUrl ? <Text style={styles.providerTinyUrl}>{provider.tinyUrl}</Text> : null}
                    </View>
                  </View>
                  <View style={styles.row}>
                    <Text style={styles.finalPrice}>{price.finalPriceSar} SAR</Text>
                    <Text style={styles.meta}>Source: {price.sourceType === 1 ? "API" : "Scraper"}</Text>
                  </View>
                  {price.originalPriceSar ? (
                    <Text style={styles.originalPrice}>Before discount: {price.originalPriceSar} SAR</Text>
                  ) : null}
                  <Text style={styles.meta}>Updated: {new Date(price.collectedAtUtc).toLocaleString()}</Text>
                  <Text style={styles.meta}>Expires: {new Date(price.expiresAtUtc).toLocaleString()}</Text>
                </View>
              );
            })
          )}
        </View>

        <View style={styles.listCard}>
          <Text style={styles.sectionTitle}>Provider JSON Documents</Text>
          {jsonDocuments.length === 0 ? (
            <Text style={styles.stateText}>No active JSON documents found.</Text>
          ) : (
            jsonDocuments.map((doc) => (
              <View style={styles.priceCard} key={doc.id}>
                <Text style={styles.providerName}>{doc.fileName}</Text>
                <Text style={styles.meta}>Key: {doc.documentKey}</Text>
                <Text style={styles.meta}>Provider: {doc.providerCode ?? "N/A"}</Text>
                <Text style={styles.meta}>Created: {new Date(doc.createdAtUtc).toLocaleString()}</Text>
                <Text style={styles.meta}>Expires: {new Date(doc.expiresAtUtc).toLocaleString()}</Text>
              </View>
            ))
          )}
        </View>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: { flex: 1, backgroundColor: Brand.colors.primaryDark },
  page: { paddingBottom: Brand.spacing.lg, backgroundColor: Brand.colors.background },
  header: {
    paddingHorizontal: Brand.spacing.md,
    paddingVertical: Brand.spacing.lg,
    backgroundColor: Brand.colors.primaryDark,
  },
  logo: {
    color: "#fff",
    fontSize: 32,
    fontWeight: "800",
    letterSpacing: 0.3,
  },
  subtitle: {
    color: "#FCE7F3",
    marginTop: Brand.spacing.xs,
    fontSize: 14,
  },
  langSwitchRow: {
    flexDirection: "row",
    marginTop: 12,
    gap: 8,
  },
  langButton: {
    borderWidth: 1,
    borderColor: "#ffffff66",
    borderRadius: 999,
    paddingHorizontal: 12,
    paddingVertical: 6,
  },
  langButtonActive: {
    backgroundColor: "#ffffff",
    borderColor: "#ffffff",
  },
  langText: {
    color: "#ffffff",
    fontWeight: "700",
    fontSize: 12,
  },
  langTextActive: {
    color: Brand.colors.primaryDark,
  },
  filterCard: {
    marginHorizontal: Brand.spacing.md,
    marginTop: -10,
    backgroundColor: Brand.colors.card,
    borderRadius: Brand.radius.md,
    borderWidth: 1,
    borderColor: Brand.colors.border,
    padding: 14,
  },
  sectionTitle: {
    color: Brand.colors.textPrimary,
    fontWeight: "700",
    fontSize: 16,
    marginBottom: 8,
  },
  wizardStepsRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    marginBottom: 10,
  },
  wizardStepItem: {
    alignItems: "center",
    flex: 1,
  },
  wizardBullet: {
    width: 24,
    height: 24,
    borderRadius: 12,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "#E5E7EB",
  },
  wizardBulletActive: {
    backgroundColor: Brand.colors.primary,
  },
  wizardBulletText: {
    color: "#fff",
    fontWeight: "700",
    fontSize: 12,
  },
  wizardStepLabel: {
    marginTop: 4,
    fontSize: 10,
    color: Brand.colors.textSecondary,
    textAlign: "center",
  },
  wizardStepLabelActive: {
    color: Brand.colors.primaryDark,
    fontWeight: "700",
  },
  subSectionTitle: {
    color: Brand.colors.textPrimary,
    fontWeight: "700",
    fontSize: 13,
    marginTop: 4,
    marginBottom: 8,
  },
  fieldHint: {
    color: Brand.colors.textSecondary,
    fontSize: 11,
    marginBottom: 8,
  },
  searchInput: {
    borderWidth: 1,
    borderColor: Brand.colors.border,
    borderRadius: 10,
    backgroundColor: "#fff",
    color: Brand.colors.textPrimary,
    paddingHorizontal: 12,
    paddingVertical: 10,
    marginBottom: 10,
  },
  chip: {
    borderWidth: 1,
    borderColor: Brand.colors.border,
    borderRadius: 999,
    paddingVertical: 8,
    paddingHorizontal: 12,
    marginRight: 8,
    backgroundColor: "#fff",
  },
  chipActive: {
    borderColor: Brand.colors.primary,
    backgroundColor: "#FDF2F8",
  },
  chipText: { color: Brand.colors.textSecondary, fontWeight: "600" },
  chipTextActive: { color: Brand.colors.primaryDark },
  rowControls: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
    marginBottom: 10,
  },
  customerInput: {
    flex: 1,
    marginBottom: 0,
  },
  refreshButton: {
    backgroundColor: Brand.colors.primary,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 10,
  },
  refreshText: { color: "#fff", fontWeight: "700", fontSize: 12 },
  locationCard: {
    borderWidth: 1,
    borderColor: Brand.colors.border,
    borderRadius: 10,
    padding: 10,
    marginBottom: 10,
    backgroundColor: "#fff",
  },
  mapLink: {
    color: Brand.colors.primaryDark,
    fontWeight: "700",
    marginTop: 6,
    fontSize: 12,
  },
  wizardActions: {
    flexDirection: "row",
    justifyContent: "space-between",
    marginTop: 8,
  },
  navButton: {
    backgroundColor: Brand.colors.primary,
    borderRadius: 10,
    paddingHorizontal: 14,
    paddingVertical: 8,
  },
  navButtonDisabled: {
    opacity: 0.5,
  },
  navText: {
    color: "#fff",
    fontWeight: "700",
  },
  summaryBar: {
    borderWidth: 1,
    borderColor: Brand.colors.border,
    borderRadius: 10,
    padding: 10,
    marginBottom: 8,
    backgroundColor: "#fff",
  },
  summaryText: {
    color: Brand.colors.textSecondary,
    fontSize: 12,
    marginBottom: 2,
  },
  listCard: {
    marginHorizontal: Brand.spacing.md,
    marginTop: 12,
    backgroundColor: Brand.colors.card,
    borderRadius: Brand.radius.md,
    borderWidth: 1,
    borderColor: Brand.colors.border,
    padding: 14,
  },
  listHeader: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 8,
  },
  stateBlock: { paddingVertical: 20, alignItems: "center", gap: 10 },
  stateText: { color: Brand.colors.textSecondary, textAlign: "center" },
  errorText: { color: "#B91C1C", fontWeight: "700", textAlign: "center" },
  priceCard: {
    borderWidth: 1,
    borderColor: Brand.colors.border,
    borderRadius: 12,
    padding: 12,
    marginTop: 10,
    backgroundColor: "#fff",
  },
  providerRow: {
    flexDirection: "row",
    alignItems: "center",
    gap: 10,
  },
  providerMetaCol: {
    flex: 1,
  },
  providerLogo: {
    width: 42,
    height: 42,
    borderRadius: 21,
    borderWidth: 1,
    borderColor: Brand.colors.border,
    backgroundColor: "#fff",
  },
  providerLogoPlaceholder: {
    width: 42,
    height: 42,
    borderRadius: 21,
    borderWidth: 1,
    borderColor: Brand.colors.border,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: Brand.colors.primaryLight,
  },
  providerLogoPlaceholderText: {
    color: Brand.colors.primaryDark,
    fontWeight: "800",
  },
  providerName: { color: Brand.colors.textPrimary, fontWeight: "700", fontSize: 15 },
  providerTinyUrl: { color: Brand.colors.textSecondary, fontSize: 11, marginTop: 2 },
  row: {
    marginTop: 8,
    marginBottom: 4,
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
  },
  finalPrice: { color: Brand.colors.primaryDark, fontWeight: "800", fontSize: 18 },
  originalPrice: {
    color: Brand.colors.textSecondary,
    fontSize: 12,
    textDecorationLine: "line-through",
    marginBottom: 4,
  },
  meta: { color: Brand.colors.textSecondary, fontSize: 12 },
});
