# Provider JSON Bundles

This folder stores source JSON bundles provided by business for bootstrapping provider scenarios.

Files:
- `fawran_public_api_probe.json`
- `enaya_fawran_real_json_bundle.json`
- `fawran_monthly_real_json_bundle.json`

The backend seeder reads these files and persists them into the `ProviderJsonDocuments` table with expiration handling (`ExpiresAtUtc`).
