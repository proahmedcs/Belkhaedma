window.BelkhedmaProviderBase = class BelkhedmaProviderBase {
  constructor(provider, mapping) {
    this.provider = provider;
    this.mapping = mapping;
  }

  getProviderId() {
    return this.provider.provider_id;
  }

  supports(serviceFamily) {
    return this.mapping.supported_service_families.includes(serviceFamily);
  }
};
