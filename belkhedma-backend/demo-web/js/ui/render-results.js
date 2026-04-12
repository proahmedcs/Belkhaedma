import { byLang } from "./render-common.js";

export function renderResultsTable(targetSelector, rows, lang) {
  const $tbody = $(targetSelector);
  $tbody.empty();

  rows.forEach((row) => {
    const status = row.status === "instant" ? byLang(lang, "فوري", "Instant") : byLang(lang, "متأخر", "Delayed");
    $tbody.append(`
      <tr>
        <td>${row.providerName}</td>
        <td>${row.mode}</td>
        <td>${row.finalPriceSar} SAR</td>
        <td>${row.originalPriceSar} SAR</td>
        <td>${status}</td>
      </tr>
    `);
  });
}
