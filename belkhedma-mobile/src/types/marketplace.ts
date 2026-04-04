export type Provider = {
  id: string;
  code: string;
  nameAr: string;
  nameEn: string;
  hasApiAccess: boolean;
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
};
