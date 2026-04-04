window.ContractFlow = {
  buildContractDraft(selectedOffer) {
    if (!selectedOffer) {
      return null;
    }
    return {
      contractType: "hourly_household_contract",
      providerId: selectedOffer.providerId,
      finalPrice: selectedOffer.finalPrice,
      createdAtUtc: new Date().toISOString(),
    };
  },
};
