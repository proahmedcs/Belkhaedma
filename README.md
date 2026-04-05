# Belkhedma Workspace

This repository contains two isolated projects that can be split into separate repositories:

- `belkhedma-backend` → ASP.NET Core backend (SQL Server + Hangfire)
- `belkhedma-mobile` → Expo React Native mobile app

Both apps are designed to use a **single shared data source**:

- The backend writes/reads SQL Server.
- The mobile app reads data only through backend APIs.

## Backend quick start

```bash
cd belkhedma-backend
dotnet restore
dotnet run --project src/Belkhedma.Api
```

Important endpoints:

- Swagger: `https://localhost:7214/swagger` (or the HTTP port from launch settings)
- Hangfire Dashboard: `https://localhost:7214/hangfire`
- Providers API: `/api/marketplace/providers`
- Latest prices API: `/api/marketplace/prices/latest`

Configuration:

- Connection string key: `ConnectionStrings:BelkhedmaSharedDb`
- Default local value is in:
  - `src/Belkhedma.Api/appsettings.json`
  - `src/Belkhedma.Api/appsettings.Development.json`

## Mobile quick start

```bash
cd belkhedma-mobile
npm install
npm run start
```

## Desktop quick links (mobile web + backend admin)

If services are already running on the standard desktop ports used in this workspace:

- Mobile web app: `http://127.0.0.1:8090`
- Backend admin dashboard: `http://127.0.0.1:5278/admin/`
- Backend admin (home promotions): `http://127.0.0.1:5278/admin/home-promotions.html`

You can print both URLs quickly with:

```bash
./open_desktop_urls.sh
```

Set the backend URL in:

- `src/config/api.ts` via `EXPO_PUBLIC_API_BASE_URL`
  - Example Android emulator: `http://10.0.2.2:5275`
  - Example iOS simulator/local web: `http://localhost:5275`

## Branding

Brand tokens are defined in:

- `belkhedma-mobile/src/theme/brand.ts`

These tokens were initialized from the selected XD option colors and can be refined when final logo assets are provided.
