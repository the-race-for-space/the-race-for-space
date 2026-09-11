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
- [ ] **Choose the next valid Science Expedition randomly.** Whenever a rival needs a new Science launch target, build the set of currently valid, unlocked, not-rival-completed Science subjects and select randomly from that set rather than ranking subjects by reward or difficulty.
- [ ] **Keep Science Expeditions free of an additional Funds cost initially.** Preparing or launching a Science Expedition does not charge a separate expedition Funds cost. The expedition still consumes time, Kerbal capacity/payroll, any hiring cost required at launch, and live-mission risk.
- [ ] **Use the Tracking Station as the only facility destination gate.** VAB and Launch Pad levels modify normal Launch Progress Chance but do not restrict destination access. Tracking Station Level 1 permits Pre-Orbit and Kerbin-orbit Contracts; Level 2 is required for Mun and Minmus missions; Level 3 permits missions to other planets and their moons. Mission-progress and tech requirements still apply inside the destinations permitted by the Tracking Station.
- [ ] **Unlock Kerbin Science situations from the start and Probe Orbit.** Kerbin `Landed`, `Splashed`, `Flying Low`, and `Flying High` situations begin available to valid unlocked experiments. Completing the Probe Orbit Contract unlocks both `Low Space` and `High Space` Science around Kerbin.
- [ ] **Unlock non-Kerbin Science situations through orbit and landing progression.** For another celestial body, completing that body's Probe Orbit Contract unlocks both `Low Space` and `High Space` Science there. A successful body-specific landing Contract, once landing Contracts are implemented, unlocks `Landed`, `Splashed`, `Flying Low`, and `Flying High` Science situations on that body wherever those stock situations are physically valid.
- [ ] **Gate Surface Sample with R&D Level 2 and surface access.** Surface Sample expeditions require Research and Development Level 2 and the target body's surface Science access to have been unlocked through the applicable starting Kerbin access or successful landing progression. R&D Level 2 alone does not unlock an otherwise inaccessible body's surface.
- [ ] **Track a clear rival Expedition Range.** Represent the locations currently available to the rival in a player-readable range such as `Kerbin only`, `Kerbin, Mun and Minmus`, or `Planets available`, while still enforcing the more detailed body/situation/biome mission gates internally.
- [ ] **Progress Launch Science Expeditions once per Kerbin day.** Each science-launch preparation receives one progress check every Kerbin day. A successful check adds 10 percentage points to `Launch Progress`, so ten successful checks are required to advance from 0% to 100% and launch the expedition.
- [ ] **Derive science-launch Progress Chance from the SPH and Runway.** The Spaceplane Hangar and Runway each contribute 20% at Level 1, giving a default combined `Progress Chance - daily` of 40%. Each Level 2 facility adds another +3% and each Level 3 facility adds another +3%. Store and expose the authoritative calculated total so the simulation, UI, and ETA use the same value.
- [ ] **Estimate science-expedition launch time from remaining successful checks.** Calculate the average remaining launch-preparation duration as `remaining 10% Launch Progress steps / daily success chance`. At 0% progress and the default 40% daily chance, ten successful checks are required and the expected launch time is 25 Kerbin days. This is an average estimate rather than a guaranteed launch date.
- [ ] **Move a science expedition into Live Mission Progress at 100% Launch Progress.** Reaching 100% should create a launched live science mission with its own mission duration, completion date, success chance, and failure chance. Do not award Science or consume the player's matching science subject at the moment of launch.
- [ ] **Use the remaining shared Science value when a rival succeeds.** At successful live-mission completion, re-check the exact stock Science subject. If the player has already fully exhausted it, the rival receives 0 Science. If the player has partially depleted it, award the rival only the remaining Science and then exhaust the subject. If the rival succeeds before the player takes any of it, award the available Science and consume the matching player subject. Launching an expedition never reserves the Science value.
- [ ] **Select a new valid Launch Science Expedition after the previous expedition launches.** Launch preparation can begin for another valid science subject while the previously launched expedition is still running as a live mission.
- [ ] **Show rival science-launch capability and current target to the player.** The Rival Agencies interface should show the rival's unlocked science experiments, current Expedition Range, current experiment/body/situation/biome target, `Launch Progress`, `Progress Chance - daily`, estimated launch, Science reward, and Kerbals assigned.
- [ ] **Select one rival research project only at funding boundaries.** If the rival has no active research project when a campaign funding boundary is processed, find eligible tech nodes that the rival can afford with its stored Science. Choose from the cheapest eligible Science-cost tier, selecting randomly when several nodes have the same cost, deduct the chosen node's Science cost immediately, and start that one research project. Science earned between funding boundaries must wait for the next funding event before it can start new research.
- [ ] **Allow only one active rival research project at a time.** Do not start another tech project until the current research project has completed and a later funding boundary selects the next project.
- [ ] **Use a fixed 90-day rival research period.** A selected tech project takes 90 campaign days to research and becomes unlocked at the first campaign funding boundary on or after the 90-day research period has elapsed. Persist the project, Science cost, start date, and eligible completion funding date through save/load.
- [ ] **Show the next rival research project under Stored Science.** In the Rival Agencies Tech Tree section, show the selected tech, its Science cost, research status, and funding-date completion/ETA directly beneath the rival's Stored Science value. Use `Paid` to show that the Science cost has already been deducted.

#### Live rival mission simulation

- [ ] **Add a Live Mission Progress phase after launch.** Normal rival missions and Launch Science Expeditions should no longer complete their gameplay result immediately when launch preparation reaches 100%. At launch, create a persistent live-mission record and move it into the Rival Agencies `Live Mission Progress` section.
- [ ] **Use only Contract or Science as the live Mission Type.** In the player-facing live mission list, a normal funding/objective mission is `Contract` and a science expedition is `Science`. Keep the values short and do not expose internal mission-class names in this column.
- [ ] **Use a fixed live-mission duration lookup by target location.** Mission Type should not independently change duration. A Contract and a Science mission targeting the same location should use the same configured live duration. Each supported Kerbin biome, KSC location biome, and celestial body has one manually assigned travel time in the tables below.
- [ ] **Treat the duration tables as authoritative balance data.** Do not calculate travel time from physical distance, orbital distance, launch windows, phase angles, live planet positions, or vessel state. The simulation should look up the configured value for the target location and use it directly.
- [ ] **Keep location durations configurable and stable.** Use stable project-owned location IDs so these manual travel times can be balanced later without changing mission logic or save interpretation. Unknown locations should fail safely rather than borrowing an unrelated travel time.
- [ ] **Give every launched mission persistent timing and difficulty data.** Store the launch date, configured base duration, expected completion date, mission type, target, assigned Kerbals, mission difficulty from 1 to 10, deterministic outcome seed, and any science subject/reward data required by science expeditions. Derive elapsed time, progress, and ETA from the stored dates rather than persisting changing presentation values.
- [ ] **Do not complete a rival mission at launch.** Reaching 100% `Launch Progress` only starts the live mission. A Contract remains incomplete, and a Science expedition grants no Science, until the configured live duration has finished and the mission passes its final success check.
- [ ] **Resolve live missions on the normal five-second simulation refresh.** Contract and Science live missions should be checked during the normal repeating rival/campaign refresh and resolve on the first refresh at or after their stored completion universal time. They must not wait for a campaign funding boundary. If time warp or a reload skips past the completion time, resolve them on the next refresh using their stored deterministic state.
- [ ] **Use mission difficulty to determine final Success Chance.** Every live Contract and Science mission uses a difficulty rating from 1 to 10. Contract missions use the contract's difficulty. Science missions derive difficulty from the science target using the lookup tables below. Difficulty 1 has a 90% Success Chance, and each additional difficulty level reduces Success Chance by 5 percentage points, down to 45% at difficulty 10. Use `Success Chance = 95% - (Difficulty × 5%)` for valid difficulty values 1–10.
- [ ] **Use biome-specific science difficulty only on Kerbin.** Kerbin surface Science missions use the target Kerbin biome to choose their 1–10 difficulty. Maintain one compact Kerbin biome difficulty table for Shores, Water, Grasslands, Highlands, Mountains, Deserts, Badlands, Tundra, Ice Caps, Northern Ice Shelf, and Southern Ice Shelf. Do not create equivalent per-biome difficulty tables for other celestial bodies.
- [ ] **Use fixed KSC location-biome science settings.** Science missions targeting KSC location biomes use a fixed 5-day live duration and Difficulty 1. Keep the KSC locations listed explicitly in the table below so these special local science targets do not inherit the wider Shores duration.
- [ ] **Use one science difficulty per non-Kerbin body.** Mun, Minmus, the planets, their moons, and other non-Kerbin science destinations each use one body-wide 1–10 difficulty regardless of the target biome. The biome may still be tracked as part of the exact science subject and shown in the UI, but it does not modify Success Chance outside Kerbin.
- [ ] **Use the Kerbin Orbit science setting for orbital science around Kerbin.** Kerbin Orbit uses a 10-day live duration and Difficulty 3 for science missions that use the Kerbin-orbit target rather than a surface biome or KSC location biome.
- [ ] **Keep science difficulty configuration compact.** The science difficulty balance data should consist of regular Kerbin biome entries, the fixed KSC location-biome rule, Kerbin Orbit, and one entry for each non-Kerbin celestial body. Do not maintain a database containing every biome on every moon and planet.
- [ ] **Resolve the mission with one deterministic outcome roll after its duration ends.** When the expected completion date is reached, generate the mission's Success Chance roll from its persisted outcome seed. A successful check completes the result; otherwise the mission fails. The result must not be pre-rolled or exposed before the live duration has elapsed, and save/reload must not change it.
- [ ] **Apply rewards only after a successful live mission.** On a successful Contract mission, mark the objective complete and apply any related satellite/infrastructure result. On a successful Science mission, apply the shared-subject rule above, record the subject as completed for the rival, and award only the Science still available from that subject at completion. No successful gameplay result is granted before this point.
- [ ] **Lose the spacecraft when a live mission fails.** A failed Contract or Science mission grants no objective completion, satellite/infrastructure result, or Science reward, and its simulated spacecraft is lost.
- [ ] **Apply a 25% Kerbal-loss chance independently to every assigned Kerbal on a failed mission.** For each Kerbal assigned to a failed crewed mission, make an independent 25% loss check. Remove every Kerbal whose check fails from the rival's employed roster; all surviving assigned Kerbals return to availability.
- [ ] **Charge 50,000 Funds insurance for each Kerbal lost.** Every Kerbal lost on a failed live mission adds 50,000 Funds to the rival's pending insurance deduction. Multiple losses stack; for example, two Kerbals lost creates a 100,000-Funds deduction at the next campaign funding boundary.
- [ ] **Start every rival with one employed Kerbal.** New rival programmes begin with one living employed Kerbal. Older saves that predate the rival roster fields should also default each rival to one employed Kerbal when the new state is introduced.
- [ ] **Track each rival's employed Kerbal roster.** Persist the number of living Kerbals employed by each rival. Derive the number currently assigned to live missions and therefore the number available for new launches from active live-mission assignments. Kerbals assigned to a live mission remain unavailable until that mission resolves.
- [ ] **Use fixed Contract crew requirements.** Every crewed Contract launch requires exactly 1 Kerbal. Every uncrewed/probe Contract and every satellite-network launch requires 0 Kerbals. Science missions continue to require at least one Kerbal under their Science rules.
- [ ] **Use the Astronaut Complex as the roster cap.** The rival may never employ more living Kerbals than its current Astronaut Complex level permits: Level 1 = 3 Kerbals, Level 2 = 8 Kerbals, Level 3 = no limit.
- [ ] **Hire missing Kerbals only when a mission is ready to launch.** When Launch Progress reaches 100%, first check whether enough Kerbals are available. If not, and the rival is below its Astronaut Complex roster cap and can pay the cost, hire the missing Kerbal or Kerbals for 100,000 Funds each, add them to the employed roster, and assign them immediately to the launch.
- [ ] **Wait at 100% Launch Progress when crew cannot be provided.** If a crew-required mission is ready but the rival is already at its Astronaut Complex roster cap, does not have enough available Kerbals, or cannot afford a required 100,000-Funds hire, keep the mission ready at 100% Launch Progress and do not launch it. Launch as soon as enough existing Kerbals return or an eligible hire can be made.
- [ ] **Do not impose a simultaneous limit on uncrewed live missions.** Uncrewed Contract missions do not consume Kerbal roster capacity and do not have a separate Mission Control or global simultaneous-live-mission cap. Their normal launch preparation, target eligibility, satellite capacity, and Funds requirements remain the relevant limits.
- [ ] **Charge 10,000 Funds payroll per living employed Kerbal at every funding boundary.** Count all living Kerbals on the rival's roster, including Kerbals currently on live missions. Deduct `10,000 Funds × employed Kerbals` from that rival's funding income at each campaign funding boundary.
- [ ] **Allow funding deductions to drive rival Funds below zero.** At a funding boundary, calculate the rival's gross income and subtract Kerbal payroll plus the full pending insurance amount. Apply the resulting signed net funding to the rival's Funds balance without flooring the payment or Funds at zero. Once pending insurance is applied, clear that pending amount; there is no unpaid-insurance carry-forward. A negative Funds balance prevents spending that requires sufficient Funds until later income recovers the balance.
- [ ] **Show roster and payroll as part of the rival's programme economy.** The Rival Agencies UI should show employed Kerbals versus the current Astronaut Complex limit, Kerbals currently on missions, Kerbals available, the next Kerbal payroll deduction, and any pending insurance deduction so the player can understand why gross funding and net funding differ.
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

##### Contract difficulty and crew database

These rules are authoritative for the current Contract catalogue. Every crewed Contract requires exactly 1 Kerbal; every uncrewed/probe Contract and satellite-network launch requires 0 Kerbals.

For all four five-stage Pre-Orbit lines, use the same difficulty progression:

| Stage | Difficulty |
| --- | ---: |
| I | 1 |
| II | 1 |
| III | 2 |
| IV | 2 |
| V | 3 |

`Directed Power`, `Mass`, and `Biome` use 0 Kerbals at every stage. `Control` uses 1 Kerbal at every stage.

For orbital Contracts, the Probe difficulty is the base value for that body. The matching Crewed Orbit is always `Probe Difficulty + 1` and requires 1 Kerbal. The matching satellite-network launch uses the same difficulty as the Probe Orbit and requires 0 Kerbals.

| Body | Probe Difficulty | Probe Kerbals | Crewed Difficulty | Crewed Kerbals | Network Difficulty | Network Kerbals |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| Kerbin | 4 | 0 | 5 | 1 | 4 | 0 |
| Mun | 5 | 0 | 6 | 1 | 5 | 0 |
| Minmus | 5 | 0 | 6 | 1 | 5 | 0 |
| Duna | 7 | 0 | 8 | 1 | 7 | 0 |
| Moho | 7 | 0 | 8 | 1 | 7 | 0 |
| Eve | 8 | 0 | 9 | 1 | 8 | 0 |
| Gilly | 7 | 0 | 8 | 1 | 7 | 0 |
| Ike | 7 | 0 | 8 | 1 | 7 | 0 |
| Dres | 7 | 0 | 8 | 1 | 7 | 0 |
| Jool | 7 | 0 | 8 | 1 | 7 | 0 |
| Laythe | 7 | 0 | 8 | 1 | 7 | 0 |
| Vall | 7 | 0 | 8 | 1 | 7 | 0 |
| Tylo | 7 | 0 | 8 | 1 | 7 | 0 |
| Bop | 7 | 0 | 8 | 1 | 7 | 0 |
| Pol | 7 | 0 | 8 | 1 | 7 | 0 |
| Eeloo | 8 | 0 | 9 | 1 | 8 | 0 |

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

`EVA Report` and `Surface Sample` are not unlocked by a stock tech-tree node. Surface Sample specifically requires R&D Level 2 plus the applicable surface-access progression; EVA Report remains gated separately by the relevant expedition/mission capability rules.

### Rival Space Centre progression

- [ ] **Simulate each rival building and upgrading its own Space Centre.** Give every rival persistent facility levels based on the nine separately upgradable stock Career facilities. Rivals should begin with Level 1 facilities and spend their own Funds to improve them through Level 2 and Level 3 rather than receiving all programme capabilities automatically.
- [ ] **Simulate rival facility construction progress.** A facility upgrade should remain under construction for its full build period, with its previous level remaining active until construction completes.
- [ ] **Start facility construction only at funding times.** Rivals should evaluate and begin eligible Space Centre upgrades when a campaign funding boundary is processed rather than starting construction at arbitrary times between funding events.
- [ ] **Charge rival Funds when construction starts.** Upgrading a facility from Level 1 to Level 2 costs 100,000 Funds. Upgrading from Level 2 to Level 3 costs 250,000 Funds. Construction may only begin if the rival can pay the full upgrade cost.
- [ ] **Use fixed rival facility construction times.** Level 1 to Level 2 takes 180 campaign days. Level 2 to Level 3 takes 270 campaign days. Record the construction start date, completion date, source level, and target level so progress survives save/load correctly.
- [ ] **Choose one affordable facility upgrade randomly at a funding boundary.** If a rival has no active construction project when its construction-scheduling step runs at a funding boundary, build the set of eligible facility upgrades whose full cost the rival can currently afford and choose one randomly. If none are affordable, start no construction. Continue to enforce only one active facility construction project at a time.
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
| **Mission Control** | Maximum 2 active contracts; no flight planning | Maximum 7 active contracts; flight planning available when navigation requirements are met | Unlimited active contracts | Limits the number of rival satellites that may be launched/maintained: Level 1 = 3 satellites, Level 2 = 8 satellites, Level 3 = no limit. It does not impose a simultaneous-live-mission limit on uncrewed Contracts. |
| **Research and Development** | May unlock tech nodes costing up to 100 Science | May unlock tech nodes costing up to 500 Science; surface sampling/resource transfer capability becomes available with the other requirements met | No tech-node Science-cost limit | Directly gates the rival stock tech tree. Level 1 permits nodes through 90 Science, Level 2 permits nodes through 300 Science, and Level 3 permits the 550- and 1000-Science nodes. Surface Sample additionally requires R&D Level 2 and unlocked surface access for the target body. |
| **Vehicle Assembly Building (VAB)** | 30-part craft limit; no action groups | 255-part craft limit; basic action groups | Unlimited parts; full action groups | Modifies normal rival Launch Progress Chance only: Level 1 = +15%; Level 2 = additional +3%; Level 3 = additional +3%. It does not gate destinations. |
| **Spaceplane Hangar (SPH)** | 30-part craft limit; no action groups | 255-part craft limit; basic action groups | Unlimited parts; full action groups | Modifies Launch Science Expedition preparation: Level 1 = Science Launch Progress Chance +20%; Level 2 = additional +3%; Level 3 = additional +3%. |
| **Launch Pad** | Small launch vehicle size/mass limit; about 18 t maximum mass | Medium launch vehicle limits; about 140 t maximum mass | Unlimited stock size/mass | Modifies normal rival Launch Progress Chance only: Level 1 = +15%; Level 2 = additional +3%; Level 3 = additional +3%. It does not gate destinations. |
| **Runway** | Small aircraft size/mass limit; about 18 t maximum mass | Medium aircraft limits; about 140 t maximum mass | Unlimited stock size/mass | Modifies Launch Science Expedition preparation: Level 1 = Science Launch Progress Chance +20%; Level 2 = additional +3%; Level 3 = additional +3%. |
| **Tracking Station** | Basic orbital tracking | Patched-conic/navigation capability | Adds unowned-object tracking and full stock tracking capability | Sole rival destination facility gate: Level 1 permits Pre-Orbit and Kerbin-orbit Contracts; Level 2 permits Mun and Minmus missions; Level 3 permits other planets and their moons. Other tech and mission-progression gates still apply. |

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
- [ ] **Show rival funding deductions explicitly.** In the Funding section, show gross next income first, then negative lines for `Kerbal Payroll` and any `Pending Insurance`, followed by the resulting `Total Next Payout`. The total is allowed to be negative and should show the signed value rather than being floored at zero.
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
- [ ] **Persist an outcome seed for every launched live mission.** Create and store a deterministic seed when the mission launches. When its duration ends, use that seed to generate the one Success Chance roll. Use deterministic derivatives of the same stored mission seed for any per-Kerbal 25% loss checks on failure so reload/time warp cannot change casualties. Do not store a pre-rolled success/failure result.
- [ ] **Allow one rival facility construction project at a time initially.** Represent construction as a collection-capable state so the schema can support more than one project later, but enforce a maximum of one active construction project for the initial implementation.
- [ ] **Use a fixed project-owned stock rival tech catalogue.** Mirror the approved stock tech progression in project-owned definitions instead of reading the installed KSP tech tree dynamically. Rival tech behaviour should therefore remain deterministic and should not silently change because another mod replaces the player's tech tree.
- [ ] **Store new rival balance data in the existing `CampaignSettings.cfg`.** Extend the current settings/config loader rather than introducing another balance file. Mission-location durations/difficulties, Kerbal costs, facility values, construction values, research timing, and other tuneable rival rules should use explicit named settings or config nodes.
- [ ] **Use first completion wins for shared Science subjects, including partial depletion.** Launching an expedition does not reserve a subject. At rival success, read the subject's remaining Science. Award only the amount still available and then exhaust it. If the player already fully exhausted the subject, the rival receives 0 Science; if the player partially depleted it, the rival receives only the remainder.
- [ ] **Start each rival with one employed Kerbal.** The initial rival roster and the compatibility default for older saves missing roster state are both one living employed Kerbal.
- [ ] **Do not impose a simultaneous uncrewed-live-mission limit.** Uncrewed Contracts may coexist without a Mission Control/global live-mission slot system; other normal eligibility, funding, satellite and preparation rules still apply.
- [ ] **Roll crew losses independently per Kerbal.** Every Kerbal assigned to a failed crewed mission has an independent 25% loss chance. Surviving Kerbals return to availability.
- [ ] **Charge insurance per lost Kerbal.** Every lost Kerbal adds 50,000 Funds to pending insurance; losses stack additively until the next funding boundary applies the deduction.
- [ ] **Allow rival Funds to become negative from funding deductions.** Gross funding minus payroll and the entire pending insurance amount is applied as a signed funding result. Do not floor the payout or Funds at zero and do not carry unpaid insurance forward after it has been applied.
- [ ] **Do not charge a separate Funds cost for Science Expeditions initially.** Science launch preparation has no additional expedition fee beyond normal staffing/hiring/payroll and mission risk.
- [ ] **Choose Science Expedition targets randomly from the valid set.** Do not optimise expedition selection by Science reward, duration or difficulty in the initial model.
- [ ] **Choose research only at funding boundaries, one project at a time.** If there is no active research project, select from affordable eligible nodes at the funding event. Choose the cheapest Science-cost tier and select randomly among equal-cost nodes; deduct the Science cost when the project is selected. Science gained between funding events waits until the next boundary.
- [ ] **Choose facility construction randomly from affordable eligible upgrades.** At a funding boundary, if no construction project is active, select randomly from upgrades the rival can fully afford. If no upgrade is affordable, do nothing.
- [ ] **Use fixed Contract crew counts and difficulty rules.** All crewed Contracts require 1 Kerbal; all uncrewed/probe Contracts and satellite-network launches require 0. Pre-Orbit stages I-V use difficulty `1, 1, 2, 2, 3` in every line. Each orbital Crewed mission is one difficulty higher than its matching Probe mission, and each satellite-network launch matches its body's Probe difficulty. Use the authoritative Contract table above for the body-specific Probe values.
- [ ] **Use the Tracking Station as the sole destination facility gate.** Level 1 permits Pre-Orbit and Kerbin-orbit Contracts, Level 2 permits Mun and Minmus missions, and Level 3 permits other planets and moons. VAB and Launch Pad affect Launch Progress Chance only and do not duplicate the Tracking Station destination gate.
- [ ] **Gate Surface Sample with R&D Level 2 plus surface access.** The rival must have R&D Level 2 and must already have the target body's surface Science access before Surface Sample becomes a valid expedition subject.
- [ ] **Use mission progression to unlock Science situations.** Kerbin begins with Landed, Splashed, Flying Low and Flying High available; completing Probe Orbit unlocks Kerbin Low Space and High Space. On other bodies, successful Probe Orbit unlocks Low Space and High Space, while a successful landing Contract unlocks Landed, Splashed, Flying Low and Flying High wherever those stock situations are valid.
- [ ] **Resolve live missions independently of funding boundaries.** Live Contract and Science missions are checked on the normal five-second simulation refresh and resolve on the first refresh at or after their completion universal time. Funding dates do not delay live-mission outcomes.
- [ ] **Use the agreed funding-boundary processing sequence.** When a funding boundary is crossed, first catch up any live missions whose completion universal time is on or before that boundary, then complete due research and facility construction, update funding eligibility, calculate income, apply payroll and insurance and credit the signed payout, choose new research, choose new affordable facility construction, and finally run the sponsor review for the next funding period. Preserve chronological universal-time ordering during time-warp/reload catch-up so only missions completed on or before that boundary can affect it.

### Recommended runtime state ownership

Keep the existing module ownership: `Agencies/` owns mutable agency state, `Rivals/` owns rival simulation behaviour, `Persistence/` owns save-state transforms, `Core/` owns balance settings, `KspIntegration/` owns raw KSP science access/config loading, `Campaign/` coordinates funding boundaries, and `UI/` remains read-only presentation. Do not create a new top-level source module for this design.

Recommended `RivalProgramState` contents:

| State | Recommended representation | Notes |
| --- | --- | --- |
| Stored Science | `double` | Persisted rival Science balance. |
| Science launch preparation | `ScienceLaunchPreparationState` | One current expedition being prepared independently of normal Contract launch preparation. |
| Live missions | `List<RivalLiveMissionState>` | Contains all launched Contract and Science missions still awaiting resolution; no separate uncrewed slot-count state is required. |
| Completed rival Science subjects | `HashSet<ScienceSubjectKey>` | Prevents a rival repeating its own completed subjects. |
| Researched tech | `HashSet<string>` | Stable project-owned tech IDs. |
| Current research | `RivalResearchProjectState` | Zero or one active research project. |
| Facility levels | `Dictionary<RivalFacilityType, int>` | Nine rival Space Centre facilities. |
| Facility construction | `List<RivalFacilityConstructionState>` | Initially enforce zero or one active entry. |
| Kerbals employed | `int` | Living employed roster count; initial/default value is 1. |
| Pending insurance | `double` | Accumulates 50,000 Funds per lost Kerbal until the next funding boundary applies the full deduction. |

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
  PlannedScienceReward
  LaunchProgressPercent
  NextProgressCheckUniversalTime
  RequiredKerbals
```

`PlannedScienceReward` is presentation/planning data from the subject at selection/launch; the actual award at successful mission completion is limited to the Science still remaining in the shared stock subject. Do not persist Progress Chance, Estimated Launch, Expedition Range, unlocked-experiment display text, Kerbals Available, a Science Expedition Funds cost, or the `Ready`/`Hire At Launch`/`Waiting For Kerbal` UI state. Derive those values from authoritative facility, tech, mission, roster, Funds, and launch-preparation state.

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
  PlannedScienceReward        // Science missions
  OutcomeSeed
```

Snapshot `DurationDays`, `Difficulty`, and `SuccessChancePercent` when the mission launches. A later balance/config edit must not alter a mission already in flight. Derive elapsed progress and ETA from current universal time, launch time, and completion time rather than persisting changing progress/ETA values.

At 100% normal or Science Launch Progress, the simulation should check/assign crew, create the live-mission record, reset the relevant preparation state, and begin the next eligible preparation. Objective completion, satellite/infrastructure results, and Science rewards occur only when the live mission later resolves successfully. Uncrewed live missions do not consume a separate mission-slot resource.

Live missions are evaluated during the normal five-second simulation refresh rather than as a funding-boundary action. When a refresh crosses a mission's completion universal time, resolve that mission immediately using its stored deterministic state; time warp and reload catch up on the next refresh.

For a failed crewed mission, use deterministic rolls derived from `OutcomeSeed` to perform one independent 25% casualty check for each assigned Kerbal. Each loss removes one employed Kerbal and adds 50,000 Funds to pending insurance; survivors return to availability.

### Kerbal roster calculations

Persist `KerbalsEmployed`; derive the rest:

```text
Initial Kerbals      = 1
Kerbals On Mission   = sum of AssignedKerbalCount across active live missions
Kerbals Available    = Kerbals Employed - Kerbals On Mission
Kerbal Payroll       = Kerbals Employed × 10,000 Funds per funding boundary
Roster Limit         = current Astronaut Complex level rule
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

Store only facility levels. Derive funding, roster caps, satellite caps, destination gates, tech-cost gates, normal Launch Progress Chance, and Science Launch Progress Chance from those levels. Mission Control does not derive or store an uncrewed live-mission count limit. Tracking Station alone supplies the facility-level destination gate; VAB and Launch Pad do not duplicate that responsibility.

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

Construction progress and ETA remain derived. Although the state may be held in a list for future flexibility, the initial simulation must permit only one active facility construction project per rival. When construction can start at a funding boundary, choose randomly from eligible upgrades whose full cost the rival can currently afford.

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

Only funding boundaries may create a new research project, and only when no project is already active. At that event, consider eligible nodes the rival can afford, choose from the cheapest Science-cost tier, select randomly among equal-cost choices, and deduct the Science cost immediately. Science acquired between funding events remains stored until the next funding boundary can select research. The UI derives status/ETA from the active research state.

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

The lookup supplies values when a mission launches; the live mission then keeps its snapped values. Target eligibility is separately constrained by Tracking Station level, technology and completed mission progression.

### Contract definition additions

Add explicit Contract difficulty and crew requirement data to objective definitions rather than deriving either value from display wording. Recommended fields are:

```text
Difficulty            // 1-10 from the authoritative Contract table/rules above
RequiredKerbalCount    // 1 for crewed Contracts; 0 for uncrewed/probe/network launches
```

Keep the existing crew-category enum where it remains useful, but use `RequiredKerbalCount` for rival crew reservation. The difficulty values and crew counts are now defined and are no longer an open balance decision.

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

Administration level supplies Base Income. Payroll and insurance are deductions. `NetPayout` is signed and may be negative. Apply it directly to `AgencyState.Funds`, allowing the rival balance to fall below zero. Pending insurance is fully consumed/cleared when that funding boundary applies it; do not create unpaid-insurance carry-forward state. `NextPayoutFunds`, launch affordability/ETA projections, funding-boundary payment, and the Rival Agencies UI should consume the same calculation rather than independently reconstructing totals.

Use this deterministic funding-boundary sequence:

```text
Catch up live missions with completion time <= funding boundary
-> Complete due research and facility construction
-> Update funding eligibility
-> Calculate income
-> Apply payroll and insurance; credit the signed payout
-> Choose new research
-> Choose new affordable facility construction
-> Sponsor review for the next funding period
```

Live missions still resolve on the normal five-second refresh. The catch-up step exists only for time warp/reload or any refresh that crosses a funding boundary, so event ordering remains chronological and a mission completed on or before the boundary can affect that funding event.

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
  RESEARCH (zero or one active)
  COMPLETED_SCIENCE (repeated)
```

Persist gameplay identity and authoritative mutable state, not presentation strings or values that can be safely derived. Older saves missing the new fields should receive safe defaults: zero stored Science, Level 1 facilities, one employed Kerbal, no live missions, no construction, no active research, no rival-completed Science subjects, and no pending insurance. Rival Funds are not clamped to zero when restoring or applying the new funding rules because negative rival balances are valid state.

### KSP science boundary

Add a small science integration component inside the existing `KspIntegration/` module, for example `KspScienceAdapter`, responsible for resolving stock experiments/subjects, reading the relevant stock Science state/reward, and consuming the player's exact matching subject after a rival wins it. Rival simulation and persistence must operate on project-owned `ScienceSubjectKey` values rather than raw KSP `ResearchAndDevelopment` or `ScienceSubject` objects.

Under the first-completion-wins rule, the adapter must re-check the subject at live-mission completion. If the subject is fully depleted, return zero available Science. If partially depleted, return only the remaining value and then exhaust it after the rival's successful result. This check is authoritative over any planned reward snapshot recorded when the expedition was selected/launched.

### Expected project changes

The recommended design should fit within the existing project modules. Expected changes when implementation is approved:

| Path | Expected change |
| --- | --- |
| `Agencies/AgencyState.cs` | Attach/access composed rival-only programme state and permit valid negative rival Funds. |
| `Agencies/RivalProgramState.cs` | New data-only rival state classes/enums. |
| `Rivals/RivalSimulation.cs` | Launch preparation, five-second live mission resolution, per-Kerbal deterministic casualties, roster, facilities, construction, destination/situation gates, and funding-boundary research selection. |
| `Rivals/RivalTechCatalogue.cs` | Fixed project-owned stock rival tech definitions. |
| `Core/CampaignSettings.cs` | New rival balance/location settings. |
| `KspIntegration/CampaignSettingsLoader.cs` | Parse the added settings from existing `CampaignSettings.cfg`. |
| `GameData/TheRaceForSpace/Config/CampaignSettings.cfg` | Store tuneable rival balance and location values. |
| `Persistence/RivalAgenciesSaveState.cs` | Persist the composed rival programme state under existing rival save records, including negative Funds. |
| `Campaign/CampaignController.cs` | Coordinate the normal live-mission refresh plus the agreed chronological funding-boundary sequence, signed payroll/insurance, construction/research completion and selection, and shared funding calculations. |
| `KspIntegration/KspScienceAdapter.cs` | Keep stock KSP Science subject access/depletion at the integration boundary. |
| `Objectives/ObjectiveDefinition.cs` and catalogue | Carry Contract difficulty and required rival crew counts. |
| `UI/CommandCenterWindow.cs` | Render the approved Rival Agencies dashboard from read-only state, including signed next payout values. |
| `tests/` | Cover location lookup, Tracking Station destination gates, Science situation progression, Surface Sample gating, success chance, deterministic five-second outcome/casualty catch-up, Contract difficulty/crew mapping, partial/complete shared-Science races, one-Kerbal defaults, roster/hiring/payroll, negative funding, unlimited uncrewed live missions, random affordable construction selection, the agreed chronological funding-boundary sequence, funding-boundary research selection, live mission resolution, and persistence round-trips. |
| `docs/STRUCTURE.md` / `docs/CODE_OVERVIEW.md` | Document final ownership/flow after implementation. |

Do not introduce new managers, services, factories, interfaces, class hierarchies, external dependencies, or top-level source modules unless a concrete implementation problem proves the current architecture cannot support the required behaviour.

### Remaining design decisions

The decisions below are intentionally still open and can be handled in later passes. Items already decided above should not be re-added to this list unless the design changes.

- [ ] **Complete the base/station content balance pass.** Define crew counts, difficulty, qualification criteria, payouts, maintenance/loss rules and exact progression for Desert, Polar, Kerbin orbital, Mun and Minmus infrastructure content.

### Implementation approval gate

The data model above intentionally changes the persisted `RIVAL_AGENCIES` schema and extends the existing campaign configuration format. Those are compatibility-sensitive structural changes under `AGENTS.md`. Before implementation, explicitly confirm the final save/config additions and obtain approval for that implementation step. Documentation-only refinement of this design does not itself require that structural approval.