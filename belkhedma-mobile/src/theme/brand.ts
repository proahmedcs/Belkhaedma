export const Brand = {
  name: "Belkhedma",
  colors: {
    // Extracted from XD option #1 dominant accent range
    primary: "#D950A0",
    primaryDark: "#A0298E",
    primaryLight: "#F7DCEB",
    textPrimary: "#1D1D1F",
    textSecondary: "#6D6D72",
    background: "#F5F5F5",
    card: "#FFFFFF",
    border: "#EBEBEB",
    success: "#00A76F",
    warning: "#F59E0B",
    danger: "#EF4444",
  },
  spacing: {
    xs: 6,
    sm: 10,
    md: 16,
    lg: 24,
    xl: 32,
  },
  radius: {
    sm: 8,
    md: 14,
    lg: 20,
  },
} as const;

export type BrandTheme = typeof Brand;
