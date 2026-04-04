export type Provider = {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  providerType: string;
  hasApiAccess: boolean;
  supportsHourly: boolean;
  supportsMonthly: boolean;
  supportsB2B: boolean;
  supportsRecruitment: boolean;
  integrationModeKey: string;
  websiteUrl?: string | null;
  providerAppUrl?: string | null;
  providerTinyUrl?: string | null;
  logoUrl?: string | null;
  notes?: string;
  isActive: boolean;
};

export type PriceSnapshot = {
  id: string;
  providerId: string;
  serviceOfferId: string;
  finalPriceSar: number;
  originalPriceSar?: number | null;
  vatAmountSar?: number | null;
  sourceType: number;
  collectedAtUtc: string;
  expiresAtUtc: string;
};

export type ProviderJsonDocument = {
  id: string;
  documentKey: string;
  fileName: string;
  providerCode?: string | null;
  serviceMode?: number | null;
  jsonContent: string;
  createdAtUtc: string;
  expiresAtUtc: string;
};
