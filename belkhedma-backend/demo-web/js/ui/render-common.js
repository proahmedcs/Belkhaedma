window.BelkhedmaRenderCommon = (() => {
  function localizedName(item, language) {
    return language === "ar" ? item.nameAr : item.nameEn;
  }

  function providerBadge(provider) {
    return `<span class="badge badge-tier">Tier ${provider.tier}</span>`;
  }

  function confidenceBadge(provider) {
    const confidence = provider.confidence || "medium";
    return `<span class="badge badge-confidence">${confidence}</span>`;
  }

  return {
    localizedName,
    providerBadge,
    confidenceBadge,
  };
})();
