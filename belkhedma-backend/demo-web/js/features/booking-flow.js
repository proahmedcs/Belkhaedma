export function createBookingDraft(providerId, serviceId) {
  return {
    providerId,
    serviceId,
    step: "address",
    status: "draft",
    createdAtUtc: new Date().toISOString(),
  };
}
