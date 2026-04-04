import { API_BASE_URL } from "../config/api";
import {
  CustomerSavedLocation,
  PriceSnapshot,
  Provider,
  ServiceOffer,
  ProviderJsonDocument,
} from "../types/marketplace";

async function fetchJson<T>(path: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`);
  if (!response.ok) {
    throw new Error(`API request failed (${response.status}) for ${path}`);
  }
  return (await response.json()) as T;
}

export async function getProviders(): Promise<Provider[]> {
  return fetchJson<Provider[]>("/api/marketplace/providers");
}

export async function getLatestPrices(providerCode?: string): Promise<PriceSnapshot[]> {
  const query = providerCode ? `?providerCode=${encodeURIComponent(providerCode)}` : "";
  return fetchJson<PriceSnapshot[]>(`/api/marketplace/prices/latest${query}`);
}

export async function getServiceOffers(providerCode?: string): Promise<ServiceOffer[]> {
  const query = providerCode ? `?providerCode=${encodeURIComponent(providerCode)}` : "";
  return fetchJson<ServiceOffer[]>(`/api/marketplace/offers${query}`);
}

export async function getProviderJsonDocuments(
  providerCode?: string,
  includeExpired = true
): Promise<ProviderJsonDocument[]> {
  const query = new URLSearchParams();
  if (providerCode) {
    query.set("providerCode", providerCode);
  }
  query.set("includeExpired", includeExpired ? "true" : "false");
  return fetchJson<ProviderJsonDocument[]>(`/api/marketplace/json-documents?${query.toString()}`);
}

export async function getCustomerSavedLocations(
  customerReference: string
): Promise<CustomerSavedLocation[]> {
  if (!customerReference.trim()) {
    return [];
  }

  return fetchJson<CustomerSavedLocation[]>(
    `/api/marketplace/customers/${encodeURIComponent(customerReference)}/locations`
  );
}
