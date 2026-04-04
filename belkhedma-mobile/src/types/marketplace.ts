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
  appUrl?: string | null;
  tinyUrl?: string | null;
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

export type ServiceOffer = {
  id: string;
  providerId: string;
  providerServiceId: string;
  serviceMode: number;
  nameAr: string;
  nameEn: string;
  isAvailable: boolean;
  updatedAtUtc: string;
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

export type CustomerSavedLocation = {
  id: string;
  customerReference: string;
  label: string;
  city: string;
  district: string;
  latitude: number;
  longitude: number;
  googleMapsUrl?: string | null;
  googlePlaceId?: string | null;
  updatedAtUtc: string;
};

export type CustomerAuthResponse = {
  customerId: string;
  customerReference: string;
  fullName: string;
  mobileNumber: string;
  authToken: string;
  expiresAtUtc: string;
  isNewAccount: boolean;
};

export type CustomerProfile = {
  customerId: string;
  customerReference: string;
  fullName: string;
  mobileNumber: string;
};
