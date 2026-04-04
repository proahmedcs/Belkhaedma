window.Belkhedma = window.Belkhedma || {};

window.Belkhedma.normalizer = {
  normalizeProvider(raw) {
    return {
      id: raw.id,
      code: raw.code,
      nameAr: raw.nameAr,
      nameEn: raw.nameEn,
      tier: raw.tier,
      confidence: raw.confidence,
      integrationMode: raw.integrationMode,
      capabilities: raw.capabilities || {},
      logoUrl: raw.logoUrl || null,
      websiteUrl: raw.websiteUrl || null,
      appUrl: raw.appUrl || null,
      tinyUrl: raw.tinyUrl || null
    };
  }
};
