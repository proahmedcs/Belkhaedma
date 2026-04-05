# Full Cycle QA (Mobile + Backend Admin)

This document explains how to run the full-cycle UI verification for both:

- Mobile web app flow
- Backend admin flow (including crawler jobs page)

## Script

The automation script is:

- `scripts/cycle_full_test_v2.js`

It validates:

1. Mobile home opens and bottom menu is visible.
2. Service cycle flow (Home -> Services -> Location -> Packages/Results).
3. Filter panel opens and Apply action is available.
4. Package click opens review screen.
5. Profile screen opens and language toggle (EN/AR) is available.
6. Admin dashboard opens.
7. Admin jobs page opens.
8. Per-provider **Start Crawler Job** button exists and can be triggered.
9. **Start All Providers** and **Run Daily Crawler Job** actions trigger.

## Run command

```bash
cd belkhedma-mobile
npm run qa:cycle
```

## Output artifacts

The script writes screenshots + report to:

- `/workspace/artifacts/screenshots/cycle-2026-04-06-v2/`

Key output:

- `cycle-findings.json` (`passed: true/false` and findings list)
- `mobile-*.png`
- `admin-*.png`

## Notes

- The script expects mobile and backend URLs:
  - Mobile: `http://127.0.0.1:8091/`
  - Admin: `http://127.0.0.1:5278/admin/`
- If your environment uses different ports, update constants at top of `scripts/cycle_full_test_v2.js`.
