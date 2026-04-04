window.BelkhedmaDemo = window.BelkhedmaDemo || {};

window.BelkhedmaDemo.MueenProvider = class extends window.BelkhedmaDemo.BaseProviderAdapter {
  constructor(provider) {
    super(provider);
    this.providerId = "mueen";
  }
};
