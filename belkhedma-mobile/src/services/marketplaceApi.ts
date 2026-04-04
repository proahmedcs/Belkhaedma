import { API_BASE_URL } from "../config/api";
import {
  CustomerSavedLocation,
  CustomerAuthResponse,
  CustomerProfile,
  PriceSnapshot,
  Provider,
  ServiceOffer,
  ProviderJsonDocument,
} from "../types/marketplace";

async function fetchJson<T>(path: string, token?: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: token
      ? {
          Authorization: `Bearer ${token}`,
        }
      : undefined,
  });
  if (!response.ok) {
    throw new Error(`API request failed (${response.status}) for ${path}`);
  }
  return (await response.json()) as T;
}

async function postJson<TRequest, TResponse>(path: string, body: TRequest): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(body),
  });

  if (!response.ok) {
    let message = `API request failed (${response.status}) for ${path}`;
    try {
      const errorPayload = (await response.json()) as { message?: string };
      if (errorPayload?.message) {
        message = errorPayload.message;
      }
    } catch {
      // ignore parse errors and keep default message
    }
    throw new Error(message);
  }

  return (await response.json()) as TResponse;
}

export async function getProviders(): Promise<Provider[]> {
  return fetchJson<Provider[]>("/api/marketplace/providers");
}

export async function getLatestPrices(providerCode?: string): Promise<PriceSnapshot[]> {
  const query = providerCode ? `?providerCode=${encodeURIComponent(providerCode)}` : "";
  return fetchJson<PriceSnapshot[]>(`/api/marketplace/prices/latest${query}`);
}

export async function getAllPrices(
  providerCode?: string,
  includeExpired = false
): Promise<PriceSnapshot[]> {
  const query = new URLSearchParams();
  if (providerCode) {
    query.set("providerCode", providerCode);
  }
  query.set("includeExpired", includeExpired ? "true" : "false");
  return fetchJson<PriceSnapshot[]>(`/api/marketplace/prices?${query.toString()}`);
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
  customerReference: string,
  authToken?: string
): Promise<CustomerSavedLocation[]> {
  if (!customerReference.trim()) {
    return [];
  }

  return fetchJson<CustomerSavedLocation[]>(
    `/api/marketplace/customers/${encodeURIComponent(customerReference)}/locations`,
    authToken
  );
}

export async function registerOrLoginCustomer(payload: {
  mobileNumber: string;
  fullName: string;
}): Promise<CustomerAuthResponse> {
  return postJson<typeof payload, CustomerAuthResponse>("/api/auth/customers/register-or-login", payload);
}

export async function getCurrentCustomer(authToken: string): Promise<CustomerProfile> {
  return fetchJson<CustomerProfile>("/api/auth/customers/me", authToken);
}
