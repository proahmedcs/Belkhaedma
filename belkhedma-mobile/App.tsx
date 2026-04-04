import { StatusBar } from "expo-status-bar";
import { useEffect, useMemo, useState } from "react";
import {
  ActivityIndicator,
  TextInput,
  Image,
  FlatList,
  SafeAreaView,
  Linking,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import {
  getCustomerSavedLocations,
  getLatestPrices,
  getProviders,
} from "./src/services/marketplaceApi";
import { Brand } from "./src/theme/brand";
import {
  CustomerSavedLocation,
  PriceSnapshot,
  Provider,
  ProviderJsonDocument,
} from "./src/types/marketplace";
import { getProviderJsonDocuments } from "./src/services/marketplaceApi";

type LanguageMode = "ar" | "en";
type ServiceTypeFilter = "all" | "hourly" | "monthly" | "recruitment";

export default function App() {
  const [providers, setProviders] = useState<Provider[]>([]);
  const [prices, setPrices] = useState<PriceSnapshot[]>([]);
  const [jsonDocuments, setJsonDocuments] = useState<ProviderJsonDocument[]>([]);
  const [savedLocations, setSavedLocations] = useState<CustomerSavedLocation[]>([]);
  const [selectedProvider, setSelectedProvider] = useState<string | null>(null);
  const [searchText, setSearchText] = useState<string>("");
  const [serviceType, setServiceType] = useState<ServiceTypeFilter>("all");
  const [serviceDate, setServiceDate] = useState<string>("");
  const [customerReference, setCustomerReference] = useState<string>("demo-customer");
  const [selectedLocationId, setSelectedLocationId] = useState<string | null>(null);
  const [languageMode, setLanguageMode] = useState<LanguageMode>("en");
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void loadData();
  }, []);

  const loadData = async () => {
    try {
      setLoading(true);
      setError(null);

      const [providersData, pricesData, docsData, locationsData] = await Promise.all([
        getProviders(),
        getLatestPrices(),
        getProviderJsonDocuments(undefined, false),
        getCustomerSavedLocations(customerReference),
      ]);

      setProviders(providersData);
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

  const visiblePrices = useMemo(() => {
    if (!selectedProvider) {
      return prices;
    }

    const provider = providers.find((p) => p.code === selectedProvider);
    if (!provider) {
      return prices;
    }

    return prices.filter((p) => p.providerId === provider.id);
  }, [prices, providers, selectedProvider]);

  const providerById = useMemo(() => {
    return providers.reduce<Record<string, Provider>>((acc, provider) => {
      acc[provider.id] = provider;
      return acc;
    }, {});
  }, [providers]);

  const searchedPrices = useMemo(() => {
    const q = searchText.trim().toLowerCase();
    return visiblePrices.filter((price) => {
      const provider = providerById[price.providerId];
      if (!provider) {
        return false;
      }

      const serviceTypeMatch =
        serviceType === "all" ||
        (serviceType === "hourly" && provider.supportsHourly) ||
        (serviceType === "monthly" && provider.supportsMonthly) ||
        (serviceType === "recruitment" && provider.supportsRecruitment);
      if (!serviceTypeMatch) {
        return false;
      }

      if (!q) {
        return true;
      }

      const haystack = [
        provider.nameAr,
        provider.nameEn,
        provider.code,
        provider.providerType,
        provider.integrationModeKey,
      ]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();

      return haystack.includes(q);
    });
  }, [searchText, visiblePrices, providerById, serviceType]);

  const allProvidersLabel = languageMode === "ar" ? "كل المزودين" : "All Providers";
  const selectedLocation = useMemo(
    () => savedLocations.find((x) => x.id === selectedLocationId) ?? null,
    [savedLocations, selectedLocationId]
  );
  const serviceTypeLabel = (value: ServiceTypeFilter) => {
    if (languageMode === "ar") {
      if (value === "hourly") return "بالساعة";
      if (value === "monthly") return "شهري";
      if (value === "recruitment") return "استقدام";
      return "كل الأنواع";
    }

    if (value === "hourly") return "Hourly";
    if (value === "monthly") return "Monthly";
    if (value === "recruitment") return "Recruitment";
    return "All Service Types";
  };

  return (
    <SafeAreaView style={styles.safeArea}>
      <StatusBar style="light" />
      <ScrollView contentContainerStyle={styles.page}>
        <View style={styles.header}>
          <Text style={styles.logo}>Belkhedma</Text>
          <Text style={styles.subtitle}>
            Unified home-services marketplace for price comparison
          </Text>
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
          <Text style={styles.sectionTitle}>Provider Filter</Text>
          <TextInput
            placeholder={languageMode === "ar" ? "ابحث عن مزود..." : "Search providers..."}
            value={searchText}
            onChangeText={setSearchText}
            style={styles.searchInput}
            placeholderTextColor={Brand.colors.textSecondary}
          />
          <Text style={styles.subSectionTitle}>
            {languageMode === "ar" ? "نوع الخدمة" : "Service Type"}
          </Text>
          <FlatList
            horizontal
            data={["all", "hourly", "monthly", "recruitment"] as ServiceTypeFilter[]}
            keyExtractor={(item) => item}
            showsHorizontalScrollIndicator={false}
            renderItem={({ item }) => {
              const active = serviceType === item;
              return (
                <TouchableOpacity
                  style={[styles.chip, active && styles.chipActive]}
                  onPress={() => setServiceType(item)}
                >
                  <Text style={[styles.chipText, active && styles.chipTextActive]}>
                    {serviceTypeLabel(item)}
                  </Text>
                </TouchableOpacity>
              );
            }}
          />
          <Text style={styles.subSectionTitle}>
            {languageMode === "ar" ? "تاريخ الخدمة" : "Service Date"}
          </Text>
          <TextInput
            placeholder={languageMode === "ar" ? "YYYY-MM-DD" : "YYYY-MM-DD"}
            value={serviceDate}
            onChangeText={setServiceDate}
            style={styles.searchInput}
            placeholderTextColor={Brand.colors.textSecondary}
          />
          <Text style={styles.subSectionTitle}>
            {languageMode === "ar" ? "مرجع العميل" : "Customer Reference"}
          </Text>
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
          <Text style={styles.subSectionTitle}>
            {languageMode === "ar" ? "الموقع المحفوظ" : "Saved Location"}
          </Text>
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
                  if (!selectedLocation.googleMapsUrl) {
                    return;
                  }

                  void Linking.openURL(selectedLocation.googleMapsUrl);
                }}
              >
                <Text style={styles.mapLink}>
                  {selectedLocation.googleMapsUrl
                    ? "Open in Google Maps"
                    : "No Google Maps link available"}
                </Text>
              </TouchableOpacity>
            </View>
          ) : null}
          <FlatList
            horizontal
            data={[
              { code: "", nameAr: allProvidersLabel, nameEn: allProvidersLabel, id: "", hasApiAccess: false, isActive: true } as Provider,
              ...providers,
            ]}
            keyExtractor={(item) => item.code || "all"}
            showsHorizontalScrollIndicator={false}
            renderItem={({ item }) => {
              const active =
                item.code === ""
                  ? selectedProvider === null
                  : selectedProvider === item.code;
              return (
                <TouchableOpacity
                  style={[styles.chip, active && styles.chipActive]}
                  onPress={() =>
                    setSelectedProvider(item.code === "" ? null : item.code)
                  }
                >
                  <Text style={[styles.chipText, active && styles.chipTextActive]}>
                    {languageMode === "ar" ? item.nameAr : item.nameEn}
                  </Text>
                </TouchableOpacity>
              );
            }}
          />
        </View>

        <View style={styles.listCard}>
          <View style={styles.listHeader}>
            <Text style={styles.sectionTitle}>Latest Prices</Text>
            <TouchableOpacity style={styles.refreshButton} onPress={loadData}>
              <Text style={styles.refreshText}>Refresh</Text>
            </TouchableOpacity>
          </View>

          {loading ? (
            <View style={styles.stateBlock}>
              <ActivityIndicator size="large" color={Brand.colors.primary} />
              <Text style={styles.stateText}>Loading data from shared backend...</Text>
            </View>
          ) : error ? (
            <View style={styles.stateBlock}>
              <Text style={styles.errorText}>{error}</Text>
              <Text style={styles.stateText}>
                Ensure backend is running and EXPO_PUBLIC_API_BASE_URL is reachable.
              </Text>
            </View>
          ) : (
            searchedPrices.map((price) => (
              <View style={styles.priceCard} key={price.id}>
                <View style={styles.providerRow}>
                  {providerById[price.providerId]?.logoUrl ? (
                    <Image
                      source={{ uri: providerById[price.providerId].logoUrl! }}
                      style={styles.providerLogo}
                    />
                  ) : (
                    <View style={styles.providerLogoPlaceholder}>
                      <Text style={styles.providerLogoPlaceholderText}>
                        {(providerById[price.providerId]?.nameEn ?? "P").substring(0, 1).toUpperCase()}
                      </Text>
                    </View>
                  )}
                  <View style={styles.providerMetaCol}>
                    <Text style={styles.providerName}>
                      {providerById[price.providerId]
                        ? (languageMode === "ar"
                            ? providerById[price.providerId].nameAr
                            : providerById[price.providerId].nameEn)
                        : "Unknown Provider"}
                    </Text>
                    {providerById[price.providerId]?.tinyUrl ? (
                      <Text style={styles.providerTinyUrl}>{providerById[price.providerId].tinyUrl}</Text>
                    ) : null}
                  </View>
                </View>
                <View style={styles.row}>
                  <Text style={styles.finalPrice}>{price.finalPriceSar} SAR</Text>
                  <Text style={styles.meta}>
                    Source: {price.sourceType === 1 ? "API" : "Scraper"}
                  </Text>
                </View>
                {price.originalPriceSar ? (
                  <Text style={styles.originalPrice}>Before discount: {price.originalPriceSar} SAR</Text>
                ) : null}
                <Text style={styles.meta}>
                  Updated: {new Date(price.collectedAtUtc).toLocaleString()}
                </Text>
                <Text style={styles.meta}>
                  Expires: {new Date(price.expiresAtUtc).toLocaleString()}
                </Text>
                {serviceDate ? <Text style={styles.meta}>Service Date: {serviceDate}</Text> : null}
              </View>
            ))
          )}
        </View>

        <View style={styles.listCard}>
          <View style={styles.listHeader}>
            <Text style={styles.sectionTitle}>Provider JSON Documents</Text>
          </View>
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
  sectionTitle: {
    color: Brand.colors.textPrimary,
    fontWeight: "700",
    fontSize: 16,
    marginBottom: 8,
  },
  subSectionTitle: {
    color: Brand.colors.textPrimary,
    fontWeight: "700",
    fontSize: 13,
    marginTop: 4,
    marginBottom: 8,
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
  refreshButton: {
    backgroundColor: Brand.colors.primary,
    borderRadius: 10,
    paddingHorizontal: 12,
    paddingVertical: 6,
  },
  refreshText: { color: "#fff", fontWeight: "700", fontSize: 12 },
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
  originalPrice: {
    color: Brand.colors.textSecondary,
    fontSize: 12,
    textDecorationLine: "line-through",
    marginBottom: 4,
  },
  meta: { color: Brand.colors.textSecondary, fontSize: 12 },
});
