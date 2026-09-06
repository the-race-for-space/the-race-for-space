# Development To-Do

This file is a lightweight reminder list for current development ideas and polish work. It is not a release plan and items are not listed in priority order unless stated otherwise.

## Command Center and contract UI

- [ ] **Order Funding Targets by player completion state.** Show objectives the player has not completed at the top of the list, with player-completed objectives moved to the bottom so current work is easier to find.
- [x] **Add a compact Flight-mode objective tracker.** `FlightActiveUI` now provides a separate Flight-only Offered Contracts window with independent expand/collapse controls, live Pre-Orbit requirement state, and completed Offered contracts grouped at the bottom. Final in-game acceptance is covered by the current v0.5 smoke test.
- [ ] **Remove duplicate live telemetry presentation from Funding Targets.** Once FlightActiveUI is accepted in-game, remove the live Flight Contract requirement display from the main Command Center so the compact Flight window is the dedicated real-time presentation while `ModRuntime` remains the single telemetry source.
- [ ] **Make objective status text more natural.** Replace all-caps labels such as `PENDING`, `OUT OF BAND`, and similar status text with normal readable wording and casing so the interface feels less mechanical and more consistent with the rest of the mod.
