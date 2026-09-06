# Development To-Do

This file is a lightweight reminder list for current development ideas and polish work. It is not a release plan and items are not listed in priority order unless stated otherwise.

## Command Center and contract UI

- [ ] **Order Funding Targets by player completion state.** Show objectives the player has not completed at the top of the list, with player-completed objectives moved to the bottom so current work is easier to find.
- [x] **Add a compact Flight-mode objective tracker.** `FlightActiveUI` now provides a separate Flight-only Offered Contracts window with independent expand/collapse controls, live Pre-Orbit requirement state, and completed Offered contracts grouped at the bottom. Final in-game acceptance is covered by the current v0.5 smoke test.
- [x] **Remove duplicate live telemetry presentation from Funding Targets.** Funding Targets now stays focused on funding and contract-lifecycle information. `FlightActiveUI` is the dedicated live Flight Contract presentation while `ModRuntime` remains the single telemetry source.
- [x] **Make Flight objective status text more natural.** `FlightActiveUI` now uses normal readable casing for live states such as `Pending`, `Met`, `Valid`, `Invalid`, `In band`, and `Out of band` instead of all-caps status text.
- [x] **Notify the player when an Offered objective funding target is completed.** `FundingNotificationUI` now sends one green stock KSP message for a newly recorded player completion, ignores rival completions, and does not replay historical completions after save restoration.
