window.Belkhedma = window.Belkhedma || {};

window.Belkhedma.state = {
  locale: "ar",
  providers: [],
  services: [],
  providerMappings: [],
  mockResults: [],
};

window.Belkhedma.setLocale = function setLocale(locale) {
  window.Belkhedma.state.locale = locale === "en" ? "en" : "ar";
};
