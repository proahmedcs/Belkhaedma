import { useMemo, useState } from "react";
import "./App.css";

type Language = "en" | "ar";
type Menu = "home" | "promotions" | "contracts" | "profile";
type Group = "cleaning" | "monthly" | "medical" | "mediation";

type PackageItem = {
  id: string;
  group: Group;
  service: string;
  serviceAr: string;
  provider: string;
  providerAr: string;
  nationality: string;
  weeklyVisits: number;
  hours: number;
  finalPrice: number;
  listPrice: number;
};

type ProfileTab = "preferences" | "notifications" | "support";

const PACKAGES: PackageItem[] = [
  {
    id: "p1",
    group: "cleaning",
    service: "Cleaning Service",
    serviceAr: "خدمة التنظيف",
    provider: "Enaya",
    providerAr: "عناية",
    nationality: "Philippines",
    weeklyVisits: 1,
    hours: 4,
    finalPrice: 84,
    listPrice: 120,
  },
  {
    id: "p2",
    group: "cleaning",
    service: "Cleaning Service",
    serviceAr: "خدمة التنظيف",
    provider: "Fawran",
    providerAr: "فوران",
    nationality: "Indonesia",
    weeklyVisits: 2,
    hours: 4,
    finalPrice: 149.5,
    listPrice: 149.5,
  },
  {
    id: "p3",
    group: "cleaning",
    service: "Cleaning Service",
    serviceAr: "خدمة التنظيف",
    provider: "Tamkeen",
    providerAr: "تمكين",
    nationality: "Africa",
    weeklyVisits: 1,
    hours: 8,
    finalPrice: 165,
    listPrice: 210,
  },
];

function App() {
  const [language, setLanguage] = useState<Language>("en");
  const [menu, setMenu] = useState<Menu>("home");
  const [group, setGroup] = useState<Group>("cleaning");
  const [service, setService] = useState<string>("Cleaning Service");
  const [date, setDate] = useState("");

  // Wego-style filter: draft values + Apply
  const [showFilter, setShowFilter] = useState(false);
  const [draftNationality, setDraftNationality] = useState("all");
  const [draftWeeklyVisits, setDraftWeeklyVisits] = useState("all");
  const [draftHours, setDraftHours] = useState("all");
  const [draftSort, setDraftSort] = useState<"recommended" | "cheapest" | "highest">("recommended");
  const [applied, setApplied] = useState({
    nationality: "all",
    weeklyVisits: "all",
    hours: "all",
    sort: "recommended" as "recommended" | "cheapest" | "highest",
  });

  const [profileTab, setProfileTab] = useState<ProfileTab>("preferences");
  const isArabic = language === "ar";

  const list = useMemo(() => {
    const rows = PACKAGES.filter((p) => {
      if (p.group !== group) return false;
      if (service !== "all" && p.service !== service) return false;
      if (!date) return true;
      if (applied.nationality !== "all" && p.nationality !== applied.nationality) return false;
      if (applied.weeklyVisits !== "all" && String(p.weeklyVisits) !== applied.weeklyVisits) return false;
      if (applied.hours !== "all" && String(p.hours) !== applied.hours) return false;
      return true;
    });

    if (applied.sort === "cheapest") {
      return rows.slice().sort((a, b) => a.finalPrice - b.finalPrice);
    }
    if (applied.sort === "highest") {
      return rows.slice().sort((a, b) => b.finalPrice - a.finalPrice);
    }
    return rows;
  }, [applied.hours, applied.nationality, applied.sort, applied.weeklyVisits, date, group, service]);

  const applyFilter = () => {
    setApplied({
      nationality: draftNationality,
      weeklyVisits: draftWeeklyVisits,
      hours: draftHours,
      sort: draftSort,
    });
    setShowFilter(false);
  };

  const resetFilter = () => {
    setDraftNationality("all");
    setDraftWeeklyVisits("all");
    setDraftHours("all");
    setDraftSort("recommended");
    setApplied({
      nationality: "all",
      weeklyVisits: "all",
      hours: "all",
      sort: "recommended",
    });
  };

  const activeFilterCount = [
    applied.nationality !== "all",
    applied.weeklyVisits !== "all",
    applied.hours !== "all",
    applied.sort !== "recommended",
  ].filter(Boolean).length;

  const renderProfileContent = () => {
    if (profileTab === "preferences") {
      return (
        <div className="profile-block">
          <h3>{isArabic ? "التفضيلات" : "Preferences"}</h3>
          <p className="hint">{isArabic ? "اللغة واتجاه الشاشة والإشعارات." : "Language, direction, and notification preferences."}</p>
          <div className="row">
            <label>{isArabic ? "اللغة" : "Language"}</label>
            <div className="lang">
              <button className={language === "en" ? "active" : ""} onClick={() => setLanguage("en")}>
                EN
              </button>
              <button className={language === "ar" ? "active" : ""} onClick={() => setLanguage("ar")}>
                AR
              </button>
            </div>
          </div>
        </div>
      );
    }
    if (profileTab === "notifications") {
      return (
        <div className="profile-block">
          <h3>{isArabic ? "الإشعارات" : "Notifications"}</h3>
          <p className="hint">{isArabic ? "تنبيهات العروض والطلبات والمواعيد." : "Alerts for offers, requests, and schedules."}</p>
        </div>
      );
    }
    return (
      <div className="profile-block">
        <h3>{isArabic ? "الدعم" : "Support"}</h3>
        <p className="hint">{isArabic ? "مركز المساعدة والتواصل." : "Help center and contact options."}</p>
      </div>
    );
  };

  return (
    <div className={`app ${isArabic ? "rtl" : "ltr"}`}>
      <header className="top">
        <h1>Belkhidmah - WebUISteps</h1>
        <div className="lang">
          <button className={language === "en" ? "active" : ""} onClick={() => setLanguage("en")}>
            EN
          </button>
          <button className={language === "ar" ? "active" : ""} onClick={() => setLanguage("ar")}>
            AR
          </button>
        </div>
      </header>

      {menu === "home" ? (
        <main className="content">
          <section className="card">
            <h2>{isArabic ? "اختر مجموعة الخدمة" : "Select Service Group"}</h2>
            <div className="chips">
              <button className={group === "cleaning" ? "active" : ""} onClick={() => setGroup("cleaning")}>
                {isArabic ? "خدمات التنظيف" : "Cleaning Services"}
              </button>
              <button className={group === "monthly" ? "active" : ""} onClick={() => setGroup("monthly")}>
                {isArabic ? "شهرية" : "Monthly"}
              </button>
              <button className={group === "medical" ? "active" : ""} onClick={() => setGroup("medical")}>
                {isArabic ? "طبية" : "Medical"}
              </button>
              <button className={group === "mediation" ? "active" : ""} onClick={() => setGroup("mediation")}>
                {isArabic ? "وساطة" : "Mediation"}
              </button>
            </div>
          </section>

          <section className="card">
            <h2>{isArabic ? "الخدمة المختارة" : "Selected Service"}</h2>
            <div className="row">
              <label>{isArabic ? "الخدمة" : "Service"}</label>
              <select value={service} onChange={(e) => setService(e.target.value)}>
                <option value="Cleaning Service">{isArabic ? "خدمة التنظيف" : "Cleaning Service"}</option>
              </select>
            </div>
            <div className="row">
              <label>{isArabic ? "تاريخ الخدمة" : "Service Date"}</label>
              <input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
            </div>
            <p className="hint">
              {isArabic
                ? "بعد اختيار مجموعة الخدمة + خدمة التنظيف، سترى كل الباقات في نفس الصفحة، ثم تطبق الفلتر."
                : "After selecting service group + Cleaning Service, all packages appear on the same page, then apply filters."}
            </p>
          </section>

          <section className="card">
            <div className="row between">
              <h2>{isArabic ? "كل الباقات (Wego Style)" : "All Packages (Wego Style)"}</h2>
              <button className="filterBtn" onClick={() => setShowFilter((v) => !v)}>
                {isArabic ? `فلتر${activeFilterCount ? ` (${activeFilterCount})` : ""}` : `Filter${activeFilterCount ? ` (${activeFilterCount})` : ""}`}
              </button>
            </div>

            {showFilter ? (
              <div className="filterPanel">
                <div className="row">
                  <label>{isArabic ? "الترتيب" : "Sort"}</label>
                  <select value={draftSort} onChange={(e) => setDraftSort(e.target.value as "recommended" | "cheapest" | "highest")}>
                    <option value="recommended">{isArabic ? "موصى به" : "Recommended"}</option>
                    <option value="cheapest">{isArabic ? "الأرخص" : "Cheapest"}</option>
                    <option value="highest">{isArabic ? "الأعلى سعراً" : "Highest Price"}</option>
                  </select>
                </div>
                <div className="row">
                  <label>{isArabic ? "الجنسية" : "Nationality"}</label>
                  <select value={draftNationality} onChange={(e) => setDraftNationality(e.target.value)}>
                    <option value="all">{isArabic ? "الكل" : "All"}</option>
                    <option value="Philippines">Philippines</option>
                    <option value="Indonesia">Indonesia</option>
                    <option value="Africa">Africa</option>
                  </select>
                </div>
                <div className="row">
                  <label>{isArabic ? "عدد الزيارات" : "Weekly Visits"}</label>
                  <select value={draftWeeklyVisits} onChange={(e) => setDraftWeeklyVisits(e.target.value)}>
                    <option value="all">{isArabic ? "الكل" : "All"}</option>
                    <option value="1">1</option>
                    <option value="2">2</option>
                  </select>
                </div>
                <div className="row">
                  <label>{isArabic ? "عدد الساعات" : "Hours"}</label>
                  <select value={draftHours} onChange={(e) => setDraftHours(e.target.value)}>
                    <option value="all">{isArabic ? "الكل" : "All"}</option>
                    <option value="4">4</option>
                    <option value="8">8</option>
                  </select>
                </div>
                <div className="actions">
                  <button onClick={resetFilter}>{isArabic ? "إعادة ضبط" : "Reset"}</button>
                  <button className="primary" onClick={applyFilter}>
                    {isArabic ? "تطبيق الفلتر" : "Apply Filter"}
                  </button>
                </div>
              </div>
            ) : null}

            <div className="packages">
              {list.map((pkg) => (
                <article key={pkg.id} className="package">
                  <h3>{isArabic ? pkg.serviceAr : pkg.service}</h3>
                  <p>{isArabic ? pkg.providerAr : pkg.provider}</p>
                  <p>
                    {isArabic ? "الجنسية" : "Nationality"}: {pkg.nationality}
                  </p>
                  <p>
                    {isArabic ? "الزيارات الأسبوعية" : "Weekly Visits"}: {pkg.weeklyVisits}
                  </p>
                  <p>
                    {isArabic ? "الساعات" : "Hours"}: {pkg.hours}
                  </p>
                  <p className="price">{pkg.finalPrice} SAR</p>
                  <p className="old">{pkg.listPrice} SAR</p>
                </article>
              ))}
              {list.length === 0 ? (
                <p className="hint">{isArabic ? "لا توجد باقات مطابقة." : "No matching packages found."}</p>
              ) : null}
            </div>
          </section>
        </main>
      ) : null}

      {menu === "profile" ? (
        <main className="content">
          <section className="card">
            <h2>{isArabic ? "الملف الشخصي" : "Profile"}</h2>
            <div className="chips">
              <button className={profileTab === "preferences" ? "active" : ""} onClick={() => setProfileTab("preferences")}>
                {isArabic ? "التفضيلات" : "Preferences"}
              </button>
              <button className={profileTab === "notifications" ? "active" : ""} onClick={() => setProfileTab("notifications")}>
                {isArabic ? "الإشعارات" : "Notifications"}
              </button>
              <button className={profileTab === "support" ? "active" : ""} onClick={() => setProfileTab("support")}>
                {isArabic ? "الدعم" : "Support"}
              </button>
            </div>
            {renderProfileContent()}
          </section>
        </main>
      ) : null}

      {menu !== "home" && menu !== "profile" ? (
        <main className="content">
          <section className="card">
            <h2>{menu === "promotions" ? (isArabic ? "العروض" : "Promotions") : isArabic ? "العقود" : "Contracts"}</h2>
            <p className="hint">
              {isArabic
                ? "هذه شاشة UX/UI مستقلة بدون باك إند. سنضيف تفاصيلها بعد تثبيت تدفق التنظيف."
                : "This is a standalone UX/UI screen without backend. We will detail it after locking cleaning flow."}
            </p>
          </section>
        </main>
      ) : null}

      <nav className="bottom">
        <button className={menu === "home" ? "active" : ""} onClick={() => setMenu("home")}>
          {isArabic ? "الرئيسية" : "Home"}
        </button>
        <button className={menu === "promotions" ? "active" : ""} onClick={() => setMenu("promotions")}>
          {isArabic ? "العروض" : "Promotions"}
        </button>
        <button className={menu === "contracts" ? "active" : ""} onClick={() => setMenu("contracts")}>
          {isArabic ? "العقود" : "Contracts"}
        </button>
        <button className={menu === "profile" ? "active" : ""} onClick={() => setMenu("profile")}>
          {isArabic ? "الملف" : "Profile"}
        </button>
      </nav>
    </div>
  );
}

export default App;
