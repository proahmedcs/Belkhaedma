export function computePaymentRoute(provider, contractMode) {
  if (provider?.payment_mode === "no_payment") {
    return { paymentRequired: false, route: "provider_follow_up" };
  }
  if (contractMode === "lead_only") {
    return { paymentRequired: false, route: "assisted_flow" };
  }
  return { paymentRequired: true, route: "platform_checkout" };
}
