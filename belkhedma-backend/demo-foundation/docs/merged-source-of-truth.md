# Belkhedma Demo Source of Truth (Merged)

This document aligns implementation with **Blueprint V2** and defines the demo-first sequence:

`data -> simulation -> routes -> screens -> state machine -> admin controls`

## Demo Phase Rule

1. Build simulation foundation first.
2. Build HTML/jQuery demo second.
3. Replace simulated behaviors with real integrations later.

## Data Backbone Files

Required bootstrap files (demo truth source):

- `demo-foundation/data/platform_seed_package.json`
- `demo-foundation/data/provider_simulation_contracts.json`
- `demo-foundation/data/enaya_flow_extracted.json`
- `demo-foundation/data/frontend_manifest.json`

Additional provider evidence:

- `demo-foundation/data/enaya_fawran_real_json_bundle.json`
- `demo-foundation/data/fawran_public_api_probe.json`
- `demo-foundation/data/fawran_monthly_real_json_bundle.json`

## Provider Tiers

- Tier A: Enaya, Tamkeen HR, Almutahidah
- Tier B: Mueen, Esad Talents, Emdad/Fawran
- Tier C: IRC Saudi, Eitinaa

## Mandatory Journeys (Demo)

1. Individual hourly booking.
2. Individual resident/monthly request.
3. Business RFQ.
4. Fallback and rescue scenarios.

## Current Implementation Status

- Backend provider table and configuration model: implemented.
- Provider links, app links, tiny URLs, logos: implemented.
- Pricing with expiration filtering: implemented.
- JSON bundle storage in DB: implemented.
- Mobile EN/AR provider display with logos and two prices: implemented.
- HTML/jQuery demo scaffold wired to demo truth files: implemented in `demo-web`.

