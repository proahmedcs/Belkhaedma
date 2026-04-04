function applyTopFilters(rows, filters) {
  return rows.filter((row) => {
    if (filters.maxPrice && row.finalPrice > filters.maxPrice) return false;
    if (filters.integrationMode && row.integrationMode !== filters.integrationMode) return false;
    return true;
  });
}
