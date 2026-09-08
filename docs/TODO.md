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
- [ ] **Derive live mission duration from target distance.** Mission Type should not independently change duration. A Contract and a Science mission targeting the same location should use the same base live duration. Duration comes from a deterministic background location table so it does not change with current planetary phase or vessel state.
- [ ] **Use KSC-relative distance for Kerbin surface targets.** Rank the eleven regular Kerbin biomes by a representative nearest-practical great-circle distance from the Kerbal Space Center. Use the static distance table below for campaign simulation rather than performing biome-map searches during rival updates. KSC facility micro-biomes are excluded.
- [ ] **Convert Kerbin surface distance to live duration at one day per 100 km.** Use `max(1, ceil(referenceDistanceKm / 100))` Kerbin days for surface Contract and Science missions. The reference distances are intentionally rounded campaign values rather than claims that every point in a biome is the same distance from KSC.
- [ ] **Use fixed Hohmann-style travel estimates for off-world targets.** Use stock orbital scale to assign one-way travel-time baselines from Kerbin. Planet durations are based on a simple Hohmann-transfer estimate between Kerbin's and the target planet's stock semi-major axes; moon durations add a local parent-system transfer estimate. Round up to whole Kerbin days for the background database.
- [ ] **Do not use live planet positions to change duration.** Ignore launch windows, phase angle, eccentric anomaly, and the bodies' instantaneous separation when resolving rival live-mission length. These abstractions keep rival progress deterministic and save-compatible.
- [ ] **Give every launched mission persistent timing data.** Store the launch date, configured base duration, elapsed time, expected completion date, mission type, target, assigned Kerbals, and any science subject/reward data required by science expeditions.
- [ ] **Resolve mission success or failure at the end of the mission.** When the configured mission duration expires, make one outcome roll using that mission's authoritative success chance and failure chance. The result is not known to the player before the mission completes.
- [ ] **Apply gameplay rewards only after a successful live mission.** Successful normal missions may complete their objective or add the relevant satellite/infrastructure state. Successful science expeditions award Science and consume the matching player science subject. A failed mission must not grant the successful mission result.
- [ ] **Define mission success/failure balance rules.** Decide the base success chance and failure chance for each mission class and what modifies those chances, such as destination difficulty, crewed/uncrewed status, technology, facility level, or prior agency achievements.
- [ ] **Define failure consequences.** Decide whether failure only withholds the mission result or can also affect Funds, satellites, Kerbals, future launch preparation, or other agency state. Do not assume crew loss or asset loss until these rules are explicitly set.
- [ ] **Define simultaneous live-mission limits.** Decide how many launched normal missions and science expeditions a rival may have active at once and whether Mission Control, Astronaut Complex, or another facility limits those live missions.
- [ ] **Persist live missions across save/load and time warp.** Live mission completion and outcome resolution must be deterministic from stored dates/state and must correctly catch up if the player time-warps past a completion date or reloads after it.

##### Kerbin biome distance database

These are proposed background simulation values. They rank the regular Kerbin biomes from the KSC using representative nearest-practical distances, not the geometric centre of each irregular biome.

| Distance Rank | Kerbin Biome | Reference Distance From KSC | Base Live Duration |
| ---: | --- | ---: | ---: |
| 1 | Shores | 0 km | 1 day |
| 2 | Water | 25 km | 1 day |
| 3 | Grasslands | 30 km | 1 day |
| 4 | Highlands | 75 km | 1 day |
| 5 | Mountains | 120 km | 2 days |
| 6 | Deserts | 300 km | 3 days |
| 7 | Badlands | 700 km | 7 days |
| 8 | Tundra | 800 km | 8 days |
| 9 | Ice Caps | 900 km | 9 days |
| 10 | Northern Ice Shelf | 950 km | 10 days |
| 10 | Southern Ice Shelf | 950 km | 10 days |

##### Stock body distance database

`Reference Orbit` is the stock semi-major-axis scale used to anchor the estimate. Planet values are relative to the Sun; moon values are relative to their parent body. `Base Live Duration` is the proposed one-way campaign travel time from Kerbin, rounded up to a whole Kerbin day. The Sun entry represents a low-Sun-space mission estimate rather than a surface landing.

| Distance Rank | Target | Parent | Reference Orbit | Base Live Duration |
| ---: | --- | --- | ---: | ---: |
| 1 | Kerbin Orbit | Kerbin | — | 1 day |
| 2 | Mun | Kerbin | 12,000 km | 2 days |
| 3 | Minmus | Kerbin | 47,000 km | 10 days |
| 4 | Sun | — | Central body | 78 days |
| 5 | Moho | Sun | 5,263,138 km | 124 days |
| 6 | Eve | Sun | 9,832,685 km | 171 days |
| 7 | Gilly | Eve | 31,500 km | 174 days |
| 8 | Duna | Sun | 20,726,155 km | 303 days |
| 9 | Ike | Duna | 3,200 km | 303 days |
| 10 | Dres | Sun | 40,839,348 km | 604 days |
| 11 | Jool | Sun | 68,773,560 km | 1,123 days |
| 12 | Laythe | Jool | 27,184 km | 1,124 days |
| 13 | Vall | Jool | 43,152 km | 1,124 days |
| 14 | Tylo | Jool | 68,500 km | 1,125 days |
| 15 | Bop | Jool | 128,500 km | 1,128 days |
| 16 | Pol | Jool | 179,890 km | 1,131 days |
| 17 | Eeloo | Sun | 90,118,820 km | 1,587 days |

The table is intended as configuration data rather than hard-coded branching. A future implementation should keep stable location IDs and read the duration value from project-owned campaign settings or a dedicated project-owned lookup, while raw KSP body/biome APIs remain behind `KspIntegration/` where practical.

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
| **Astronaut Complex** | Roster limit 5; no off-Kerbin EVA | Roster limit 12; off-Kerbin EVA and flag planting available | Unlimited roster | Limits the rival's simulated Kerbal roster: Level 1 = 3 Kerbals, Level 2 = 8 Kerbals, Level 3 = no limit. |
| **Mission Control** | Maximum 2 active contracts; no flight planning | Maximum 7 active contracts; flight planning available when navigation requirements are met | Unlimited active contracts | Limits the number of rival satellites that may be launched/maintained: Level 1 = 3 satellites, Level 2 = 8 satellites, Level 3 = no limit. |
| **Research and Development** | May unlock tech nodes costing up to 100 Science | May unlock tech nodes costing up to 500 Science; surface sampling/resource transfer capability becomes available with the other requirements met | No tech-node Science-cost limit | Directly gates the rival stock tech tree. Level 1 permits nodes through 90 Science, Level 2 permits nodes through 300 Science, and Level 3 permits the 550- and 1000-Science nodes. Also gates Surface Sample expeditions. |
| **Vehicle Assembly Building (VAB)** | 30-part craft limit; no action groups | 255-part craft limit; basic action groups | Unlimited parts; full action groups | Modifies rival launch development and can be used as a mission-body capability gate: Level 1 = Launch Progress Chance +15%; Level 2 = additional Launch Progress Chance +3%; Level 3 = additional Launch Progress Chance +3%. |
| **Spaceplane Hangar (SPH)** | 30-part craft limit; no action groups | 255-part craft limit; basic action groups | Unlimited parts; full action groups | Modifies Launch Science Expedition preparation: Level 1 = Science Launch Progress Chance +20%; Level 2 = additional +3%; Level 3 = additional +3%. |
| **Launch Pad** | Small launch vehicle size/mass limit; about 18 t maximum mass | Medium launch vehicle limits; about 140 t maximum mass | Unlimited stock size/mass | Modifies rival launch development: Level 1 = Launch Progress Chance +15%; Level 2 = additional Launch Progress Chance +3%; Level 3 = additional Launch Progress Chance +3%. |
| **Runway** | Small aircraft size/mass limit; about 18 t maximum mass | Medium aircraft limits; about 140 t maximum mass | Unlimited stock size/mass | Modifies Launch Science Expedition preparation: Level 1 = Science Launch Progress Chance +20%; Level 2 = additional +3%; Level 3 = additional +3%. |
| **Tracking Station** | Basic orbital tracking | Patched-conic/navigation capability | Adds unowned-object tracking and full stock tracking capability | Limits rival launch destinations: Level 1 = Kerbin only; Level 2 = Kerbin orbit and Kerbin's moons; Level 3 = no destination limit, allowing missions to other planets. |

The stock Flag Pole, Crawlerway, water tower, tanks, and other KSC scenery are not separately upgradable programme facilities, so they do not need independent rival simulation states.

### Rival Agencies UI redesign

- [ ] **Redesign only the Rival Agencies tab around current rival activity.** Preserve the existing rival Funds, next mission, launch preparation, launch-progress cost, launch ETA, funding income, completed objectives, satellite-network income, and total next payout while adding live missions, science-launch preparation, research, tech-tree, facility, and construction information.
- [ ] **Rename preparation progress to Launch Progress.** The existing normal rival mission `Mission Progress` display and the former science `Expedition Progress` display should both use `Launch Progress`, because reaching 100% launches the mission rather than immediately completing its gameplay result.
- [ ] **Make normal launch Progress Chance a main visible stat.** Show `Progress Chance - each 5 days` as the authoritative combined value from the VAB and Launch Pad. With both at Level 1 the default is 30% (15% + 15%); Level 2 on either facility adds +3%, and Level 3 on either facility adds another +3%. The UI should display the calculated total rather than reconstructing it independently.
- [ ] **Rename the Science Expedition card to Launch Science Expedition.** Show `Launch Progress`, `Progress Chance - daily`, and `Estimated Launch` beside the target. The authoritative science-launch chance is the combined SPH + Runway value: 40% at the default Level 1/Level 1 facilities, giving an average 25-day launch-preparation time from 0%.
- [ ] **Show science-expedition capability above the launch target.** The Launch Science Expedition section should show unlocked experiments and current Expedition Range before the current experiment/body/situation/biome target.
- [ ] **Add Live Mission Progress immediately below Programme Status.** This should be the highest-priority detailed section in the rival card and list every launched normal mission and launched science expedition currently underway.
- [ ] **Show Live Mission Progress as a compact row-and-column list.** Use the columns `Item`, `Location`, `Mission Type`, `Duration`, `Progress`, `ETA`, and `Success Chance`. `Item` is the contract name for a normal mission or the experiment name for a science expedition. `Location` is the body and biome where relevant. `Mission Type` must be only `Contract` or `Science`. Do not show Status, Outcome, or Failure Chance in this list.
- [ ] **Keep science mission identity readable in the live-mission list.** For science rows, use the experiment as `Item`, include the body/biome in `Location`, and use `Science` as the Mission Type. For objective/funding rows, use the contract name as `Item` and `Contract` as the Mission Type. The detailed science reward and subject state remain part of the simulation even though the compact live list does not need extra columns for them.
- [ ] **Prioritize the rival card as Programme Status → Live Mission Progress → launch preparation → construction → facilities → Tech Tree/Research → Funding.** Place Current Launch Programme and Launch Science Expedition together after live missions so the player can distinguish missions already underway from missions still being prepared.
- [ ] **Use title case instead of all-caps UI headings and wording.** Rival Agencies headings, section titles, facility levels, construction states, research states, and other display wording should use normal title case rather than all-capital text. Standard acronyms such as ETA, VAB, and SPH may remain uppercase.
- [ ] **Show one rival card cleanly and reuse the layout for additional rivals.** The card should be readable as a self-contained programme dashboard so the same component can be repeated for however many rivals are configured.
- [ ] **Show facility capabilities after every facility level.** Do not display only `Level 1/2/3`; immediately state the capability the current level provides, such as Kerbal limit, satellite limit, launch/science-launch chance modifiers, tech-cost ceiling, base funding, or destination access.
- [ ] **Show ongoing construction with a clear ETA.** Display facility, source/target level, elapsed construction days, remaining days/ETA, and paid cost. Do not include explanatory text stating that the old level remains active while construction is underway.
- [ ] **Show researched techs clearly and keep the long tree collapsible.** The compact Tech Tree view should emphasize researched nodes and their experiment unlocks, with a `Show Full Tech Tree` control for the complete progression.

#### Rival Agencies UI text example

Mission duration and success-chance values in this mock-up use the proposed distance table where possible; success-chance values remain illustrative until those balance rules are defined.

```text
Rival Agencies                                      Next Funding: Year 2, Day 120

══════════════════════════════ Kerbal Dynamics ═══════════════════════════════════════

Programme Status

Funds:               186,500              Stored Science:          72
Kerbals on Mission:        2 / 8           Satellites:               4 / 8
Total Next Payout:    42,000


════════════════════════ Live Mission Progress ═══════════════════════════════════════

Item                    Location              Mission Type   Duration   Progress      ETA       Success Chance
──────────────────────  ────────────────────  ─────────────  ─────────  ────────────  ────────  ──────────────
Kerbin Crewed Orbit     Kerbin Orbit          Contract       1 day      Day 0 / 1     1 day     80%
Mystery Goo             Kerbin / Highlands    Science        1 day      Day 0 / 1     1 day     90%


┌─ Current Launch Programme ─────────────────────────────────────────────────────────┐
│ Next Mission:                    Mun Probe Orbit                                    │
│ Launch Progress:                 60%                                                │
│ Progress Chance - each 5 days:   36%                                                │
│ Progress Cost:                   25,000 Funds                                       │
│ Estimated Launch:                80 days                                            │
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
│ Kerbals Assigned:                 1                                                 │
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

                                                   ──────
Total Next Payout                                 42,000
```