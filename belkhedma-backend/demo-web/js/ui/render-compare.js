import { getLang, t } from "./render-common.js";

export function renderCompare($mount, rows) {
  const lang = getLang();
  const title = lang === "ar" ? "مقارنة المزودين" : "Provider Comparison";
  const headProvider = lang === "ar" ? "المزود" : "Provider";
  const headPrice = lang === "ar" ? "السعر" : "Price";
  const headConf = lang === "ar" ? "الثقة" : "Confidence";
  const headMode = lang === "ar" ? "النمط" : "Mode";

  const html = `
    <section class="panel">
      <h2>${title}</h2>
      <table class="table">
        <thead>
          <tr>
            <th>${headProvider}</th>
            <th>${headMode}</th>
            <th>${headPrice}</th>
            <th>${headConf}</th>
          </tr>
        </thead>
        <tbody>
          ${rows
            .map(
              (x) => `
            <tr>
              <td>${x.displayName}</td>
              <td>${x.integrationMode}</td>
              <td>${x.finalPrice} ${t.sar}</td>
              <td>${x.confidenceLabel}</td>
            </tr>
          `
            )
            .join("")}
        </tbody>
      </table>
    </section>
  `;

  $mount.html(html);
}

