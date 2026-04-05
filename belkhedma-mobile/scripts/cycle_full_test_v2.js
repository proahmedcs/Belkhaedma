const { chromium } = require("playwright");
const fs = require("fs");
const path = require("path");

const OUT_DIR = process.env.CYCLE_QA_OUT_DIR || "/workspace/artifacts/screenshots/cycle-latest";
const MOBILE_URL = process.env.CYCLE_QA_MOBILE_URL || "http://127.0.0.1:8091/";
const ADMIN_URL = process.env.CYCLE_QA_ADMIN_URL || "http://127.0.0.1:5278/admin/";
const ADMIN_JOBS_URL = process.env.CYCLE_QA_ADMIN_JOBS_URL || "http://127.0.0.1:5278/admin/jobs.html";

if (!fs.existsSync(OUT_DIR)) fs.mkdirSync(OUT_DIR, { recursive: true });
const findings = [];

async function snap(page, name) {
  await page.screenshot({ path: path.join(OUT_DIR, name), fullPage: true });
}

function fail(message) { findings.push(message); }

async function tryClick(page, selectors) {
  for (const sel of selectors) {
    const locator = page.locator(sel).first();
    if (await locator.count()) {
      try {
        await locator.click();
        return true;
      } catch {}
    }
  }
  return false;
}

async function wait(page) {
  try { await page.waitForLoadState("networkidle", { timeout: 5000 }); } catch {}
  await page.waitForTimeout(900);
}

(async () => {
  const browser = await chromium.launch({ headless: true });

  // MOBILE
  const mctx = await browser.newContext({ viewport: { width: 1365, height: 900 } });
  const m = await mctx.newPage();
  await m.goto(MOBILE_URL, { waitUntil: "domcontentloaded", timeout: 120000 });
  await wait(m);
  await snap(m, "mobile-01-home.png");

  // Bottom menu visible
  const bottomVisible = await m.locator("text=Home").first().isVisible().catch(() => false);
  if (!bottomVisible) fail("Mobile: Bottom menu not visible on home.");

  // Ensure we are on Main tab
  await tryClick(m, ["text=Home", "text=الرئيسية"]);
  await wait(m);

  // Select hourly services group
  const selectedGroup = await tryClick(m, [
    "text=/Hourly Services/i",
    "text=/الخدمات بالساعة/i",
    "text=/Services/i"
  ]);
  if (!selectedGroup) fail("Mobile: Unable to select Hourly Services group.");
  await wait(m);
  await snap(m, "mobile-02-after-group.png");

  // Move to Services step using Next if needed
  await tryClick(m, ["role=button[name=/Next/i]", "role=button[name=/التالي/i]"]); // may no-op in locator syntax
  // fallback robust by text
  await tryClick(m, ["text=Next", "text=التالي"]);
  await wait(m);
  await snap(m, "mobile-03-services-step.png");

  const chooseServiceVisible = (await m.getByText(/Choose Service|اختر الخدمة/i).count()) > 0;
  if (!chooseServiceVisible) {
    // force step via search menu
    await tryClick(m, ["text=Search", "text=البحث"]);
    await wait(m);
  }

  // choose first chip-like item
  const chip = m.locator("[class*=chip], [style*='padding']").filter({ hasText: /Hours|Service|ساعات|خدمات/i }).first();
  if (await chip.count()) {
    try { await chip.click(); } catch {}
  } else {
    // fallback any visible service text
    await tryClick(m, ["text=/Hours/i", "text=/ساعات/i"]);
  }
  await wait(m);

  // browse services button
  const browseClicked = await tryClick(m, ["text=/Browse services/i", "text=/استعرض الخدمات/i"]);
  if (!browseClicked) {
    // fallback to Next
    await tryClick(m, ["text=Next", "text=التالي"]);
  }
  await wait(m);
  await snap(m, "mobile-04-location-step.png");

  const locationVisible = (await m.getByText(/Choose Delivery Address|اختر عنوان توصيل|Location Screen|شاشة الموقع/i).count()) > 0;
  if (!locationVisible) fail("Mobile: Location step not visible in cycle.");

  // advance to packages/results
  await tryClick(m, ["text=Next", "text=التالي"]);
  await wait(m);
  await snap(m, "mobile-05-packages-step.png");

  // filter button existence (global result card)
  const filterCount = await m.getByText(/Filter|فلتر/i).count();
  if (filterCount <= 0) fail("Mobile: Filter button missing.");
  else {
    await tryClick(m, ["text=Filter", "text=فلتر"]);
    await wait(m);
    await snap(m, "mobile-06-filter-open.png");
    const applyCount = await m.getByText(/Apply|تطبيق/i).count();
    if (applyCount <= 0) fail("Mobile: Filter apply button missing.");
    else {
      await tryClick(m, ["text=Apply Filter", "text=Apply", "text=تطبيق الفلتر", "text=تطبيق"]);
      await wait(m);
      await snap(m, "mobile-07-filter-applied.png");
    }
  }

  // open request review from any price card
  await tryClick(m, ["text=/SAR/i", "text=/Tap to review package/i", "text=/اضغط لمراجعة الباقة/i"]);
  await wait(m);
  await snap(m, "mobile-08-request-review.png");
  const reviewCount = await m.getByText(/Request Review|مراجعة الطلب/i).count();
  if (reviewCount <= 0) fail("Mobile: Request review screen missing after package click.");

  // profile + language toggle
  await tryClick(m, ["text=Profile", "text=الملف الشخصي"]);
  await wait(m);
  await snap(m, "mobile-09-profile-en.png");

  const langExists = (await m.getByText(/EN|AR/).count()) > 0;
  if (!langExists) fail("Mobile: Profile language switch (EN/AR) not visible.");
  else {
    await tryClick(m, ["text=AR", "text=EN"]);
    await wait(m);
    await snap(m, "mobile-10-profile-lang-toggle.png");
  }

  // ADMIN
  const actx = await browser.newContext({ viewport: { width: 1365, height: 900 } });
  const a = await actx.newPage();
  await a.goto(ADMIN_URL, { waitUntil: "domcontentloaded", timeout: 120000 });
  await wait(a);
  await snap(a, "admin-01-dashboard.png");

  if ((await a.getByText(/Dashboard/i).count()) <= 0) fail("Admin: Dashboard screen not visible.");

  await a.goto(ADMIN_JOBS_URL, { waitUntil: "domcontentloaded", timeout: 120000 });
  await wait(a);
  await snap(a, "admin-02-jobs.png");

  if ((await a.getByText(/Crawler Jobs|Jobs/i).count()) <= 0) fail("Admin: Jobs page title missing.");
  const startCount = await a.getByRole("button", { name: /Start Crawler Job/i }).count();
  if (startCount <= 0) fail("Admin: Start Crawler Job buttons missing per provider.");
  else {
    await a.getByRole("button", { name: /Start Crawler Job/i }).first().click();
    await wait(a);
    await snap(a, "admin-03-start-provider.png");
  }

  if ((await a.getByRole("button", { name: /Start All Providers/i }).count()) > 0) {
    await a.getByRole("button", { name: /Start All Providers/i }).first().click();
    await wait(a);
    await snap(a, "admin-04-start-all.png");
  }

  if ((await a.getByRole("button", { name: /Run Daily Crawler Job/i }).count()) > 0) {
    await a.getByRole("button", { name: /Run Daily Crawler Job/i }).first().click();
    await wait(a);
    await snap(a, "admin-05-run-daily.png");
  }

  const summary = {
    at: new Date().toISOString(),
    mobileUrl: MOBILE_URL,
    adminUrl: ADMIN_URL,
    findings,
    passed: findings.length === 0,
  };

  fs.writeFileSync(path.join(OUT_DIR, "cycle-findings.json"), JSON.stringify(summary, null, 2));
  console.log(JSON.stringify(summary, null, 2));
  await browser.close();
})();
