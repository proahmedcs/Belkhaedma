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
  getCurrentCustomer,
  getCustomerSavedLocations,
  getAllPrices,
  getLatestPrices,
  getProviderJsonDocuments,
  getProviders,
  registerOrLoginCustomer,
  getServiceOffers,
} from "./src/services/marketplaceApi";
import { Brand } from "./src/theme/brand";
import {
  CustomerAuthResponse,
  CustomerProfile,
  CustomerSavedLocation,
  PriceSnapshot,
  Provider,
  ProviderJsonDocument,
  ServiceOffer,
} from "./src/types/marketplace";

type LanguageMode = "ar" | "en";
type ServiceGroup = "hourly-cleaning" | "monthly" | "medical-services" | "mediation-services";
type WizardStep = 0 | 1 | 2 | 3;
type PersistedAuthSession = {
  authToken: string;
  customerReference: string;
};
type JsonDrivenOptions = {
  shifts: string[];
  nationalityGroups: string[];
  contractDurations: string[];
  workerCounts: number[];
  hoursPerVisit: number[];
  weeklyVisits: number[];
  deliveryWindows: string[];
};
type ServiceDateOption = {
  value: string;
  weekdayLabel: string;
  dateLabel: string;
};
type MandatoryFieldKey = "serviceDate" | "shift" | "contractDurationName";
type ResultsSortMode = "recommended" | "cheapest" | "highest";
type PrimaryMenuKey = "main" | "search" | "orders" | "account";
type SecondaryMenuItem = { key: string; labelEn: string; labelAr: string };
type SampleNotification = { id: string; titleEn: string; titleAr: string; metaEn: string; metaAr: string };

const PRIMARY_MENUS: Array<{
  key: PrimaryMenuKey;
  labelEn: string;
  labelAr: string;
  icon: string;
  secondary: SecondaryMenuItem[];
}> = [
  {
    key: "main",
    labelEn: "Main",
    labelAr: "الرئيسية",
    icon: "🏠",
    secondary: [
      { key: "overview", labelEn: "Overview", labelAr: "نظرة عامة" },
      { key: "offers", labelEn: "Offers", labelAr: "العروض" },
      { key: "providers", labelEn: "Providers", labelAr: "المزودون" },
    ],
  },
  {
    key: "search",
    labelEn: "Search",
    labelAr: "البحث",
    icon: "🔎",
    secondary: [
      { key: "hourly", labelEn: "Hourly", labelAr: "بالساعة" },
      { key: "monthly", labelEn: "Monthly", labelAr: "شهري" },
      { key: "medical", labelEn: "Medical", labelAr: "طبي" },
      { key: "mediation", labelEn: "Mediation", labelAr: "وساطة" },
    ],
  },
  {
    key: "orders",
    labelEn: "Orders",
    labelAr: "طلباتي",
    icon: "📄",
    secondary: [
      { key: "active-orders", labelEn: "Active", labelAr: "نشطة" },
      { key: "history-orders", labelEn: "History", labelAr: "السجل" },
      { key: "draft-orders", labelEn: "Drafts", labelAr: "مسودات" },
    ],
  },
  {
    key: "account",
    labelEn: "Account",
    labelAr: "الحساب",
    icon: "👤",
    secondary: [
      { key: "profile", labelEn: "Profile", labelAr: "الملف" },
      { key: "notifications", labelEn: "Notifications", labelAr: "الإشعارات" },
      { key: "support", labelEn: "Support", labelAr: "الدعم" },
    ],
  },
];

const SAMPLE_CALL_CENTER_NUMBER = "+966920000000";
const SAMPLE_NOTIFICATIONS: SampleNotification[] = [
  {
    id: "notif-1",
    titleEn: "Contract reminder",
    titleAr: "تذكير بالعقد",
    metaEn: "Your monthly package starts tomorrow.",
    metaAr: "باقتك الشهرية تبدأ غداً.",
  },
  {
    id: "notif-2",
    titleEn: "Price drop alert",
    titleAr: "تنبيه انخفاض السعر",
    metaEn: "Enaya copied package is now cheaper.",
    metaAr: "باقة نسخة عناية أصبحت أرخص.",
  },
];

const AUTH_SESSION_STORAGE_KEY = "belkhedma.auth.session.v1";

function getWebStorage():
  | {
      getItem(key: string): string | null;
      setItem(key: string, value: string): void;
      removeItem(key: string): void;
    }
  | null {
  const maybeGlobal = globalThis as {
    localStorage?: {
      getItem(key: string): string | null;
      setItem(key: string, value: string): void;
      removeItem(key: string): void;
    };
  };
  return maybeGlobal.localStorage ?? null;
}

function readPersistedAuthSession(): PersistedAuthSession | null {
  const storage = getWebStorage();
  if (!storage) return null;

  try {
    const rawValue = storage.getItem(AUTH_SESSION_STORAGE_KEY);
    if (!rawValue) return null;
    const parsed = JSON.parse(rawValue) as Partial<PersistedAuthSession>;
    if (!parsed.authToken || !parsed.customerReference) return null;
    return {
      authToken: parsed.authToken,
      customerReference: parsed.customerReference,
    };
  } catch {
    return null;
  }
}

function persistAuthSession(session: PersistedAuthSession): void {
  const storage = getWebStorage();
  if (!storage) return;
  try {
    storage.setItem(AUTH_SESSION_STORAGE_KEY, JSON.stringify(session));
  } catch {
    // Ignore storage write errors and continue app flow.
  }
}

function clearPersistedAuthSession(): void {
  const storage = getWebStorage();
  if (!storage) return;
  try {
    storage.removeItem(AUTH_SESSION_STORAGE_KEY);
  } catch {
    // Ignore storage delete errors and continue app flow.
  }
}

function normalizeText(value: string): string {
  return value.toLowerCase().trim();
}

function inferServiceGroup(offer: ServiceOffer, provider?: Provider): ServiceGroup {
  const source = normalizeText(`${offer.nameEn} ${offer.nameAr} ${provider?.providerType ?? ""}`);

  // ServiceMode must have priority over text heuristics to avoid misclassifying
  // monthly offers from providers that also support recruitment.
  if (offer.serviceMode === 2 || offer.serviceMode === 3 || source.includes("month") || source.includes("شهري")) {
    return "monthly";
  }

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

function inferOfferHours(offer?: ServiceOffer | null): number | null {
  if (!offer) return null;
  const source = `${offer.nameEn} ${offer.nameAr}`;
  const numbers = extractNumbersByRegex(source, /\d+\s*(hour|hours|ساعة|ساعات)/gi);
  return numbers[0] ?? null;
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
    candidates.add(`https://${host}/favicon.ico`);
    candidates.add(`https://${host}/favicon.png`);
    candidates.add(`https://www.google.com/s2/favicons?domain=${host}&sz=128`);
    candidates.add(`https://icons.duckduckgo.com/ip3/${host}.ico`);
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

function getServiceGroupDescription(group: ServiceGroup, languageMode: LanguageMode): string {
  if (languageMode === "ar") {
    if (group === "hourly-cleaning") return "خدمة عاملة منزلية محترفة بنظام الساعة";
    if (group === "monthly") return "باقات شهرية للإقامة الكاملة أو الزيارات المنتظمة";
    if (group === "medical-services") return "خدمات طبية وتمريض منزلي باحترافية";
    return "خدمات الوساطة والاستقدام حسب متطلباتك";
  }

  if (group === "hourly-cleaning") return "Professional hourly home cleaning services.";
  if (group === "monthly") return "Monthly packages for full stay or recurring visits.";
  if (group === "medical-services") return "Home medical and nursing services.";
  return "Mediation and recruitment services for your needs.";
}

function getServiceGroupIcon(group: ServiceGroup): string {
  if (group === "hourly-cleaning") return "🕒";
  if (group === "monthly") return "📅";
  if (group === "medical-services") return "🩺";
  return "🤝";
}

export default function App() {
  const [authToken, setAuthToken] = useState<string | null>(null);
  const [currentCustomer, setCurrentCustomer] = useState<CustomerProfile | null>(null);
  const [customerFullName, setCustomerFullName] = useState<string>("");
  const [customerMobileNumber, setCustomerMobileNumber] = useState<string>("");
  const [authLoading, setAuthLoading] = useState<boolean>(false);
  const [authBootstrapping, setAuthBootstrapping] = useState<boolean>(true);

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
  const [customerReference, setCustomerReference] = useState<string>("");
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
  const [showResultsFilters, setShowResultsFilters] = useState<boolean>(false);
  const [resultsSortMode, setResultsSortMode] = useState<ResultsSortMode>("recommended");
  const [resultsSourceFilter, setResultsSourceFilter] = useState<"all" | "api" | "scraper" | "demo">("all");
  const [resultsShiftFilter, setResultsShiftFilter] = useState<string | null>(null);
  const [resultsContractDurationFilter, setResultsContractDurationFilter] = useState<string | null>(null);
  const [resultsHoursFilter, setResultsHoursFilter] = useState<number | null>(null);
  const [activePrimaryMenu, setActivePrimaryMenu] = useState<PrimaryMenuKey>("main");
  const [activeSecondaryMenu, setActiveSecondaryMenu] = useState<string>("overview");
  const [notificationCount] = useState<number>(3);

  const [wizardStep, setWizardStep] = useState<WizardStep>(0);
  const [languageMode, setLanguageMode] = useState<LanguageMode>("en");
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [logoFallbackIndex, setLogoFallbackIndex] = useState<Record<string, number>>({});

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
  const selectedServiceMode = selectedSubService?.serviceMode ?? null;
  const mandatoryFieldKeys = useMemo<MandatoryFieldKey[]>(() => {
    if (selectedServiceMode === 1 || selectedGroup === "hourly-cleaning") {
      return ["serviceDate", "shift"];
    }
    if (selectedServiceMode === 2 || selectedServiceMode === 3 || selectedGroup === "monthly") {
      return ["serviceDate", "contractDurationName"];
    }
    return ["serviceDate"];
  }, [selectedGroup, selectedServiceMode]);
  const mandatoryFieldLabels = useMemo(() => {
    return mandatoryFieldKeys.map((key) => {
      if (key === "serviceDate") return languageMode === "ar" ? "تاريخ الخدمة" : "Service Date";
      if (key === "shift") return languageMode === "ar" ? "الفترة (صباح/مساء)" : "Shift (Morning/Evening)";
      return languageMode === "ar" ? "مدة التعاقد" : "Contract Duration";
    });
  }, [languageMode, mandatoryFieldKeys]);

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

  const serviceDateOptions = useMemo<ServiceDateOption[]>(() => {
    const baseDate = new Date();
    baseDate.setHours(0, 0, 0, 0);

    return Array.from({ length: 21 }, (_, index) => {
      const date = new Date(baseDate);
      date.setDate(baseDate.getDate() + index);
      const value = date.toISOString().slice(0, 10);
      const weekdayLabel = date.toLocaleDateString(languageMode === "ar" ? "ar-SA" : "en-US", { weekday: "short" });
      const dateLabel = date.toLocaleDateString(languageMode === "ar" ? "ar-SA" : "en-US", {
        month: "short",
        day: "numeric",
      });
      return { value, weekdayLabel, dateLabel };
    });
  }, [languageMode]);
  const effectiveServiceDateOptions = useMemo(() => {
    if (serviceDateOptions.length > 0) {
      return serviceDateOptions;
    }
    return [{ value: "", weekdayLabel: "-", dateLabel: "-" }];
  }, [serviceDateOptions]);

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
  const effectiveShiftOptions = useMemo(() => {
    if (selectedServiceMode === 1 || selectedGroup === "hourly-cleaning") {
      return shiftOptions.filter((option) => {
        const normalized = normalizeText(option);
        return normalized.includes("morning") || normalized.includes("evening") || normalized.includes("صباح") || normalized.includes("مساء");
      });
    }
    return shiftOptions;
  }, [selectedGroup, selectedServiceMode, shiftOptions]);

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
    if (!selectedShift && effectiveShiftOptions.length > 0) {
      setSelectedShift(effectiveShiftOptions[0]);
    } else if (selectedShift && !effectiveShiftOptions.includes(selectedShift)) {
      setSelectedShift(effectiveShiftOptions[0] ?? null);
    }
  }, [effectiveShiftOptions, selectedShift]);

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

  useEffect(() => {
    if (!serviceDate && effectiveServiceDateOptions.length > 0) {
      setServiceDate(effectiveServiceDateOptions[0].value);
    }
  }, [effectiveServiceDateOptions, serviceDate]);

  const selectedLocation = useMemo(
    () => savedLocations.find((x) => x.id === selectedLocationId) ?? null,
    [savedLocations, selectedLocationId]
  );
  const topLocationTitle = languageMode === "ar" ? "العنوان" : "Location";
  const topLocationValue = useMemo(() => {
    if (!selectedLocation) {
      return languageMode === "ar" ? "اختر العنوان" : "Select location";
    }
    return `${selectedLocation.label} - ${selectedLocation.city}`;
  }, [languageMode, selectedLocation]);

  const groupComparisonOfferIds = useMemo(() => {
    if (!selectedGroup) {
      return null;
    }

    return new Set(
      serviceOffers
        .filter((offer) => inferServiceGroup(offer, providerById[offer.providerId]) === selectedGroup)
        .map((offer) => offer.id)
    );
  }, [providerById, selectedGroup, serviceOffers]);

  const searchedPrices = useMemo(() => {
    const q = searchText.trim().toLowerCase();
    return prices.filter((price) => {
      const provider = providerById[price.providerId];
      if (!provider) return false;

      if (selectedProvider && provider.code !== selectedProvider) return false;
      if (groupComparisonOfferIds && !groupComparisonOfferIds.has(price.serviceOfferId)) return false;

      if (!q) return true;
      const haystack = [provider.nameAr, provider.nameEn, provider.code, provider.providerType]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();
      return haystack.includes(q);
    });
  }, [groupComparisonOfferIds, prices, providerById, searchText, selectedProvider]);

  const buildSyntheticProviderPrice = (
    basePriceSar: number,
    providerCode: string,
    providerIndex: number
  ): number => {
    const codeSeed = providerCode.split("").reduce((sum, char) => sum + char.charCodeAt(0), 0);
    const multiplier = 0.9 + ((codeSeed + providerIndex * 13) % 31) / 100;
    return Math.round(basePriceSar * multiplier * 100) / 100;
  };

  const wegoStyleRows = useMemo(() => {
    const realRows = searchedPrices.map((price) => ({
      price,
      provider: providerById[price.providerId],
      offer: offerById[price.serviceOfferId],
    }));

    const rowsWithProvider = realRows.filter((row) => !!row.provider);
    const baselineRow = rowsWithProvider.find((row) => row.provider?.code === "enaya") ?? rowsWithProvider[0];
    const providerIdsInRows = new Set(rowsWithProvider.map((row) => row.provider?.id).filter((id): id is string => !!id));

    const syntheticRows = hasSearched && baselineRow
      ? filteredProviders
          .filter((provider) => !providerIdsInRows.has(provider.id))
          .filter((provider) => {
            if (!selectedGroup) return true;
            if (selectedGroup === "hourly-cleaning") return provider.supportsHourly;
            if (selectedGroup === "monthly") return provider.supportsMonthly;
            if (selectedGroup === "medical-services") {
              const normalizedType = normalizeText(provider.providerType);
              return normalizedType.includes("medical") || normalizedType.includes("طبي");
            }
            return provider.supportsRecruitment;
          })
          .map((provider, index) => {
            const providerGroupOffer = groupFilteredOffers.find((offer) => offer.providerId === provider.id);
            const syntheticFinalPrice = buildSyntheticProviderPrice(
              baselineRow.price.finalPriceSar,
              provider.code,
              index
            );
            const syntheticOffer =
              providerGroupOffer ??
              ({
                ...(selectedSubService ?? baselineRow.offer),
                id: `synthetic-offer-${provider.id}`,
                providerId: provider.id,
                updatedAtUtc: new Date().toISOString(),
              } as ServiceOffer);
            const now = new Date();
            const expires = new Date(now.getTime() + 6 * 60 * 60 * 1000);
            return {
              provider,
              offer: syntheticOffer,
              price: {
                ...baselineRow.price,
                id: `synthetic-price-${provider.id}`,
                providerId: provider.id,
                serviceOfferId: syntheticOffer.id,
                sourceType: 2,
                finalPriceSar: syntheticFinalPrice,
                originalPriceSar: Math.round(syntheticFinalPrice * 1.15 * 100) / 100,
                vatAmountSar: Math.round(syntheticFinalPrice * 0.15 * 100) / 100,
                collectedAtUtc: now.toISOString(),
                expiresAtUtc: expires.toISOString(),
              },
            };
          })
      : [];

    return [...rowsWithProvider, ...syntheticRows].sort((a, b) => a.price.finalPriceSar - b.price.finalPriceSar);
  }, [
    filteredProviders,
    groupFilteredOffers,
    hasSearched,
    offerById,
    providerById,
    searchedPrices,
    selectedGroup,
    selectedSubService,
  ]);
  const serviceModeFilteredRows = useMemo(() => {
    return wegoStyleRows.filter(({ offer }) => {
      if (!selectedServiceMode) return true;
      if (!offer) return false;
      return offer.serviceMode === selectedServiceMode;
    });
  }, [selectedServiceMode, wegoStyleRows]);
  const resultsRows = useMemo(() => {
    const rows = serviceModeFilteredRows.filter(({ price }) => {
      const sourceLabel = price.id.startsWith("synthetic-price-")
        ? "demo"
        : price.sourceType === 1
          ? "api"
          : "scraper";
      if (resultsSourceFilter !== "all" && sourceLabel !== resultsSourceFilter) {
        return false;
      }
      if (resultsShiftFilter && selectedShift && selectedShift !== resultsShiftFilter) {
        return false;
      }
      if (resultsContractDurationFilter && selectedContractDurationName && selectedContractDurationName !== resultsContractDurationFilter) {
        return false;
      }
      if (resultsHoursFilter && selectedHoursPerVisit && selectedHoursPerVisit !== resultsHoursFilter) {
        return false;
      }
      return true;
    });

    if (resultsSortMode === "cheapest") {
      return rows.slice().sort((a, b) => a.price.finalPriceSar - b.price.finalPriceSar);
    }
    if (resultsSortMode === "highest") {
      return rows.slice().sort((a, b) => b.price.finalPriceSar - a.price.finalPriceSar);
    }
    return rows.slice().sort((a, b) => a.price.finalPriceSar - b.price.finalPriceSar);
  }, [
    resultsContractDurationFilter,
    resultsHoursFilter,
    resultsShiftFilter,
    resultsSortMode,
    resultsSourceFilter,
    selectedContractDurationName,
    selectedHoursPerVisit,
    selectedShift,
    serviceModeFilteredRows,
  ]);

  const loadData = async (activeToken: string | null = authToken, activeCustomerReference: string | null = customerReference || currentCustomer?.customerReference || null) => {
    if (!activeToken || !activeCustomerReference) {
      setProviders([]);
      setServiceOffers([]);
      setPrices([]);
      setJsonDocuments([]);
      setSavedLocations([]);
      setSelectedLocationId(null);
      return;
    }

    try {
      setLoading(true);
      setError(null);

      const [providersData, offersData, pricesData, docsData, locationsData] = await Promise.all([
        getProviders(),
        getServiceOffers(),
        getLatestPrices(),
        getProviderJsonDocuments(undefined, false),
        getCustomerSavedLocations(activeCustomerReference, activeToken),
      ]);

      setProviders(providersData);
      setServiceOffers(offersData);
      setPrices(pricesData);
      setJsonDocuments(docsData);
      setSavedLocations(locationsData);
      setSelectedLocationId((current) => current ?? locationsData[0]?.id ?? null);
      if (customerReference !== activeCustomerReference) {
        setCustomerReference(activeCustomerReference);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Unexpected error.");
    } finally {
      setLoading(false);
    }
  };

  const handleRegisterOrLogin = async () => {
    try {
      setAuthLoading(true);
      setError(null);

      const response = await registerOrLoginCustomer({
        mobileNumber: customerMobileNumber,
        fullName: customerFullName,
      });

      setAuthToken(response.authToken);
      const profile = await getCurrentCustomer(response.authToken);
      setCurrentCustomer(profile);
      setCustomerReference(profile.customerReference);
      setCustomerFullName(profile.fullName);
      setCustomerMobileNumber(profile.mobileNumber);
      persistAuthSession({
        authToken: response.authToken,
        customerReference: profile.customerReference,
      });
      await loadData(response.authToken, profile.customerReference);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Authentication failed.");
    } finally {
      setAuthLoading(false);
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
      setWizardStep(3);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to search prices.");
    } finally {
      setLoading(false);
    }
  };

  const loadSavedLocations = async () => {
    if (!authToken) {
      setError("Authentication token is missing. Please login again.");
      return;
    }

    try {
      const locationsData = await getCustomerSavedLocations(customerReference, authToken);
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
  const mandatoryFiltersSatisfied = useMemo(() => {
    if (!serviceDate) return false;
    if (mandatoryFieldKeys.includes("shift")) return !!selectedShift;
    if (mandatoryFieldKeys.includes("contractDurationName")) return !!selectedContractDurationName;
    return true;
  }, [mandatoryFieldKeys, selectedContractDurationName, selectedShift, serviceDate]);
  const dynamicMandatoryText = useMemo(() => {
    const joined = mandatoryFieldLabels.join(languageMode === "ar" ? " + " : " + ");
    return languageMode === "ar" ? `الحقول الإلزامية: ${joined}` : `Mandatory fields: ${joined}`;
  }, [languageMode, mandatoryFieldLabels]);

  const wizardSteps = [
    languageMode === "ar" ? "الخدمة" : "Service",
    languageMode === "ar" ? "الموقع" : "Location",
    languageMode === "ar" ? "الباقات" : "Packages",
    languageMode === "ar" ? "النتائج" : "Results",
  ];

  const canGoNext = useMemo(() => {
    if (wizardStep === 0) return !!selectedGroup && !!selectedSubServiceId;
    if (wizardStep === 1) return savedLocations.length === 0 || !!selectedLocationId;
    if (wizardStep === 2) {
      return mandatoryFiltersSatisfied;
    }
    return true;
  }, [
    mandatoryFiltersSatisfied,
    savedLocations.length,
    selectedGroup,
    selectedLocationId,
    selectedSubServiceId,
    wizardStep,
  ]);

  const sourceFilterLabel = (value: "all" | "api" | "scraper" | "demo") => {
    if (languageMode === "ar") {
      if (value === "all") return "كل المصادر";
      if (value === "api") return "API فقط";
      if (value === "scraper") return "Scraper فقط";
      return "نسخ Enaya (تجريبي)";
    }
    if (value === "all") return "All Sources";
    if (value === "api") return "API only";
    if (value === "scraper") return "Scraper only";
    return "Enaya copy (demo)";
  };

  const sortFilterLabel = (value: ResultsSortMode) => {
    if (languageMode === "ar") {
      if (value === "recommended") return "موصى به";
      if (value === "cheapest") return "الأرخص";
      return "الأعلى سعراً";
    }
    if (value === "recommended") return "Recommended";
    if (value === "cheapest") return "Cheapest";
    return "Highest Price";
  };

  const goNext = () => {
    if (!canGoNext) return;
    setWizardStep((prev) => Math.min(3, prev + 1) as WizardStep);
  };

  const goBack = () => {
    setWizardStep((prev) => Math.max(0, prev - 1) as WizardStep);
  };

  useEffect(() => {
    let isCancelled = false;

    const bootstrapAuth = async () => {
      const persistedSession = readPersistedAuthSession();
      if (!persistedSession) {
        if (!isCancelled) {
          setAuthBootstrapping(false);
        }
        return;
      }

      try {
        const profile = await getCurrentCustomer(persistedSession.authToken);
        if (isCancelled) return;

        setAuthToken(persistedSession.authToken);
        setCurrentCustomer(profile);
        setCustomerReference(profile.customerReference);
        setCustomerFullName(profile.fullName);
        setCustomerMobileNumber(profile.mobileNumber);
        await loadData(persistedSession.authToken, profile.customerReference);
      } catch {
        clearPersistedAuthSession();
        if (isCancelled) return;
        setAuthToken(null);
        setCurrentCustomer(null);
        setCustomerReference("");
      } finally {
        if (!isCancelled) {
          setAuthBootstrapping(false);
        }
      }
    };

    bootstrapAuth();
    return () => {
      isCancelled = true;
    };
  }, []);

  if (authBootstrapping) {
    return (
      <SafeAreaView style={styles.safeArea}>
        <StatusBar style="light" />
        <View style={[styles.page, styles.stateBlock]}>
          <ActivityIndicator size="large" color={Brand.colors.primary} />
          <Text style={styles.stateText}>
            {languageMode === "ar" ? "جاري تحميل بيانات العميل..." : "Loading customer session..."}
          </Text>
        </View>
      </SafeAreaView>
    );
  }

  if (!currentCustomer || !authToken) {
    return (
      <SafeAreaView style={styles.safeArea}>
        <StatusBar style="light" />
        <ScrollView contentContainerStyle={styles.page}>
          <View style={styles.header}>
            <Text style={styles.logo}>Belkhedma</Text>
            <Text style={styles.subtitle}>
              {languageMode === "ar"
                ? "تسجيل العميل عبر رقم الجوال والاسم"
                : "Customer authentication with mobile and name"}
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
            <Text style={styles.sectionTitle}>{languageMode === "ar" ? "تسجيل / دخول العميل" : "Customer Register / Login"}</Text>
            <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "الاسم الكامل" : "Full Name"}</Text>
            <TextInput
              placeholder={languageMode === "ar" ? "مثال: أحمد عبدالغني" : "e.g. Ahmed Abdelghany"}
              value={customerFullName}
              onChangeText={setCustomerFullName}
              style={styles.searchInput}
              placeholderTextColor={Brand.colors.textSecondary}
            />
            <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "رقم الجوال" : "Mobile Number"}</Text>
            <TextInput
              placeholder={languageMode === "ar" ? "مثال: +966500000000" : "e.g. +966500000000"}
              value={customerMobileNumber}
              onChangeText={setCustomerMobileNumber}
              style={styles.searchInput}
              placeholderTextColor={Brand.colors.textSecondary}
              keyboardType="phone-pad"
            />
            <TouchableOpacity
              style={[styles.searchButton, authLoading && styles.navButtonDisabled]}
              onPress={handleRegisterOrLogin}
            >
              <Text style={styles.searchButtonText}>
                {authLoading
                  ? languageMode === "ar"
                    ? "جاري التحقق..."
                    : "Authenticating..."
                  : languageMode === "ar"
                    ? "تسجيل / دخول"
                    : "Register / Login"}
              </Text>
            </TouchableOpacity>
            <Text style={styles.meta}>
              {languageMode === "ar"
                ? "باستعمال رقم الجوال سيتم إنشاء حساب جديد أو تسجيل الدخول مباشرة."
                : "Using mobile number will create a new account or log in directly."}
            </Text>
          </View>
        </ScrollView>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.safeArea}>
      <StatusBar style="light" />
      <ScrollView contentContainerStyle={styles.page}>
        <View style={styles.header}>
          <View style={styles.topUtilityRow}>
            <TouchableOpacity style={styles.locationPill} onPress={() => setWizardStep(1)}>
              <Text style={styles.locationPillIcon}>📍</Text>
              <View style={styles.locationPillTextWrap}>
                <Text style={styles.locationPillTitle}>{topLocationTitle}</Text>
                <Text style={styles.locationPillValue} numberOfLines={1}>
                  {topLocationValue}
                </Text>
              </View>
              <Text style={styles.locationPillChevron}>›</Text>
            </TouchableOpacity>
          </View>
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
          <Text style={styles.metaHeaderText}>
            {languageMode === "ar"
              ? `العميل: ${currentCustomer.fullName} (${currentCustomer.mobileNumber})`
              : `Customer: ${currentCustomer.fullName} (${currentCustomer.mobileNumber})`}
          </Text>
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
              <Text style={styles.subSectionTitle}>
                {languageMode === "ar" ? "اختر مجموعة الخدمة المطلوبة" : "Choose required service group"}
              </Text>
              <View style={styles.homeGroupsGrid}>
                {availableGroups.map((group) => {
                  const active = selectedGroup === group;
                  return (
                    <TouchableOpacity
                      key={group}
                      style={[styles.homeGroupCard, active && styles.homeGroupCardActive]}
                      onPress={() => setSelectedGroup(group)}
                    >
                      <View style={styles.homeGroupIconWrap}>
                        <Text style={styles.homeGroupIcon}>{getServiceGroupIcon(group)}</Text>
                      </View>
                      <View style={styles.homeGroupTextWrap}>
                        <Text style={[styles.homeGroupTitle, active && styles.homeGroupTitleActive]}>
                          {groupLabel(group)}
                        </Text>
                        <Text style={[styles.homeGroupDescription, active && styles.homeGroupDescriptionActive]}>
                          {getServiceGroupDescription(group, languageMode)}
                        </Text>
                      </View>
                    </TouchableOpacity>
                  );
                })}
              </View>
              {availableGroups.length === 0 ? <Text style={styles.meta}>No service groups available.</Text> : null}
            </>
          ) : null}

          {wizardStep === 0 ? (
            <>
              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "اختر الخدمة" : "Choose Service"}</Text>
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

              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "اختر موقع سابق" : "Choose Saved Location"}</Text>
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

          {wizardStep === 2 ? (
            <>
              <Text style={styles.fieldHint}>{dynamicMandatoryText}</Text>
              <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "تاريخ الخدمة (إلزامي)" : "Service Date (mandatory)"}</Text>
              <FlatList
                horizontal
                data={effectiveServiceDateOptions}
                keyExtractor={(item) => item.value}
                showsHorizontalScrollIndicator={false}
                renderItem={({ item }) => {
                  const active = item.value === serviceDate;
                  return (
                    <TouchableOpacity
                      style={[styles.dateCard, active && styles.dateCardActive]}
                      onPress={() => setServiceDate(item.value)}
                    >
                      <Text style={[styles.dateCardWeekday, active && styles.dateCardTextActive]}>{item.weekdayLabel}</Text>
                      <Text style={[styles.dateCardDay, active && styles.dateCardTextActive]}>{item.dateLabel}</Text>
                      <Text style={[styles.dateCardIso, active && styles.dateCardTextActive]}>{item.value}</Text>
                    </TouchableOpacity>
                  );
                }}
              />
              {mandatoryFieldKeys.includes("shift") ? (
                <>
                  <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "الفترة (إلزامي)" : "Shift (mandatory)"}</Text>
                  <FlatList
                    horizontal
                    data={effectiveShiftOptions}
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
                </>
              ) : null}
              {mandatoryFieldKeys.includes("contractDurationName") ? (
                <>
                  <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "مدة التعاقد (إلزامي)" : "Contract Duration (mandatory)"}</Text>
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
                </>
              ) : null}
              {!mandatoryFiltersSatisfied ? (
                <Text style={styles.errorText}>
                  {languageMode === "ar"
                    ? "يرجى اختيار الحقول الإلزامية أولاً قبل عرض النتائج."
                    : "Please select all mandatory fields before viewing results."}
                </Text>
              ) : null}
            </>
          ) : null}

          {wizardStep === 3 ? (
            <>
              <View style={styles.rowBetween}>
                <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "النتائج" : "Results"}</Text>
                <TouchableOpacity style={styles.filterToggleButton} onPress={() => setShowResultsFilters((prev) => !prev)}>
                  <Text style={styles.filterToggleText}>{languageMode === "ar" ? "فلاتر" : "Filters"}</Text>
                </TouchableOpacity>
              </View>
              {showResultsFilters ? (
                <View style={styles.summaryBar}>
                  <Text style={styles.subSectionTitle}>{languageMode === "ar" ? "Sort & Filter" : "Sort & Filter"}</Text>
                  <Text style={styles.fieldHint}>{languageMode === "ar" ? "الترتيب" : "Sort"}</Text>
                  <View style={styles.rowWrap}>
                    {([
                      { key: "recommended", labelEn: "Recommended", labelAr: "موصى به" },
                      { key: "cheapest", labelEn: "Cheapest", labelAr: "الأرخص" },
                      { key: "highest", labelEn: "Highest", labelAr: "الأعلى" },
                    ] as Array<{ key: ResultsSortMode; labelEn: string; labelAr: string }>).map((item) => (
                      <TouchableOpacity
                        key={item.key}
                        style={[styles.chip, resultsSortMode === item.key && styles.chipActive]}
                        onPress={() => setResultsSortMode(item.key)}
                      >
                        <Text style={[styles.chipText, resultsSortMode === item.key && styles.chipTextActive]}>
                          {languageMode === "ar" ? item.labelAr : item.labelEn}
                        </Text>
                      </TouchableOpacity>
                    ))}
                  </View>
                  <Text style={styles.fieldHint}>{languageMode === "ar" ? "مصدر السعر" : "Source"}</Text>
                  <View style={styles.rowWrap}>
                    {([
                      { key: "all", labelEn: "All", labelAr: "الكل" },
                      { key: "api", labelEn: "API", labelAr: "API" },
                      { key: "scraper", labelEn: "Scraper", labelAr: "استخراج" },
                      { key: "demo", labelEn: "Enaya Copy", labelAr: "نسخة عناية" },
                    ] as Array<{ key: "all" | "api" | "scraper" | "demo"; labelEn: string; labelAr: string }>).map((item) => (
                      <TouchableOpacity
                        key={item.key}
                        style={[styles.chip, resultsSourceFilter === item.key && styles.chipActive]}
                        onPress={() => setResultsSourceFilter(item.key)}
                      >
                        <Text style={[styles.chipText, resultsSourceFilter === item.key && styles.chipTextActive]}>
                          {languageMode === "ar" ? item.labelAr : item.labelEn}
                        </Text>
                      </TouchableOpacity>
                    ))}
                  </View>
                </View>
              ) : null}
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
                {selectedGroup === "monthly" ? (
                  <Text style={styles.summaryText}>
                    {languageMode === "ar"
                      ? `المدة المختارة: ${monthlyDurationLabel(monthlyDurationMonths)}`
                      : `Selected monthly duration: ${monthlyDurationLabel(monthlyDurationMonths)}`}
                  </Text>
                ) : null}
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
            {wizardStep < 3 ? (
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
            resultsRows.map(({ price, provider, offer }) => {
              const isMonthlyFlow = selectedGroup === "monthly";
              const durationFromOffer = parseDurationToMonths(`${offer?.nameEn ?? ""} ${offer?.nameAr ?? ""}`);
              const durationFromSelection = parseDurationToMonths(selectedContractDurationName ?? "");
              const baseDurationMonths = Math.max(1, durationFromOffer ?? durationFromSelection ?? 1);
              const workersMultiplier = Math.max(1, selectedWorkersCount ?? 1);
              const monthlyEstimatedTotalSar = isMonthlyFlow
                ? Math.round((price.finalPriceSar / baseDurationMonths) * monthlyDurationMonths * workersMultiplier * 100) / 100
                : null;
              const effectiveDisplayPriceSar = monthlyEstimatedTotalSar ?? price.finalPriceSar;
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
                    <Text style={styles.wegoPrice}>{effectiveDisplayPriceSar} SAR</Text>
                  </View>
                  <View style={styles.row}>
                    <Text style={styles.meta}>
                      Source: {price.id.startsWith("synthetic-price-") ? "Enaya copy (demo)" : price.sourceType === 1 ? "API" : "Scraper"}
                    </Text>
                    {price.originalPriceSar ? (
                      <Text style={styles.originalPrice}>Was {price.originalPriceSar} SAR</Text>
                    ) : (
                      <Text style={styles.meta}>Direct fare</Text>
                    )}
                  </View>
                  {monthlyEstimatedTotalSar != null ? (
                    <Text style={styles.meta}>
                      {languageMode === "ar"
                        ? `تقدير إجمالي ${monthlyDurationMonths} شهر${monthlyDurationMonths > 1 ? " (أشهر)" : ""} لعدد ${workersMultiplier} عامل: ${monthlyEstimatedTotalSar} SAR`
                        : `Estimated ${monthlyDurationMonths}-month total for ${workersMultiplier} worker(s): ${monthlyEstimatedTotalSar} SAR`}
                    </Text>
                  ) : null}
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
  topUtilityRow: {
    marginBottom: 10,
  },
  locationPill: {
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: "#ffffff22",
    borderWidth: 1,
    borderColor: "#ffffff44",
    borderRadius: 12,
    paddingHorizontal: 10,
    paddingVertical: 8,
  },
  locationPillIcon: {
    fontSize: 15,
    marginRight: 8,
  },
  locationPillTextWrap: {
    flex: 1,
  },
  locationPillTitle: {
    color: "#FCE7F3",
    fontSize: 10,
    fontWeight: "700",
  },
  locationPillValue: {
    color: "#fff",
    fontSize: 12,
    fontWeight: "700",
  },
  locationPillChevron: {
    color: "#fff",
    fontSize: 18,
    marginLeft: 8,
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
  dateCard: {
    borderWidth: 1,
    borderColor: Brand.colors.border,
    borderRadius: 12,
    backgroundColor: "#fff",
    paddingVertical: 10,
    paddingHorizontal: 12,
    marginRight: 8,
    marginBottom: 10,
    minWidth: 110,
  },
  dateCardActive: {
    borderColor: Brand.colors.primary,
    backgroundColor: "#FDF2F8",
  },
  dateCardWeekday: {
    color: Brand.colors.primaryDark,
    fontWeight: "700",
    fontSize: 12,
  },
  dateCardDay: {
    color: Brand.colors.textPrimary,
    fontWeight: "700",
    fontSize: 13,
    marginTop: 2,
  },
  dateCardIso: {
    color: Brand.colors.textSecondary,
    fontSize: 11,
    marginTop: 4,
  },
  dateCardTextActive: {
    color: Brand.colors.primaryDark,
  },
  rowBetween: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    marginBottom: 8,
  },
  filterToggleButton: {
    borderWidth: 1,
    borderColor: Brand.colors.border,
    borderRadius: 999,
    paddingHorizontal: 12,
    paddingVertical: 6,
    backgroundColor: "#fff",
  },
  filterToggleText: {
    color: Brand.colors.primaryDark,
    fontWeight: "700",
    fontSize: 12,
  },
  rowWrap: {
    flexDirection: "row",
    flexWrap: "wrap",
    gap: 8,
    marginBottom: 10,
  },
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
  metaHeaderText: {
    color: Brand.colors.textSecondary,
    fontSize: 12,
    marginTop: 6,
    marginBottom: 4,
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
  homeGroupsGrid: {
    gap: 10,
    marginBottom: 8,
  },
  homeGroupCard: {
    flexDirection: "row",
    alignItems: "center",
    borderRadius: 14,
    borderWidth: 1,
    borderColor: "#E5E7EB",
    backgroundColor: "#fff",
    paddingVertical: 12,
    paddingHorizontal: 12,
    marginBottom: 8,
    shadowColor: "#000",
    shadowOpacity: 0.08,
    shadowRadius: 6,
    shadowOffset: { width: 0, height: 2 },
    elevation: 2,
  },
  homeGroupCardActive: {
    backgroundColor: "#8F1121",
    borderColor: "#8F1121",
  },
  homeGroupIconWrap: {
    width: 48,
    height: 48,
    borderRadius: 24,
    backgroundColor: "#ffffff22",
    borderWidth: 1,
    borderColor: "#ffffff55",
    alignItems: "center",
    justifyContent: "center",
    marginRight: 10,
  },
  homeGroupIcon: {
    fontSize: 22,
  },
  homeGroupTextWrap: {
    flex: 1,
  },
  homeGroupTitle: {
    color: Brand.colors.primaryDark,
    fontWeight: "800",
    fontSize: 18,
  },
  homeGroupTitleActive: {
    color: "#fff",
  },
  homeGroupDescription: {
    color: Brand.colors.textSecondary,
    fontSize: 12,
    marginTop: 3,
    lineHeight: 18,
  },
  homeGroupDescriptionActive: {
    color: "#FDE8EE",
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
