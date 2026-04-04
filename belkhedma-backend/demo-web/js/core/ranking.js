window.DemoRanking = (function () {
  function scoreProvider(provider, result) {
    const confidenceScore = {
      high: 30,
      medium: 18,
      conservative: 10,
    }[provider.confidence || "medium"] || 0;

    const availabilityScore = result.availability_status === "available" ? 25 : 10;
    const priceScore = Math.max(0, 40 - Number(result.final_price_sar || 0) / 10);

    return Math.round(confidenceScore + availabilityScore + priceScore);
  }

  function rank(providers, resultsByProviderId) {
    return providers
      .map((p) => ({
        provider: p,
        score: scoreProvider(p, resultsByProviderId[p.provider_id] || {}),
      }))
      .sort((a, b) => b.score - a.score);
  }

  return {
    rank,
  };
})();
