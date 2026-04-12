window.BelkhedmaCore = window.BelkhedmaCore || {};

window.BelkhedmaCore.runOrchestration = function runOrchestration(state, providers, rawResults) {
  const providerAdapters = {
    mueen: window.BelkhedmaProviders?.MueenProvider,
    tamkeen: window.BelkhedmaProviders?.TamkeenProvider,
    enaya: window.BelkhedmaProviders?.EnayaProvider,
    "emdad-hr": window.BelkhedmaProviders?.EmdadProvider
  };

  const normalized = [];
  rawResults.forEach((result) => {
    const adapter = providerAdapters[result.provider_id];
    if (adapter && typeof adapter.adapt === "function") {
      normalized.push(adapter.adapt(result, state));
      return;
    }
    normalized.push(window.BelkhedmaCore.normalizeResult(result, state.language));
  });

  return window.BelkhedmaCore.rankResults(normalized);
};

