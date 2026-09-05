# Version 0.4 Alpha Scope - Historical Design Record

> **Historical document.** Version 0.4 was the cleanup and architecture-consolidation phase before the current Pre-Orbit work. This summary uses current terminology where possible so it remains easy to compare with the 0.5 codebase.

## Purpose

Version 0.4 started from the working 0.3 campaign and focused on making the code easier to extend safely.

The main goals were:

- move gameplay ownership out of the UI;
- put raw KSP vessel access behind a clear integration boundary;
- make rival and funding state collection-driven instead of hardcoded to a few named bodies/agencies;
- centralise objective and funding definitions;
- add flexible unlock rules;
- strengthen persistence and automated regression coverage;
- remove compatibility APIs that no longer matched the direction of the prototype.

The first 0.4 branch was:

```text
Alpha/Cleanup-0.4
```

## 0.4 campaign baseline

The 0.4 campaign included:

- Probe Orbit and Crewed Orbit around Kerbin;
- Probe Orbit and Crewed Orbit around Mun, Minmus, and Duna;
- satellite-network funding around Kerbin, Mun, Minmus, and Duna;
- player, Aster, and Cobalt agency state;
- shared 90 Kerbin-day funding dates;
- declining one-off objective funding;
- simulated rival missions;
- persistent campaign, funding, objective, satellite, and rival state;
- one Command Center with Overview, Funding Targets, Rival Agencies, and a progression view.

The current twenty Pre-Orbit contracts were not part of the original 0.4 baseline.

## 1. Runtime ownership moved out of UI

A major 0.4 change was making campaign progression independent of whether the Command Center was open.

The responsibility now represented by `ModRuntime` became the owner of the live campaign controller and its refresh cadence.

The UI became presentation-only.

This established the rule that still applies today:

> Gameplay continues even when the Command Center is hidden.

## 2. KSP vessel discovery became an integration responsibility

Raw KSP vessel discovery was moved out of gameplay tracking.

The modern form of that boundary is:

```text
KspIntegration/KspVesselMonitor
    |
    v
project-owned snapshots
    |
    v
Tracking/
```

Loaded vessels use live KSP state. Unloaded vessels use persistent KSP state.

The tracking layer receives normalized project-owned data instead of direct KSP `Vessel` or `ProtoVessel` objects.

This remains one of the main architectural boundaries in 0.5.

## 3. Satellite counts stopped depending on the objective catalogue

Version 0.4 separated satellite presence from objective definitions.

The tracker could represent qualifying satellites around any observed celestial body, even if the current objective catalogue did not yet contain an objective for that body.

This removed an important hidden hardcoding limitation.

The modern `OrbitalVesselTracker` still follows this principle.

## 4. Rival persistence became collection-driven

Rival save data moved away from fixed slots for specific rivals.

The current idea is:

- save each rival by stable agency ID;
- store only mutable campaign state;
- do not rely on list order or display name for identity;
- allow additional rivals without needing a new save field for each one.

The modern persistence type is `RivalAgenciesSaveState`.

This was an intentional compatibility break during prototype development.

## 5. Funding definitions moved into a catalogue

Funding target construction moved out of the main campaign controller.

The modern equivalent is `FundingContractCatalogue`.

This established a useful ownership rule:

- objective definitions belong in `Objectives/`;
- funding-contract construction belongs in `Funding/`;
- campaign coordination belongs in `Campaign/`.

The controller should consume collections rather than manually construct every target.

## 6. Stable IDs became authoritative

Version 0.4 removed several older fixed-target and display-name compatibility paths.

Stable IDs became authoritative for:

- rival mission targets;
- objectives;
- funding contracts;
- agencies;
- persistence matching.

Display text is presentation only.

This rule remains central to the current design.

## 7. Controller regression tests and CI

Version 0.4 added a second KSP-independent test suite around the real campaign controller.

The current test layout is:

- `tests/TheRaceForSpace.Tests/` — domain/tracking/funding/rival/persistence logic;
- `tests/TheRaceForSpace.ControllerTests/` — `CampaignController` ordering and cross-module behaviour using test-only KSP boundary stubs.

Both are run by:

```bash
bash tools/run-logic-tests.sh
```

GitHub Actions runs the same script in `.github/workflows/logic-tests.yml`.

The real KSP assembly is still tested manually because KSP/Unity DLLs are not stored in the repository.

## 8. Funding and UI performance cleanup

Version 0.4 reduced unnecessary repeated calculations and allocations in frequent refresh/UI paths.

Important principles established here:

- calculate projected funding once per controlled campaign refresh where practical;
- reuse values for repeated UI queries;
- preserve exact historical funding-boundary calculations separately;
- avoid recurring allocations in hot IMGUI paths when a simple reusable buffer is enough;
- do not make performance changes so abstract that the code becomes harder to understand.

These changes were intended to preserve behaviour, not rebalance the campaign.

## 9. Flexible unlock rules

One of the most important 0.4 additions was a shared unlock-rule model.

The modern types are:

- `UnlockRuleDefinition`;
- `UnlockPathDefinition`;
- `UnlockConditionDefinition`;
- `UnlockRuleEvaluator`.

The rule structure supports:

- alternative paths (**OR**);
- multiple required conditions inside one path (**AND**);
- objective-completion conditions;
- agency scopes such as player, any rival, or any agency;
- required agency counts;
- universal-time conditions;
- collective satellite-count conditions.

A null rule means available from campaign start. Invalid rules fail closed.

Most importantly, one evaluator became authoritative for campaign availability, rival target selection, tracking, historical funding evaluation, and read-only UI progress.

This prevented different parts of the mod from interpreting unlock rules differently.

## 10. Progression view became more useful

The fourth Command Center view was turned into a real progression-information view.

It showed:

- current opportunities;
- upcoming locked targets;
- completed/historical targets;
- live unlock-condition progress.

That work later evolved into today's **Contract Catalogue** with explicit `Offered`, `Unlocked`, `Locked`, and `Expired` states.

## 11. Funding-boundary persistence improved

Version 0.4 began persisting the next shared funding time so a save created near a funding boundary would not skip or duplicate the expected funding cycle after reload.

The current save model keeps this in `CampaignFundingSaveState`.

The modern persistence rule is simple:

> Save mutable campaign timing that cannot be safely reconstructed without changing behaviour.

## 12. What 0.4 deliberately did not do

Version 0.4 was not intended to introduce a large new gameplay phase.

It deliberately avoided:

- a major rival-AI redesign;
- a general external rule/config framework;
- unnecessary new architectural layers;
- large target-count expansion before the ownership model was ready.

The purpose was to make later expansion safer.

## How 0.4 maps to the current 0.5 structure

| 0.4 design direction | Current 0.5 owner |
| --- | --- |
| Runtime progression outside UI | `Core/ModRuntime` |
| Campaign coordination | `Campaign/CampaignController` |
| Player/rival state | `Agencies/AgencyState` |
| Objective catalogue | `Objectives/ObjectiveCatalogue` |
| Shared unlock rules | `Objectives/UnlockRuleEvaluator` |
| Funding catalogue | `Funding/FundingContractCatalogue` |
| Rival simulation | `Rivals/RivalSimulation` |
| Loaded/unloaded vessel evaluation | `Tracking/OrbitalVesselTracker` |
| Raw KSP vessel access | `KspIntegration/KspVesselMonitor` |
| Campaign save bridge | `KspIntegration/ModPersistenceScenario` |
| UI presentation | `UI/CommandCenterWindow` |

## Why this matters for 0.5

The current Pre-Orbit implementation was able to reuse the 0.4 foundation rather than create a parallel game system.

Version 0.5 adds:

- a generic fast `FlightContractTracker` path for active-vessel contracts;
- twenty Pre-Orbit objectives;
- four converging progression lines;
- Level V -> Probe Orbit convergence;
- live Funding Targets telemetry;
- temporary active-flight persistence.

Those additions still use the same campaign, agency, objective, funding, rival, persistence, and UI ownership boundaries established during 0.4.
