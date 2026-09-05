# The Race for Space - Simple Code Guide

> **Branch covered:** `Alpha/KerbalContracts-v0.5`
>
> This guide explains the project in simple language. It is written so that someone who does not program can still understand what each file is for.

---

## How to read this guide

The main reason for the **Code refs** column is to help find code that may be unused, left over from an older version, or only present as a future placeholder.

- **File lines** = total lines in the source file.
- **Member lines** = approximate size of the function/property itself. Comments above it are not counted.
- **Code refs** = direct use anywhere in repository C# code. Use in the **same file counts**. Test code also counts. The member's own declaration does not count.
- Documentation and config files do **not** count as code references.
- **0** = no C# use was found anywhere in the repository. This is the strongest signal for possible dead code.
- **1+** = at least one C# use exists somewhere in production code or tests.
- **KSP/Unity** = KSP or Unity calls this member automatically. A normal C# call does not need to exist.
- **CLR** = .NET calls this automatically when the type is first used.

### Why positive references are shown as `1+`

The connected GitHub source view can reliably prove that a member has **no** repository reference, which is what matters most for this unused-code audit. It does not provide compiler-level semantic reference counting. Exact positive totals can be misleading for overloaded methods and common property names such as `Id`, `Name` or `IsOffered`.

For that reason this document avoids false precision:

- `0` means a verified zero C# references outside the declaration;
- `1+` means the member is definitely used somewhere in repository C# code;
- framework entry points are marked separately.

If exact positive totals are ever required, they should be generated from a local compiler/Roslyn reference scan. For cleanup work, the important difference is normally **zero versus non-zero**.

### Usage labels

- **USED** = part of the current running mod.
- **TEST-ONLY** = current tests use it, but production runtime code does not.
- **UNUSED CANDIDATE** = no production or test caller/reader was found. Review before deleting.
- **WRITE-ONLY / REVIEW** = code stores a value, but current production code never reads the result.
- **FUTURE HOOK** = not used by the current campaign, but clearly supports a planned/general feature and is covered by tests.
- **FRAMEWORK** = KSP, Unity or .NET calls it automatically.

A public member with zero internal references could theoretically be used by another mod. This repository does not currently define a supported external API, so zero-reference public members are reviewed carefully before removal.

---

# 1. Unused-code audit

The previous audit mixed together two different ideas: **unused by the running mod** and **unused by the repository as a whole**. This section now separates them.

## Removed after verification

These three members had no production caller, no test caller, no framework/reflection role, and no save-format role. They were removed after review.

| Removed member | File | Code refs before removal | Why removal was safe |
| --- | --- | ---: | --- |
| `NextFundingDay` | `Competition/SatelliteRaceController.cs` | **0** | Nothing read it. The UI already uses `NextFundingUniversalTime` with `GetKerbinDay()`. |
| `GetAchievementAgencyCount()` | `Competition/SatelliteRaceController.cs` | **0** | Nothing called it. Current code uses the time-aware private `GetAchievementAgencyCountAtTime()` instead. |
| Three-argument `FundingContractsSaveState.Capture(...)` | `Persistence/FundingContractsSaveState.cs` | **0** | Nothing called it. Production and tests use the four-argument form that also stores the next funding date. |

The logic test workflow passed after these removals.

## Retained because current tests use them

These members have no production-runtime caller, but they are **not unused by the repository**. Current tests call or read them, so they were not deleted in this pass.

| Member | File | Code refs | Status | Why it remains |
| --- | --- | ---: | --- | --- |
| `FindProgramById()` | `Competition/SatelliteRaceController.cs` | 1+ | TEST-ONLY | Controller tests use it to find Aster/Cobalt by stable ID. |
| `Refresh()` with no arguments | `Competition/SatelliteRaceController.cs` | 1+ | TEST-ONLY | Controller tests use the full-refresh convenience wrapper. |
| Basic `AchievementFundingProgramme(...)` constructor | `Funding/AchievementFundingProgramme.cs` | 1+ | TEST-ONLY | Funding and persistence tests build simple test contracts with it. |
| Text-only `AchievementFundingProgramme(...)` constructor | `Funding/AchievementFundingProgramme.cs` | 1+ | TEST-ONLY | Tests use it to check text-only/default unlock behaviour. |
| Basic `FundingProgramme(...)` constructor | `Funding/FundingProgramme.cs` | 1+ | TEST-ONLY | Funding tests build simple network contracts with it. |
| Text-only `FundingProgramme(...)` constructor | `Funding/FundingProgramme.cs` | 1+ | TEST-ONLY | Tests use it to exercise unlock-text/default-rule behaviour. |
| `SpaceProgramState(name, isPlayer)` | `Programs/SpaceProgramState.cs` | 1+ | TEST-ONLY | Many standalone tests use the short constructor for simple agencies. |
| `CurrentSurfaceSpeedMetersPerSecond` | `Tracking/StarterFlightTracker.cs` | 1+ | TEST-ONLY / REVIEW | Starter-flight tests directly verify that the latest speed sample is stored. The current UI does not display this value. |

These can still be reviewed later if the aim is to reduce the public/test convenience API, but they are not repository-wide dead code.

## Write-only / probably obsolete chain

The current UI displays unlock rules directly from `UnlockRule`. It no longer reads the older stored unlock-description string.

| Member | Current behaviour | Review reason |
| --- | --- | --- |
| `AchievementFundingProgramme.UnlockRequirement` | Written when the contract is created. | No production reader was found. |
| `FundingProgramme.UnlockRequirement` | Written when the contract is created. | No production reader was found. |
| `PrototypeFundingCatalogue.CreateUnlockRequirementText()` | Builds those old strings. | Its result is stored in the two write-only properties above. |
| `PrototypeFundingCatalogue.IsSingleAgencyAchievementCondition()` | Helps build the old text. | Its useful effect exists only through `CreateUnlockRequirementText()`. |
| `PrototypeFundingCatalogue.FindRequiredMilestone()` | Helps build the old text. | Its useful effect exists only through `CreateUnlockRequirementText()`. |

This group is more important than a simple zero-reference search because the functions **are called**, but their final result appears to have no effect on the running mod. It should be reviewed as a separate cleanup task with tests.

## Dormant future feature, not necessarily obsolete

`UnlockConditionDefinition.AfterUniversalTime()` has no current production caller, but several tests use it and the evaluator/UI fully support it.

This looks like a deliberate future extension point for date/era-based unlocks. It should not be treated as dead code simply because the current catalogue does not create a date rule.

The `Player` and `AnyRival` unlock scopes are also supported by the evaluator/UI even though the current catalogue mainly creates `AnyAgency` rules. They are dormant flexibility rather than obviously obsolete code.

---

# 2. Very simple project flow

1. KSP gives the mod vessel, time, money and save information.
2. Tracking code turns vessel information into race progress.
3. `SatelliteRaceController` updates rivals, contracts, funding and unlocks.
4. Persistence code stores important state in the KSP save.
5. `RaceWindow` reads the state and shows it to the player.

The UI does not decide race progress. It only displays it.

---

# 3. Quick file list

| File | File lines | Simple purpose |
| --- | ---: | --- |
| `Competition/SatelliteRaceController.cs` | 944 | Main race coordinator. Handles funding dates, offers, rivals and shared race progress. |
| `Core/RaceRuntime.cs` | 268 | Keeps the race running and decides how often checks happen. |
| `Core/RaceSettings.cs` | 132 | Stores built-in balance settings. |
| `Funding/AchievementFundingProgramme.cs` | 175 | Stores one achievement contract and its declining payouts. |
| `Funding/FundingProgramme.cs` | 148 | Stores one satellite-network contract and its payout. |
| `Funding/PrototypeFundingCatalogue.cs` | 366 | Builds the current funding-contract catalogue. |
| `KspIntegration/CareerFundingAdapter.cs` | 28 | Adds real Career money to the player. |
| `KspIntegration/KspCelestialBodyOrdering.cs` | 182 | Sorts planets/moons from Kerbin outward for the UI. |
| `KspIntegration/KspVesselDiscovery.cs` | 547 | Reads KSP vessels and converts them to simple project data. |
| `KspIntegration/RacePersistenceScenario.cs` | 221 | Connects project save-state classes to KSP saves. |
| `KspIntegration/RaceSettingsLoader.cs` | 176 | Reads `RaceSettings.cfg`. |
| `Milestones/MilestoneDefinition.cs` | 365 | Describes milestone rules and starter-contract targets. |
| `Milestones/PrototypeMilestones.cs` | 658 | Contains the current starter/orbital milestone catalogue. |
| `Milestones/UnlockRuleDefinition.cs` | 211 | Describes how unlock requirements are represented. |
| `Milestones/UnlockRuleEvaluator.cs` | 223 | Checks whether unlock requirements are complete. |
| `Persistence/ActiveContractProgressSaveState.cs` | 299 | Saves temporary starter-flight progress. |
| `Persistence/FundingContractsSaveState.cs` | 384 | Saves player achievements and funding contracts. |
| `Persistence/RivalProgramsSaveState.cs` | 382 | Saves rival agencies. |
| `Programs/SpaceProgramState.cs` | 141 | Stores the current state of one agency. |
| `Simulation/RivalSimulation.cs` | 573 | Simulates rival mission planning and progress. |
| `Tracking/ActiveVesselTrackingSnapshot.cs` | 137 | Defines active-vessel data needed by starter contracts. |
| `Tracking/VesselTrackingSnapshot.cs` | 34 | Defines the small vessel record used by orbital tracking. |
| `Tracking/SatelliteTracker.cs` | 138 | Counts satellites and records orbital milestones. |
| `Tracking/StarterFlightTracker.cs` | 537 | Checks Directed Power, Mass, Control and Biome contracts. |
| `Tracking/SurfaceImpactEvaluator.cs` | 75 | Decides whether a vessel destruction was probably a surface impact. |
| `UI/RaceWindow.cs` | 1,697 | Draws the Command Center. |
| `TheRaceForSpace.csproj` | 92 | Build/deployment instructions for .NET. |

---

# 4. Detailed production file guide

## 4.1 `Competition/SatelliteRaceController.cs`

**Purpose:** Main coordinator for the race. It joins funding, rivals, milestones, vessel tracking and saving together.

| Member | Kind | Member lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `SatelliteRaceController()` | Constructor | ~73 | 1+ | USED | Creates the player, rivals and funding contracts. |
| `PlayerProgram` | Property | 1 | 1+ | USED | Gives access to the player's race state. |
| `Programs` | Property | 1 | 1+ | USED | Gives the list of every agency. |
| `RivalPrograms` | Property | 1 | 1+ | USED | Gives the list of rivals. |
| `FundingProgrammes` | Property | 1 | 1+ | USED | Gives satellite funding contracts. |
| `AchievementFundingProgrammes` | Property | ~4 | 1+ | USED | Gives achievement funding contracts. |
| `ActiveStarterContracts` | Property | ~4 | 1+ | USED | Gives the starter contracts that should currently be checked. |
| `RivalBaseIncomePerFundingPeriod` | Property | 1 | 1+ | USED | Shows the guaranteed rival income per funding date. |
| `NextFundingUniversalTime` | Property | 1 | 1+ | USED | Exact KSP time of the next funding date. |
| `NextFundingYear` | Property | ~4 | 1+ | USED | Kerbin year of the next funding date. |
| `DaysUntilNextFunding` | Property | ~16 | 1+ | USED | Days remaining until funding. |
| `NotifyPlayerStarterAchievementRecorded()` | Function | ~4 | 1+ | USED | Says the active starter list must be rebuilt. |
| `FindProgramById()` | Function | ~21 | 1+ | TEST-ONLY | Finds an agency by stable ID. Current controller tests use it. |
| `GetKerbinYear()` | Function | ~11 | 1+ | USED | Converts KSP time to Kerbin year. |
| `GetKerbinDay()` | Function | ~11 | 1+ | USED | Converts KSP time to Kerbin day. |
| `GetRivalLaunchProgressCost()` | Function | ~14 | 1+ | USED | Gets the next rival development cost. |
| `GetEstimatedRivalLaunchDays()` | Function | ~16 | 1+ | USED | Estimates rival mission completion time. |
| `HasProgramAchieved()` | Function | ~9 | 1+ | USED | Checks whether an agency completed an achievement. |
| `IsAchievementProgrammeAvailable()` | Function | ~16 | 1+ | USED | Checks whether an achievement is unlocked now. |
| `GetSatelliteCurrentPayout()` | Function | ~28 | 1+ | USED | Gets an agency's expected satellite payout. |
| `GetAchievementCurrentPayout()` | Function | ~26 | 1+ | USED | Gets an agency's expected achievement payout. |
| `Refresh()` | Function | ~4 | 1+ | TEST-ONLY | Convenience full refresh. Current controller tests use it. |
| `Refresh(bool)` | Function | ~112 | 1+ | USED | Main race update. Can skip the expensive vessel scan. |
| `RefreshRivals()` | Function | ~8 | 1+ | USED | Advances rival simulation. |
| `UpdateFundingAvailability()` | Function | ~22 | 1+ | USED | Unlocks satellite funding targets. |
| `UpdateSpecialAchievementOffers()` | Function | ~28 | 1+ | USED | Immediately offers Probe Orbit after a Level V starter completion. |
| `UpdateSatelliteTargetReachedState()` | Function | ~20 | 1+ | USED | Records when a satellite network reached its full target. |
| `StartAchievementContracts()` | Function | ~17 | 1+ | USED | Starts payouts after an offered achievement is completed. |
| `ProcessDueFunding()` | Function | ~77 | 1+ | USED | Pays crossed funding dates in order. |
| `ReviewFundingOffers()` | Function | ~101 | 1+ | USED | Chooses sponsor offers. |
| `RebuildActiveStarterContractPlanIfNeeded()` | Function | ~33 | 1+ | USED | Rebuilds the live starter-contract check list. |
| `AwardProgramFunds()` | Function | ~18 | 1+ | USED | Gives money to player/rival. |
| `EvaluateFundingProgrammes()` | Function | ~62 | 1+ | USED | Recalculates next payouts. |
| `CalculateSatelliteFundingForProgram()` | Function | ~18 | 1+ | USED | Adds all satellite payouts for one agency. |
| `CalculateSatelliteCurrentPayout()` | Function | ~20 | 1+ | USED | Calculates one satellite payout. |
| `GetCollectiveSatelliteCount()` | Function | ~17 | 1+ | USED | Counts all agencies' satellites around one body. |
| `CalculateAchievementCurrentPayout()` | Function | ~22 | 1+ | USED | Calculates one achievement payout. |
| `IsAchievementProgrammeAvailableAtTime()` | Function | ~10 | 1+ | USED | Checks unlock state at a chosen KSP time. |
| `GetAchievementAgencyCountAtTime()` | Function | ~22 | 1+ | USED | Counts agencies completed by a chosen time. |
| `HasProgramAchievedByTime()` | Function | ~15 | 1+ | USED | Checks whether one agency completed before a deadline. |

---

## 4.2 `Core/RaceRuntime.cs`

**Purpose:** Keeps the race running across KSP scenes. It schedules the 1-second, 5-second and 20-second work.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `Controller` | Property | ~18 | 1+ | USED | Gives the UI the current controller. |
| `StarterFlightState` | Property | ~18 | 1+ | USED | Gives the UI current starter-flight progress. |
| `Awake()` | Function | ~31 | KSP/Unity | FRAMEWORK | Starts the runtime and blocks duplicate copies. |
| `OnDestroy()` | Function | ~8 | KSP/Unity | FRAMEWORK | Clears the active runtime reference. |
| `Update()` | Function | ~73 | KSP/Unity | FRAMEWORK | Runs scheduled work. |
| `RefreshStarterFlightState()` | Function | ~85 | 1+ | USED | Samples the active vessel and checks starter contracts. |
| `EnsureControllerForCurrentGame()` | Function | ~40 | 1+ | USED | Creates fresh race state when a different save becomes active. |

---

## 4.3 `Core/RaceSettings.cs`

**Purpose:** Stores built-in balance values used when config does not replace them.

### `RaceBodySettings`

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `RaceBodySettings(...)` | Constructor | ~18 | 1+ | USED | Creates one body balance group. |
| `ProbeProgressCostFunds` | Property | 1 | 1+ | USED | Rival probe development cost. |
| `CrewedProgressCostFunds` | Property | 1 | 1+ | USED | Rival crewed development cost. |
| `ProbeRewardFunds` | Property | 1 | 1+ | USED | Probe achievement reward. |
| `CrewedRewardFunds` | Property | 1 | 1+ | USED | Crewed achievement reward. |
| `SatelliteProgressCostFunds` | Property | 1 | 1+ | USED | Rival satellite development cost. |
| `SatelliteNetworkSize` | Property | 1 | 1+ | USED | Full network satellite target. |
| `SatelliteNetworkValueFunds` | Property | 1 | 1+ | USED | Total network funding value. |

### `RaceSettings`

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `RaceSettings()` | Static constructor | ~4 | CLR | FRAMEWORK | Loads defaults when the type is first used. |
| `Kerbin` | Property | 1 | 1+ | USED | Kerbin balance group. |
| `KerbinMoons` | Property | 1 | 1+ | USED | Mun/Minmus balance group. |
| `InterplanetaryPlanets` | Property | 1 | 1+ | USED | Other-planet balance group. |
| `InterplanetaryMoons` | Property | 1 | 1+ | USED | Other-moon balance group. |
| `FundingIntervalDays` | Property | 1 | 1+ | USED | Days between funding dates. |
| `RivalStartingFunds` | Property | 1 | 1+ | USED | Starting rival money. |
| `RivalProgressChance` | Property | 1 | 1+ | USED | Chance a rival progresses. |
| `NumberOfRivals` | Property | 1 | 1+ | USED | Number of rivals to create. |
| `ResetToDefaults()` | Function | ~50 | 1+ | USED | Restores built-in values. |
| `GetBodySettings()` | Function | ~36 | 1+ | USED | Chooses a balance group for a body name. |

---

## 4.4 `Funding/AchievementFundingProgramme.cs`

**Purpose:** Represents one one-off achievement contract and its ten declining payments.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| Basic constructor | Constructor | ~18 | 1+ | TEST-ONLY | Creates a simple test contract with default unlock text. |
| Text-only constructor | Constructor | ~19 | 1+ | TEST-ONLY | Creates a test contract with custom text but no structured rule. |
| Full constructor | Constructor | ~20 | 1+ | USED | Creates the real current contract including unlock rule. |
| `Id` | Property | 1 | 1+ | USED | Stable contract ID. |
| `Name` | Property | 1 | 1+ | USED | Player-facing name. |
| `ObjectiveDescription` | Property | 1 | 1+ | USED | Player-facing task. |
| `UnlockRequirement` | Property | 1 | 1+ write | WRITE-ONLY / REVIEW | Stores old unlock text. No production reader found. |
| `UnlockRule` | Property | 1 | 1+ | USED | Real unlock rule. |
| `BaseRewardFunds` | Property | 1 | 1+ | USED | Full starting contract value. |
| `IsOffered` | Property | 1 | 1+ | USED | Whether sponsors offered it. |
| `HasStarted` | Property | 1 | 1+ | USED | Whether first completion started payouts. |
| `PaymentsProcessed` | Property | 1 | 1+ | USED | Number of payments already made. |
| `IsExpired` | Property | ~4 | 1+ | USED | True after ten payments. |
| `CurrentInterestPercent` | Property | ~15 | 1+ | USED | Current 100%-10% interest level. |
| `CurrentTotalPayoutFunds` | Property | ~4 | 1+ | USED | Money available at next payment. |
| `Offer()` | Function | ~10 | 1+ | USED | Marks contract offered. |
| `Start()` | Function | ~10 | 1+ | USED | Starts payout lifecycle. |
| `CalculateCurrentPayout()` | Function | ~12 | 1+ | USED | Calculates one agency's share. |
| `AdvancePayout()` | Function | ~10 | 1+ | USED | Moves to the next payment. |
| `RestoreState()` | Function | ~6 | 1+ | USED | Restores lifecycle from save. |
| `RestoreOfferState()` | Function | ~4 | 1+ | USED | Restores offered state. |

---

## 4.5 `Funding/FundingProgramme.cs`

**Purpose:** Represents one satellite-network funding target.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| Basic constructor | Constructor | ~20 | 1+ | TEST-ONLY | Creates a simple network contract for tests. |
| Text-only constructor | Constructor | ~22 | 1+ | TEST-ONLY | Creates a test network contract with text but no structured rule. |
| Full constructor | Constructor | ~20 | 1+ | USED | Creates the current network contract including unlock rule. |
| `Id` | Property | 1 | 1+ | USED | Stable contract ID. |
| `Name` | Property | 1 | 1+ | USED | Player-facing name. |
| `CelestialBodyName` | Property | 1 | 1+ | USED | Target body. |
| `RequiredSatellites` | Property | 1 | 1+ | USED | Full network size. |
| `RewardFunds` | Property | 1 | 1+ | USED | Total funding pool. |
| `UnlockRequirement` | Property | 1 | 1+ write | WRITE-ONLY / REVIEW | Stores old unlock text. No production reader found. |
| `UnlockRule` | Property | 1 | 1+ | USED | Real unlock rule. |
| `IsAvailable` | Property | 1 | 1+ | USED | Whether requirements are complete. |
| `IsOffered` | Property | 1 | 1+ | USED | Whether sponsor offered it. |
| `HasReachedSatelliteTarget` | Property | 1 | 1+ | USED | Remembers full target was reached. |
| `Unlock()` | Function | ~4 | 1+ | USED | Makes contract available. |
| `Offer()` | Function | ~4 | 1+ | USED | Marks contract offered. |
| `MarkSatelliteTargetReached()` | Function | ~4 | 1+ | USED | Records saturation. |
| `RestoreAvailability()` | Function | ~4 | 1+ | USED | Restores availability. |
| `RestoreOfferState()` | Function | ~5 | 1+ | USED | Restores offered/saturation state. |
| `CalculateCurrentPayout()` | Function | ~31 | 1+ | USED | Calculates one agency's network share. |

---

## 4.6 `Funding/PrototypeFundingCatalogue.cs`

**Purpose:** Builds the achievement and satellite funding targets used by v0.5.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `CreateAchievementProgrammes()` | Function | ~28 | 1+ | USED | Builds achievement contracts. |
| `CreateSatelliteProgrammes()` | Function | ~140 | 1+ | USED | Builds satellite contracts. |
| `AddAchievementProgramme()` | Function | ~34 | 1+ | USED | Converts one milestone into funding. |
| `CreateSatelliteProgramme()` | Function | ~20 | 1+ | USED | Creates one satellite target. |
| `CreateKerbinMoonNetworkUnlockRule()` | Function | ~12 | 1+ | USED | Creates Mun/Minmus network rule. |
| `CreateInterplanetaryPlanetNetworkUnlockRule()` | Function | ~14 | 1+ | USED | Creates planet network rule. |
| `CreatePlanetaryMoonNetworkUnlockRule()` | Function | ~12 | 1+ | USED | Creates moon network rule. |
| `CreateNetworkProgressCondition()` | Function | ~14 | 1+ | USED | Makes the 60%-network requirement. |
| `CreateUnlockRequirementText()` | Function | ~92 | 1+ | WRITE-ONLY / REVIEW | Builds old text that is stored but not read by current production UI. |
| `IsSingleAgencyAchievementCondition()` | Function | ~10 | 1+ | REVIEW CHAIN | Helper only for old unlock text. |
| `FindRequiredMilestone()` | Function | ~12 | 1+ | REVIEW CHAIN | Helper only for old unlock text. |

---

## 4.7 `KspIntegration/CareerFundingAdapter.cs`

**Purpose:** Small bridge from race funding to KSP Career money.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `TryAddFunds()` | Function | ~18 | 1+ | USED | Adds a positive reward to Career funds. |

---

## 4.8 `KspIntegration/KspCelestialBodyOrdering.cs`

**Purpose:** Gives celestial bodies a stable distance from Kerbin for UI sorting.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `GetSortDistanceFromKerbin()` | Function | ~76 | 1+ | USED | Gets sort distance. |
| `CreatePathToRoot()` | Function | ~27 | 1+ | USED | Builds body-parent path. |
| `SumOrbitRadii()` | Function | ~20 | 1+ | USED | Adds orbit distances. |
| `FindBody()` | Function | ~27 | 1+ | USED | Finds KSP body by name. |
| `GetOrbitRadius()` | Function | ~12 | 1+ | USED | Gets safe orbital radius. |
| `GetParent()` | Function | ~14 | 1+ | USED | Gets parent body. |

---

## 4.9 `KspIntegration/KspVesselDiscovery.cs`

**Purpose:** Reads real KSP vessel objects and converts them into project-owned snapshots.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `TryCaptureOrbitingVessels()` | Function | ~100 | 1+ | USED | Reads all orbiting vessels. |
| `TryCaptureActiveVessel()` | Function | ~105 | 1+ | USED | Reads the currently controlled vessel. |
| `TryConsumeActiveVesselSurfaceImpact()` | Function | ~25 | 1+ | USED | Returns one pending impact event. |
| `DisableActiveVesselSurfaceImpactTracking()` | Function | ~24 | 1+ | USED | Removes crash tracking when not needed. |
| `ResetActiveVesselTracking()` | Function | ~5 | 1+ | USED | Clears active-vessel tracking on save change. |
| `EnsureActiveTrackingGame()` | Function | ~9 | 1+ | USED | Keeps callbacks tied to correct save. |
| `EnsureVesselWillDestroySubscription()` | Function | ~14 | 1+ | USED | Adds global destruction callback. |
| `TrackActiveVesselDestruction()` | Function | ~25 | 1+ | USED | Adds destruction callback to active vessel. |
| `DetachDestructionCallback()` | Function | ~11 | 1+ | USED | Removes old vessel callback. |
| `CaptureDestructionTelemetry()` | Function | ~40 | 1+ | USED | Stores last reliable pre-crash data. |
| `OnVesselWillDestroy()` | Function | ~4 | 1+ / KSP | USED | KSP event handler for vessel destruction. |
| `RecordPotentialSurfaceImpact()` | Function | ~45 | 1+ | USED | Decides whether to record a pending impact. |
| `GetSurfaceClearanceMeters()` | Function | ~19 | 1+ | USED | Gets best height above surface. |
| `ConvertSituation()` | Function | ~27 | 1+ | USED | Converts KSP situation to project situation. |
| `ConvertVesselType()` | Function | ~16 | 1+ | USED | Converts KSP vessel type. |
| `GetProtoCrewCount()` | Function | ~30 | 1+ | USED | Counts crew on unloaded vessel. |
| `IsFinite()` | Function | ~4 | 1+ | USED | Rejects NaN/infinity. |

---

## 4.10 `KspIntegration/RacePersistenceScenario.cs`

**Purpose:** KSP save/load bridge.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `OnLoad()` | Function | ~37 | KSP/Unity | FRAMEWORK | Reads mod save nodes. |
| `OnSave()` | Function | ~31 | KSP/Unity | FRAMEWORK | Writes mod save nodes. |
| `TryRestoreCommandCenterVisibility()` | Function | ~12 | 1+ | USED | Restores window visibility. |
| `CaptureCommandCenterVisibility()` | Function | ~10 | 1+ | USED | Stores window visibility. |
| `TryRestoreRivalState()` | Function | ~17 | 1+ | USED | Restores rivals. |
| `TryRestoreRaceProgress()` | Function | ~24 | 1+ | USED | Restores player/funding progress. |
| `TryRestoreActiveContractProgress()` | Function | ~17 | 1+ | USED | Restores starter-flight progress. |
| `CaptureRivalState()` | Function | ~15 | 1+ | USED | Copies rival state for saving. |
| `CaptureRaceProgress()` | Function | ~21 | 1+ | USED | Copies player/funding state for saving. |
| `CaptureActiveContractProgress()` | Function | ~16 | 1+ | USED | Copies starter-flight state for saving. |

---

## 4.11 `KspIntegration/RaceSettingsLoader.cs`

**Purpose:** Reads user-editable balance config once.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `EnsureLoaded()` | Function | ~74 | 1+ | USED | Loads config once. |
| `ApplyBodySettings()` | Function | ~32 | 1+ | USED | Applies one body settings section. |
| `ReadDouble()` | Function | ~34 | 1+ | USED | Reads checked decimal value. |
| `ReadInt()` | Function | ~25 | 1+ | USED | Reads checked whole number. |
| `LogInvalidValue()` | Function | ~15 | 1+ | USED | Logs bad config values. |

---

## 4.12 `Milestones/MilestoneDefinition.cs`

**Purpose:** Defines what milestones and starter targets mean.

### `StarterContractCriteria`

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| Private constructor | Constructor | ~25 | 1+ | USED | Stores one set of starter target values. |
| `RequiredSpeedMetersPerSecond` | Property | 1 | 1+ | USED | Directed Power speed. |
| `RequiredMassTonnes` | Property | 1 | 1+ | USED | Mass target. |
| `RequiredDistanceMeters` | Property | 1 | 1+ | USED | Distance target. |
| `MinimumAltitudeMeters` | Property | 1 | 1+ | USED | Control band bottom. |
| `MaximumAltitudeMeters` | Property | 1 | 1+ | USED | Control top / Directed Power ceiling. |
| `RequiredDurationSeconds` | Property | 1 | 1+ | USED | Control hold time. |
| `RequiredBiomeName` | Property | 1 | 1+ | USED | Biome target. |
| `DirectedPower()` | Function | ~13 | 1+ | USED | Creates Directed Power criteria. |
| `Mass()` | Function | ~13 | 1+ | USED | Creates Mass criteria. |
| `Control()` | Function | ~13 | 1+ | USED | Creates Control criteria. |
| `Biome()` | Function | ~13 | 1+ | USED | Creates Biome criteria. |

### `MilestoneVesselObservation`

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| Constructor | Constructor | ~10 | 1+ | USED | Creates a simple orbital observation. |
| `CelestialBodyName` | Property | 1 | 1+ | USED | Body being orbited. |
| `Situation` | Property | 1 | 1+ | USED | Flight situation. |
| `CrewQualification` | Property | 1 | 1+ | USED | Probe/crewed qualification. |

### `MilestoneDefinition`

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| Orbital constructor | Constructor | ~27 | 1+ | USED | Creates normal orbital milestone. |
| Extended constructor | Constructor | ~29 | 1+ | USED | Constructor chain used by orbital milestones. |
| Full constructor | Constructor | ~50 | 1+ | USED | Creates full milestone including starter criteria. |
| `Id` | Property | 1 | 1+ | USED | Stable milestone ID. |
| `Name` | Property | 1 | 1+ | USED | Display name. |
| `CelestialBodyName` | Property | 1 | 1+ | USED | Target body. |
| `Situation` | Property | 1 | 1+ | USED | Required orbital situation. |
| `CrewRequirement` | Property | 1 | 1+ | USED | Crew/probe requirement. |
| `ObjectiveDescription` | Property | 1 | 1+ | USED | Player-facing objective. |
| `UnlockRule` | Property | 1 | 1+ | USED | Unlock rule. |
| `ObjectiveType` | Property | 1 | 1+ | USED | Objective family. |
| `StarterLine` | Property | 1 | 1+ | USED | Starter line. |
| `StarterLevel` | Property | 1 | 1+ | USED | Starter level I-V. |
| `BaseRewardFunds` | Property | 1 | 1+ | USED | Starter reward. |
| `RivalProgressCostFunds` | Property | 1 | 1+ | USED | Rival starter development cost. |
| `RequiredSpeedMetersPerSecond` | Property | 1 | 1+ | USED | Directed Power speed. |
| `RequiredMassTonnes` | Property | 1 | 1+ | USED | Mass target. |
| `RequiredDistanceMeters` | Property | 1 | 1+ | USED | Distance target. |
| `MinimumAltitudeMeters` | Property | 1 | 1+ | USED | Control lower altitude. |
| `MaximumAltitudeMeters` | Property | 1 | 1+ | USED | Control upper/Power ceiling. |
| `RequiredDurationSeconds` | Property | 1 | 1+ | USED | Control hold time. |
| `RequiredBiomeName` | Property | 1 | 1+ | USED | Biome target. |
| `IsStarterContract` | Property | ~4 | 1+ | USED | Says whether this is a starter contract. |
| `IsSatisfiedBy()` | Function | ~25 | 1+ | USED | Checks normal orbital observation. |
| `CreateObjectiveDescription()` | Function | ~60 | 1+ | USED | Builds starter text from gameplay numbers. |

---

## 4.13 `Milestones/PrototypeMilestones.cs`

**Purpose:** Contains the current milestone catalogue.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `All` | Property | ~4 | 1+ | USED | Normal orbital milestones. |
| `StarterContracts` | Property | ~4 | 1+ | USED | Twenty starter milestones. |
| `FindById()` | Function | ~14 | 1+ | USED | Finds milestone by ID. |
| `CreateMilestoneIndex()` | Function | ~30 | 1+ | USED | Builds fast lookup dictionary. |
| `CreateStarterMilestone()` | Function | ~32 | 1+ | USED | Creates starter definition. |
| `CreateProbeOrbitUnlockRule()` | Function | ~20 | 1+ | USED | Creates four-way Level V Probe Orbit rule. |
| `CreateInterplanetaryProbeUnlockRule()` | Function | ~12 | 1+ | USED | Requires Mun + Minmus probe achievements. |
| `CreateInterplanetaryCrewedUnlockRule()` | Function | ~12 | 1+ | USED | Requires Mun + Minmus crewed achievements. |

---

## 4.14 `Milestones/UnlockRuleDefinition.cs`

**Purpose:** Describes unlock requirements. It stores rules but does not evaluate them.

### `UnlockConditionDefinition`

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| Private constructor | Constructor | ~20 | 1+ | USED | Stores one condition. |
| `ConditionType` | Property | 1 | 1+ | USED | Achievement/date/satellite type. |
| `ProgramScope` | Property | 1 | 1+ | USED | Player/rival/any agency. |
| `MilestoneId` | Property | 1 | 1+ | USED | Required achievement ID. |
| `RequiredProgramCount` | Property | 1 | 1+ | USED | Number of agencies required. |
| `RequiredUniversalTime` | Property | 1 | 1+ | USED | Required campaign time. |
| `CelestialBodyName` | Property | 1 | 1+ | USED | Satellite-count body. |
| `RequiredSatelliteCount` | Property | 1 | 1+ | USED | Required satellites. |
| `Achievement()` simple | Function | ~5 | 1+ | USED | Makes one-agency achievement condition. |
| `Achievement()` count | Function | ~31 | 1+ | USED | Makes achievement condition with count. |
| `AfterUniversalTime()` | Function | ~24 | 1+ | FUTURE HOOK | Tests use it; current production catalogue does not. |
| `SatelliteCount()` | Function | ~28 | 1+ | USED | Makes satellite-count condition. |

### `UnlockPathDefinition`

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| Constructor | Constructor | ~8 | 1+ | USED | Creates one AND path. |
| `Conditions` | Property | ~4 | 1+ | USED | Conditions inside path. |

### `UnlockRuleDefinition`

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| Constructor | Constructor | ~8 | 1+ | USED | Creates an OR-of-AND rule. |
| `Paths` | Property | ~4 | 1+ | USED | Possible unlock paths. |
| `AnyAgencyAchievement()` | Function | ~9 | 1+ | USED | Common one-achievement rule. |

---

## 4.15 `Milestones/UnlockRuleEvaluator.cs`

**Purpose:** Decides whether an unlock rule is complete.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `IsSatisfied()` | Function | ~30 | 1+ | USED | Checks whole rule. |
| `IsConditionSatisfied()` | Function | ~29 | 1+ | USED | Checks one condition. |
| `GetSatisfiedProgramCount()` | Function | ~34 | 1+ | USED | Counts agencies satisfying achievement condition. |
| `GetSatelliteCount()` | Function | ~31 | 1+ | USED | Counts qualifying satellites for condition. |
| `DoesProgramSatisfyAchievementCondition()` | Function | ~25 | 1+ | USED | Checks one agency against condition/time. |
| `IsPathSatisfied()` | Function | ~20 | 1+ | USED | Checks every condition in one path. |
| `ProgramMatchesScope()` | Function | ~25 | 1+ | USED | Applies Player/Rival/Any scope. |
| `IsValidEvaluationTime()` | Function | ~6 | 1+ | USED | Rejects bad times. |

---

## 4.16 `Persistence/ActiveContractProgressSaveState.cs`

**Purpose:** Saves temporary starter-flight state.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `HasData` | Property | 1 | 1+ | USED | Says whether anything needs saving. |
| `Capture()` | Function | ~42 | 1+ | USED | Copies tracker progress. |
| `ApplyTo()` | Function | ~35 | 1+ | USED | Restores tracker progress. |
| `Load()` | Function | ~90 | 1+ | USED | Reads save node. |
| `Save()` | Function | ~35 | 1+ | USED | Writes save node. |
| `ClearState()` | Function | ~19 | 1+ | USED | Clears saved values. |
| `AddDouble()` | Function | ~4 | 1+ | USED | Writes decimal safely. |
| `ParseBool()` | Function | ~5 | 1+ | USED | Reads optional bool. |
| `TryParseBool()` | Function | ~5 | 1+ | USED | Validates bool. |
| `TryParseFiniteDouble()` | Function | ~8 | 1+ | USED | Validates decimal. |

### Private `SavedControlContractProgress`

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| Constructor | Constructor | ~10 | 1+ | USED | Creates saved Control record. |
| `MilestoneId` | Property | 1 | 1+ | USED | Control contract ID. |
| `HoldSeconds` | Property | 1 | 1+ | USED | Saved hold time. |
| `WasSampleInBand` | Property | 1 | 1+ | USED | Saved last-band state. |
| `IsQualified` | Property | 1 | 1+ | USED | Saved qualified state. |

---

## 4.17 `Persistence/FundingContractsSaveState.cs`

**Purpose:** Saves player achievements, funding lifecycles and next funding date.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `HasData` | Property | 1 | 1+ | USED | Says whether funding data exists. |
| `NextFundingUniversalTime` | Property | 1 | 1+ | USED | Saved next funding date. |
| `Capture()` four-argument | Function | ~61 | 1+ | USED | Captures current funding state including next funding date. |
| `ApplyTo()` | Function | ~67 | 1+ | USED | Restores runtime state. |
| `Load()` | Function | ~78 | 1+ | USED | Reads save nodes. |
| `Save()` | Function | ~72 | 1+ | USED | Writes save nodes. |
| `ClearState()` | Function | ~9 | 1+ | USED | Clears dictionaries. |
| `StorePlayerAchievement()` | Function | ~11 | 1+ | USED | Stores earliest achievement time. |
| `TryParseBool()` | Function | ~5 | 1+ | USED | Reads bool safely. |
| `TryParsePaymentCount()` | Function | ~18 | 1+ | USED | Reads payment count safely. |
| `TryParseFiniteDouble()` | Function | ~8 | 1+ | USED | Reads decimal safely. |
| `IsFinite()` | Function | ~4 | 1+ | USED | Rejects invalid decimals. |

### Private saved contract records

All constructors/properties in `SavedAchievementContract` and `SavedSatelliteContract` have **1+ code references**. They are actively used by capture/load/save/restore and are not dead code.

---

## 4.18 `Persistence/RivalProgramsSaveState.cs`

**Purpose:** Saves all simulated rivals by stable ID.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `HasData` | Property | ~4 | 1+ | USED | Says whether rival data exists. |
| `Capture()` | Function | ~29 | 1+ | USED | Captures all rivals. |
| `ApplyTo()` | Function | ~24 | 1+ | USED | Restores all rivals. |
| `Load()` | Function | ~31 | 1+ | USED | Reads rivals from save. |
| `Save()` | Function | ~20 | 1+ | USED | Writes rivals to save. |

### Private `SavedRivalProgram`

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `HasData` | Property | 1 | 1+ | USED | Valid saved record. |
| `ProgramId` | Property | 1 | 1+ | USED | Rival stable ID. |
| `Funds` | Property | 1 | 1+ | USED | Saved money. |
| `NextMissionTargetId` | Property | 1 | 1+ | USED | Saved planned mission. |
| `LaunchProgressPercent` | Property | 1 | 1+ | USED | Saved mission progress. |
| `NextLaunchProgressCheckUniversalTime` | Property | 1 | 1+ | USED | Saved next check time. |
| `Capture()` | Function | ~38 | 1+ | USED | Captures one rival. |
| `ApplyTo()` | Function | ~37 | 1+ | USED | Restores one rival. |
| `Load()` | Function | ~60 | 1+ | USED | Reads one rival record. |
| `Save()` | Function | ~52 | 1+ | USED | Writes one rival record. |
| `ClearState()` | Function | ~14 | 1+ | USED | Clears saved rival. |
| `StoreAchievement()` | Function | ~11 | 1+ | USED | Saves earliest achievement. |
| `TryParseFiniteDouble()` | Function | ~14 | 1+ | USED | Validates decimal. |
| `IsFinite()` | Function | ~4 | 1+ | USED | Rejects bad numbers. |

---

## 4.19 `Programs/SpaceProgramState.cs`

**Purpose:** Stores state for one player or rival agency.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `SpaceProgramState(name, isPlayer)` | Constructor | ~4 | 1+ | TEST-ONLY | Convenience constructor used by standalone tests. |
| `SpaceProgramState(id, name, isPlayer)` | Constructor | ~7 | 1+ | USED | Current production constructor with stable ID. |
| `Id` | Property | 1 | 1+ | USED | Stable agency ID. |
| `Name` | Property | 1 | 1+ | USED | Display name. |
| `IsPlayer` | Property | 1 | 1+ | USED | Player/rival flag. |
| `Funds` | Property | 1 | 1+ | USED | Rival spendable money. |
| `NextPayoutFunds` | Property | 1 | 1+ | USED | Projected next funding. |
| `NextMissionTargetId` | Property | 1 | 1+ | USED | Rival mission ID. |
| `NextMissionDisplayName` | Property | 1 | 1+ | USED | Rival mission text. |
| `LaunchProgressPercent` | Property | 1 | 1+ | USED | Rival mission progress. |
| `NextLaunchProgressCheckUniversalTime` | Property | 1 | 1+ | USED | Rival next development check time. |
| `RecordedAchievements` | Property | ~4 | 1+ | USED | Save-friendly achievement collection. |
| `SatelliteCountsByBody` | Property | ~4 | 1+ | USED | Save-friendly satellite collection. |
| `ClearRecordedAchievements()` | Function | ~4 | 1+ | USED | Clears achievements before restore. |
| `ClearSatelliteCounts()` | Function | ~4 | 1+ | USED | Clears satellite counts. |
| `HasAchievement()` | Function | ~10 | 1+ | USED | Checks an achievement. |
| `GetAchievementUniversalTime()` | Function | ~13 | 1+ | USED | Gets first completion time. |
| `RecordAchievement()` | Function | ~16 | 1+ | USED | Records first completion. |
| `GetSatelliteCount()` | Function | ~12 | 1+ | USED | Gets body satellite count. |
| `SetSatelliteCount()` | Function | ~16 | 1+ | USED | Sets/removes body satellite count. |

---

## 4.20 `Simulation/RivalSimulation.cs`

**Purpose:** Simulates rival agencies planning and developing missions.

### `RivalSimulationContext`

Its constructor and fields are used inside this same file. It is active internal support, not dead code.

### Functions

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `Refresh()` | Function | ~31 | 1+ | USED | Advances all rivals. |
| `GetMissionTargetDisplayName()` | Function | ~20 | 1+ | USED | Converts target ID into readable text. |
| `CalculateLaunchProgressIncrementPercent()` | Function | ~13 | 1+ | USED | Returns 20% starter / 10% normal step. |
| `CalculateLaunchProgressCost()` | Function | ~17 | 1+ | USED | Gets next development cost. |
| `CalculateEstimatedLaunchDays()` public | Function | ~14 | 1+ | USED | Starts ETA calculation. |
| `CalculateEstimatedLaunchDays()` private | Function | ~95 | 1+ | USED | Simulates future progress/funding for ETA. |
| `RefreshProgram()` | Function | ~78 | 1+ | USED | Updates one rival. |
| `TryCompleteLaunch()` | Function | ~57 | 1+ | USED | Completes rival mission. |
| `IsTargetAvailable()` | Function | ~39 | 1+ | USED | Checks whether planned target is still valid. |
| `ChooseNextMissionTarget()` | Function | ~67 | 1+ | USED | Picks a new offered mission. |
| `CalculateLaunchProgressCostForTarget()` | Function | ~32 | 1+ | USED | Gets cost for selected target. |
| `SetMissionTarget()` | Function | ~15 | 1+ | USED | Changes rival target. |
| `FindAchievementProgramme()` | Function | ~20 | 1+ | USED | Finds achievement target. |
| `FindFundingProgramme()` | Function | ~20 | 1+ | USED | Finds satellite target. |
| `IsFinite()` | Function | ~4 | 1+ | USED | Rejects bad numbers. |

---

## 4.21 `Tracking/ActiveVesselTrackingSnapshot.cs`

**Purpose:** Defines live vessel data and the telemetry plan used by starter contracts.

### `StarterTelemetryPlan`

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `GetRequirements()` | Function | ~39 | 1+ | USED | Requests only telemetry needed by active starter contracts. |

### `ActiveVesselTrackingSnapshot`

The constructor and all thirteen data properties (`VesselId`, `CelestialBodyName`, `Situation`, `AltitudeMeters`, `SurfaceSpeedMetersPerSecond`, `MassTonnes`, `LatitudeDegrees`, `LongitudeDegrees`, `BodyRadiusMeters`, `BiomeName`, `CrewCount`, `LaunchUniversalTime`, `ObservationUniversalTime`) have **1+ code references**. The KSP adapter fills them and `StarterFlightTracker` reads them.

---

## 4.22 `Tracking/VesselTrackingSnapshot.cs`

**Purpose:** Small record used by the slower orbital scan.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| Constructor | Constructor | ~9 | 1+ | USED | Creates orbital snapshot. |
| `CelestialBodyName` | Property | 1 | 1+ | USED | Body being orbited. |
| `VesselType` | Property | 1 | 1+ | USED | Probe/Relay/Other. |
| `CrewCount` | Property | 1 | 1+ | USED | Crew aboard. |

---

## 4.23 `Tracking/SatelliteTracker.cs`

**Purpose:** Rebuilds player satellite counts and records normal orbital milestones.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `RefreshPlayerSatelliteCounts()` | Function | ~73 | 1+ | USED | Main orbital vessel evaluation. |
| `EvaluateMilestones()` | Function | ~54 | 1+ | USED | Repeats checks so one snapshot can unlock chained achievements. |

---

## 4.24 `Tracking/StarterFlightTracker.cs`

**Purpose:** Tracks one player launch and checks all four starter-contract families.

### Properties

| Member | Lines | Code refs | Status | Simple purpose |
| --- | ---: | ---: | --- | --- |
| `HasActiveAttempt` | 1 | 1+ | USED | Whether a starter attempt exists. |
| `VesselId` | 1 | 1+ | USED | Tracked vessel ID. |
| `CelestialBodyName` | 1 | 1+ | USED | Attempt body. |
| `LaunchUniversalTime` | 1 | 1+ | USED | Launch time. |
| `StartLatitudeDegrees` | 1 | 1+ | USED | Start latitude. |
| `StartLongitudeDegrees` | 1 | 1+ | USED | Start longitude. |
| `LastSampleUniversalTime` | 1 | 1+ | USED | Last sample time. |
| `MaximumAltitudeMeters` | 1 | 1+ | USED | Highest altitude. |
| `MaximumSurfaceSpeedMetersPerSecond` | 1 | 1+ | USED | Highest speed. |
| `CurrentAltitudeMeters` | 1 | 1+ | USED | Current altitude for UI. |
| `CurrentSurfaceSpeedMetersPerSecond` | 1 | 1+ | TEST-ONLY / REVIEW | Tests verify latest speed storage. Current production UI does not read it. |
| `CurrentMassTonnes` | 1 | 1+ | USED | Current mass for UI. |
| `CurrentDistanceMeters` | 1 | 1+ | USED | Current launch distance. |
| `CurrentBiomeName` | 1 | 1+ | USED | Current biome. |
| `CurrentCrewCount` | 1 | 1+ | USED | Current crew count. |
| `CurrentSituation` | 1 | 1+ | USED | Current flight state. |
| `EnteredOrbit` | 1 | 1+ | USED | Remembers orbit disqualification. |
| `ControlStateMilestoneIds` | 1 | 1+ | USED | IDs of saved Control progress. |

### Functions

| Member | Lines | Code refs | Status | Simple purpose |
| --- | ---: | ---: | --- | --- |
| `GetControlHoldSeconds()` | ~9 | 1+ | USED | Gets Control hold time. |
| `IsControlMilestoneQualified()` | ~9 | 1+ | USED | Says Control hold is complete. |
| `IsControlSampleInBand()` | ~9 | 1+ | USED | Says last Control sample was in band. |
| `RestoreControlState()` | ~17 | 1+ | USED | Restores saved Control state. |
| `RefreshPlayerMilestones()` | ~137 | 1+ | USED | Main live starter-contract check. |
| `RecordSurfaceImpact()` | ~58 | 1+ | USED | Evaluates Directed Power after impact. |
| `RestoreState()` | ~65 | 1+ | USED | Restores tracked attempt. |
| `ClearAttempt()` | ~22 | 1+ | USED | Clears attempt. |
| `EvaluateControlMilestones()` | ~62 | 1+ | USED | Updates Control contracts. |
| `GetOrCreateControlState()` | ~12 | 1+ | USED | Gets Control progress record. |
| `ResetUnqualifiedControlStates()` | ~17 | 1+ | USED | Resets broken Control holds. |
| `IsSameAttempt()` | ~25 | 1+ | USED | Handles same launch/staging identity. |
| `BeginAttempt()` | ~17 | 1+ | USED | Starts tracking a launch. |
| `CalculateSurfaceDistanceMeters()` | ~31 | 1+ | USED | Calculates distance from launch point. |
| `IsFinite()` | ~4 | 1+ | USED | Rejects invalid numbers. |

---

## 4.25 `Tracking/SurfaceImpactEvaluator.cs`

**Purpose:** Uses recent flight data to decide whether destruction was a real surface impact.

| Member | Kind | Lines | Code refs | Status | Simple purpose |
| --- | --- | ---: | ---: | --- | --- |
| `IsEligible()` | Function | ~55 | 1+ | USED | Checks impact evidence. |
| `IsFinite()` | Function | ~4 | 1+ | USED | Rejects invalid numbers. |

---

## 4.26 `UI/RaceWindow.cs`

**Purpose:** Draws the Command Center. It reads race state but does not advance gameplay.

### `SpaceRaceFundingEntry`

The constructor and properties `AchievementProgramme`, `SatelliteProgramme`, `CelestialBodyName`, `BodySortDistance`, `CatalogueOrder`, `IsAchievement`, `Id` and `Name` all have **1+ references inside `RaceWindow.cs`**.

### Main UI functions

| Member | Lines | Code refs | Status | Simple purpose |
| --- | ---: | ---: | --- | --- |
| `Awake()` | ~39 | KSP/Unity | FRAMEWORK | Sets up window. |
| `OnDestroy()` | ~21 | KSP/Unity | FRAMEWORK | Removes launcher button/clears references. |
| `Update()` | ~59 | KSP/Unity | FRAMEWORK | Handles save changes, launcher and F8. |
| `CreateCenteredWindowRect()` | ~7 | 1+ | USED | Centres window. |
| `EnsureApplicationLauncherButton()` | ~40 | 1+ | USED | Creates stock launcher button. |
| `SetCommandCenterVisible()` | ~25 | 1+ | USED | Opens/closes window and saves state. |
| `OnGUI()` | ~28 | KSP/Unity | FRAMEWORK | Unity draw entry point. |
| `DrawWindow()` | ~82 | 1+ | USED | Draws tabs and selected view. |
| `DrawOverview()` | ~122 | 1+ | USED | Draws player overview. |
| `DrawFundingTargets()` | ~40 | 1+ | USED | Draws offered funding. |
| `DrawAchievementFundingCard()` | ~95 | 1+ | USED | Draws achievement card. |
| `DrawStarterFundingLiveProgress()` | ~28 | 1+ | USED | Adds live starter progress. |
| `DrawSatelliteFundingCard()` | ~78 | 1+ | USED | Draws satellite card. |
| `DrawPayoutLinesByAmount()` | ~51 | 1+ | USED | Shows payout rows. |
| `DrawRivalAgencies()` | ~27 | 1+ | USED | Draws rival page. |
| `DrawHelpGuide()` | ~39 | 1+ | USED | Draws player guide. |
| `DrawSpaceRace()` | ~63 | 1+ | USED | Draws contract catalogue. |
| `DrawStarterLiveProgress()` | ~105 | 1+ | USED | Draws starter telemetry. |
| `DrawSelectedSpaceRaceFundingEntry()` | ~47 | 1+ | USED | Draws selected contract details. |
| `DrawSpaceRaceFundingSection()` | ~72 | 1+ | USED | Draws collapsible contract group. |
| `EnsureSpaceRaceFundingEntries()` | ~60 | 1+ | USED | Builds combined catalogue list. |
| `CompareSpaceRaceFundingEntries()` | ~25 | 1+ | USED | Sorts catalogue entries. |
| `EnsureSelectedSpaceRaceFundingEntry()` | ~48 | 1+ | USED | Keeps/chooses selection. |
| `GetSpaceRaceFundingCategory()` | ~31 | 1+ | USED | Places contract in Offered/Unlocked/Locked/Expired. |
| `DrawUnlockRuleProgress()` | ~45 | 1+ | USED | Draws unlock paths. |
| `DrawUnlockConditionProgress()` | ~73 | 1+ | USED | Draws one unlock condition. |
| `AppendAchievementConditionText()` | ~70 | 1+ | USED | Builds live achievement-condition text. |
| `FindFirstProgramSatisfyingCondition()` | ~22 | 1+ | USED | Finds agency satisfying condition. |
| `GetMilestoneDisplayName()` | ~9 | 1+ | USED | Converts milestone ID to name. |
| `DrawProgramCard()` | ~122 | 1+ | USED | Draws one rival. |
| `EnsurePayoutScratchBuffers()` | ~16 | 1+ | USED | Resizes reusable payout arrays. |
| `GetProgramDisplayName()` | ~9 | 1+ | USED | Returns Player/rival display name. |
| `DrawCenteredCardTitle()` | ~8 | 1+ | USED | Draws centred card title. |
| `FormatNextFundingDate()` | ~18 | 1+ | USED | Builds next-funding text. |
| `FormatKerbinDate()` | ~17 | 1+ | USED | Builds Kerbin year/day text. |

---

# 5. Files that are data/build support rather than gameplay classes

## `GameData/TheRaceForSpace/Config/RaceSettings.cfg`

User-editable balance defaults. Read once when KSP starts.

## `src/TheRaceForSpace/TheRaceForSpace.csproj`

Builds for .NET Framework 4.7.2 / C# 7.3, references KSP assemblies through `KSP_ROOT`, and can deploy the DLL/config when `DeployToKsp=true`.

## `tools/run-logic-tests.sh`

Runs both standalone test projects.

## `.github/workflows/logic-tests.yml`

Runs the same logic tests on GitHub Actions.

---

# 6. Tests and reference counting

Tests now **do count** in the `Code refs` column. This is deliberate because the current audit is trying to answer: “Does any C# code in this repository still use this member?”

The `Status` column then separates the meaning:

- **USED** = production runtime uses it;
- **TEST-ONLY** = tests use it but runtime production code does not;
- **FUTURE HOOK** = tests cover it and the design deliberately supports it, but the current campaign does not create it;
- **UNUSED CANDIDATE** = no production or test use was found.

The two test projects are:

- `tests/TheRaceForSpace.Tests/` - funding, milestones, persistence, rival simulation and tracking logic;
- `tests/TheRaceForSpace.ControllerTests/` - controller ordering, offers, starter telemetry and cross-module behaviour.

This distinction prevents useful test helpers from being mistaken for completely dead repository code.

---

# 7. Where to make common changes

| Desired change | Main file |
| --- | --- |
| Starter speed/mass/control/biome targets | `Milestones/PrototypeMilestones.cs` |
| Orbital milestone list | `Milestones/PrototypeMilestones.cs` |
| Satellite funding body list | `Funding/PrototypeFundingCatalogue.cs` |
| Body balance tiers | `Core/RaceSettings.cs` and `RaceSettings.cfg` |
| Rival mission behaviour | `Simulation/RivalSimulation.cs` |
| Funding review/order | `Competition/SatelliteRaceController.cs` |
| Active vessel/KSP API reading | `KspIntegration/KspVesselDiscovery.cs` |
| Starter-flight rules | `Tracking/StarterFlightTracker.cs` |
| Unlock meaning | `Milestones/UnlockRuleEvaluator.cs` |
| Save layout | `Persistence/*` plus `KspIntegration/RacePersistenceScenario.cs` |
| Command Center display | `UI/RaceWindow.cs` |

---

# 8. Current extension seams and hardcoded areas

Already fairly generic:

- `SpaceProgramState` stores arbitrary achievement IDs and body names.
- save-state classes use stable IDs and collections.
- unlock rules support OR paths and AND conditions.
- `SatelliteTracker` works from supplied milestone definitions.
- `RivalSimulation` works from supplied programme collections.
- body ordering follows KSP's body graph.
- UI handles an arbitrary rival count and current contract collections.

Still hardcoded:

- milestone/body list in `PrototypeMilestones.cs`;
- satellite funding body list in `PrototypeFundingCatalogue.cs`;
- body-to-tier mapping in `RaceSettings.GetBodySettings()`;
- four balance tiers in config rather than per-body definitions;
- Aster and Cobalt are the named first two rivals; extras are generic.

---

# 9. Cleanup priority after this verification pass

Completed in this pass:

1. Removed `NextFundingDay`.
2. Removed `GetAchievementAgencyCount()`.
3. Removed the unused three-argument `FundingContractsSaveState.Capture(...)` overload.
4. Ran the logic test workflow successfully after the removals.

Good candidates for a separate future cleanup review:

1. **Write-only unlock text chain** - likely the clearest obsolete runtime code because the UI now renders `UnlockRule` live.
2. **`CurrentSurfaceSpeedMetersPerSecond`** - currently test-read but not production-read. Decide whether the live-speed state is intentionally useful before removing it and changing its test.
3. **Test-only convenience APIs** - `FindProgramById()`, parameterless `Refresh()`, short funding constructors and `SpaceProgramState(name, isPlayer)`. These are not dead repository code, but they could be removed if tests are deliberately rewritten to use the production-facing APIs.
4. **Keep `AfterUniversalTime()` unless the date-based unlock idea is deliberately abandoned.** It is test-covered and matches the extensible unlock design even though the current campaign catalogue does not use it.

The next strongest runtime-obsolescence candidate is the **write-only unlock text chain**, not the test-only helpers.
