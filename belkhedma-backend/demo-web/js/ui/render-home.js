import { currentLang, i18n } from "./render-common.js";

export function renderHomeCards($container, providers) {
  $container.empty();
  providers.forEach((provider) => {
    const name = currentLang() === "ar" ? provider.name_ar : provider.name_en;
    $container.append(`
      <article class="provider-card">
        <div class="provider-title">${name}</div>
        <div class="provider-meta">Tier ${provider.tier} · ${provider.integration_mode}</div>
        <div class="provider-meta">${provider.provider_type}</div>
      </article>
    `);
  });
}

export function setHomeLabels() {
  $("#providersTitle").text(i18n("providers"));
  $("#pricesTitle").text(i18n("results"));
  $("#thProvider").text(i18n("provider"));
  $("#thMode").text(i18n("mode"));
  $("#thPrice").text(i18n("finalPrice"));
  $("#thOriginal").text(i18n("beforeDiscount"));
  $("#thStatus").text(i18n("status"));
}
