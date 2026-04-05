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
  hourOptions: number[];
  nationalityOptions: string[];
  serviceAttributes: ServiceAttribute[];
  isAvailable: boolean;
  updatedAtUtc: string;
};

export type ServiceAttribute = {
  id: string;
  serviceOfferId: string;
  attributeKey: string;
  nameAr: string;
  nameEn: string;
  type: number;
  optionSetJson?: string | null;
  isMandatory: boolean;
  filterScope: number;
  displayOrder: number;
  isActive: boolean;
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

export type CustomerRequestAttributeValue = {
  attributeKey: string;
  attributeNameAr: string;
  attributeNameEn: string;
  value: string;
  valueAr: string;
  valueEn: string;
};

export type CreateCustomerServiceRequestPayload = {
  serviceOfferId: string;
  providerId: string;
  priceSnapshotId?: string | null;
  locationId?: string | null;
  locationLabel?: string | null;
  locationCity?: string | null;
  locationDistrict?: string | null;
  locationLatitude?: number | null;
  locationLongitude?: number | null;
  locationGoogleMapsUrl?: string | null;
  locationGooglePlaceId?: string | null;
  serviceDate?: string | null;
  shift?: string | null;
  nationality?: string | null;
  contractDuration?: string | null;
  workersCount?: number | null;
  hoursPerVisit?: number | null;
  weeklyVisits?: number | null;
  deliveryWindow?: string | null;
  providerSource?: string | null;
  notes?: string | null;
  packageAttributes?: CustomerRequestAttributeValue[];
};

export type CustomerServiceRequest = {
  id: string;
  customerId: string;
  customerReference: string;
  customerSavedLocationId?: string | null;
  locationLabel: string;
  locationCity: string;
  locationDistrict: string;
  locationLatitude?: number | null;
  locationLongitude?: number | null;
  locationGoogleMapsUrl?: string | null;
  locationGooglePlaceId?: string | null;
  providerId: string;
  serviceOfferId: string;
  priceSnapshotId?: string | null;
  serviceMode: number;
  packageNameAr: string;
  packageNameEn: string;
  finalPriceSar: number;
  originalPriceSar?: number | null;
  vatAmountSar?: number | null;
  currency: string;
  serviceDate?: string | null;
  selectedShift?: string | null;
  selectedNationality?: string | null;
  selectedContractDuration?: string | null;
  selectedWorkersCount?: number | null;
  selectedHoursPerVisit?: number | null;
  selectedWeeklyVisits?: number | null;
  selectedDeliveryWindow?: string | null;
  selectedProviderSource?: string | null;
  notes: string;
  packageAttributes: CustomerRequestAttributeValue[];
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type CustomerAuthResponse = {
  customerId: string;
  customerReference: string;
  fullName: string;
  mobileNumber: string;
  authToken: string;
  refreshToken: string;
  email: string;
  expiresAtUtc: string;
  isNewAccount: boolean;
};

export type CustomerProfile = {
  customerId: string;
  customerReference: string;
  fullName: string;
  mobileNumber: string;
  email?: string;
};

export type HomePromotion = {
  id: string;
  code: string;
  companyNameAr: string;
  companyNameEn: string;
  titleAr: string;
  titleEn: string;
  subtitleAr: string;
  subtitleEn: string;
  imageUrl: string;
  targetUrl?: string | null;
  deepLink?: string | null;
  items: string[];
  providerCode?: string | null;
  displayOrder: number;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
};
