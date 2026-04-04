import { API_BASE_URL } from "../config/api";
import { PriceSnapshot, Provider } from "../types/marketplace";

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
