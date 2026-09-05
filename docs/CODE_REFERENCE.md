# The Race for Space - Simple Code Guide

> **Branch covered:** `Alpha/KerbalContracts-v0.5`
>
> This guide explains the project in simple language. It is written so that someone who does not program can still understand what each file is for.

---

## How to read this guide

Each production code file has two numbers:

- **File lines** = the total number of lines in that file on this branch.
- **Member lines** = the approximate size of one function or property. This starts at the function/property declaration and ends at its closing brace. Comments above it are not counted.
- **Other-file refs** = how many **different production `.cs` files** use that function or property. If one file uses it ten times, it still counts as one file here. Test files and documentation are not counted.
- **KSP** in the reference column means KSP or Unity calls that function directly. Another project file does not need to call it.

The reference numbers are a simple maintenance guide. They show how widely something is used. They are not a replacement for a compiler search before deleting or renaming code.

### Simple meanings of common words

- **Controller** = the main coordinator. It tells other parts of the mod when to do their work.
- **Property** = a named value stored or shown by a class.
- **Function / method** = a named action the code can perform.
- **State** = information the mod needs to remember, such as money, achievements or progress.
- **Snapshot** = a small copy of vessel information taken at one moment.
- **Persistence** = save/load code.
- **Catalogue** = a list of contracts or milestones that exist in the mod.
- **Stable ID** = an internal name that should not change because save files use it.

---

# 1. Very simple project flow

The mod works in this order:

1. **KSP gives the mod information** about vessels, time, money and saves.
2. **Tracking code checks the vessel information** and decides whether the player completed something.
3. **The race controller updates the competition.** It handles rivals, funding, offers and unlocks.
4. **Persistence code remembers important information** in the KSP save.
5. **The UI reads the information** and shows it to the player.

The UI does not decide race progress. It only displays it.

---

# 2. Quick file list

## Production code

| File | File lines | Simple purpose |
| --- | ---: | --- |
| `Competition/SatelliteRaceController.cs` | 961 | Main coordinator for the race. Handles funding dates, offers, rivals and shared race progress. |
| `Core/RaceRuntime.cs` | 268 | Keeps the race system running while KSP changes scenes. Decides how often different checks happen. |
| `Core/RaceSettings.cs` | 132 | Stores the default balance numbers used by the mod. |
| `Funding/AchievementFundingProgramme.cs` | 175 | Stores the life of a one-off achievement contract and works out its payout. |
| `Funding/FundingProgramme.cs` | 148 | Stores a satellite-network contract and works out its payout. |
| `Funding/PrototypeFundingCatalogue.cs` | 366 | Builds the list of funding contracts used by the current version of the mod. |
| `KspIntegration/CareerFundingAdapter.cs` | 28 | Gives real KSP Career funds to the player. |
| `KspIntegration/KspCelestialBodyOrdering.cs` | 182 | Sorts planets and moons from Kerbin outward for the UI. |
| `KspIntegration/KspVesselDiscovery.cs` | 547 | Reads vessel information from KSP and turns it into simpler project data. |
| `KspIntegration/RacePersistenceScenario.cs` | 221 | Connects the mod's save data to the KSP save system. |
| `KspIntegration/RaceSettingsLoader.cs` | 176 | Reads `RaceSettings.cfg` when the mod starts. |
| `Milestones/MilestoneDefinition.cs` | 365 | Describes what a milestone is and what must be done to complete it. |
| `Milestones/PrototypeMilestones.cs` | 658 | Contains the actual starter and orbital milestones used by this version. |
| `Milestones/UnlockRuleDefinition.cs` | 211 | Describes rules that decide when something becomes unlocked. |
| `Milestones/UnlockRuleEvaluator.cs` | 223 | Checks whether an unlock rule has been completed. |
| `Persistence/ActiveContractProgressSaveState.cs` | 299 | Saves temporary starter-flight progress. |
| `Persistence/FundingContractsSaveState.cs` | 392 | Saves player achievements and funding-contract progress. |
| `Persistence/RivalProgramsSaveState.cs` | 382 | Saves the simulated rival agencies. |
| `Programs/SpaceProgramState.cs` | 141 | Stores the current information for one space agency. |
| `Simulation/RivalSimulation.cs` | 573 | Simulates rival agencies choosing and working on missions. |
| `Tracking/ActiveVesselTrackingSnapshot.cs` | 137 | Defines the small set of active-vessel information used by starter contracts. |
| `Tracking/SatelliteTracker.cs` | 138 | Counts player satellites and checks normal orbital milestones. |
| `Tracking/StarterFlightTracker.cs` | 537 | Checks Directed Power, Mass, Control and Biome starter contracts. |
| `Tracking/SurfaceImpactEvaluator.cs` | 75 | Decides whether a destroyed vessel probably hit the surface. |
| `Tracking/VesselTrackingSnapshot.cs` | 34 | Defines the small vessel record used by normal orbital tracking. |
| `UI/RaceWindow.cs` | 1,697 | Draws the Command Center and all four player-facing tabs. |
| `TheRaceForSpace.csproj` | 92 | Tells .NET how to build and optionally install the mod. |

---

# 3. Detailed production file guide

## 3.1 `Competition/SatelliteRaceController.cs`

**Simple purpose:** This is the main race coordinator. It joins the player, rivals, milestones, funding and save system together. It also decides the order that race events happen.

**Main class:** `SatelliteRaceController`

### Functions and properties

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `SatelliteRaceController()` | Constructor | ~73 | 1 | Creates a fresh race. Creates the player, rivals and funding contracts. |
| `PlayerProgram` | Property | 1 | 2 | Gives other code access to the player's race information. |
| `Programs` | Property | 1 | 1 | Gives a read-only list of every agency in the race. |
| `RivalPrograms` | Property | 1 | 1 | Gives a read-only list of rival agencies. |
| `FundingProgrammes` | Property | 1 | 1 | Gives a read-only list of satellite funding contracts. |
| `AchievementFundingProgrammes` | Property | ~4 | 1 | Gives a read-only list of achievement funding contracts. |
| `ActiveStarterContracts` | Property | ~4 | 1 | Gives the runtime the starter contracts that should be checked right now. |
| `RivalBaseIncomePerFundingPeriod` | Property | 1 | 1 | Shows the guaranteed money each rival gets on a funding date. |
| `NextFundingUniversalTime` | Property | 1 | 1 | Stores the exact KSP time of the next funding payment. |
| `NextFundingYear` | Property | ~4 | 1 | Shows the Kerbin year of the next funding date. |
| `NextFundingDay` | Property | ~4 | 0 | Shows the Kerbin day number of the next funding date. |
| `DaysUntilNextFunding` | Property | ~16 | 1 | Shows how many whole Kerbin days remain until funding. |
| `NotifyPlayerStarterAchievementRecorded()` | Function | ~4 | 1 | Tells the controller that the active starter-contract list must be rebuilt. |
| `FindProgramById()` | Function | ~21 | 0 | Finds an agency by its internal ID. |
| `GetKerbinYear()` | Function | ~11 | 1 | Converts KSP time into a Kerbin year number. |
| `GetKerbinDay()` | Function | ~11 | 1 | Converts KSP time into a Kerbin day number. |
| `GetRivalLaunchProgressCost()` | Function | ~14 | 1 | Asks how much a rival's next development step will cost. |
| `GetEstimatedRivalLaunchDays()` | Function | ~16 | 1 | Estimates how long a rival may take to finish its current mission. |
| `HasProgramAchieved()` | Function | ~9 | 1 | Checks whether an agency has completed an achievement. |
| `IsAchievementProgrammeAvailable()` | Function | ~16 | 1 | Checks whether an achievement contract is unlocked now. |
| `GetAchievementAgencyCount()` | Function | ~12 | 0 | Counts how many agencies have completed an achievement. |
| `GetSatelliteCurrentPayout()` | Function | ~28 | 1 | Returns one agency's expected satellite-contract payout. |
| `GetAchievementCurrentPayout()` | Function | ~26 | 1 | Returns one agency's expected achievement-contract payout. |
| `Refresh()` | Function | ~4 | 0 | Runs a full controller update including player vessel checks. |
| `Refresh(bool)` | Function | ~112 | 1 | Runs the main race update. It can skip the slower player vessel scan. |
| `RefreshRivals()` | Function | ~8 | 0 | Runs the rival simulation. |
| `UpdateFundingAvailability()` | Function | ~22 | 0 | Unlocks satellite contracts whose requirements are now complete. |
| `UpdateSpecialAchievementOffers()` | Function | ~28 | 0 | Offers Probe Orbit immediately after any starter line reaches Level V. |
| `UpdateSatelliteTargetReachedState()` | Function | ~20 | 0 | Remembers when a satellite network has reached its full target. |
| `StartAchievementContracts()` | Function | ~17 | 0 | Starts the payout life of an offered achievement after an agency completes it. |
| `ProcessDueFunding()` | Function | ~77 | 0 | Pays every funding date that has been reached. It also runs the sponsor review. |
| `ReviewFundingOffers()` | Function | ~101 | 0 | Chooses which unlocked contracts sponsors will offer next. |
| `RebuildActiveStarterContractPlanIfNeeded()` | Function | ~33 | 0 | Rebuilds the list of starter contracts that the player's active flight must check. |
| `AwardProgramFunds()` | Function | ~18 | 0 | Gives money to the player or adds simulated money to a rival. |
| `EvaluateFundingProgrammes()` | Function | ~62 | 0 | Recalculates the expected next payout for every agency. |
| `CalculateSatelliteFundingForProgram()` | Function | ~18 | 0 | Adds together all satellite funding for one agency. |
| `CalculateSatelliteCurrentPayout()` | Function | ~20 | 0 | Calculates one satellite contract payout for one agency. |
| `GetCollectiveSatelliteCount()` | Function | ~17 | 0 | Counts all qualifying satellites from all agencies around one body. |
| `CalculateAchievementCurrentPayout()` | Function | ~22 | 0 | Calculates one achievement payout for one agency. |
| `IsAchievementProgrammeAvailableAtTime()` | Function | ~10 | 0 | Checks whether an achievement was unlocked at a specific KSP time. |
| `GetAchievementAgencyCountAtTime()` | Function | ~22 | 0 | Counts agencies that had completed an achievement by a specific time. |
| `HasProgramAchievedByTime()` | Function | ~15 | 0 | Checks whether one agency had completed an achievement before a funding deadline. |

**Important rule:** Funding due at an old funding date is paid before a newer vessel observation is added. This stops a new achievement from receiving money for a funding date that already passed.

---

## 3.2 `Core/RaceRuntime.cs`

**Simple purpose:** This file keeps the race alive while the player moves between KSP screens. It decides when the fast and slow checks happen.

**Main class:** `RaceRuntime`

The main timings are:

- starter-flight check: about every 1 second;
- normal race update: about every 5 seconds;
- full player vessel scan: about every 20 seconds.

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `Controller` | Property | ~18 | 1 | Gives the UI the controller for the current KSP save. |
| `StarterFlightState` | Property | ~18 | 1 | Gives the UI read-only access to the current starter-flight progress. |
| `Awake()` | Function | ~31 | KSP | Starts this runtime when KSP creates it. It also blocks duplicate copies. |
| `OnDestroy()` | Function | ~8 | KSP | Clears the active runtime reference when KSP removes it. |
| `Update()` | Function | ~73 | KSP | Checks the timers and runs the correct race work when each timer is due. |
| `RefreshStarterFlightState()` | Function | ~85 | 0 | Reads the active vessel, checks starter contracts and saves temporary progress. |
| `EnsureControllerForCurrentGame()` | Function | ~40 | 0 | Creates a new controller and starter tracker when a different KSP save becomes active. |

---

## 3.3 `Core/RaceSettings.cs`

**Simple purpose:** This file stores the default balance numbers. These values are used if the config file does not replace them.

### Class: `RaceBodySettings`

This holds the money and satellite settings for one group of celestial bodies.

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `RaceBodySettings(...)` | Constructor | ~18 | 0 | Creates one set of body balance values. |
| `ProbeProgressCostFunds` | Property | 1 | 2 | Cost of one successful rival probe-development step. |
| `CrewedProgressCostFunds` | Property | 1 | 2 | Cost of one successful rival crewed-development step. |
| `ProbeRewardFunds` | Property | 1 | 2 | Base reward for an uncrewed orbital achievement. |
| `CrewedRewardFunds` | Property | 1 | 2 | Base reward for a crewed orbital achievement. |
| `SatelliteProgressCostFunds` | Property | 1 | 2 | Cost of one successful rival satellite-network step. |
| `SatelliteNetworkSize` | Property | 1 | 2 | Number of satellites needed to fill the network target. |
| `SatelliteNetworkValueFunds` | Property | 1 | 2 | Total funding value of the satellite network. |

### Class: `RaceSettings`

This holds the four body groups and the global rival/funding settings.

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `RaceSettings()` | Static constructor | ~4 | 0 | Loads the built-in defaults when this class is first used. |
| `Kerbin` | Property | 1 | 2 | Balance settings for Kerbin. |
| `KerbinMoons` | Property | 1 | 1 | Balance settings for Mun and Minmus. |
| `InterplanetaryPlanets` | Property | 1 | 1 | Balance settings for planets beyond the Kerbin system. |
| `InterplanetaryMoons` | Property | 1 | 1 | Balance settings for moons of other planets. |
| `FundingIntervalDays` | Property | 1 | 2 | Number of Kerbin days between funding dates. |
| `RivalStartingFunds` | Property | 1 | 2 | Money each rival starts with. |
| `RivalProgressChance` | Property | 1 | 2 | Chance that a rival makes progress at a progress check. |
| `NumberOfRivals` | Property | 1 | 2 | Number of rival agencies to create. |
| `ResetToDefaults()` | Function | ~50 | 1 | Restores all built-in balance values. |
| `GetBodySettings()` | Function | ~36 | 2 | Chooses which balance group a planet or moon should use. |

**Current limitation:** Body names are still listed directly inside `GetBodySettings()`. This is one place that will need work if body support becomes fully data-driven.

---

## 3.4 `Funding/AchievementFundingProgramme.cs`

**Simple purpose:** This file represents one achievement contract. It remembers whether the contract was offered, completed and how many declining payments have already happened.

**Main class:** `AchievementFundingProgramme`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `AchievementFundingProgramme(...)` basic overload | Constructor | ~18 | 0 | Creates a simple achievement contract with default unlock text. |
| `AchievementFundingProgramme(...)` text overload | Constructor | ~19 | 0 | Creates a contract with custom unlock text. |
| `AchievementFundingProgramme(...)` full overload | Constructor | ~20 | 1 | Creates a contract with a real unlock rule. |
| `Id` | Property | 1 | 4 | Internal contract name used by code and saves. |
| `Name` | Property | 1 | 2 | Player-facing contract name. |
| `ObjectiveDescription` | Property | 1 | 1 | Player-facing explanation of the task. |
| `UnlockRequirement` | Property | 1 | 0 | Stored player-facing unlock text. |
| `UnlockRule` | Property | 1 | 2 | The real rule that decides when the contract unlocks. |
| `BaseRewardFunds` | Property | 1 | 1 | Full starting value of the contract. |
| `IsOffered` | Property | 1 | 4 | True after sponsors have offered the contract. |
| `HasStarted` | Property | 1 | 3 | True after at least one agency completes the task. |
| `PaymentsProcessed` | Property | 1 | 1 | Number of contract payments already paid. |
| `IsExpired` | Property | ~4 | 3 | True after all ten payments are finished. |
| `CurrentInterestPercent` | Property | ~15 | 1 | Shows the current value percentage: 100%, 90%, down to 10%. |
| `CurrentTotalPayoutFunds` | Property | ~4 | 1 | Shows the total money available at the next payment. |
| `Offer()` | Function | ~10 | 2 | Marks the contract as offered. |
| `Start()` | Function | ~10 | 1 | Starts the ten-payment countdown after the first completion. |
| `CalculateCurrentPayout()` | Function | ~12 | 1 | Works out one eligible agency's share of the next payment. |
| `AdvancePayout()` | Function | ~10 | 1 | Moves the contract to the next lower payment level. |
| `RestoreState()` | Function | ~6 | 1 | Restores payment progress from a save. |
| `RestoreOfferState()` | Function | ~4 | 1 | Restores whether the contract had been offered. |

---

## 3.5 `Funding/FundingProgramme.cs`

**Simple purpose:** This file represents one satellite-network funding contract. It remembers whether the network is available, offered and has ever reached its target.

**Main class:** `FundingProgramme`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `FundingProgramme(...)` basic overload | Constructor | ~20 | 0 | Creates a simple satellite contract. |
| `FundingProgramme(...)` text overload | Constructor | ~22 | 0 | Creates a contract with custom unlock text. |
| `FundingProgramme(...)` full overload | Constructor | ~20 | 1 | Creates a contract with its real unlock rule. |
| `Id` | Property | 1 | 3 | Internal contract name used by code and saves. |
| `Name` | Property | 1 | 1 | Player-facing network name. |
| `CelestialBodyName` | Property | 1 | 3 | Planet or moon this network belongs to. |
| `RequiredSatellites` | Property | 1 | 2 | Number of satellites needed to fill the target. |
| `RewardFunds` | Property | 1 | 1 | Total money in the network funding pool. |
| `UnlockRequirement` | Property | 1 | 0 | Stored player-facing unlock text. |
| `UnlockRule` | Property | 1 | 2 | The real rule that unlocks the network. |
| `IsAvailable` | Property | 1 | 4 | True when the requirements have been completed. |
| `IsOffered` | Property | 1 | 4 | True when sponsors have offered the network. |
| `HasReachedSatelliteTarget` | Property | 1 | 2 | Remembers whether the full network target has ever been reached. |
| `Unlock()` | Function | ~4 | 1 | Makes the contract available. |
| `Offer()` | Function | ~4 | 1 | Marks the contract as offered. |
| `MarkSatelliteTargetReached()` | Function | ~4 | 1 | Records that the network target was reached. |
| `RestoreAvailability()` | Function | ~4 | 1 | Restores availability from a save. |
| `RestoreOfferState()` | Function | ~5 | 1 | Restores offered and completed-network state from a save. |
| `CalculateCurrentPayout()` | Function | ~31 | 1 | Works out how much of the network fund one agency should receive. |

---

## 3.6 `Funding/PrototypeFundingCatalogue.cs`

**Simple purpose:** This file builds the funding contracts that exist in version 0.5. If a new funding target is added, this is one of the main files to check.

**Main class:** `PrototypeFundingCatalogue`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `CreateAchievementProgrammes()` | Function | ~28 | 1 | Builds a fresh achievement-contract list for a new race. |
| `CreateSatelliteProgrammes()` | Function | ~140 | 1 | Builds a fresh satellite-contract list for a new race. |
| `AddAchievementProgramme()` | Function | ~34 | 0 | Turns one milestone into an achievement funding contract. |
| `CreateSatelliteProgramme()` | Function | ~20 | 0 | Creates one satellite funding contract using body balance settings. |
| `CreateKerbinMoonNetworkUnlockRule()` | Function | ~12 | 0 | Builds the unlock rule used by Mun/Minmus networks. |
| `CreateInterplanetaryPlanetNetworkUnlockRule()` | Function | ~14 | 0 | Builds the unlock rule used by interplanetary planet networks. |
| `CreatePlanetaryMoonNetworkUnlockRule()` | Function | ~12 | 0 | Builds the unlock rule used by moons of other planets. |
| `CreateNetworkProgressCondition()` | Function | ~14 | 0 | Creates the 60%-complete satellite-network requirement. |
| `CreateUnlockRequirementText()` | Function | ~92 | 0 | Turns a technical unlock rule into readable text. |
| `IsSingleAgencyAchievementCondition()` | Function | ~10 | 0 | Checks whether an unlock condition has the simple shape this file expects. |
| `FindRequiredMilestone()` | Function | ~12 | 0 | Finds a milestone and stops with an error if the catalogue points at a missing ID. |

**Current limitation:** The list of supported satellite bodies is written directly in this file.

---

## 3.7 `KspIntegration/CareerFundingAdapter.cs`

**Simple purpose:** This is a very small bridge to KSP's real Career money system.

**Main class:** `CareerFundingAdapter`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `TryAddFunds()` | Function | ~18 | 1 | Safely adds a positive contract reward to the player's KSP Career funds. |

---

## 3.8 `KspIntegration/KspCelestialBodyOrdering.cs`

**Simple purpose:** This file gives planets and moons a stable distance from Kerbin. The Space Race screen uses that distance to keep targets in a sensible order.

**Main class:** `KspCelestialBodyOrdering`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `GetSortDistanceFromKerbin()` | Function | ~76 | 1 | Gives one body a stable sorting distance from Kerbin. |
| `CreatePathToRoot()` | Function | ~27 | 0 | Walks from a moon/planet up through its parent bodies. |
| `SumOrbitRadii()` | Function | ~20 | 0 | Adds orbital distances along part of that parent path. |
| `FindBody()` | Function | ~27 | 0 | Finds a KSP celestial body by name. |
| `GetOrbitRadius()` | Function | ~12 | 0 | Gets a safe orbital radius for sorting. |
| `GetParent()` | Function | ~14 | 0 | Finds the parent body if one exists. |

---

## 3.9 `KspIntegration/KspVesselDiscovery.cs`

**Simple purpose:** This file reads real KSP vessels. It removes the complicated KSP details and gives the rest of the mod simple vessel records.

**Main class:** `KspVesselDiscovery`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `TryCaptureOrbitingVessels()` | Function | ~100 | 1 | Reads all saved orbiting vessels and creates simple orbital snapshots. |
| `TryCaptureActiveVessel()` | Function | ~105 | 1 | Reads only the vessel the player is currently controlling. It only asks KSP for data the active starter contracts need. |
| `TryConsumeActiveVesselSurfaceImpact()` | Function | ~25 | 1 | Gives the runtime one saved crash event and then clears it. |
| `DisableActiveVesselSurfaceImpactTracking()` | Function | ~24 | 1 | Removes crash callbacks when Directed Power no longer needs them. |
| `ResetActiveVesselTracking()` | Function | ~5 | 1 | Clears all active-vessel crash tracking when the KSP save changes. |
| `EnsureActiveTrackingGame()` | Function | ~9 | 0 | Makes sure crash tracking belongs to the current KSP save. |
| `EnsureVesselWillDestroySubscription()` | Function | ~14 | 0 | Connects to KSP's vessel-destroyed event when needed. |
| `TrackActiveVesselDestruction()` | Function | ~25 | 0 | Watches the current vessel for its final destruction callback. |
| `DetachDestructionCallback()` | Function | ~11 | 0 | Stops watching the old vessel. |
| `CaptureDestructionTelemetry()` | Function | ~40 | 0 | Keeps the last useful speed, height and flight state before destruction. |
| `OnVesselWillDestroy()` | Function | ~4 | 0 | Receives KSP's vessel-destroyed event. |
| `RecordPotentialSurfaceImpact()` | Function | ~45 | 0 | Checks whether the destruction looks like a real surface impact. |
| `GetSurfaceClearanceMeters()` | Function | ~19 | 0 | Gets the best available distance between the vessel and the surface. |
| `ConvertSituation()` | Function | ~27 | 0 | Changes a KSP flight-state value into the mod's simpler flight-state value. |
| `ConvertVesselType()` | Function | ~16 | 0 | Changes a KSP vessel type into Probe, Relay or Other. |
| `GetProtoCrewCount()` | Function | ~30 | 0 | Counts crew on an unloaded saved vessel. |
| `IsFinite()` | Function | ~4 | 0 | Rejects invalid numbers such as infinity or NaN. |

---

## 3.10 `KspIntegration/RacePersistenceScenario.cs`

**Simple purpose:** This file is the bridge between KSP's save system and the mod's own save classes.

**Main class:** `RacePersistenceScenario`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `OnLoad()` | Function | ~37 | KSP | Called by KSP when save data is loaded. Reads Race for Space data for a new save. |
| `OnSave()` | Function | ~31 | KSP | Called by KSP when the game is saved. Writes Race for Space data. |
| `TryRestoreCommandCenterVisibility()` | Function | ~12 | 1 | Restores whether the Command Center window was open. |
| `CaptureCommandCenterVisibility()` | Function | ~10 | 1 | Remembers whether the Command Center window is open. |
| `TryRestoreRivalState()` | Function | ~17 | 1 | Restores saved rival information. |
| `TryRestoreRaceProgress()` | Function | ~24 | 1 | Restores player achievements, funding contracts and the next funding date. |
| `TryRestoreActiveContractProgress()` | Function | ~17 | 1 | Restores temporary starter-flight progress. |
| `CaptureRivalState()` | Function | ~15 | 1 | Copies current rival information into the save-state object. |
| `CaptureRaceProgress()` | Function | ~21 | 1 | Copies player and funding progress into the save-state object. |
| `CaptureActiveContractProgress()` | Function | ~16 | 1 | Copies current starter-flight progress into the save-state object. |

---

## 3.11 `KspIntegration/RaceSettingsLoader.cs`

**Simple purpose:** This file reads the user-editable balance config once when the race system starts. Bad or missing values fall back to safe defaults.

**Main class:** `RaceSettingsLoader`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `EnsureLoaded()` | Function | ~74 | 1 | Loads `RaceSettings.cfg` once. |
| `ApplyBodySettings()` | Function | ~32 | 0 | Copies one config section into one body-settings group. |
| `ReadDouble()` | Function | ~34 | 0 | Reads and checks a decimal number from the config. |
| `ReadInt()` | Function | ~25 | 0 | Reads and checks a whole number from the config. |
| `LogInvalidValue()` | Function | ~15 | 0 | Writes a warning when a config value is invalid. |

---

## 3.12 `Milestones/MilestoneDefinition.cs`

**Simple purpose:** This file explains what a milestone looks like. It stores the rules and numbers for orbital milestones and starter contracts.

### Class: `StarterContractCriteria`

This is the small group of numbers that describes one starter-contract target.

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `StarterContractCriteria(...)` | Constructor | ~25 | 0 | Stores one set of starter-contract target values. |
| `RequiredSpeedMetersPerSecond` | Property | 1 | 0 | Required speed for Directed Power. |
| `RequiredMassTonnes` | Property | 1 | 0 | Required final mass for Mass contracts. |
| `RequiredDistanceMeters` | Property | 1 | 0 | Required travel distance for Mass contracts. |
| `MinimumAltitudeMeters` | Property | 1 | 0 | Bottom of a Control altitude band. |
| `MaximumAltitudeMeters` | Property | 1 | 0 | Top of a Control band or Directed Power ceiling. |
| `RequiredDurationSeconds` | Property | 1 | 0 | Time the player must remain in a Control band. |
| `RequiredBiomeName` | Property | 1 | 0 | Required biome for a Biome contract. |
| `DirectedPower()` | Function | ~13 | 1 | Creates Directed Power criteria. |
| `Mass()` | Function | ~13 | 1 | Creates Mass criteria. |
| `Control()` | Function | ~13 | 1 | Creates Control criteria. |
| `Biome()` | Function | ~13 | 1 | Creates Biome criteria. |

### Class: `MilestoneVesselObservation`

This is a tiny description of an orbiting vessel used by normal orbital milestones.

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `MilestoneVesselObservation(...)` | Constructor | ~10 | 1 | Creates a simple orbital-vessel observation. |
| `CelestialBodyName` | Property | 1 | 0 | Body being orbited. |
| `Situation` | Property | 1 | 0 | Required flight situation. Currently this is orbit. |
| `CrewQualification` | Property | 1 | 0 | Says whether the craft counts as uncrewed probe or crewed. |

### Class: `MilestoneDefinition`

This is the main description of one milestone.

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `MilestoneDefinition(...)` orbital overload | Constructor | ~27 | 1 | Creates a normal orbital milestone. |
| `MilestoneDefinition(...)` extended overload | Constructor | ~29 | 0 | Creates a milestone with extra starter-style information but no criteria object. |
| `MilestoneDefinition(...)` full overload | Constructor | ~50 | 1 | Creates the full milestone and copies all starter criteria into properties. |
| `Id` | Property | 1 | 7 | Stable internal milestone name used across most race systems. |
| `Name` | Property | 1 | 2 | Player-facing milestone name. |
| `CelestialBodyName` | Property | 1 | 3 | Body where the milestone happens. |
| `Situation` | Property | 1 | 0 | Required situation for normal milestones. |
| `CrewRequirement` | Property | 1 | 2 | Says whether the milestone needs crew or an uncrewed probe. |
| `ObjectiveDescription` | Property | 1 | 1 | Player-facing task description. |
| `UnlockRule` | Property | 1 | 2 | Rule that must be complete before this milestone can count. |
| `ObjectiveType` | Property | 1 | 1 | Says what kind of objective this is. |
| `StarterLine` | Property | 1 | 3 | Says which starter line this milestone belongs to. |
| `StarterLevel` | Property | 1 | 1 | Starter level number from I to V. |
| `BaseRewardFunds` | Property | 1 | 1 | Base reward used by starter achievement funding. |
| `RivalProgressCostFunds` | Property | 1 | 1 | Rival development-step cost for this starter milestone. |
| `RequiredSpeedMetersPerSecond` | Property | 1 | 2 | Directed Power speed target. |
| `RequiredMassTonnes` | Property | 1 | 2 | Mass target. |
| `RequiredDistanceMeters` | Property | 1 | 2 | Mass travel-distance target. |
| `MinimumAltitudeMeters` | Property | 1 | 2 | Bottom of Control altitude band. |
| `MaximumAltitudeMeters` | Property | 1 | 2 | Top of Control band or Directed Power ceiling. |
| `RequiredDurationSeconds` | Property | 1 | 2 | Required Control hold time. |
| `RequiredBiomeName` | Property | 1 | 2 | Biome target. |
| `IsStarterContract` | Property | ~4 | 4 | Says whether this is one of the special pre-orbit starter contracts. |
| `IsSatisfiedBy()` | Function | ~25 | 1 | Checks whether a normal orbital vessel observation completes this milestone. |
| `CreateObjectiveDescription()` | Function | ~60 | 0 | Builds starter-contract text from the same numbers used by gameplay. |

---

## 3.13 `Milestones/PrototypeMilestones.cs`

**Simple purpose:** This file contains the actual milestone list for version 0.5. It is the main place where starter targets and orbital progression are defined.

**Main class:** `PrototypeMilestones`

### Current starter lines

- **Directed Power:** 600, 1,100, 1,400, 1,700 and 2,000 m/s. Stay at or below 70 km. Then impact Kerbin.
- **Mass:** land with 1 t at 25 km, 2.5 t at 75 km, 5 t at 150 km, 10 t at 300 km, then 20 t at 600 km.
- **Control:** complete a crewed altitude hold, then land safely with crew.
- **Biome:** land in Grasslands, Highlands, Mountains, Deserts, then Ice Caps.

Completing Level V of **any one** starter line unlocks Probe Orbit.

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `All` | Property | ~4 | 2 | Gives the list of normal orbital milestones. |
| `StarterContracts` | Property | ~4 | 1 | Gives the list of twenty starter milestones. |
| `FindById()` | Function | ~14 | 4 | Finds any milestone by its stable ID. |
| `CreateMilestoneIndex()` | Function | ~30 | 0 | Builds the fast milestone lookup table. |
| `CreateStarterMilestone()` | Function | ~32 | 0 | Creates one starter milestone with its common settings. |
| `CreateProbeOrbitUnlockRule()` | Function | ~20 | 0 | Creates the four-way Level V rule that unlocks Probe Orbit. |
| `CreateInterplanetaryProbeUnlockRule()` | Function | ~12 | 0 | Requires both Mun Probe Orbit and Minmus Probe Orbit. |
| `CreateInterplanetaryCrewedUnlockRule()` | Function | ~12 | 0 | Requires both Mun Crewed Orbit and Minmus Crewed Orbit. |

**Current limitation:** The complete body and milestone list is written directly in this file.

---

## 3.14 `Milestones/UnlockRuleDefinition.cs`

**Simple purpose:** This file describes unlock rules. It does not decide whether a rule is complete. It only describes what must happen.

### Class: `UnlockConditionDefinition`

One condition can ask for an achievement, a campaign time, or a satellite count.

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `UnlockConditionDefinition(...)` | Constructor | ~20 | 0 | Stores one unlock condition. |
| `ConditionType` | Property | 1 | 3 | Says what kind of condition this is. |
| `ProgramScope` | Property | 1 | 3 | Says whether player, rivals or any agency may satisfy it. |
| `MilestoneId` | Property | 1 | 3 | Milestone needed by an achievement condition. |
| `RequiredProgramCount` | Property | 1 | 3 | Number of agencies that must meet an achievement condition. |
| `RequiredUniversalTime` | Property | 1 | 2 | KSP time needed by a time condition. |
| `CelestialBodyName` | Property | 1 | 3 | Body used by a satellite-count condition. |
| `RequiredSatelliteCount` | Property | 1 | 3 | Number of satellites needed. |
| `Achievement()` simple overload | Function | ~5 | 2 | Creates a one-agency achievement requirement. |
| `Achievement()` count overload | Function | ~31 | 0 | Creates an achievement requirement that can need several agencies. |
| `AfterUniversalTime()` | Function | ~24 | 0 | Creates a rule that unlocks after a fixed KSP time. |
| `SatelliteCount()` | Function | ~28 | 1 | Creates a rule that needs a number of satellites around one body. |

### Class: `UnlockPathDefinition`

A path is a group of conditions that must all be complete.

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `UnlockPathDefinition(...)` | Constructor | ~8 | 2 | Creates one possible route to unlock something. |
| `Conditions` | Property | ~4 | 3 | Gives the conditions inside that route. |

### Class: `UnlockRuleDefinition`

A rule can contain several paths. Completing any one path unlocks the target.

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `UnlockRuleDefinition(...)` | Constructor | ~8 | 2 | Creates an unlock rule from one or more paths. |
| `Paths` | Property | ~4 | 3 | Gives all possible routes through the rule. |
| `AnyAgencyAchievement()` | Function | ~9 | 2 | Creates the common rule: any agency completes one milestone. |

---

## 3.15 `Milestones/UnlockRuleEvaluator.cs`

**Simple purpose:** This file checks unlock rules. It is the single place that decides whether the rule is complete.

**Main class:** `UnlockRuleEvaluator`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `IsSatisfied()` | Function | ~30 | 2 | Checks whether an entire unlock rule is complete. |
| `IsConditionSatisfied()` | Function | ~29 | 1 | Checks one condition. |
| `GetSatisfiedProgramCount()` | Function | ~34 | 1 | Counts agencies that meet an achievement condition. |
| `GetSatelliteCount()` | Function | ~31 | 1 | Counts all qualifying satellites for a satellite condition. |
| `DoesProgramSatisfyAchievementCondition()` | Function | ~25 | 1 | Checks whether one agency meets one achievement condition at a specific time. |
| `IsPathSatisfied()` | Function | ~20 | 0 | Checks whether every condition in one path is complete. |
| `ProgramMatchesScope()` | Function | ~25 | 0 | Checks whether the agency is allowed by Player, Rival or Any Agency scope. |
| `IsValidEvaluationTime()` | Function | ~6 | 0 | Rejects invalid KSP time values. |

---

## 3.16 `Persistence/ActiveContractProgressSaveState.cs`

**Simple purpose:** This file saves temporary starter-flight information. It lets a partly completed starter flight survive save/load.

**Main class:** `ActiveContractProgressSaveState`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `HasData` | Property | 1 | 1 | Says whether this save-state object contains anything to save. |
| `Capture()` | Function | ~42 | 1 | Copies current starter-flight progress from the tracker. |
| `ApplyTo()` | Function | ~35 | 1 | Restores saved starter-flight progress into the tracker. |
| `Load()` | Function | ~90 | 1 | Reads starter-flight progress from a KSP save node. |
| `Save()` | Function | ~35 | 1 | Writes starter-flight progress to a KSP save node. |
| `ClearState()` | Function | ~19 | 0 | Clears the temporary saved values. |
| `AddDouble()` | Function | ~4 | 0 | Writes a decimal number in a safe save-file format. |
| `ParseBool()` | Function | ~5 | 0 | Reads a true/false value where missing means false. |
| `TryParseBool()` | Function | ~5 | 0 | Tries to read a true/false value and reports whether it worked. |
| `TryParseFiniteDouble()` | Function | ~8 | 0 | Reads a valid decimal number and rejects invalid values. |

### Private saved Control record

These members are only used inside this file, so all have **0 other-file refs**.

| Member | Kind | Approx. lines | Simple purpose |
| --- | --- | ---: | --- |
| `SavedControlContractProgress(...)` | Constructor | ~10 | Creates one saved Control-contract progress record. |
| `MilestoneId` | Property | 1 | Which Control contract this record belongs to. |
| `HoldSeconds` | Property | 1 | Saved hold time. |
| `WasSampleInBand` | Property | 1 | Whether the last sample was inside the altitude band. |
| `IsQualified` | Property | 1 | Whether the required hold had already been completed. |

---

## 3.17 `Persistence/FundingContractsSaveState.cs`

**Simple purpose:** This file saves the player's achievements and all funding-contract progress.

**Main class:** `FundingContractsSaveState`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `HasData` | Property | 1 | 1 | Says whether there is funding state to save. |
| `NextFundingUniversalTime` | Property | 1 | 1 | Stores the next funding date. |
| `Capture()` simple overload | Function | ~4 | 0 | Captures funding state without supplying a funding date. |
| `Capture()` full overload | Function | ~61 | 1 | Copies player achievements, funding contracts and next funding time. |
| `ApplyTo()` | Function | ~67 | 1 | Restores the saved information into fresh runtime objects. |
| `Load()` | Function | ~78 | 1 | Reads funding state from KSP save nodes. |
| `Save()` | Function | ~72 | 1 | Writes funding state to KSP save nodes. |
| `ClearState()` | Function | ~9 | 0 | Clears the internal saved dictionaries. |
| `StorePlayerAchievement()` | Function | ~11 | 0 | Stores the earliest valid time for one player achievement. |
| `TryParseBool()` | Function | ~5 | 0 | Safely reads a true/false value. |
| `TryParsePaymentCount()` | Function | ~18 | 0 | Safely reads and limits the number of processed payments. |
| `TryParseFiniteDouble()` | Function | ~8 | 0 | Safely reads a decimal number. |
| `IsFinite()` | Function | ~4 | 0 | Rejects invalid decimal values. |

### Private saved achievement record

All members below have **0 other-file refs**.

| Member | Kind | Approx. lines | Simple purpose |
| --- | --- | ---: | --- |
| `SavedAchievementContract(...)` | Constructor | ~7 | Creates one saved achievement-contract record. |
| `IsOffered` | Property | 1 | Saved offered state. |
| `HasStarted` | Property | 1 | Saved started state. |
| `PaymentsProcessed` | Property | 1 | Saved number of payments. |

### Private saved satellite record

All members below have **0 other-file refs**.

| Member | Kind | Approx. lines | Simple purpose |
| --- | --- | ---: | --- |
| `SavedSatelliteContract(...)` | Constructor | ~10 | Creates one saved satellite-contract record. |
| `IsAvailable` | Property | 1 | Saved unlocked state. |
| `IsOffered` | Property | 1 | Saved offered state. |
| `HasReachedSatelliteTarget` | Property | 1 | Saved network-target state. |

---

## 3.18 `Persistence/RivalProgramsSaveState.cs`

**Simple purpose:** This file saves every rival agency. Rivals do not have real KSP vessels, so their simulated progress must be saved by the mod.

**Main class:** `RivalProgramsSaveState`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `HasData` | Property | ~4 | 1 | Says whether any rival records exist. |
| `Capture()` | Function | ~29 | 1 | Copies all rival agencies into saved records. |
| `ApplyTo()` | Function | ~24 | 1 | Restores saved records into the current rival list. |
| `Load()` | Function | ~31 | 1 | Reads rival records from the KSP save. |
| `Save()` | Function | ~20 | 1 | Writes rival records to the KSP save. |

### Private class: `SavedRivalProgram`

Everything in this private class is used only by `RivalProgramsSaveState`, so it has **0 other-file refs**.

| Member | Kind | Approx. lines | Simple purpose |
| --- | --- | ---: | --- |
| `HasData` | Property | 1 | Says whether this rival record is valid. |
| `ProgramId` | Property | 1 | Stable ID of the rival. |
| `Funds` | Property | 1 | Saved rival money. |
| `NextMissionTargetId` | Property | 1 | Saved ID of the rival's planned mission. |
| `LaunchProgressPercent` | Property | 1 | Saved mission-development percentage. |
| `NextLaunchProgressCheckUniversalTime` | Property | 1 | Saved time of the next rival progress check. |
| `Capture()` | Function | ~38 | Copies one rival into the saved record. |
| `ApplyTo()` | Function | ~37 | Restores one saved rival. |
| `Load()` | Function | ~60 | Reads one rival from a save node. |
| `Save()` | Function | ~52 | Writes one rival to a save node. |
| `ClearState()` | Function | ~14 | Clears this saved rival record. |
| `StoreAchievement()` | Function | ~11 | Stores the earliest time for a rival achievement. |
| `TryParseFiniteDouble()` | Function | ~14 | Safely reads a decimal value. |
| `IsFinite()` | Function | ~4 | Rejects invalid decimal values. |

---

## 3.19 `Programs/SpaceProgramState.cs`

**Simple purpose:** This file stores the current information for one agency. The same class is used for the player and for rivals.

**Main class:** `SpaceProgramState`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `SpaceProgramState(name, isPlayer)` | Constructor | ~4 | 0 | Creates an agency using its name as its ID. |
| `SpaceProgramState(id, name, isPlayer)` | Constructor | ~7 | 1 | Creates an agency with a separate stable ID and display name. |
| `Id` | Property | 1 | 2 | Stable internal agency ID. |
| `Name` | Property | 1 | 1 | Player-facing agency name. |
| `IsPlayer` | Property | 1 | 4 | Says whether this agency is the human player. |
| `Funds` | Property | 1 | 4 | Simulated money for rivals. The player's real money stays in KSP. |
| `NextPayoutFunds` | Property | 1 | 3 | Expected money at the next funding date. |
| `NextMissionTargetId` | Property | 1 | 2 | Stable ID of a rival's planned mission. |
| `NextMissionDisplayName` | Property | 1 | 3 | Readable name of the rival's planned mission. |
| `LaunchProgressPercent` | Property | 1 | 3 | Rival mission-development percentage. |
| `NextLaunchProgressCheckUniversalTime` | Property | 1 | 2 | Time of the rival's next progress check. |
| `RecordedAchievements` | Property | ~4 | 2 | Lets save code read all achievement IDs and times. |
| `SatelliteCountsByBody` | Property | ~4 | 1 | Lets save code read all simulated satellite counts. |
| `ClearRecordedAchievements()` | Function | ~4 | 2 | Removes all recorded achievements before restoring saved data. |
| `ClearSatelliteCounts()` | Function | ~4 | 2 | Removes all current satellite counts before replacing them. |
| `HasAchievement()` | Function | ~10 | 4 | Checks whether this agency has an achievement. |
| `GetAchievementUniversalTime()` | Function | ~13 | 2 | Gets the first time this agency completed an achievement. |
| `RecordAchievement()` | Function | ~16 | 5 | Records an achievement once. Later completions do not replace the original time. |
| `GetSatelliteCount()` | Function | ~12 | 4 | Gets this agency's current satellite count around one body. |
| `SetSatelliteCount()` | Function | ~16 | 3 | Sets or removes this agency's satellite count around one body. |

---

## 3.20 `Simulation/RivalSimulation.cs`

**Simple purpose:** This file pretends that rival space agencies are planning, paying for and completing missions in the background.

**Main class:** `RivalSimulation`

### Private helper: `RivalSimulationContext`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `RivalSimulationContext(...)` | Constructor | ~11 | 0 | Bundles the current time and contract lists so the simulation can pass them around easily. |

### Main functions

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `Refresh()` | Function | ~31 | 1 | Advances every rival agency to the current KSP time. |
| `GetMissionTargetDisplayName()` | Function | ~20 | 0 | Turns a stable mission ID into readable text. |
| `CalculateLaunchProgressIncrementPercent()` | Function | ~13 | 1 | Returns 20% progress for starter contracts and 10% for normal missions. |
| `CalculateLaunchProgressCost()` | Function | ~17 | 1 | Works out how much the next successful rival progress step costs. |
| `CalculateEstimatedLaunchDays()` public | Function | ~14 | 1 | Starts the rival ETA calculation using the current mission cost. |
| `CalculateEstimatedLaunchDays()` private | Function | ~95 | 0 | Simulates expected progress and future funding to estimate completion time. |
| `RefreshProgram()` | Function | ~78 | 0 | Updates one rival: target choice, progress checks, spending and completion. |
| `TryCompleteLaunch()` | Function | ~57 | 0 | Finishes a rival mission when its progress reaches 100%. |
| `IsTargetAvailable()` | Function | ~39 | 0 | Checks whether a planned mission is still allowed. |
| `ChooseNextMissionTarget()` | Function | ~67 | 0 | Picks a new offered mission for a rival. |
| `CalculateLaunchProgressCostForTarget()` | Function | ~32 | 0 | Chooses the correct cost based on target type and body. |
| `SetMissionTarget()` | Function | ~15 | 0 | Changes a rival's planned mission and readable mission name. |
| `FindAchievementProgramme()` | Function | ~20 | 0 | Finds an achievement contract by ID. |
| `FindFundingProgramme()` | Function | ~20 | 0 | Finds a satellite contract by ID. |
| `IsFinite()` | Function | ~4 | 0 | Rejects invalid numbers. |

---

## 3.21 `Tracking/ActiveVesselTrackingSnapshot.cs`

**Simple purpose:** This file defines the small package of live vessel information used by starter contracts. It also decides which pieces of live KSP telemetry are needed.

### Class: `StarterTelemetryPlan`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `GetRequirements()` | Function | ~39 | 1 | Looks at active starter contracts and asks KSP only for the data those contracts need. |

### Class: `ActiveVesselTrackingSnapshot`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `ActiveVesselTrackingSnapshot(...)` | Constructor | ~33 | 1 | Creates one small live-vessel record. |
| `VesselId` | Property | 1 | 1 | ID of the active vessel. |
| `CelestialBodyName` | Property | 1 | 1 | Body the vessel is near. |
| `Situation` | Property | 1 | 1 | Current flight situation. |
| `AltitudeMeters` | Property | 1 | 1 | Current altitude. |
| `SurfaceSpeedMetersPerSecond` | Property | 1 | 1 | Current speed over the surface. |
| `MassTonnes` | Property | 1 | 1 | Current vessel mass. |
| `LatitudeDegrees` | Property | 1 | 1 | Current latitude. |
| `LongitudeDegrees` | Property | 1 | 1 | Current longitude. |
| `BodyRadiusMeters` | Property | 1 | 1 | Radius of the current body. Used for travel-distance maths. |
| `BiomeName` | Property | 1 | 1 | Current Kerbin biome name when requested. |
| `CrewCount` | Property | 1 | 1 | Number of crew aboard when requested. |
| `LaunchUniversalTime` | Property | 1 | 1 | KSP time when the launch started. |
| `ObservationUniversalTime` | Property | 1 | 1 | KSP time when this snapshot was taken. |

---

## 3.22 `Tracking/VesselTrackingSnapshot.cs`

**Simple purpose:** This is a much smaller vessel record used by the slower orbital scan.

**Main class:** `VesselTrackingSnapshot`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `VesselTrackingSnapshot(...)` | Constructor | ~9 | 1 | Creates one simple orbiting-vessel record. |
| `CelestialBodyName` | Property | 1 | 1 | Body being orbited. |
| `VesselType` | Property | 1 | 1 | Probe, Relay or Other. |
| `CrewCount` | Property | 1 | 1 | Number of crew aboard. |

---

## 3.23 `Tracking/SatelliteTracker.cs`

**Simple purpose:** This file uses the simple orbital vessel records. It updates player satellite counts and records normal orbital achievements.

**Main class:** `SatelliteTracker`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `RefreshPlayerSatelliteCounts()` | Function | ~73 | 1 | Rebuilds the player's satellite counts and checks orbital milestones. |
| `EvaluateMilestones()` | Function | ~54 | 0 | Keeps checking newly unlocked milestones until the same vessel snapshot cannot unlock anything else. |

---

## 3.24 `Tracking/StarterFlightTracker.cs`

**Simple purpose:** This file checks the four pre-orbit contract lines. It remembers one active launch and the progress made during that launch.

**Main class:** `StarterFlightTracker`

### Properties

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `HasActiveAttempt` | Property | 1 | 2 | Says whether a starter-contract flight is currently being tracked. |
| `VesselId` | Property | 1 | 1 | ID of the vessel being tracked. |
| `CelestialBodyName` | Property | 1 | 2 | Body where the attempt is happening. |
| `LaunchUniversalTime` | Property | 1 | 1 | Launch start time. |
| `StartLatitudeDegrees` | Property | 1 | 1 | Latitude where the attempt started. |
| `StartLongitudeDegrees` | Property | 1 | 1 | Longitude where the attempt started. |
| `LastSampleUniversalTime` | Property | 1 | 1 | Time of the last live-vessel sample. |
| `MaximumAltitudeMeters` | Property | 1 | 2 | Highest altitude reached during the attempt. |
| `MaximumSurfaceSpeedMetersPerSecond` | Property | 1 | 2 | Highest surface speed reached during the attempt. |
| `CurrentAltitudeMeters` | Property | 1 | 1 | Latest altitude for the UI. |
| `CurrentSurfaceSpeedMetersPerSecond` | Property | 1 | 0 | Latest surface speed. Currently kept for state visibility but not read by another production file. |
| `CurrentMassTonnes` | Property | 1 | 1 | Latest vessel mass for the UI. |
| `CurrentDistanceMeters` | Property | 1 | 1 | Latest distance from launch for the UI. |
| `CurrentBiomeName` | Property | 1 | 1 | Latest biome for the UI. |
| `CurrentCrewCount` | Property | 1 | 1 | Latest crew count for the UI. |
| `CurrentSituation` | Property | 1 | 1 | Latest flight situation for the UI. |
| `EnteredOrbit` | Property | 1 | 2 | Remembers whether this attempt ever entered orbit. |
| `ControlStateMilestoneIds` | Property | 1 | 1 | Gives save code the IDs of active Control progress records. |

### Functions

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `GetControlHoldSeconds()` | Function | ~9 | 2 | Gets the current hold time for one Control contract. |
| `IsControlMilestoneQualified()` | Function | ~9 | 2 | Says whether one Control hold is finished and only needs the final landing. |
| `IsControlSampleInBand()` | Function | ~9 | 1 | Says whether the last sample was inside one Control altitude band. |
| `RestoreControlState()` | Function | ~17 | 1 | Restores one saved Control progress record. |
| `RefreshPlayerMilestones()` | Function | ~137 | 1 | Main live-flight check. Updates the attempt and checks Mass, Biome and Control contracts. |
| `RecordSurfaceImpact()` | Function | ~58 | 1 | Checks Directed Power contracts when the tracked vessel crashes into Kerbin. |
| `RestoreState()` | Function | ~65 | 1 | Restores the saved history of a starter-flight attempt. |
| `ClearAttempt()` | Function | ~22 | 1 | Clears the current starter-flight attempt. |
| `EvaluateControlMilestones()` | Function | ~62 | 0 | Updates the independent hold state for every active Control contract. |
| `GetOrCreateControlState()` | Function | ~12 | 0 | Finds or creates the small progress record for one Control contract. |
| `ResetUnqualifiedControlStates()` | Function | ~17 | 0 | Clears incomplete Control holds after a bad sample gap or invalid flight state. |
| `IsSameAttempt()` | Function | ~25 | 0 | Decides whether a new vessel snapshot belongs to the same launch, including staging. |
| `BeginAttempt()` | Function | ~17 | 0 | Starts tracking a new launch. |
| `CalculateSurfaceDistanceMeters()` | Function | ~31 | 0 | Calculates distance over the planet surface from the launch point. |
| `IsFinite()` | Function | ~4 | 0 | Rejects invalid numbers. |

### Starter-contract rules in simple words

- **Directed Power:** reach the speed, never go above the altitude ceiling, do not orbit, then crash into Kerbin.
- **Mass:** land far enough away while the final craft still has enough mass.
- **Control:** keep crew inside the altitude band for the full time, then land with crew.
- **Biome:** land in the required biome without entering orbit.

---

## 3.25 `Tracking/SurfaceImpactEvaluator.cs`

**Simple purpose:** KSP sometimes changes vessel values during a crash. This file uses the last good flight information to decide whether the destruction was probably a real surface impact.

**Main class:** `SurfaceImpactEvaluator`

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `IsEligible()` | Function | ~55 | 1 | Checks whether the destruction looks like a recent, fast, near-surface impact. |
| `IsFinite()` | Function | ~4 | 0 | Rejects invalid numbers. |

---

## 3.26 `UI/RaceWindow.cs`

**Simple purpose:** This file draws the whole Command Center. It reads race information and shows it. It does not advance the race.

**Main class:** `RaceWindow`

### Private helper class: `SpaceRaceFundingEntry`

This helper only exists inside `RaceWindow`, so every member below has **0 other-file refs**.

| Member | Kind | Approx. lines | Simple purpose |
| --- | --- | ---: | --- |
| `SpaceRaceFundingEntry(...)` | Constructor | ~15 | Creates one UI entry for either an achievement or satellite contract. |
| `AchievementProgramme` | Property | 1 | Holds the achievement contract when this is an achievement entry. |
| `SatelliteProgramme` | Property | 1 | Holds the satellite contract when this is a satellite entry. |
| `CelestialBodyName` | Property | 1 | Body used for sorting. |
| `BodySortDistance` | Property | 1 | Distance used for sorting from Kerbin outward. |
| `CatalogueOrder` | Property | 1 | Original order used when two entries otherwise sort the same. |
| `IsAchievement` | Property | ~4 | Says whether this UI entry contains an achievement contract. |
| `Id` | Property | ~4 | Gets the stable ID of whichever contract this entry contains. |
| `Name` | Property | ~4 | Gets the display name of whichever contract this entry contains. |

### Main `RaceWindow` functions

Most functions are private UI helpers, so they have **0 other-file refs**. `Awake`, `Update`, `OnGUI` and `OnDestroy` are called directly by KSP/Unity.

| Member | Kind | Approx. lines | Other-file refs | Simple purpose |
| --- | --- | ---: | ---: | --- |
| `Awake()` | Function | ~39 | KSP | Sets up the window when KSP creates it. Blocks duplicate windows. |
| `OnDestroy()` | Function | ~21 | KSP | Removes the launcher button and clears window references. |
| `Update()` | Function | ~59 | KSP | Keeps the window connected to the current save and handles F8. |
| `CreateCenteredWindowRect()` | Function | ~7 | 0 | Creates the starting window position in the middle of the screen. |
| `EnsureApplicationLauncherButton()` | Function | ~40 | 0 | Adds the mod button to KSP's stock launcher. |
| `SetCommandCenterVisible()` | Function | ~25 | 0 | Opens or closes the window and remembers that choice. |
| `OnGUI()` | Function | ~28 | KSP | Asks Unity to draw the Command Center when it is visible. |
| `DrawWindow()` | Function | ~82 | 0 | Draws the main tabs and chooses which page to show. |
| `DrawOverview()` | Function | ~122 | 0 | Draws the player's current objectives, networks and next payout. |
| `DrawFundingTargets()` | Function | ~40 | 0 | Draws all currently offered funding contracts. |
| `DrawAchievementFundingCard()` | Function | ~95 | 0 | Draws one achievement-contract information card. |
| `DrawStarterFundingLiveProgress()` | Function | ~28 | 0 | Adds live starter-flight progress to an offered starter contract card. |
| `DrawSatelliteFundingCard()` | Function | ~78 | 0 | Draws one satellite-network information card. |
| `DrawPayoutLinesByAmount()` | Function | ~51 | 0 | Shows agency payouts with the biggest amount first. |
| `DrawRivalAgencies()` | Function | ~27 | 0 | Draws the scrollable rival-agency page. |
| `DrawHelpGuide()` | Function | ~39 | 0 | Draws the built-in player help text. |
| `DrawSpaceRace()` | Function | ~63 | 0 | Draws the full contract catalogue grouped by Offered, Unlocked, Locked and Expired. |
| `DrawStarterLiveProgress()` | Function | ~105 | 0 | Draws the correct live values for Directed Power, Mass, Control or Biome. |
| `DrawSelectedSpaceRaceFundingEntry()` | Function | ~47 | 0 | Draws details for the contract selected on the Space Race page. |
| `DrawSpaceRaceFundingSection()` | Function | ~72 | 0 | Draws one collapsible contract group. |
| `EnsureSpaceRaceFundingEntries()` | Function | ~60 | 0 | Builds the combined UI list of achievement and satellite contracts when needed. |
| `CompareSpaceRaceFundingEntries()` | Function | ~25 | 0 | Sorts Space Race entries from Kerbin outward. |
| `EnsureSelectedSpaceRaceFundingEntry()` | Function | ~48 | 0 | Keeps a valid selected contract or chooses a sensible first one. |
| `GetSpaceRaceFundingCategory()` | Function | ~31 | 0 | Decides whether a contract belongs under Offered, Unlocked, Locked or Expired. |
| `DrawUnlockRuleProgress()` | Function | ~45 | 0 | Draws all paths and conditions in an unlock rule. |
| `DrawUnlockConditionProgress()` | Function | ~73 | 0 | Draws progress for one achievement, date or satellite-count condition. |
| `AppendAchievementConditionText()` | Function | ~70 | 0 | Builds readable text for an achievement unlock condition. |
| `FindFirstProgramSatisfyingCondition()` | Function | ~22 | 0 | Finds an agency that has completed the displayed condition. |
| `GetMilestoneDisplayName()` | Function | ~9 | 0 | Converts a milestone ID into its readable name. |
| `DrawProgramCard()` | Function | ~122 | 0 | Draws one rival's money, mission progress, ETA and income. |
| `EnsurePayoutScratchBuffers()` | Function | ~16 | 0 | Keeps small reusable arrays ready for payout display. |
| `GetProgramDisplayName()` | Function | ~10 | 0 | Returns `Player` for the player or the rival's agency name. |
| `DrawCenteredCardTitle()` | Function | ~8 | 0 | Draws a centered heading on a card. |
| `FormatNextFundingDate()` | Function | ~19 | 0 | Creates the readable next-funding-date sentence. |
| `FormatKerbinDate()` | Function | ~16 | 0 | Changes KSP time into `Year X, Day Y` text. |

---

## 3.27 `TheRaceForSpace.csproj`

**Simple purpose:** This is not gameplay code. It tells .NET how to build the DLL.

**File lines:** 92

It does four main jobs:

1. Builds the mod for `.NET Framework 4.7.2`, which matches KSP 1.12.x.
2. Finds the KSP and Unity DLL files from the local `KSP_ROOT` folder.
3. Stops with a useful error if the KSP files cannot be found.
4. Can copy the finished mod DLL and config into KSP when `DeployToKsp=true` is used.

There are no C# functions or properties in this file because it is an MSBuild project file.

---

# 4. Config and scripts

## `GameData/TheRaceForSpace/Config/RaceSettings.cfg`

**File lines:** 55

**Simple purpose:** This is the user-editable balance file. It changes funding timing, rival settings, rewards, costs and satellite-network sizes without changing C# code.

The four balance groups are:

- Kerbin;
- Kerbin moons;
- interplanetary planets;
- interplanetary moons.

---

## `tools/run-logic-tests.sh`

**File lines:** 18

**Simple purpose:** Runs both standalone test programs. It is the normal quick regression-test command.

---

## `tools/test-prototype.sh`

**File lines:** 95

**Simple purpose:** Helps a developer switch to a chosen existing branch, update it, build the mod and copy it into a Linux KSP installation for live testing. It refuses to switch branches when there are uncommitted changes.

---

## `.github/workflows/logic-tests.yml`

**File lines:** 24

**Simple purpose:** Runs the same logic tests automatically on GitHub after pushes and pull requests.

---

# 5. Test files

The test code does **not** run inside KSP. It exists to catch mistakes when gameplay code changes.

The detailed member/reference tables above cover production code under `src/TheRaceForSpace/`. Test functions are not included in the reference counts because that would make heavily tested code look more tightly connected to the game than it really is.

## 5.1 `tests/TheRaceForSpace.Tests/`

| File | File lines | Simple purpose |
| --- | ---: | --- |
| `TheRaceForSpace.Tests.csproj` | 39 | Builds the KSP-independent test program. It links real production logic directly. |
| `Program.cs` | 684 | Runs the main logic tests and reports pass/fail results. |
| `ConfigNode.cs` | 48 | Small fake version of KSP's save-node class so save code can be tested without KSP. |
| `PrototypeFundingCatalogueTests.cs` | 674 | Checks that the funding catalogue contains the expected contracts and rules. |
| `Milestones/MilestoneEvaluationTests.cs` | 80 | Checks normal orbital milestone matching. |
| `Persistence/CollectionPersistenceTests.cs` | 391 | Checks save/load of arbitrary IDs, rival data and malformed save data. |
| `Simulation/RivalSimulationCollectionTests.cs` | 485 | Checks rival target choice, repeated satellite missions, costs and completion. |
| `Tracking/SatelliteTrackerTests.cs` | 247 | Checks satellite counts and orbital milestone recording. |
| `Tracking/StarterFlightTrackerTests.cs` | 576 | Checks all four starter-contract lines, staging and save/load progress. |
| `Tracking/SurfaceImpactEvaluatorTests.cs` | 150 | Checks valid and invalid crash detection. |

## 5.2 `tests/TheRaceForSpace.ControllerTests/`

| File | File lines | Simple purpose |
| --- | ---: | --- |
| `TheRaceForSpace.ControllerTests.csproj` | 35 | Builds the controller test program without a real KSP installation. |
| `Program.cs` | 161 | Runs all controller and unlock-rule tests. |
| `ControllerKspStubs.cs` | 165 | Provides small fake KSP systems for time, funds, vessels and save readiness. |
| `UnlockRuleEvaluatorTests.cs` | 354 | Checks AND/OR unlock rules, agency scopes, dates and satellite counts. |
| `UnlockConsumerIntegrationTests.cs` | 70 | Checks that rivals only choose contracts that sponsors have actually offered. |
| `SatelliteRaceControllerTests.cs` | 447 | Checks funding timing, rival setup, vessel refreshes and payout behaviour. |
| `ActiveStarterContractPlanTests.cs` | 219 | Checks the cached list of starter contracts that should be active. |
| `StarterTelemetryPlanTests.cs` | 76 | Checks that each starter line asks KSP only for the telemetry it needs. |
| `ActiveStarterEvaluationTests.cs` | 299 | Checks that several offered starter levels can be tracked independently. |
| `FundingOfferControllerTests.cs` | 586 | Checks sponsor reviews, offer limits and the four-way path to Probe Orbit. |

---

# 6. Where to change common things

| What you want to change | Start here | Simple reason |
| --- | --- | --- |
| Starter speed, mass, altitude or biome targets | `Milestones/PrototypeMilestones.cs` | The actual starter targets are written here. |
| How starter flights are judged | `Tracking/StarterFlightTracker.cs` | This file decides whether the flight completed the contract. |
| Crash detection | `Tracking/SurfaceImpactEvaluator.cs` | This file decides whether destruction counts as an impact. |
| Orbital milestone list | `Milestones/PrototypeMilestones.cs` | The orbital targets and progression are written here. |
| Unlock logic | `Milestones/UnlockRuleDefinition.cs` and `UnlockRuleEvaluator.cs` | One describes rules and the other checks them. |
| Funding contract list | `Funding/PrototypeFundingCatalogue.cs` | The current funding targets are created here. |
| Achievement payout maths | `Funding/AchievementFundingProgramme.cs` | This file owns declining one-off payouts. |
| Satellite payout maths | `Funding/FundingProgramme.cs` | This file owns satellite-network payouts. |
| Funding dates and sponsor review behaviour | `Competition/SatelliteRaceController.cs` | The controller decides when funding and offers happen. |
| Rival behaviour | `Simulation/RivalSimulation.cs` | Rival target choice, spending and progress happen here. |
| Balance numbers | `RaceSettings.cfg` and `Core/RaceSettings.cs` | Config values replace the built-in defaults. |
| Reading KSP vessels | `KspIntegration/KspVesselDiscovery.cs` | This is the main bridge from real KSP vessels to project data. |
| Save/load of funding | `Persistence/FundingContractsSaveState.cs` | This file stores player achievement and funding state. |
| Save/load of rivals | `Persistence/RivalProgramsSaveState.cs` | This file stores rival state. |
| Save/load of an active starter flight | `Persistence/ActiveContractProgressSaveState.cs` | This file stores temporary flight progress. |
| Command Center layout | `UI/RaceWindow.cs` | The whole window is drawn here. |
| Add/remove target bodies | `PrototypeMilestones.cs` and `PrototypeFundingCatalogue.cs` | The current body lists are still written directly in code. |

---

# 7. What is already easy to expand

Some parts of the code are already flexible:

- `SpaceProgramState` can remember any milestone ID.
- `SpaceProgramState` can remember satellite counts for any body name.
- Save files store milestones and bodies by name/ID instead of fixed fields.
- Unlock rules can combine achievements, dates and satellite counts.
- Rival simulation reads the current contract lists instead of only knowing three fixed targets.
- The Space Race UI builds its list from the controller's current contract lists.

---

# 8. What is still hardcoded

These areas still have fixed lists in C#:

1. `PrototypeMilestones.cs` contains the full milestone/body list.
2. `PrototypeFundingCatalogue.cs` contains the full satellite-funding body list.
3. `RaceSettings.GetBodySettings()` chooses a balance group by specific stock body names.
4. `RaceSettings.cfg` has four broad body groups rather than a separate section for every possible body.
5. The first two rivals are named Aster and Cobalt in the controller. Extra rivals use generic names.

These are the main places to look when the project moves toward fully expandable targets and celestial bodies.

---

# 9. Important save rules

Keep these rules in mind when changing code:

- Do not casually change stable IDs. Old saves use them.
- The first achievement time is important. Later completions must not replace it.
- Player satellite counts come from real KSP vessels. They are rebuilt from the game.
- Rival satellite counts are simulated, so the mod must save them.
- Temporary starter-flight progress is saved separately from funding-contract progress.
- Moving between KSP scenes is not the same as loading a different save.

---

# 10. Important performance rules

Some code runs often:

- `RaceRuntime.Update()` is checked every Unity frame.
- Active starter-flight data can be sampled every second.
- The normal race controller updates about every five seconds.
- The full saved-vessel scan runs about every twenty seconds.
- `RaceWindow.OnGUI()` may be called several times while Unity draws one frame.

For that reason, do not add heavy vessel searches or large new allocations to these paths without a clear need.

---

# 11. Simple checklist before changing code

1. Read `AGENTS.md`.
2. Find the file that already owns the behaviour.
3. Change the existing code rather than creating a second system.
4. Keep KSP-specific vessel code inside `KspIntegration/` where practical.
5. Keep the UI as a display layer. Do not make the UI advance race progress.
6. Keep stable IDs and save compatibility in mind.
7. Update the relevant tests when gameplay rules change.
8. Do not create a new branch without permission.
