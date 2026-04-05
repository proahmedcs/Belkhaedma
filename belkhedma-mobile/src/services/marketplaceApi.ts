import { API_BASE_URL } from "../config/api";
import {
  CustomerSavedLocation,
  CustomerAuthResponse,
  CustomerProfile,
  HomePromotion,
  PriceSnapshot,
  Provider,
  ServiceAttribute,
  ServiceOffer,
  ProviderJsonDocument,
} from "../types/marketplace";

class ApiHttpError extends Error {
  status: number;
  path: string;

  constructor(path: string, status: number, message: string) {
    super(`${message} (HTTP ${status})`);
    this.name = "ApiHttpError";
    this.status = status;
    this.path = path;
  }
}

function normalizeCustomerAuthResponse(
  value: Partial<CustomerAuthResponse>,
  fallback?: { email?: string; refreshToken?: string }
): CustomerAuthResponse {
  if (!value.authToken) {
    throw new Error("Authentication response is missing auth token.");
  }

  return {
    customerId: value.customerId ?? "",
    customerReference: value.customerReference ?? "",
    fullName: value.fullName ?? "",
    mobileNumber: value.mobileNumber ?? "",
    authToken: value.authToken,
    refreshToken: value.refreshToken ?? fallback?.refreshToken ?? value.authToken,
    email: value.email ?? fallback?.email ?? "",
    expiresAtUtc: value.expiresAtUtc ?? new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(),
    isNewAccount: Boolean(value.isNewAccount),
  };
}

function isMissingAuthEndpoint(error: unknown): boolean {
  return error instanceof ApiHttpError && (error.status === 404 || error.status === 405);
}

async function fetchJson<T>(path: string, token?: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: token
      ? {
          Authorization: `Bearer ${token}`,
        }
      : undefined,
  });
  if (!response.ok) {
    let message = `API request failed for ${path}`;
    try {
      const payload = (await response.json()) as { message?: string };
      if (payload?.message) {
        message = payload.message;
      }
    } catch {
      // Keep default message if response body is not JSON.
    }
    throw new ApiHttpError(path, response.status, message);
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
    let message = `API request failed for ${path}`;
    try {
      const errorPayload = (await response.json()) as { message?: string };
      if (errorPayload?.message) {
        message = errorPayload.message;
      }
    } catch {
      // ignore parse errors and keep default message
    }
    throw new ApiHttpError(path, response.status, message);
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

export async function getServiceAttributes(
  providerCode?: string,
  serviceOfferId?: string,
  serviceMode?: number
): Promise<ServiceAttribute[]> {
  const query = new URLSearchParams();
  if (providerCode) {
    query.set("providerCode", providerCode);
  }
  if (serviceOfferId) {
    query.set("serviceOfferId", serviceOfferId);
  }
  if (typeof serviceMode === "number") {
    query.set("serviceMode", String(serviceMode));
  }
  const suffix = query.toString() ? `?${query.toString()}` : "";
  return fetchJson<ServiceAttribute[]>(`/api/marketplace/service-attributes${suffix}`);
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
  email?: string;
  mobileNumber?: string;
  fullName?: string;
  password: string;
  userNameOrEmail?: string;
}): Promise<CustomerAuthResponse> {
  const response = await postJson<typeof payload, Partial<CustomerAuthResponse>>(
    "/api/auth/customers/register-or-login",
    payload
  );
  return normalizeCustomerAuthResponse(response, {
    email: payload.email ?? payload.userNameOrEmail,
    refreshToken: response.authToken,
  });
}

export async function registerCustomer(payload: {
  email: string;
  mobileNumber: string;
  fullName: string;
  password: string;
}): Promise<CustomerAuthResponse> {
  try {
    const response = await postJson<typeof payload, Partial<CustomerAuthResponse>>("/api/auth/customers/register", payload);
    return normalizeCustomerAuthResponse(response, {
      email: payload.email,
      refreshToken: response.authToken,
    });
  } catch (error) {
    if (!isMissingAuthEndpoint(error)) {
      throw error;
    }
    return registerOrLoginCustomer({
      email: payload.email,
      mobileNumber: payload.mobileNumber,
      fullName: payload.fullName,
      password: payload.password,
      userNameOrEmail: payload.email,
    });
  }
}

export async function loginCustomer(payload: {
  userNameOrEmail: string;
  password: string;
}): Promise<CustomerAuthResponse> {
  try {
    const response = await postJson<typeof payload, Partial<CustomerAuthResponse>>("/api/auth/customers/login", payload);
    return normalizeCustomerAuthResponse(response, {
      email: payload.userNameOrEmail,
      refreshToken: response.authToken,
    });
  } catch (error) {
    if (!isMissingAuthEndpoint(error)) {
      throw error;
    }
    return registerOrLoginCustomer({
      email: payload.userNameOrEmail.includes("@") ? payload.userNameOrEmail : undefined,
      password: payload.password,
      userNameOrEmail: payload.userNameOrEmail,
    });
  }
}

export async function refreshCustomerToken(payload: {
  refreshToken: string;
}): Promise<CustomerAuthResponse> {
  return postJson<typeof payload, CustomerAuthResponse>("/api/auth/customers/refresh", payload);
}

export async function revokeCustomerRefreshToken(payload: {
  refreshToken: string;
}): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/auth/customers/revoke`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(payload),
  });
  if (!response.ok) {
    throw new Error(`API request failed (${response.status}) for /api/auth/customers/revoke`);
  }
}

export async function getCurrentCustomer(authToken: string): Promise<CustomerProfile> {
  return fetchJson<CustomerProfile>("/api/auth/customers/me", authToken);
}

export async function getHomePromotions(includeInactive = false): Promise<HomePromotion[]> {
  const query = new URLSearchParams();
  query.set("includeInactive", includeInactive ? "true" : "false");
  return fetchJson<HomePromotion[]>(`/api/marketplace/home-promotions?${query.toString()}`);
}

export async function createHomePromotion(payload: {
  code?: string | null;
  companyNameAr: string;
  companyNameEn: string;
  titleAr: string;
  titleEn: string;
  subtitleAr: string;
  subtitleEn: string;
  imageUrl: string;
  targetUrl?: string | null;
  deepLink?: string | null;
  items?: string[];
  providerCode?: string | null;
  displayOrder: number;
  isActive: boolean;
}): Promise<HomePromotion> {
  return postJson<typeof payload, HomePromotion>("/api/marketplace/home-promotions", payload);
}

export async function deleteHomePromotion(promotionId: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/marketplace/home-promotions/${encodeURIComponent(promotionId)}`, {
    method: "DELETE",
  });
  if (!response.ok) {
    throw new Error(`API request failed (${response.status}) for deleting promotion.`);
  }
}
