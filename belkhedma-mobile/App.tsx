import { StatusBar } from "expo-status-bar";
import { useEffect, useMemo, useState } from "react";
import {
  ActivityIndicator,
  Image,
  FlatList,
  SafeAreaView,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { getLatestPrices, getProviders } from "./src/services/marketplaceApi";
import { Brand } from "./src/theme/brand";
import { PriceSnapshot, Provider, ProviderJsonDocument } from "./src/types/marketplace";
import { getProviderJsonDocuments } from "./src/services/marketplaceApi";

type LanguageMode = "ar" | "en";

export default function App() {
  const [providers, setProviders] = useState<Provider[]>([]);
  const [prices, setPrices] = useState<PriceSnapshot[]>([]);
  const [jsonDocuments, setJsonDocuments] = useState<ProviderJsonDocument[]>([]);
  const [selectedProvider, setSelectedProvider] = useState<string | null>(null);
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

      const [providersData, pricesData, docsData] = await Promise.all([
        getProviders(),
        getLatestPrices(),
        getProviderJsonDocuments(undefined, false),
      ]);

      setProviders(providersData);
      setPrices(pricesData);
      setJsonDocuments(docsData);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unexpected error.");
    } finally {
      setLoading(false);
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

  const allProvidersLabel = languageMode === "ar" ? "كل المزودين" : "All Providers";

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
            visiblePrices.map((price) => (
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
                    {providerById[price.providerId]?.providerTinyUrl ? (
                      <Text style={styles.providerTinyUrl}>{providerById[price.providerId].providerTinyUrl}</Text>
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
  originalPrice: {
    color: Brand.colors.textSecondary,
    fontSize: 12,
    textDecorationLine: "line-through",
    marginBottom: 4,
  },
  meta: { color: Brand.colors.textSecondary, fontSize: 12 },
});
