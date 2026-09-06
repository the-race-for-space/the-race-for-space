# Development To-Do

This file is a lightweight reminder list for current development ideas and polish work. It is not a release plan and items are not listed in priority order unless stated otherwise.

## Command Center and contract UI

- [ ] **Order Funding Targets by player completion state.** Show objectives the player has not completed at the top of the list, with player-completed objectives moved to the bottom so current work is easier to find.
- [x] **Add a compact Flight-mode objective tracker.** `FlightActiveUI` now provides a separate Flight-only Offered Contracts window with independent expand/collapse controls, live Pre-Orbit requirement state, and completed Offered contracts grouped at the bottom. Final in-game acceptance is covered by the current v0.5 smoke test.
- [x] **Remove duplicate live telemetry presentation from Funding Targets.** Funding Targets now stays focused on funding and contract-lifecycle information. `FlightActiveUI` is the dedicated live Flight Contract presentation while `ModRuntime` remains the single telemetry source.
- [x] **Make Flight objective status text more natural.** `FlightActiveUI` now uses normal readable casing for live states such as `Pending`, `Met`, `Valid`, `Invalid`, `In band`, and `Out of band` instead of all-caps status text.
- [x] **Notify the player when an Offered objective funding target is completed.** `FundingNotificationUI` now sends one green stock KSP message for a newly recorded player completion, ignores rival completions, and does not replay historical completions after save restoration.

## Persistent Flight Attempts

- [x] **Step 1 - separate Flight Attempt state from evaluation.** `FlightAttemptState` now owns the mutable state for the single attempt currently evaluated by `FlightContractTracker`. This is an internal refactor only: vessel switching, staging continuity, contract rules, and the existing single-attempt persistence format are unchanged.
- [x] **Step 2 - retain multiple attempts in memory.** `FlightContractTracker` now keeps independent remembered attempt state for unrelated vessel IDs during the current session, so switching A -> B -> A restores A's unfinished maxima and other attempt history. The existing same-launch staging fallback is retained temporarily, and save/load still persists only the currently active attempt until Step 6.
- [ ] **Step 3 - add stable craft-lineage identity.** Pass persistent part-lineage identifiers through the KSP integration boundary so attempts are not tied to temporary KSP vessel IDs.
- [ ] **Step 4 - reconcile staging and vessel switching by lineage.** Preserve one attempt on the correct continuing branch without cloning historical progress.
- [ ] **Step 5 - handle docking and undocking.** Keep separate attempt histories when craft combine and restore the correct histories when they separate.
- [ ] **Step 6 - replace single-attempt persistence.** Store repeated Flight Attempt records under `FLIGHT_CONTRACT_PROGRESS`. Previous build compatibility is not required for this planned format change.
- [ ] **Step 7 - define contract-specific topology rules.** Prevent progress from unrelated docked craft being combined, with particular care for Mass and unfinished continuous Control holds.
- [ ] **Step 8 - prune obsolete attempt state.** Remove dead/recovered attempt records when their historical tracking state is no longer useful.
- [ ] **Step 9 - adapt presentation and complete regression coverage.** Show the correct active attempt in `FlightActiveUI` and cover switching, save/load, staging, docking, undocking, recovery, destruction, and constructed craft.
