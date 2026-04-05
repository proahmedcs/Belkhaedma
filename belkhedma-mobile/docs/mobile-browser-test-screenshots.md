# Mobile Browser Test Screenshots

These screenshots were captured in a browser using an iPhone viewport profile.

## Scenario

- Test date: 2026-04-04
- Main-screen UX alignment: Service Group first (rich cards), then Service list
- Current cycle: Service Group -> Service -> Location -> Featured Packages -> Results
- Group: Hourly Services
- Service: Hourly Services 4 Hours
- Date: 2026-04-06
- Location: demo-customer saved location
- API base URL used by Expo web: `http://127.0.0.1:5276`
- Web app URL: `http://127.0.0.1:8089`
- UX enhancement verified: step 1 now renders Enaya-style rich Service Group cards (title + description + visual icon), then step 2 shows Service choices.
- Final action in current UI: `Refresh` on Results step (provider filter is optional inside results)
- Logo verification: provider cards now always render a visible logo image via multi-source fallback (`logoUrl` -> known domain clearbit -> host-derived clearbit -> ui-avatars -> inline SVG data URI).

## Screenshots

1. Step 1 - Group selection  
   `/opt/cursor/artifacts/screenshots/mobile-01-step1-group.png`
2. Step 2 - Sub-service selection  
   `/opt/cursor/artifacts/screenshots/mobile-02-step2-subservice.png`
3. Step 3 - Details entry  
   `/opt/cursor/artifacts/screenshots/mobile-03-step3-details.png`
4. Step 4 - Location selection  
   `/opt/cursor/artifacts/screenshots/mobile-04-step4-location.png`
5. Step 5 - Before search  
   `/opt/cursor/artifacts/screenshots/mobile-05-step5-before-search.png`
6. Step 5 - After search results  
   `/opt/cursor/artifacts/screenshots/mobile-06-step5-after-search-results.png`
