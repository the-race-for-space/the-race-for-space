# Development To-Do

This file tracks current development ideas and polish work only. Completed items are removed once finished; implementation history remains available in Git and the current project documentation. Items are not listed in priority order unless stated otherwise.

## Funding Targets

- [ ] **Order Funding Targets by player completion state.** Show objectives the player has not completed at the top of the list, with player-completed objectives moved to the bottom so current work is easier to find.

## Flight UI

- [ ] **Display funding reward beside each contract.** Show the contract's funding reward in the compact Offered Contracts window so the player can see the value of the objective without opening the full Command Center.

## Notifications

- [ ] **Announce completed sponsor reviews.** When a sponsor review makes new funding targets `Offered`, notify the player and identify the newly offered targets.
- [ ] **Announce rival objective completions.** Notify the player when a rival agency completes an objective so important competitive progress is visible without continuously checking the Rival Agencies view.
- [ ] **Announce funding payouts received.** When a funding boundary pays the player's agency, notify the player of the funds received from the campaign funding system.

## Help / tutorial

- [ ] **Add a Pre-Orbit line quick guide.** Add a short player-facing explanation of Directed Power, Mass, Control, and Biome, including the basic purpose of each progression line.
- [ ] **Explain funding sharing with one worked example.** Add one simple example showing how a one-off objective payout is shared when multiple agencies are eligible, so the declining funding and competition rules are easier to understand.

## Content

### Base and station progression

- [ ] **Add three Desert Base tiers on Kerbin.** Create Desert Base I, II, and III as progressively harder long-duration surface-base objectives.
- [ ] **Add three Polar Base tiers on Kerbin.** Create Polar Base I, II, and III as progressively harder long-duration surface-base objectives in Kerbin's polar region.
- [ ] **Unlock Kerbin orbital bases from either surface programme.** Completing either Desert Base III or Polar Base III should unlock the Kerbin orbital-base programme.
- [ ] **Add three Kerbin Orbital Base tiers.** Create Kerbin Orbital Base I, II, and III as the next progression step after the Kerbin surface-base demonstration.
- [ ] **Unlock Mun and Minmus base funding from the Kerbin orbital-base programme.** Completing the required Kerbin orbital-base progression should open the Mun and Minmus base-funding programmes.
- [ ] **Add three Mun Orbital Base funding tiers.** Create Mun Orbital Base I, II, and III as recurring funding contracts tied to maintaining qualifying orbital infrastructure around the Mun.
- [ ] **Add three Mun Ground Base funding tiers.** Create Mun Ground Base I, II, and III as recurring funding contracts tied to maintaining qualifying landed infrastructure on the Mun.
- [ ] **Add three Minmus Orbital Base funding tiers.** Create Minmus Orbital Base I, II, and III as recurring funding contracts tied to maintaining qualifying orbital infrastructure around Minmus.
- [ ] **Add three Minmus Ground Base funding tiers.** Create Minmus Ground Base I, II, and III as recurring funding contracts tied to maintaining qualifying landed infrastructure on Minmus.
- [ ] **Define the twelve Mun/Minmus recurring funding contracts.** Balance the eligibility criteria, tier progression, payout values, and loss/resumption rules for the three orbital and three ground-base tiers on each body.

### Science expeditions and rival tech progression

- [ ] **Add a rival Launch Science Expedition system.** Allow each rival agency to prepare one science expedition independently of its normal launch programme. Science-expedition launch preparation and normal mission launch preparation should be able to progress at the same time.
- [ ] **Give each rival its own stored Science balance.** Science earned from successful rival science expeditions should be recorded against that agency and remain available for later technology purchases.
- [ ] **Model a rival version of the stock tech tree.** Give each rival agency its own technology progression based on the stock KSP tech tree, with Science spent to unlock tech nodes.
- [ ] **Use rival technology to gate agency capabilities.** A rival should only be able to select launches, destinations, experiments, and other activities supported by the technologies it has unlocked.
- [ ] **Create Launch Science Expeditions from stock experiments.** Rival science expeditions should represent completing specific stock science experiments in valid situations and locations. Preparing an expedition to 100% launches it; Science is awarded only if the resulting live science mission later succeeds.
- [ ] **Choose expeditions only from unlocked experiments and locations.** A rival may select a Launch Science Expedition only when the experiment has been unlocked by its technology and the body, situation, and biome are inside its achieved expedition access. The selected science subject must also be one the rival has not already completed.
- [ ] **Gate expedition locations by achieved mission capability.** Rivals may begin with accessible Kerbin surface expeditions, but new situations and celestial bodies should become available only after the agency has demonstrated the required mission progress; for example, Low Kerbin Space after completing a probe-orbit objective, Mun orbital science after first orbiting the Mun, and Mun surface science after first landing there.
- [ ] **Track a clear rival Expedition Range.** Represent the locations currently available to the rival in a player-readable range such as `Kerbin only`, `Kerbin, Mun and Minmus`, or `Planets available`, while still enforcing the more detailed body/situation/biome mission gates internally.
- [ ] **Progress Launch Science Expeditions once per Kerbin day.** Each science-launch preparation receives one progress check every Kerbin day. A successful check adds 10 percentage points to `Launch Progress`, so ten successful checks are required to advance from 0% to 100% and launch the expedition.
- [ ] **Derive science-launch Progress Chance from the SPH and Runway.** The Spaceplane Hangar and Runway each contribute 20% at Level 1, giving a default combined `Progress Chance - daily` of 40%. Each Level 2 facility adds another +3% and each Level 3 facility adds another +3%. Store and expose the authoritative calculated total so the simulation, UI, and ETA use the same value.
- [ ] **Estimate science-expedition launch time from remaining successful checks.** Calculate the average remaining launch-preparation duration as `remaining 10% Launch Progress steps / daily success chance`. At 0% progress and the default 40% daily chance, ten successful checks are required and the expected launch time is 25 Kerbin days. This is an average estimate rather than a guaranteed launch date.
- [ ] **Move a science expedition into Live Mission Progress at 100% Launch Progress.** Reaching 100% should create a launched live science mission with its own mission duration, completion date, success chance, and failure chance. Do not award Science or consume the player's matching science subject at the moment of launch.
- [ ] **Remove successful rival discoveries from the player's science pool.** When a launched rival science expedition reaches the end of its live mission and succeeds, award the Science to the rival and consume that same stock science subject from the science pool available to the player so the player can no longer earn its Science. For example, a successful Crew Report expedition in Kerbin's Shores biome removes that Crew Report science subject from the player's available pool.
- [ ] **Select a new valid Launch Science Expedition after the previous expedition launches.** Launch preparation can begin for another valid science subject while the previously launched expedition is still running as a live mission, subject to any later limits placed on simultaneous missions.
- [ ] **Show rival science-launch capability and current target to the player.** The Rival Agencies interface should show the rival's unlocked science experiments, current Expedition Range, current experiment/body/situation/biome target, `Launch Progress`, `Progress Chance - daily`, estimated launch, Science reward, and Kerbals assigned.
- [ ] **Simulate a next rival research project.** Once a rival has enough stored Science for an eligible tech node, select a next research project and deduct that node's Science cost from the rival's stored balance when research begins.
- [ ] **Use a fixed 90-day rival research period.** A selected tech project takes 90 campaign days to research and becomes unlocked at the first campaign funding boundary on or after the 90-day research period has elapsed. Persist the project, Science cost, start date, and eligible completion funding date through save/load.
- [ ] **Show the next rival research project under Stored Science.** In the Rival Agencies Tech Tree section, show the selected tech, its Science cost, research status, and funding-date completion/ETA directly beneath the rival's Stored Science value. Use `Paid` to show that the Science cost has already been deducted.
- [ ] **Define remaining Expedition and Science spending rules.** Balance any expedition Funds cost, how rivals choose between valid science subjects, how they choose which eligible tech node to research next, and how science activity interacts with sponsor/funding progression.

#### Live rival mission simulation

- [ ] **Add a Live Mission Progress phase after launch.** Normal rival missions and Launch Science Expeditions should no longer complete their gameplay result immediately when launch preparation reaches 100%. At launch, create a persistent live-mission record and move it into the Rival Agencies `Live Mission Progress` section.
- [ ] **Use only Contract or Science as the live Mission Type.** In the player-facing live mission list, a normal funding/objective mission is `Contract` and a science expedition is `Science`. Keep the values short and do not expose internal mission-class names in this column.
- [ ] **Use a fixed live-mission duration lookup by target location.** Mission Type should not independently change duration. A Contract and a Science mission targeting the same location should use the same configured live duration. Each supported Kerbin biome, KSC location biome, and celestial body has one manually assigned travel time in the tables below.
- [ ] **Treat the duration tables as authoritative balance data.** Do not calculate travel time from physical distance, orbital distance, launch windows, phase angles, live planet positions, or vessel state. The simulation should look up the configured value for the target location and use it directly.
- [ ] **Keep location durations configurable and stable.** Use stable project-owned location IDs so these manual travel times can be balanced later without changing mission logic or save interpretation. Unknown locations should fail safely rather than borrowing an unrelated travel time.
- [ ] **Give every launched mission persistent timing and difficulty data.** Store the launch date, configured base duration, elapsed time, expected completion date, mission type, target, assigned Kerbals, mission difficulty from 1 to 10, and any science subject/reward data required by science expeditions.
- [ ] **Do not complete a rival mission at launch.** Reaching 100% `Launch Progress` only starts the live mission. A Contract remains incomplete, and a Science expedition grants no Science, until the configured live duration has finished and the mission passes its final success check.
- [ ] **Use mission difficulty to determine final Success Chance.** Every live Contract and Science mission uses a difficulty rating from 1 to 10. Contract missions use the contract's difficulty. Science missions derive difficulty from the science target using the lookup tables below. Difficulty 1 has a 90% Success Chance, and each additional difficulty level reduces Success Chance by 5 percentage points, down to 45% at difficulty 10. Use `Success Chance = 95% - (Difficulty × 5%)` for valid difficulty values 1–10.
- [ ] **Use biome-specific science difficulty only on Kerbin.** Kerbin surface Science missions use the target Kerbin biome to choose their 1–10 difficulty. Maintain one compact Kerbin biome difficulty table for Shores, Water, Grasslands, Highlands, Mountains, Deserts, Badlands, Tundra, Ice Caps, Northern Ice Shelf, and Southern Ice Shelf. Do not create equivalent per-biome difficulty tables for other celestial bodies.
- [ ] **Use fixed KSC location-biome science settings.** Science missions targeting KSC location biomes use a fixed 5-day live duration and Difficulty 1. Keep the KSC locations listed explicitly in the table below so these special local science targets do not inherit the wider Shores duration.
- [ ] **Use one science difficulty per non-Kerbin body.** Mun, Minmus, the planets, their moons, and other non-Kerbin science destinations each use one body-wide 1–10 difficulty regardless of the target biome. The biome may still be tracked as part of the exact science subject and shown in the UI, but it does not modify Success Chance outside Kerbin.
- [ ] **Use the Kerbin Orbit science setting for orbital science around Kerbin.** Kerbin Orbit uses a 10-day live duration and Difficulty 3 for science missions that use the Kerbin-orbit target rather than a surface biome or KSC location biome.
- [ ] **Keep science difficulty configuration compact.** The science difficulty balance data should consist of regular Kerbin biome entries, the fixed KSC location-biome rule, Kerbin Orbit, and one entry for each non-Kerbin celestial body. Do not maintain a database containing every biome on every moon and planet.
- [ ] **Resolve the mission with one outcome roll after its duration ends.** When the expected completion date is reached, make one random check against the mission's Success Chance. A successful check completes the result; otherwise the mission fails. The result must not be rolled or known before the live duration has elapsed.
- [ ] **Apply rewards only after a successful live mission.** On a successful Contract mission, mark the objective complete and apply any related satellite/infrastructure result. On a successful Science mission, award the expedition's Science to the rival, record the subject as completed, and consume the matching player science subject. No successful gameplay result is granted before this point.
- [ ] **Lose the spacecraft when a live mission fails.** A failed Contract or Science mission grants no objective completion, satellite/infrastructure result, or Science reward, and its simulated spacecraft is lost.
- [ ] **Apply a 25% Kerbal-loss chance on failed crewed missions.** If a failed mission has one or more Kerbals assigned, make a 25% crew-loss check. When this triggers, record a Kerbal loss for the rival. Multi-Kerbal loss handling should not exceed the explicitly defined rule until it is balanced separately.
- [ ] **Charge 50,000 Funds insurance after a Kerbal loss.** A Kerbal loss caused by a failed live mission creates a 50,000 Funds insurance payout for the rival. Do not deduct it immediately; subtract the 50,000 Funds from that rival's income at the next campaign funding boundary and persist the pending deduction through save/load.
- [ ] **Track each rival's employed Kerbal roster.** Persist the number of living Kerbals employed by each rival, the number currently assigned to live missions, and therefore the number available for new launches. Kerbals assigned to a live mission remain unavailable until that mission resolves.
- [ ] **Require Kerbals for crewed Contracts and all Science missions.** Any Contract defined as crewed must reserve its required Kerbals at launch. Every Science mission requires at least one Kerbal and must reserve its configured crew requirement at launch. Uncrewed Contracts do not consume Kerbal roster capacity.
- [ ] **Use the Astronaut Complex as the roster cap.** The rival may never employ more living Kerbals than its current Astronaut Complex level permits: Level 1 = 3 Kerbals, Level 2 = 8 Kerbals, Level 3 = no limit.
- [ ] **Hire missing Kerbals only when a mission is ready to launch.** When Launch Progress reaches 100%, first check whether enough Kerbals are available. If not, and the rival is below its Astronaut Complex roster cap and can pay the cost, hire the missing Kerbal or Kerbals for 100,000 Funds each, add them to the employed roster, and assign them immediately to the launch.
- [ ] **Wait at 100% Launch Progress when crew cannot be provided.** If a crew-required mission is ready but the rival is already at its Astronaut Complex roster cap, does not have enough available Kerbals, or cannot afford a required 100,000-Funds hire, keep the mission ready at 100% Launch Progress and do not launch it. Launch as soon as enough existing Kerbals return or an eligible hire can be made.
- [ ] **Return surviving Kerbals after live missions resolve.** When a live mission ends, surviving assigned Kerbals return to the rival's available roster. On a failed crewed mission, apply the existing 25% Kerbal-loss rule first; any Kerbals not lost become available again.
- [ ] **Charge 10,000 Funds payroll per living employed Kerbal at every funding boundary.** Count all living Kerbals on the rival's roster, including Kerbals currently on live missions. Deduct `10,000 Funds × employed Kerbals` from that rival's funding income at each campaign funding boundary and persist the roster/payroll state through save/load.
- [ ] **Show roster and payroll as part of the rival's programme economy.** The Rival Agencies UI should show employed Kerbals versus the current Astronaut Complex limit, Kerbals currently on missions, Kerbals available, the next Kerbal payroll deduction, and any pending insurance deduction so the player can understand why gross funding and net funding differ.
- [ ] **Define any additional simultaneous uncrewed-mission limits.** Kerbal-requiring mission capacity is limited by the available roster. Decide separately whether uncrewed Contract missions also need an overall simultaneous-mission cap from Mission Control or another facility.
- [ ] **Persist live missions across save/load and time warp.** Live mission completion and outcome resolution must be deterministic from stored dates/state and must correctly catch up if the player time-warps past a completion date or reloads after it.

##### Mission difficulty success table

| Difficulty | Success Chance | Failure Chance |
| ---: | ---: | ---: |
| 1 | 90% | 10% |
| 2 | 85% | 15% |
| 3 | 80% | 20% |
| 4 | 75% | 25% |
| 5 | 70% | 30% |
| 6 | 65% | 35% |
| 7 | 60% | 40% |
| 8 | 55% | 45% |
| 9 | 50% | 50% |
| 10 | 45% | 55% |

##### Science difficulty lookup scope

The configured values below are authoritative balance data. Keep this lookup deliberately small.

| Scope | Difficulty entries |
| --- | --- |
| **KSC location biomes** | Every listed KSC location biome uses 5 days and Difficulty 1. |
| **Kerbin surface** | One difficulty per regular Kerbin biome. |
| **Kerbin Orbit** | One orbital setting: 10 days and Difficulty 3. |
| **All other bodies** | One body-wide difficulty per target body. No per-biome difficulty entries. |

##### Kerbin biome live-duration and science-difficulty database

These are manually assigned campaign travel times and science difficulties. The runtime should use these values directly rather than calculating physical distance.

| Travel Rank | Kerbin Biome | Base Live Duration | Science Difficulty |
| ---: | --- | ---: | ---: |
| 1 | Shores | 10 days | 1 |
| 2 | Water | 10 days | 1 |
| 3 | Grasslands | 10 days | 1 |
| 4 | Highlands | 10 days | 1 |
| 5 | Mountains | 20 days | 3 |
| 6 | Deserts | 30 days | 2 |
| 7 | Badlands | 70 days | 4 |
| 8 | Tundra | 80 days | 3 |
| 9 | Ice Caps | 90 days | 4 |
| 10 | Northern Ice Shelf | 100 days | 4 |
| 10 | Southern Ice Shelf | 100 days | 4 |

##### KSC location-biome science database

All KSC location-biome Science missions use a fixed 5-day live duration and Difficulty 1.

| KSC Location Biome | Base Live Duration | Science Difficulty |
| --- | ---: | ---: |
| KSC | 5 days | 1 |
| Administration | 5 days | 1 |
| Astronaut Complex | 5 days | 1 |
| Crawlerway | 5 days | 1 |
| Flag Pole | 5 days | 1 |
| Launch Pad | 5 days | 1 |
| Mission Control | 5 days | 1 |
| R&D | 5 days | 1 |
| R&D Central Building | 5 days | 1 |
| R&D Corner Lab | 5 days | 1 |
| R&D Main Building | 5 days | 1 |
| R&D Observatory | 5 days | 1 |
| R&D Side Lab | 5 days | 1 |
| R&D Small Lab | 5 days | 1 |
| R&D Tanks | 5 days | 1 |
| R&D Wind Tunnel | 5 days | 1 |
| Runway | 5 days | 1 |
| SPH | 5 days | 1 |
| SPH Main Building | 5 days | 1 |
| SPH Round Tank | 5 days | 1 |
| SPH Tanks | 5 days | 1 |
| SPH Water Tower | 5 days | 1 |
| Tracking Station | 5 days | 1 |
| Tracking Station Dish East | 5 days | 1 |
| Tracking Station Dish North | 5 days | 1 |
| Tracking Station Dish South | 5 days | 1 |
| Tracking Station Hub | 5 days | 1 |
| VAB | 5 days | 1 |
| VAB Main Building | 5 days | 1 |
| VAB Pod Memorial | 5 days | 1 |
| VAB Round Tank | 5 days | 1 |
| VAB South Complex | 5 days | 1 |
| VAB Tanks | 5 days | 1 |

##### Celestial body live-duration and science-difficulty database

These are manually assigned campaign travel times from Kerbin and body-wide Science difficulties. The values are authoritative balance data; no stock reference orbit, transfer calculation, or non-Kerbin biome difficulty lookup is required at runtime.

| Travel Rank | Target | Base Live Duration | Science Difficulty |
| ---: | --- | ---: | ---: |
| 1 | Kerbin Orbit | 10 days | 3 |
| 2 | Mun | 30 days | 4 |
| 3 | Minmus | 50 days | 4 |
| 4 | Sun | 78 days | 5 |
| 5 | Moho | 124 days | 9 |
| 6 | Eve | 171 days | 10 |
| 7 | Gilly | 174 days | 5 |
| 8 | Duna | 303 days | 7 |
| 9 | Ike | 303 days | 5 |
| 10 | Dres | 604 days | 5 |
| 11 | Jool | 1,123 days | 7 |
| 12 | Laythe | 1,124 days | 8 |
| 13 | Vall | 1,124 days | 8 |
| 14 | Tylo | 1,125 days | 8 |
| 15 | Bop | 1,128 days | 8 |
| 16 | Pol | 1,131 days | 8 |
| 17 | Eeloo | 1,587 days | 10 |

The tables are intended as configuration data rather than hard-coded branching. A future implementation should keep stable location IDs and read the duration and Science difficulty directly from project-owned campaign settings or a dedicated project-owned lookup.

#### Rival stock tech tree

```text
0 Science
└─ Start (Crew Report, Mystery Goo Observation)

5 Science
├─ Basic Rocketry
└─ Engineering 101 (Temperature Scan)

15 Science
└─ Survivability (Atmospheric Pressure Scan)

18 Science
└─ Stability

20 Science
└─ General Rocketry

45 Science
├─ Aviation
├─ Basic Science (Materials Study)
├─ Flight Control
├─ Advanced Rocketry
└─ General Construction

90 Science
├─ Propulsion Systems
├─ Space Exploration
├─ Advanced Flight Control
├─ Landing
├─ Aerodynamics
├─ Electrics
├─ Heavy Rocketry
├─ Fuel Systems
├─ Advanced Construction
└─ Miniaturization (EVA Science)

160 Science
├─ Actuators
├─ Command Modules
├─ Heavier Rocketry
├─ Precision Engineering
├─ Advanced Exploration
├─ Specialized Control
├─ Advanced Landing
├─ Supersonic Flight
├─ Adv. Fuel Systems
├─ Advanced Electrics
├─ Specialized Construction
├─ Precision Propulsion
└─ Advanced Aerodynamics

300 Science
├─ Heavy Landing
├─ Scanning Tech (Atmosphere Analysis, SENTINEL Infrared Telescope)
├─ Unmanned Tech
├─ Nuclear Propulsion
├─ Advanced MetalWorks
├─ Field Science
├─ High Altitude Flight
├─ Large Volume Containment
├─ Composites
├─ Electronics (Seismic Scan, Magnetometer Report)
├─ High-Power Electrics
└─ Heavy Aerodynamics

550 Science
├─ Ion Propulsion
├─ Hypersonic Flight
├─ Nanolathing
├─ Advanced Unmanned Tech
├─ Meta-Materials
├─ Very Heavy Rocketry
├─ Advanced Science Tech (Gravity Scan)
├─ Advanced Motors
├─ Specialized Electrics
├─ High-Performance Fuel Systems
├─ Experimental Aerodynamics
└─ Automation

1000 Science
├─ Aerospace Tech
├─ Large Probes
├─ Experimental Science
├─ Experimental Motors
└─ Experimental Electrics
```

`EVA Report` and `Surface Sample` are not unlocked by a stock tech-tree node; rival access to those subjects should be gated separately by expedition/mission capability rules.

### Rival Space Centre progression

- [ ] **Simulate each rival building and upgrading its own Space Centre.** Give every rival persistent facility levels based on the nine separately upgradable stock Career facilities. Rivals should begin with Level 1 facilities and spend their own Funds to improve them through Level 2 and Level 3 rather than receiving all programme capabilities automatically.
- [ ] **Simulate rival facility construction progress.** A facility upgrade should remain under construction for its full build period, with its previous level remaining active until construction completes.
- [ ] **Start facility construction only at funding times.** Rivals should evaluate and begin eligible Space Centre upgrades when a campaign funding boundary is processed rather than starting construction at arbitrary times between funding events.
- [ ] **Charge rival Funds when construction starts.** Upgrading a facility from Level 1 to Level 2 costs 100,000 Funds. Upgrading from Level 2 to Level 3 costs 250,000 Funds. Construction may only begin if the rival can pay the full upgrade cost.
- [ ] **Use fixed rival facility construction times.** Level 1 to Level 2 takes 180 campaign days. Level 2 to Level 3 takes 270 campaign days. Record the construction start date, completion date, source level, and target level so progress survives save/load correctly.
- [ ] **Define rival construction scheduling.** Decide how rivals choose which eligible facility to upgrade at each funding boundary and whether an agency may have more than one Space Centre facility under construction at the same time.
- [ ] **Use facility levels as capability gates.** Launches, expeditions, technology purchases, crew activity, mission planning, and strategic behaviour should check the relevant simulated rival facilities as well as the rival's tech and mission progression.
- [ ] **Show rival Space Centre development to the player.** Add facility levels, construction progress, and any upgrade currently under construction to the Rival Agencies interface so the player can see how each competing programme is developing.

#### Rival facility construction rules

| Upgrade | Cost | Construction time | May start |
| --- | ---: | ---: | --- |
| **Level 1 → Level 2** | 100,000 Funds | 180 days | At a campaign funding boundary, if the rival has sufficient Funds |
| **Level 2 → Level 3** | 250,000 Funds | 270 days | At a campaign funding boundary, if the rival has sufficient Funds |

All nine stock Career facilities have three upgrade levels: Level 1, Level 2, and Level 3. The table below records the stock progression and the intended role for the rival simulation.

| Facility | Stock Level 1 | Stock Level 2 | Stock Level 3 | Rival simulation role |
| --- | --- | --- | --- | --- |
| **Administration Building** | 1 active strategy; 25% maximum commitment | 3 active strategies; 60% maximum commitment | 5 active strategies; 100% maximum commitment | Sets the rival's base funding income: Level 1 = 10,000 Funds, Level 2 = 20,000 Funds, Level 3 = 40,000 Funds. |
| **Astronaut Complex** | Roster limit 5; no off-Kerbin EVA | Roster limit 12; off-Kerbin EVA and flag planting available | Unlimited roster | Limits the rival's simulated Kerbal roster: Level 1 = 3 Kerbals, Level 2 = 8 Kerbals, Level 3 = no limit. A crew-required mission that reaches 100% Launch Progress may hire missing Kerbals for 100,000 Funds each only if this rival roster cap permits it; otherwise launch waits for a Kerbal to return. |
| **Mission Control** | Maximum 2 active contracts; no flight planning | Maximum 7 active contracts; flight planning available when navigation requirements are met | Unlimited active contracts | Limits the number of rival satellites that may be launched/maintained: Level 1 = 3 satellites, Level 2 = 8 satellites, Level 3 = no limit. |
| **Research and Development** | May unlock tech nodes costing up to 100 Science | May unlock tech nodes costing up to 500 Science; surface sampling/resource transfer capability becomes available with the other requirements met | No tech-node Science-cost limit | Directly gates the rival stock tech tree. Level 1 permits nodes through 90 Science, Level 2 permits nodes through 300 Science, and Level 3 permits the 550- and 1000-Science nodes. Also gates Surface Sample expeditions. |
| **Vehicle Assembly Building (VAB)** | 30-part craft limit; no action groups | 255-part craft limit; basic action groups | Unlimited parts; full action groups | Modifies rival launch development and can be used as a mission-body capability gate: Level 1 = Launch Progress Chance +15%; Level 2 = additional Launch Progress Chance +3%; Level 3 = additional Launch Progress Chance +3%. |
| **Spaceplane Hangar (SPH)** | 30-part craft limit; no action groups | 255-part craft limit; basic action groups | Unlimited parts; full action groups | Modifies Launch Science Expedition preparation: Level 1 = Science Launch Progress Chance +20%; Level 2 = additional +3%; Level 3 = additional +3%. |
| **Launch Pad** | Small launch vehicle size/mass limit; about 18 t maximum mass | Medium launch vehicle limits; about 140 t maximum mass | Unlimited stock size/mass | Modifies rival launch development: Level 1 = Launch Progress Chance +15%; Level 2 = additional Launch Progress Chance +3%; Level 3 = additional Launch Progress Chance +3%. |
| **Runway** | Small aircraft size/mass limit; about 18 t maximum mass | Medium aircraft limits; about 140 t maximum mass | Unlimited stock size/mass | Modifies Launch Science Expedition preparation: Level 1 = Science Launch Progress Chance +20%; Level 2 = additional +3%; Level 3 = additional +3%. |
| **Tracking Station** | Basic orbital tracking | Patched-conic/navigation capability | Adds unowned-object tracking and full stock tracking capability | Limits rival launch destinations: Level 1 = Kerbin only; Level 2 = Kerbin orbit and Kerbin's moons; Level 3 = no destination limit, allowing missions to other planets. |

The stock Flag Pole, Crawlerway, water tower, tanks, and other KSC scenery are not separately upgradable programme facilities, so they do not need independent rival simulation states.

### Rival Agencies UI redesign

- [ ] **Redesign only the Rival Agencies tab around current rival activity.** Preserve the existing rival Funds, next mission, launch preparation, launch-progress cost, launch ETA, funding income, completed objectives, satellite-network income, and total next payout while adding live missions, science-launch preparation, research, tech-tree, facility, construction, Kerbal roster, and payroll information.
- [ ] **Rename preparation progress to Launch Progress.** The existing normal rival mission `Mission Progress` display and the former science `Expedition Progress` display should both use `Launch Progress`, because reaching 100% launches the mission rather than immediately completing its gameplay result.
- [ ] **Make normal launch Progress Chance a main visible stat.** Show `Progress Chance - each 5 days` as the authoritative combined value from the VAB and Launch Pad. With both at Level 1 the default is 30% (15% + 15%); Level 2 on either facility adds +3%, and Level 3 on either facility adds another +3%. The UI should display the calculated total rather than reconstructing it independently.
- [ ] **Rename the Science Expedition card to Launch Science Expedition.** Show `Launch Progress`, `Progress Chance - daily`, and `Estimated Launch` beside the target. The authoritative science-launch chance is the combined SPH + Runway value: 40% at the default Level 1/Level 1 facilities, giving an average 25-day launch-preparation time from 0%.
- [ ] **Show science-expedition capability above the launch target.** The Launch Science Expedition section should show unlocked experiments and current Expedition Range before the current experiment/body/situation/biome target.
- [ ] **Show crew readiness on launch-preparation cards.** For any crew-required Contract or Science launch, show the required Kerbals and whether existing crew are available. At 100% Launch Progress, display `Ready`, `Hire At Launch`, or `Waiting For Kerbal` as appropriate; a mission must not move into Live Mission Progress until its crew has actually been assigned.
- [ ] **Add Live Mission Progress immediately below Programme Status.** This should be the highest-priority detailed section in the rival card and list every launched normal mission and launched science expedition currently underway.
- [ ] **Show Live Mission Progress as a compact row-and-column list.** Use the columns `Item`, `Location`, `Mission Type`, `Duration`, `Progress`, `ETA`, and `Success Chance`. `Item` is the contract name for a normal mission or the experiment name for a science expedition. `Location` is the body and biome where relevant. `Mission Type` must be only `Contract` or `Science`. Do not show Status, Outcome, or Failure Chance in this list.
- [ ] **Keep science mission identity readable in the live-mission list.** For science rows, use the experiment as `Item`, include the body/biome in `Location`, and use `Science` as the Mission Type. For objective/funding rows, use the contract name as `Item` and `Contract` as the Mission Type. The detailed science reward and subject state remain part of the simulation even though the compact live list does not need extra columns for them.
- [ ] **Use mission difficulty as the source for the live Success Chance column.** Display the calculated Success Chance from the mission's 1–10 difficulty rating; do not independently calculate or invent a different UI percentage.
- [ ] **Show Kerbal staffing in Programme Status.** Display `Kerbals Employed` against the Astronaut Complex roster limit, `Kerbals On Mission`, `Kerbals Available`, and `Next Kerbal Payroll` so crew capacity is visible before the player scans individual missions.
- [ ] **Show rival funding deductions explicitly.** In the Funding section, show gross next income first, then negative lines for `Kerbal Payroll` and any `Pending Insurance`, followed by the resulting `Total Next Payout` so recurring staffing costs are visible rather than hidden inside the total.
- [ ] **Prioritize the rival card as Programme Status → Live Mission Progress → launch preparation → construction → facilities → Tech Tree/Research → Funding.** Place Current Launch Programme and Launch Science Expedition together after live missions so the player can distinguish missions already underway from missions still being prepared.
- [ ] **Use title case instead of all-caps UI headings and wording.** Rival Agencies headings, section titles, facility levels, construction states, research states, and other display wording should use normal title case rather than all-capital text. Standard acronyms such as ETA, VAB, and SPH may remain uppercase.
- [ ] **Show one rival card cleanly and reuse the layout for additional rivals.** The card should be readable as a self-contained programme dashboard so the same component can be repeated for however many rivals are configured.
- [ ] **Show facility capabilities after every facility level.** Do not display only `Level 1/2/3`; immediately state the capability the current level provides, such as Kerbal limit, satellite limit, launch/science-launch chance modifiers, tech-cost ceiling, base funding, or destination access.
- [ ] **Show ongoing construction with a clear ETA.** Display facility, source/target level, elapsed construction days, remaining days/ETA, and paid cost. Do not include explanatory text stating that the old level remains active while construction is underway.
- [ ] **Show researched techs clearly and keep the long tree collapsible.** The compact Tech Tree view should emphasize researched nodes and their experiment unlocks, with a `Show Full Tech Tree` control for the complete progression.

#### Rival Agencies UI text example

Mission duration values in this mock-up use the configured location-duration table. Success Chance values are derived from the mission difficulty table.

```text
Rival Agencies                                      Next Funding: Year 2, Day 120

══════════════════════════════ Kerbal Dynamics ═══════════════════════════════════════

Programme Status

Funds:               186,500              Stored Science:          72
Kerbals Employed:          3 / 8           Satellites:               4 / 8
Kerbals On Mission:        3               Kerbals Available:        0
Next Kerbal Payroll:  30,000 Funds
Total Next Payout:    12,000


════════════════════════ Live Mission Progress ═══════════════════════════════════════

Item                    Location              Mission Type   Duration   Progress      ETA       Success Chance
──────────────────────  ────────────────────  ─────────────  ─────────  ────────────  ────────  ──────────────
Kerbin Crewed Orbit     Kerbin Orbit          Contract       10 days    Day 4 / 10    6 days    80%
Mystery Goo             Kerbin / Highlands    Science        10 days    Day 2 / 10    8 days    90%


┌─ Current Launch Programme ─────────────────────────────────────────────────────────┐
│ Next Mission:                    Mun Probe Orbit                                    │
│ Launch Progress:                 60%                                                │
│ Progress Chance - each 5 days:   36%                                                │
│ Progress Cost:                   25,000 Funds                                       │
│ Estimated Launch:                80 days                                            │
│ Kerbals Required:                0                                                  │
└─────────────────────────────────────────────────────────────────────────────────────┘

┌─ Launch Science Expedition ────────────────────────────────────────────────────────┐
│ Expedition Range:                Kerbin, Mun and Minmus                            │
│ Unlocked Experiments:            Crew Report, Mystery Goo,                        │
│                                  Temperature Scan, Pressure Scan,                  │
│                                  Materials Study                                   │
│                                                                                     │
│ Status:                           Preparing                                          │
│ Experiment:                       Temperature Scan                                  │
│ Target:                           Kerbin - Shores                                   │
│ Situation:                        Landed                                            │
│ Launch Progress:                  40%                                               │
│ Progress Chance - daily:          40%                                               │
│ Estimated Launch:                 15 days                                           │
│ Science Reward:                   2.4 Science                                       │
│ Kerbals Required:                 1                                                 │
│ Kerbals Available:                0                                                 │
│ Crew At Launch:                   Hire At Launch - 100,000 Funds                    │
└─────────────────────────────────────────────────────────────────────────────────────┘


┌─ Space Centre Construction ────────────────────────────────────────────────────────┐
│ Research & Development                                                             │
│ Level 1  ───────────────────────────────►  Level 2                                 │
│                                                                                     │
│ Progress:       Day 74 / 180                                                        │
│ ETA:            106 days                                                           │
│ Cost:           100,000 Funds - Paid                                               │
└─────────────────────────────────────────────────────────────────────────────────────┘


Space Centre Facilities

Administration Building     Level 2
  └─ Base funding: 20,000 Funds per funding period

Astronaut Complex           Level 2
  └─ Maximum rival Kerbals: 8
     New hire: 100,000 Funds per Kerbal when required at launch

Mission Control             Level 2
  └─ Maximum satellites: 8

Research & Development      Level 1       [Upgrading → Level 2]
  └─ May research tech nodes costing up to 90 Science

Vehicle Assembly Building   Level 2
  └─ Launch Progress Chance +15% base
     Additional +3% at Level 2

Launch Pad                  Level 2
  └─ Launch Progress Chance +15% base
     Additional +3% at Level 2

Spaceplane Hangar           Level 1
  └─ Science Launch Progress Chance +20%

Runway                      Level 1
  └─ Science Launch Progress Chance +20%

Tracking Station            Level 2
  └─ Missions permitted around Kerbin, Mun and Minmus


Tech Tree

Stored Science: 72
Next Research Project: Advanced Rocketry
Science Cost: 45 [Paid]
Research Time: 90 days
Status: In Progress
Completion: Next eligible funding date
ETA: 38 days

Researched

  ✓ Start
      └─ Crew Report
      └─ Mystery Goo Observation

  ✓ Basic Rocketry

  ✓ Engineering 101
      └─ Temperature Scan

  ✓ Survivability
      └─ Atmospheric Pressure Scan

  ✓ Stability

  ✓ General Rocketry

  ✓ Basic Science
      └─ Materials Study

------------------------------------------------------------
[ Show Full Tech Tree ]
------------------------------------------------------------


Funding & Existing Programme Information

Base Income                                      20,000

Completed Objective Funding
  Probe Orbit                                     8,000
  Crewed Kerbin Orbit                             6,000

Satellite Network Funding
  Kerbin Satellites: 4 / 8                        8,000

Gross Next Income                                42,000
Kerbal Payroll (3 × 10,000)                     -30,000
Pending Insurance                                     0
                                                   ──────
Total Next Payout                                 12,000
```

## Rival Programme Data Design

This section records the agreed implementation direction for the rival-programme work above. It is design guidance only; implementing it will change rival save/config data and therefore still requires the normal `AGENTS.md` structural-change approval before code changes begin.

### Locked design decisions

- [ ] **Use `RivalProgramState` composition for rival-only mutable state.** Keep common agency identity, Funds, objective completions, satellite counts, and the existing normal Contract launch-preparation fields on `AgencyState`. Give rival agencies a composed `RivalProgramState` for Science, live missions, Kerbals, facilities, construction, research, and other rival-only systems rather than making `AgencyState` carry every rival field.
- [ ] **Persist an outcome seed for every launched live mission.** Create and store a deterministic seed when the mission launches. When its duration ends, use that seed to generate the one Success Chance roll. Do not store a pre-rolled success/failure result, and do not use non-deterministic completion-time RNG that can change after reload.
- [ ] **Allow one rival facility construction project at a time initially.** Represent construction as a collection-capable state so the schema can support more than one project later, but enforce a maximum of one active construction project for the initial implementation.
- [ ] **Use a fixed project-owned stock rival tech catalogue.** Mirror the approved stock tech progression in project-owned definitions instead of reading the installed KSP tech tree dynamically. Rival tech behaviour should therefore remain deterministic and should not silently change because another mod replaces the player's tech tree.
- [ ] **Store new rival balance data in the existing `CampaignSettings.cfg`.** Extend the current settings/config loader rather than introducing another balance file. Mission-location durations/difficulties, Kerbal costs, facility values, construction values, research timing, and other tuneable rival rules should use explicit named settings or config nodes.
- [ ] **Use first completion wins for shared Science subjects.** A Science subject remains available until either the player or a rival successfully completes it. If the player earns the subject before a rival live Science mission finishes, the later rival success receives no Science from that already-depleted subject. If the rival succeeds first, award the Science to that rival and consume the matching player subject at that completion time. Launching an expedition does not reserve the subject.

### Recommended runtime state ownership

Keep the existing module ownership: `Agencies/` owns mutable agency state, `Rivals/` owns rival simulation behaviour, `Persistence/` owns save-state transforms, `Core/` owns balance settings, `KspIntegration/` owns raw KSP science access/config loading, `Campaign/` coordinates funding boundaries, and `UI/` remains read-only presentation. Do not create a new top-level source module for this design.

Recommended `RivalProgramState` contents:

| State | Recommended representation | Notes |
| --- | --- | --- |
| Stored Science | `double` | Persisted rival Science balance. |
| Science launch preparation | `ScienceLaunchPreparationState` | One current expedition being prepared independently of normal Contract launch preparation. |
| Live missions | `List<RivalLiveMissionState>` | Contains all launched Contract and Science missions still awaiting resolution. |
| Completed rival Science subjects | `HashSet<ScienceSubjectKey>` | Prevents a rival repeating its own completed subjects. |
| Researched tech | `HashSet<string>` | Stable project-owned tech IDs. |
| Current research | `RivalResearchProjectState` | One active research project. |
| Facility levels | `Dictionary<RivalFacilityType, int>` | Nine rival Space Centre facilities. |
| Facility construction | `List<RivalFacilityConstructionState>` | Initially enforce zero or one active entry. |
| Kerbals employed | `int` | Living employed roster count. |
| Pending insurance | `double` | Funding deduction carried to the next funding boundary. |

Keep the existing normal rival Contract preparation fields on `AgencyState` initially so implementation does not require migrating working Contract-launch state merely for symmetry.

### Science subject identity

Use one explicit project-owned `ScienceSubjectKey` containing:

```text
ExperimentId
BodyName
Situation
BiomeName
```

The exact biome remains part of the Science subject even when a non-Kerbin body's difficulty is body-wide. For a non-biome-specific stock subject, `BiomeName` may be empty/null. Save these fields explicitly rather than packing them into a delimiter-separated string.

Recommended Science preparation state:

```text
ScienceLaunchPreparationState
  Subject
  ScienceReward
  LaunchProgressPercent
  NextProgressCheckUniversalTime
  RequiredKerbals
```

Do not persist Progress Chance, Estimated Launch, Expedition Range, unlocked-experiment display text, Kerbals Available, or the `Ready`/`Hire At Launch`/`Waiting For Kerbal` UI state. Derive those values from authoritative facility, tech, mission, roster, Funds, and launch-preparation state.

### Live mission state

Use a simple data class rather than an inheritance hierarchy:

```text
RivalLiveMissionState
  MissionSequence
  MissionType                 // Contract or Science
  ContractId                  // Contract missions
  ScienceSubject              // Science missions
  LocationId
  LaunchUniversalTime
  DurationDays
  CompletionUniversalTime
  Difficulty
  SuccessChancePercent
  AssignedKerbalCount
  ScienceReward               // Science missions
  OutcomeSeed
```

Snapshot `DurationDays`, `Difficulty`, and `SuccessChancePercent` when the mission launches. A later balance/config edit must not alter a mission already in flight. Derive elapsed progress and ETA from current universal time, launch time, and completion time rather than persisting changing progress/ETA values.

At 100% normal or Science Launch Progress, the simulation should check/assign crew, create the live-mission record, reset the relevant preparation state, and begin the next eligible preparation. Objective completion, satellite/infrastructure results, and Science rewards occur only when the live mission later resolves successfully.

### Kerbal roster calculations

Persist `KerbalsEmployed`; derive the rest:

```text
Kerbals On Mission = sum of AssignedKerbalCount across active live missions
Kerbals Available  = Kerbals Employed - Kerbals On Mission
Kerbal Payroll     = Kerbals Employed × 10,000 Funds per funding boundary
Roster Limit       = current Astronaut Complex level rule
```

Do not persist a separate waiting-for-crew boolean. A 100%-prepared crew-required mission is `Ready`, `Hire At Launch`, or `Waiting For Kerbal` based on the current roster cap, available crew, required crew, and available Funds.

Individual rival Kerbal names/identities are not required for the initial model; roster counts and assigned counts are sufficient unless a later design explicitly needs named rival Kerbals.

### Facility and construction state

Use a stable `RivalFacilityType` enum for the nine simulated facilities:

```text
Administration
AstronautComplex
MissionControl
ResearchAndDevelopment
VehicleAssemblyBuilding
LaunchPad
SpaceplaneHangar
Runway
TrackingStation
```

Store only facility levels. Derive funding, roster caps, satellite caps, destination gates, tech-cost gates, normal Launch Progress Chance, and Science Launch Progress Chance from those levels.

Recommended construction state:

```text
RivalFacilityConstructionState
  Facility
  SourceLevel
  TargetLevel
  StartUniversalTime
  CompletionUniversalTime
  CostPaidFunds
```

Construction progress and ETA remain derived. Although the state may be held in a list for future flexibility, the initial simulation must permit only one active facility construction project per rival.

### Rival tech and research data

Use fixed project-owned definitions such as:

```text
RivalTechNodeDefinition
  TechId
  DisplayName
  ScienceCost
  PrerequisiteTechIds
  UnlockedExperimentIds
```

Use stable tech IDs as gameplay identity and keep researched IDs in a `HashSet<string>`. Do not modify the player's stock Research and Development state to represent rival technology.

Recommended active research state:

```text
RivalResearchProjectState
  TechId
  ScienceCostPaid
  StartUniversalTime
  ResearchReadyUniversalTime
  EligibleCompletionFundingUniversalTime
```

The rival's stored Science is deducted when research starts, and the UI derives status/ETA from this state.

### Mission-location balance lookup

Read the approved location duration and Science difficulty tables from the existing campaign settings into a project-owned lookup keyed by stable location ID, for example:

```text
ksc:vab
ksc:tracking-station
kerbin:Shores
kerbin:Mountains
kerbin:orbit
body:Mun
body:Minmus
body:Duna
```

Recommended value object:

```text
RivalMissionLocationSettings
  LocationId
  DurationDays
  ScienceDifficulty
```

Resolution order for Science targets:

```text
KSC location science  -> exact KSC location entry
Kerbin surface        -> Kerbin biome entry
Kerbin orbit          -> Kerbin Orbit entry
Non-Kerbin science    -> body-wide entry; biome does not change difficulty
```

The lookup supplies values when a mission launches; the live mission then keeps its snapped values.

### Contract definition additions

Add explicit Contract difficulty and crew requirement data to objective definitions rather than deriving it from display wording. Recommended fields are:

```text
Difficulty            // 1-10
RequiredKerbalCount
```

Keep the existing crew-category enum where it remains useful, but use the explicit required count for rival crew reservation so future multi-Kerbal objectives do not need another data-model redesign.

### Funding calculation structure

Create one authoritative calculated funding breakdown for both gameplay and UI:

```text
RivalFundingBreakdown
  BaseIncome
  ObjectiveIncome
  SatelliteIncome
  GrossIncome
  KerbalPayroll
  InsuranceDeduction
  NetPayout
```

Administration level supplies Base Income. Payroll and insurance are deductions. `NextPayoutFunds`, launch affordability/ETA projections, funding-boundary payment, and the Rival Agencies UI should consume the same calculation rather than independently reconstructing totals.

### Persistence design

Keep the existing top-level `RIVAL_AGENCIES` save section and extend each stable-ID `RIVAL` record. Do not create another ScenarioModule save section merely for the new rival systems.

Recommended nested save records include:

```text
RIVAL
  scalar Funds / Science / Kerbal / insurance state
  existing normal Contract launch preparation
  SCIENCE_LAUNCH
  LIVE_MISSION (repeated)
  FACILITY (repeated)
  CONSTRUCTION (zero or one active initially)
  RESEARCHED_TECH (repeated)
  RESEARCH
  COMPLETED_SCIENCE (repeated)
```

Persist gameplay identity and authoritative mutable state, not presentation strings or values that can be safely derived. Older saves missing the new fields should receive safe defaults: zero stored Science, Level 1 facilities, the chosen initial Kerbal roster default, no live missions, no construction, no active research, no rival-completed Science subjects, and no pending insurance.

### KSP science boundary

Add a small science integration component inside the existing `KspIntegration/` module, for example `KspScienceAdapter`, responsible for resolving stock experiments/subjects, reading the relevant stock Science state/reward, and consuming the player's exact matching subject after a rival wins it. Rival simulation and persistence must operate on project-owned `ScienceSubjectKey` values rather than raw KSP `ResearchAndDevelopment` or `ScienceSubject` objects.

Under the first-completion-wins rule, the adapter must re-check the subject at live-mission completion. A rival must not receive the Science reward if the player already depleted that subject while the rival mission was in flight.

### Expected project changes

The recommended design should fit within the existing project modules. Expected changes when implementation is approved:

| Path | Expected change |
| --- | --- |
| `Agencies/AgencyState.cs` | Attach/access composed rival-only programme state. |
| `Agencies/RivalProgramState.cs` | New data-only rival state classes/enums. |
| `Rivals/RivalSimulation.cs` | Launch preparation, live mission creation/resolution, roster, facilities, construction, and research behaviour. |
| `Rivals/RivalTechCatalogue.cs` | Fixed project-owned stock rival tech definitions. |
| `Core/CampaignSettings.cs` | New rival balance/location settings. |
| `KspIntegration/CampaignSettingsLoader.cs` | Parse the added settings from existing `CampaignSettings.cfg`. |
| `GameData/TheRaceForSpace/Config/CampaignSettings.cfg` | Store tuneable rival balance and location values. |
| `Persistence/RivalAgenciesSaveState.cs` | Persist the composed rival programme state under existing rival save records. |
| `Campaign/CampaignController.cs` | Coordinate funding-boundary payroll/insurance, construction and research completion, and shared funding calculations. |
| `KspIntegration/KspScienceAdapter.cs` | Keep stock KSP Science subject access/depletion at the integration boundary. |
| `Objectives/ObjectiveDefinition.cs` and catalogue | Carry Contract difficulty and required rival crew counts. |
| `UI/CommandCenterWindow.cs` | Render the approved Rival Agencies dashboard from read-only state. |
| `tests/` | Cover location lookup, success chance, deterministic outcome seeds, first-completion Science races, roster/hiring/payroll, facilities, construction, research, live mission resolution, and persistence round-trips. |
| `docs/STRUCTURE.md` / `docs/CODE_OVERVIEW.md` | Document final ownership/flow after implementation. |

Do not introduce new managers, services, factories, interfaces, class hierarchies, external dependencies, or top-level source modules unless a concrete implementation problem proves the current architecture cannot support the required behaviour.

### Implementation approval gate

The data model above intentionally changes the persisted `RIVAL_AGENCIES` schema and extends the existing campaign configuration format. Those are compatibility-sensitive structural changes under `AGENTS.md`. Before implementation, explicitly confirm the final save/config additions and obtain approval for that implementation step. Documentation-only refinement of this design does not itself require that structural approval.
