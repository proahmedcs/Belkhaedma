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
  const [group, setGroup] = useState<Group | null>(null);
  const [service, setService] = useState<string>("Cleaning Service");
  const [date, setDate] = useState("");
  const [nationality, setNationality] = useState<string>("all");
  const [weeklyVisits, setWeeklyVisits] = useState<string>("all");
  const [hours, setHours] = useState<string>("all");
  const [showFilter, setShowFilter] = useState(false);
  const [applied, setApplied] = useState({
    nationality: "all",
    weeklyVisits: "all",
    hours: "all",
  });

  const isArabic = language === "ar";

  const list = useMemo(() => {
    return PACKAGES.filter((p) => {
      if (group && p.group !== group) return false;
      if (service !== "all" && p.service !== service) return false;
      if (!date) return true;
      if (applied.nationality !== "all" && p.nationality !== applied.nationality) return false;
      if (applied.weeklyVisits !== "all" && String(p.weeklyVisits) !== applied.weeklyVisits) return false;
      if (applied.hours !== "all" && String(p.hours) !== applied.hours) return false;
      return true;
    }).sort((a, b) => a.finalPrice - b.finalPrice);
  }, [applied.hours, applied.nationality, applied.weeklyVisits, date, group, service]);

  const applyFilter = () => {
    setApplied({ nationality, weeklyVisits, hours });
    setShowFilter(false);
  };

  const resetFilter = () => {
    setNationality("all");
    setWeeklyVisits("all");
    setHours("all");
    setApplied({ nationality: "all", weeklyVisits: "all", hours: "all" });
  };

  return (
    <div className={`app ${isArabic ? "rtl" : "ltr"}`}>
      <header className="top">
        <h1>Belkhidmah - Web UI Steps</h1>
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
                ? "بعد اختيار التاريخ، سترى كل الباقات في نفس الصفحة ويمكنك تطبيق الفلتر."
                : "After choosing date, all packages appear on this same page and you can apply filters."}
            </p>
          </section>

          <section className="card">
            <div className="row between">
              <h2>{isArabic ? "كل الباقات" : "All Packages"}</h2>
              <button className="filterBtn" onClick={() => setShowFilter((v) => !v)}>
                {isArabic ? "فلتر" : "Filter"}
              </button>
            </div>

            {showFilter ? (
              <div className="filterPanel">
                <div className="row">
                  <label>{isArabic ? "الجنسية" : "Nationality"}</label>
                  <select value={nationality} onChange={(e) => setNationality(e.target.value)}>
                    <option value="all">{isArabic ? "الكل" : "All"}</option>
                    <option value="Philippines">Philippines</option>
                    <option value="Indonesia">Indonesia</option>
                    <option value="Africa">Africa</option>
                  </select>
                </div>
                <div className="row">
                  <label>{isArabic ? "عدد الزيارات" : "Weekly Visits"}</label>
                  <select value={weeklyVisits} onChange={(e) => setWeeklyVisits(e.target.value)}>
                    <option value="all">{isArabic ? "الكل" : "All"}</option>
                    <option value="1">1</option>
                    <option value="2">2</option>
                  </select>
                </div>
                <div className="row">
                  <label>{isArabic ? "عدد الساعات" : "Hours"}</label>
                  <select value={hours} onChange={(e) => setHours(e.target.value)}>
                    <option value="all">{isArabic ? "الكل" : "All"}</option>
                    <option value="4">4</option>
                    <option value="8">8</option>
                  </select>
                </div>
                <div className="actions">
                  <button onClick={resetFilter}>{isArabic ? "إعادة ضبط" : "Reset"}</button>
                  <button className="primary" onClick={applyFilter}>
                    {isArabic ? "تطبيق" : "Apply"}
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
      ) : (
        <main className="content">
          <section className="card">
            <h2>{menu === "profile" ? (isArabic ? "الملف والإعدادات" : "Profile & Settings") : menu}</h2>
            <p className="hint">
              {isArabic
                ? "هذه شاشة تدفق واجهة فقط (بدون باك إند) لتثبيت خطوات UX/UI."
                : "This is UI flow-only prototype (no backend) to lock UX/UI steps."}
            </p>
          </section>
        </main>
      )}

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
