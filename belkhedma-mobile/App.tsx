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
  getAllPrices,
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
type JsonDrivenOptions = {
  shifts: string[];
  nationalityGroups: string[];
  contractDurations: string[];
  workerCounts: number[];
  hoursPerVisit: number[];
  weeklyVisits: number[];
  deliveryWindows: string[];
};

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

function parseDurationToMonths(value: string): number | null {
  const normalized = value.toLowerCase();
  const numMatch = normalized.match(/\d+/);
  if (!numMatch) return null;
  const amount = Number(numMatch[0]);
  if (!Number.isFinite(amount) || amount <= 0) return null;

  if (normalized.includes("week") || normalized.includes("أسبوع") || normalized.includes("اسبوع")) {
    return Math.max(1, Math.ceil(amount / 4));
  }
  if (normalized.includes("month") || normalized.includes("شهر") || normalized.includes("أشهر")) {
    return amount;
  }
  return null;
}

function extractJsonDrivenOptions(docs: ProviderJsonDocument[]): JsonDrivenOptions {
  const shifts = new Set<string>();
  const nationalityGroups = new Set<string>();
  const contractDurations = new Set<string>();
  const workerCounts = new Set<number>();
  const hoursPerVisit = new Set<number>();
  const weeklyVisits = new Set<number>();
  const deliveryWindows = new Set<string>();

  const maybeAddString = (target: Set<string>, value: unknown) => {
    if (typeof value !== "string") return;
    const normalized = value.trim();
    if (!normalized || normalized.length > 60) return;
    target.add(normalized);
  };

  const maybeAddNumber = (target: Set<number>, value: unknown) => {
    if (typeof value !== "number") return;
    if (!Number.isFinite(value)) return;
    if (value <= 0 || value > 1000) return;
    target.add(value);
  };

  const walk = (node: unknown) => {
    if (Array.isArray(node)) {
      for (const item of node) walk(item);
      return;
    }

    if (node && typeof node === "object") {
      for (const [key, value] of Object.entries(node as Record<string, unknown>)) {
        const lowerKey = key.toLowerCase();

        if (lowerKey.includes("shift") || lowerKey === "period_tabs" || lowerKey === "shift_period") {
          if (Array.isArray(value)) {
            value.forEach((x) => maybeAddString(shifts, x));
          } else {
            maybeAddString(shifts, value);
          }
        }

        if (lowerKey.includes("resourcegroupname") || lowerKey.includes("nationality")) {
          if (Array.isArray(value)) {
            value.forEach((x) => maybeAddString(nationalityGroups, x));
          } else {
            maybeAddString(nationalityGroups, value);
          }
        }

        if (lowerKey.includes("contractdurationname") || lowerKey.includes("contract_duration_months")) {
          if (Array.isArray(value)) {
            value.forEach((x) => maybeAddString(contractDurations, x));
          } else {
            maybeAddString(contractDurations, value);
          }
        }
        if (lowerKey === "contractduration") {
          if (typeof value === "number") {
            contractDurations.add(`${value} Week`);
          } else {
            maybeAddString(contractDurations, value);
          }
        }

        if (lowerKey.includes("employeenumber") || lowerKey.includes("workercount")) {
          maybeAddNumber(workerCounts, value);
        }

        if (lowerKey.includes("hoursnumber") || lowerKey.includes("visithours")) {
          maybeAddNumber(hoursPerVisit, value);
        }

        if (lowerKey.includes("weeklyvisits")) {
          maybeAddNumber(weeklyVisits, value);
        }

        if (lowerKey.includes("deliverywindow") || lowerKey.includes("delivery_windows")) {
          if (Array.isArray(value)) {
            value.forEach((x) => maybeAddString(deliveryWindows, x));
          } else {
            maybeAddString(deliveryWindows, value);
          }
        }

        walk(value);
      }
    }
  };

  for (const doc of docs) {
    try {
      const parsed = JSON.parse(doc.jsonContent);
      walk(parsed);
    } catch {
      // Ignore malformed json content and continue with other docs.
    }
  }

  return {
    shifts: Array.from(shifts),
    nationalityGroups: Array.from(nationalityGroups),
    contractDurations: Array.from(contractDurations),
    workerCounts: Array.from(workerCounts).sort((a, b) => a - b),
    hoursPerVisit: Array.from(hoursPerVisit).sort((a, b) => a - b),
    weeklyVisits: Array.from(weeklyVisits).sort((a, b) => a - b),
    deliveryWindows: Array.from(deliveryWindows),
  };
}

function tryExtractHost(urlValue?: string | null): string | null {
  if (!urlValue) return null;
  try {
    const parsed = new URL(urlValue);
    return parsed.hostname.replace(/^www\./i, "");
  } catch {
    return null;
  }
}

function buildInlineLogoDataUri(provider?: Provider): string {
  const displayName = provider?.nameEn?.trim() || provider?.code?.trim() || "Provider";
  const initials = displayName
    .split(/\s+/)
    .filter(Boolean)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("")
    .slice(0, 2) || "P";
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="128" height="128"><rect width="100%" height="100%" fill="#FCE7F3"/><text x="50%" y="55%" dominant-baseline="middle" text-anchor="middle" font-family="Arial, sans-serif" font-size="54" font-weight="700" fill="#9D208C">${initials}</text></svg>`;
  return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`;
}

function buildLogoCandidates(provider?: Provider): string[] {
  if (!provider) return [];

  const knownProviderDomains: Record<string, string> = {
    cleanpro: "cleaningservices.com",
    medisupport: "medicalnews.today",
    wasata: "wasatah.sa",
    enaya: "enaya.sa",
    "emdad-hr": "emdadhr.com",
    mueen: "mueen.com.sa",
    tamkeen: "tamkeenhr.sa",
    almutahidah: "almutahidah.com",
    "irc-saudi": "own.irc.sa",
    "esad-talents": "esadtalents.com",
    eitinaa: "eitinaa.com",
  };

  const candidates = new Set<string>();
  if (provider.logoUrl) {
    candidates.add(provider.logoUrl);
  }

  const derivedHosts = [
    tryExtractHost(provider.websiteUrl),
    tryExtractHost(provider.appUrl),
    tryExtractHost(provider.tinyUrl),
    knownProviderDomains[provider.code],
  ].filter((x): x is string => !!x);

  for (const host of derivedHosts) {
    candidates.add(`https://logo.clearbit.com/${host}`);
  }

  // Extra provider-code fallback when backend does not provide URLs.
  candidates.add(`https://logo.clearbit.com/${provider.code}.com`);

  // Last remote fallback: generated avatar image to avoid empty logo slots.
  const providerDisplayName = encodeURIComponent(provider.nameEn || provider.code);
  candidates.add(`https://ui-avatars.com/api/?name=${providerDisplayName}&background=FCE7F3&color=9D208C&bold=true&size=128`);
  // Guaranteed local fallback that does not require network access.
  candidates.add(buildInlineLogoDataUri(provider));

  return Array.from(candidates);
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
  const [hasSearched, setHasSearched] = useState<boolean>(false);
  const [selectedShift, setSelectedShift] = useState<string | null>(null);
  const [selectedNationalityGroup, setSelectedNationalityGroup] = useState<string | null>(null);
  const [selectedContractDurationName, setSelectedContractDurationName] = useState<string | null>(null);
  const [selectedWorkersCount, setSelectedWorkersCount] = useState<number | null>(null);
  const [selectedHoursPerVisit, setSelectedHoursPerVisit] = useState<number | null>(null);
  const [selectedWeeklyVisits, setSelectedWeeklyVisits] = useState<number | null>(null);
  const [selectedDeliveryWindow, setSelectedDeliveryWindow] = useState<string | null>(null);

  const [wizardStep, setWizardStep] = useState<WizardStep>(0);
  const [languageMode, setLanguageMode] = useState<LanguageMode>("en");
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [logoFallbackIndex, setLogoFallbackIndex] = useState<Record<string, number>>({});

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

  const jsonDrivenOptions = useMemo(() => {
    const scopedDocs = jsonDocuments.filter((doc) => {
      if (selectedProvider && doc.providerCode && doc.providerCode !== selectedProvider) {
        return false;
      }

      if (!selectedGroup || doc.serviceMode == null) {
        return true;
      }

      if (selectedGroup === "hourly-cleaning") {
        return doc.serviceMode === 1;
      }

      if (selectedGroup === "monthly") {
        return doc.serviceMode === 2 || doc.serviceMode === 3;
      }

      return true;
    });

    return extractJsonDrivenOptions(scopedDocs.length > 0 ? scopedDocs : jsonDocuments);
  }, [jsonDocuments, selectedGroup, selectedProvider]);

  const shiftOptions = useMemo(() => {
    if (jsonDrivenOptions.shifts.length > 0) return jsonDrivenOptions.shifts;
    return ["Morning", "Evening"];
  }, [jsonDrivenOptions.shifts]);

  const nationalityOptions = useMemo(() => {
    if (jsonDrivenOptions.nationalityGroups.length > 0) return jsonDrivenOptions.nationalityGroups;
    return ["Africa", "Philippines", "Indonesia"];
  }, [jsonDrivenOptions.nationalityGroups]);

  const contractDurationNameOptions = useMemo(() => {
    if (jsonDrivenOptions.contractDurations.length > 0) return jsonDrivenOptions.contractDurations;
    if (selectedGroup === "monthly") return ["1 Month", "3 Months", "6 Months", "12 Months"];
    return ["1 Week", "2 Weeks", "1 Month"];
  }, [jsonDrivenOptions.contractDurations, selectedGroup]);

  const workerCountOptions = useMemo(() => {
    if (jsonDrivenOptions.workerCounts.length > 0) return jsonDrivenOptions.workerCounts;
    return [1, 2, 3];
  }, [jsonDrivenOptions.workerCounts]);

  const hoursPerVisitOptions = useMemo(() => {
    const values = new Set<number>(jsonDrivenOptions.hoursPerVisit);
    hourlyOptions.forEach((x) => values.add(x));
    if (values.size === 0) {
      values.add(4);
      values.add(8);
    }
    return Array.from(values).sort((a, b) => a - b);
  }, [hourlyOptions, jsonDrivenOptions.hoursPerVisit]);

  const weeklyVisitOptions = useMemo(() => {
    if (jsonDrivenOptions.weeklyVisits.length > 0) return jsonDrivenOptions.weeklyVisits;
    return [1, 2, 3, 4];
  }, [jsonDrivenOptions.weeklyVisits]);

  const deliveryWindowOptions = useMemo(() => {
    if (jsonDrivenOptions.deliveryWindows.length > 0) return jsonDrivenOptions.deliveryWindows;
    return ["07:00-09:00", "15:00-17:00"];
  }, [jsonDrivenOptions.deliveryWindows]);

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

  useEffect(() => {
    if (!selectedShift && shiftOptions.length > 0) {
      setSelectedShift(shiftOptions[0]);
    } else if (selectedShift && !shiftOptions.includes(selectedShift)) {
      setSelectedShift(shiftOptions[0] ?? null);
    }
  }, [selectedShift, shiftOptions]);

  useEffect(() => {
    if (!selectedNationalityGroup && nationalityOptions.length > 0) {
      setSelectedNationalityGroup(nationalityOptions[0]);
    } else if (selectedNationalityGroup && !nationalityOptions.includes(selectedNationalityGroup)) {
      setSelectedNationalityGroup(nationalityOptions[0] ?? null);
    }
  }, [nationalityOptions, selectedNationalityGroup]);

  useEffect(() => {
    if (!selectedContractDurationName && contractDurationNameOptions.length > 0) {
      setSelectedContractDurationName(contractDurationNameOptions[0]);
      return;
    }

    if (selectedContractDurationName && !contractDurationNameOptions.includes(selectedContractDurationName)) {
      setSelectedContractDurationName(contractDurationNameOptions[0] ?? null);
      return;
    }

    if (selectedContractDurationName) {
      const months = parseDurationToMonths(selectedContractDurationName);
      if (months && months !== monthlyDurationMonths) {
        setMonthlyDurationMonths(months);
      }
    }
  }, [contractDurationNameOptions, monthlyDurationMonths, selectedContractDurationName]);

  useEffect(() => {
    if (selectedWorkersCount == null || !workerCountOptions.includes(selectedWorkersCount)) {
      setSelectedWorkersCount(workerCountOptions[0] ?? null);
    }
  }, [selectedWorkersCount, workerCountOptions]);

  useEffect(() => {
    if (selectedHoursPerVisit == null || !hoursPerVisitOptions.includes(selectedHoursPerVisit)) {
      setSelectedHoursPerVisit(hoursPerVisitOptions[0] ?? null);
    }
  }, [hoursPerVisitOptions, selectedHoursPerVisit]);

  useEffect(() => {
    if (selectedWeeklyVisits == null || !weeklyVisitOptions.includes(selectedWeeklyVisits)) {
      setSelectedWeeklyVisits(weeklyVisitOptions[0] ?? null);
    }
  }, [selectedWeeklyVisits, weeklyVisitOptions]);

  useEffect(() => {
    if (!selectedDeliveryWindow || !deliveryWindowOptions.includes(selectedDeliveryWindow)) {
      setSelectedDeliveryWindow(deliveryWindowOptions[0] ?? null);
    }
  }, [deliveryWindowOptions, selectedDeliveryWindow]);

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

  const wegoStyleRows = useMemo(() => {
    return searchedPrices.map((price) => ({
      price,
      provider: providerById[price.providerId],
      offer: offerById[price.serviceOfferId],
    }));
  }, [offerById, providerById, searchedPrices]);

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

  const runSearch = async () => {
    try {
      setLoading(true);
      setError(null);
      const providerCode = selectedProvider ?? undefined;
      const allPrices = await getAllPrices(providerCode, false);
      setPrices(allPrices);
      setHasSearched(true);
      setWizardStep(4);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to search prices.");
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
    languageMode === "ar" ? "الخدمة" : "Service",
    languageMode === "ar" ? "المزود" : "Provider",
    languageMode === "ar" ? "التفاصيل" : "Details",
    languageMode === "ar" ? "الموقع" : "Location",
    languageMode === "ar" ? "النتائج" : "Results",
  ];

  const canGoNext = useMemo(() => {
    if (wizardStep === 0) return !!selectedGroup && !!selectedSubServiceId;
    if (wizardStep === 1) return true;
    if (wizardStep === 2) {
      return (
        !!serviceDate &&
        !!selectedShift &&
        !!selectedNationalityGroup &&
        !!selectedContractDurationName &&
        selectedWorkersCount != null &&
        selectedHoursPerVisit != null &&
        selectedWeeklyVisits != null &&
        !!selectedDeliveryWindow
      );
    }
    if (wizardStep === 3) return savedLocations.length === 0 || !!selectedLocationId;
    return true;
  }, [
    savedLocations.length,
    selectedContractDurationName,
    selectedDeliveryWindow,
    selectedGroup,
    selectedHoursPerVisit,
    selectedLocationId,
    selectedNationalityGroup,
    selectedShift,
    selectedSubServiceId,
    selectedWeeklyVisits,
    selectedWorkersCount,
    serviceDate,
    wizardStep,
  ]);

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
                ListEmptyComponent={<Text style={styles.meta}>No service groups available.</Text>}
              />

              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "الخدمة" : "Service"}</Text>
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
                ListEmptyComponent={<Text style={styles.meta}>No services found for this group.</Text>}
              />
            </>
          ) : null}

          {wizardStep === 1 ? (
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
                ListEmptyComponent={<Text style={styles.meta}>No providers found.</Text>}
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

              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "الفترة" : "Shift / Period"}</Text>
              <Text style={styles.fieldHint}>JSON: visitShiftName / shift_period / period_tabs</Text>
              <FlatList
                horizontal
                data={shiftOptions}
                keyExtractor={(item) => item}
                showsHorizontalScrollIndicator={false}
                renderItem={({ item }) => {
                  const active = selectedShift === item;
                  return (
                    <TouchableOpacity style={[styles.chip, active && styles.chipActive]} onPress={() => setSelectedShift(item)}>
                      <Text style={[styles.chipText, active && styles.chipTextActive]}>{item}</Text>
                    </TouchableOpacity>
                  );
                }}
              />

              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "الجنسية / مجموعة الموارد" : "Nationality / Resource Group"}</Text>
              <Text style={styles.fieldHint}>JSON: resourceGroupName / nationality</Text>
              <FlatList
                horizontal
                data={nationalityOptions}
                keyExtractor={(item) => item}
                showsHorizontalScrollIndicator={false}
                renderItem={({ item }) => {
                  const active = selectedNationalityGroup === item;
                  return (
                    <TouchableOpacity
                      style={[styles.chip, active && styles.chipActive]}
                      onPress={() => setSelectedNationalityGroup(item)}
                    >
                      <Text style={[styles.chipText, active && styles.chipTextActive]}>{item}</Text>
                    </TouchableOpacity>
                  );
                }}
              />

              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "اسم مدة التعاقد" : "Contract Duration Name"}</Text>
              <Text style={styles.fieldHint}>JSON: contractDurationName / contract_duration_months</Text>
              <FlatList
                horizontal
                data={contractDurationNameOptions}
                keyExtractor={(item) => item}
                showsHorizontalScrollIndicator={false}
                renderItem={({ item }) => {
                  const active = selectedContractDurationName === item;
                  return (
                    <TouchableOpacity
                      style={[styles.chip, active && styles.chipActive]}
                      onPress={() => setSelectedContractDurationName(item)}
                    >
                      <Text style={[styles.chipText, active && styles.chipTextActive]}>{item}</Text>
                    </TouchableOpacity>
                  );
                }}
              />

              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "عدد العاملات" : "Workers Count"}</Text>
              <Text style={styles.fieldHint}>JSON: employeeNumber / workerCount</Text>
              <FlatList
                horizontal
                data={workerCountOptions}
                keyExtractor={(item) => item.toString()}
                showsHorizontalScrollIndicator={false}
                renderItem={({ item }) => {
                  const active = selectedWorkersCount === item;
                  return (
                    <TouchableOpacity
                      style={[styles.chip, active && styles.chipActive]}
                      onPress={() => setSelectedWorkersCount(item)}
                    >
                      <Text style={[styles.chipText, active && styles.chipTextActive]}>
                        {item} {languageMode === "ar" ? "عاملة" : "Worker"}
                      </Text>
                    </TouchableOpacity>
                  );
                }}
              />

              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "عدد الساعات لكل زيارة" : "Hours Per Visit"}</Text>
              <Text style={styles.fieldHint}>JSON: hoursNumber / visitHours</Text>
              <FlatList
                horizontal
                data={hoursPerVisitOptions}
                keyExtractor={(item) => item.toString()}
                showsHorizontalScrollIndicator={false}
                renderItem={({ item }) => {
                  const active = selectedHoursPerVisit === item;
                  return (
                    <TouchableOpacity
                      style={[styles.chip, active && styles.chipActive]}
                      onPress={() => setSelectedHoursPerVisit(item)}
                    >
                      <Text style={[styles.chipText, active && styles.chipTextActive]}>
                        {item} {languageMode === "ar" ? "ساعات" : "Hours"}
                      </Text>
                    </TouchableOpacity>
                  );
                }}
              />

              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "الزيارات الأسبوعية" : "Weekly Visits"}</Text>
              <Text style={styles.fieldHint}>JSON: weeklyVisits</Text>
              <FlatList
                horizontal
                data={weeklyVisitOptions}
                keyExtractor={(item) => item.toString()}
                showsHorizontalScrollIndicator={false}
                renderItem={({ item }) => {
                  const active = selectedWeeklyVisits === item;
                  return (
                    <TouchableOpacity
                      style={[styles.chip, active && styles.chipActive]}
                      onPress={() => setSelectedWeeklyVisits(item)}
                    >
                      <Text style={[styles.chipText, active && styles.chipTextActive]}>
                        {item} {languageMode === "ar" ? "زيارة" : "Visit"}
                      </Text>
                    </TouchableOpacity>
                  );
                }}
              />

              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "نافذة وقت الزيارة" : "Delivery Window"}</Text>
              <Text style={styles.fieldHint}>JSON: deliveryWindow / delivery_windows</Text>
              <FlatList
                horizontal
                data={deliveryWindowOptions}
                keyExtractor={(item) => item}
                showsHorizontalScrollIndicator={false}
                renderItem={({ item }) => {
                  const active = selectedDeliveryWindow === item;
                  return (
                    <TouchableOpacity
                      style={[styles.chip, active && styles.chipActive]}
                      onPress={() => setSelectedDeliveryWindow(item)}
                    >
                      <Text style={[styles.chipText, active && styles.chipTextActive]}>{item}</Text>
                    </TouchableOpacity>
                  );
                }}
              />

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
                <Text style={styles.summaryText}>
                  Shift: {selectedShift ?? "N/A"} | Nationality/Group: {selectedNationalityGroup ?? "N/A"}
                </Text>
                <Text style={styles.summaryText}>
                  Duration: {selectedContractDurationName ?? "N/A"} | Workers: {selectedWorkersCount ?? "N/A"}
                </Text>
                <Text style={styles.summaryText}>
                  Hours/Visit: {selectedHoursPerVisit ?? "N/A"} | Weekly Visits: {selectedWeeklyVisits ?? "N/A"}
                </Text>
                <Text style={styles.summaryText}>Delivery Window: {selectedDeliveryWindow ?? "N/A"}</Text>
              </View>
              <TouchableOpacity style={styles.searchButton} onPress={runSearch}>
                <Text style={styles.searchButtonText}>{languageMode === "ar" ? "عرض كل الأسعار" : "Search All Prices"}</Text>
              </TouchableOpacity>
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
              <TouchableOpacity style={styles.navButton} onPress={runSearch}>
                <Text style={styles.navText}>{languageMode === "ar" ? "بحث" : "Search"}</Text>
              </TouchableOpacity>
            )}
          </View>
        </View>

        <View style={styles.listCard}>
          <View style={styles.listHeader}>
            <Text style={styles.sectionTitle}>{hasSearched ? "All Prices" : "Latest Prices"}</Text>
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
            wegoStyleRows.map(({ price, provider, offer }) => {
              const logoCandidates = buildLogoCandidates(provider);
              const logoIndex = provider?.id ? logoFallbackIndex[provider.id] ?? 0 : 0;
              const logoUri = logoCandidates[logoIndex];
              return (
                <View style={styles.priceCard} key={price.id}>
                  <View style={styles.packageProviderRow}>
                    <Text style={styles.packageName}>
                      {offer ? (languageMode === "ar" ? offer.nameAr : offer.nameEn) : "Package"}
                    </Text>
                    <Text style={styles.providerSideLabel}>
                      {provider ? (languageMode === "ar" ? provider.nameAr : provider.nameEn) : "Provider"}
                    </Text>
                  </View>
                  <View style={styles.wegoRow}>
                    <View style={styles.providerRow}>
                      {logoUri ? (
                        <Image
                          source={{ uri: logoUri }}
                          style={styles.providerLogo}
                          onError={() => {
                            if (!provider || logoCandidates.length <= 1) return;
                            setLogoFallbackIndex((prev) => {
                              const current = prev[provider.id] ?? 0;
                              if (current >= logoCandidates.length - 1) {
                                return prev;
                              }

                              return {
                                ...prev,
                                [provider.id]: current + 1,
                              };
                            });
                          }}
                        />
                      ) : (
                        <View style={styles.providerLogoPlaceholder}>
                          <Text style={styles.providerLogoPlaceholderText}>
                            {(provider?.nameEn ?? "P").substring(0, 1).toUpperCase()}
                          </Text>
                        </View>
                      )}
                      <View style={styles.providerMetaCol}>
                        <Text style={styles.providerName}>{provider ? (languageMode === "ar" ? provider.nameAr : provider.nameEn) : "Unknown Provider"}</Text>
                        {provider?.tinyUrl ? <Text style={styles.providerTinyUrl}>{provider.tinyUrl}</Text> : null}
                      </View>
                    </View>
                    <Text style={styles.wegoPrice}>{price.finalPriceSar} SAR</Text>
                  </View>
                  <View style={styles.row}>
                    <Text style={styles.meta}>Source: {price.sourceType === 1 ? "API" : "Scraper"}</Text>
                    {price.originalPriceSar ? (
                      <Text style={styles.originalPrice}>Was {price.originalPriceSar} SAR</Text>
                    ) : (
                      <Text style={styles.meta}>Direct fare</Text>
                    )}
                  </View>
                  {!logoUri ? (
                    <View style={styles.providerRow}>
                      <View style={styles.providerLogoPlaceholder}>
                        <Text style={styles.providerLogoPlaceholderText}>
                          {(provider?.nameEn ?? "P").substring(0, 1).toUpperCase()}
                        </Text>
                      </View>
                    </View>
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
  searchButton: {
    backgroundColor: Brand.colors.primaryDark,
    borderRadius: 12,
    paddingVertical: 10,
    paddingHorizontal: 14,
    marginBottom: 8,
    alignItems: "center",
  },
  searchButtonText: {
    color: "#fff",
    fontWeight: "800",
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
  wegoRow: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
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
  packageProviderRow: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 6,
  },
  packageName: {
    color: Brand.colors.primaryDark,
    fontWeight: "800",
    fontSize: 14,
    flex: 1,
    marginRight: 8,
  },
  providerSideLabel: {
    color: Brand.colors.textPrimary,
    fontWeight: "700",
    fontSize: 13,
  },
  row: {
    marginTop: 8,
    marginBottom: 4,
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
  },
  finalPrice: { color: Brand.colors.primaryDark, fontWeight: "800", fontSize: 18 },
  wegoPrice: { color: Brand.colors.primaryDark, fontWeight: "900", fontSize: 22 },
  originalPrice: {
    color: Brand.colors.textSecondary,
    fontSize: 12,
    textDecorationLine: "line-through",
    marginBottom: 4,
  },
  meta: { color: Brand.colors.textSecondary, fontSize: 12 },
});
