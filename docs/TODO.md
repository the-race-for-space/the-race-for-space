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

- [ ] **Add a rival Expedition system.** Allow each rival agency to run science expeditions independently of its normal launch programme, so research activity can progress at the same time as launch development.
- [ ] **Give each rival its own stored Science balance.** Science earned from completed rival expeditions should be recorded against that agency and remain available for later technology purchases.
- [ ] **Model a rival version of the stock tech tree.** Give each rival agency its own technology progression based on the stock KSP tech tree, with Science spent to unlock tech nodes.
- [ ] **Use rival technology to gate agency capabilities.** A rival should only be able to select launches, destinations, experiments, and other activities supported by the technologies it has unlocked.
- [ ] **Create science expeditions from stock experiments.** Rival expeditions should represent completing specific stock science experiments in valid situations and locations, awarding the rival the corresponding configured Science value when completed.
- [ ] **Gate expedition locations by achieved mission capability.** Rivals may begin with accessible Kerbin surface expeditions, but new situations and celestial bodies should become available only after the agency has demonstrated the required mission progress; for example, Low Kerbin Space after completing a probe-orbit objective, Mun orbital science after first orbiting the Mun, and Mun surface science after first landing there.
- [ ] **Keep launch and expedition programmes simultaneous.** Rival agencies should be able to work toward one launch target and one science expedition at the same time rather than science replacing their existing mission-development activity.
- [ ] **Remove rival discoveries from the player's science pool.** When a rival completes a specific experiment/body/situation/biome science subject, consume that same stock science subject from the science pool available to the player so the player can no longer earn its Science. For example, if a rival completes a Crew Report in Kerbin's Shores biome, that Crew Report science subject is no longer available for the player to collect.
- [ ] **Select valid next rival expeditions from current access and technology.** Expedition selection should consider the agency's completed missions, unlocked destinations and situations, available experiment technologies, and science subjects it has not already completed.
- [ ] **Show the rival's current science expedition to the player.** The Rival Agencies interface should identify the experiment or science subject each rival is currently working on, alongside its existing launch-programme information.
- [ ] **Simulate a next rival research project.** Once a rival has enough stored Science for an eligible tech node, select a next research project and deduct that node's Science cost from the rival's stored balance when research begins.
- [ ] **Use a fixed 90-day rival research period.** A selected tech project takes 90 campaign days to research and becomes unlocked at the first campaign funding boundary on or after the 90-day research period has elapsed. Persist the project, Science cost, start date, and eligible completion funding date through save/load.
- [ ] **Show the next rival research project under Stored Science.** In the Rival Agencies Tech Tree section, show the selected tech, its Science cost, research status, and funding-date completion/ETA directly beneath the rival's Stored Science value.
- [ ] **Define expedition progress, duration, and Science spending rules.** Balance how quickly rival expeditions complete, whether they cost funds, how Science is awarded, how rivals choose which tech node to unlock next, and how expedition activity interacts with sponsor/funding progression.

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
| **Vehicle Assembly Building (VAB)** | 30-part craft limit; no action groups | 255-part craft limit; basic action groups | Unlimited parts; full action groups | Modifies rival launch development and can be used as a mission-body capability gate: Level 1 = starting Launch Chance +15%; Level 2 = Launch Progress Chance +3%; Level 3 = Launch Progress Chance +3%. |
| **Spaceplane Hangar (SPH)** | 30-part craft limit; no action groups | 255-part craft limit; basic action groups | Unlimited parts; full action groups | Modifies rival research development: Level 1 = starting Research Chance +15%; Level 2 = Research Progress Chance +3%; Level 3 = Research Progress Chance +3%. |
| **Launch Pad** | Small launch vehicle size/mass limit; about 18 t maximum mass | Medium launch vehicle limits; about 140 t maximum mass | Unlimited stock size/mass | Modifies rival launch development: Level 1 = starting Launch Chance +15%; Level 2 = Launch Progress Chance +3%; Level 3 = Launch Progress Chance +3%. |
| **Runway** | Small aircraft size/mass limit; about 18 t maximum mass | Medium aircraft limits; about 140 t maximum mass | Unlimited stock size/mass | Modifies rival research development: Level 1 = starting Research Chance +15%; Level 2 = Research Progress Chance +3%; Level 3 = Research Progress Chance +3%. |
| **Tracking Station** | Basic orbital tracking | Patched-conic/navigation capability | Adds unowned-object tracking and full stock tracking capability | Limits rival launch destinations: Level 1 = Kerbin only; Level 2 = Kerbin orbit and Kerbin's moons; Level 3 = no destination limit, allowing missions to other planets. |

The stock Flag Pole, Crawlerway, water tower, tanks, and other KSC scenery are not separately upgradable programme facilities, so they do not need independent rival simulation states.

### Rival Agencies UI redesign

- [ ] **Redesign only the Rival Agencies tab around current rival activity.** Preserve the existing rival Funds, next mission, mission progress, mission-progress cost, launch ETA, funding income, completed objectives, satellite-network income, and total next payout while adding the new science, research, tech-tree, facility, and construction information.
- [ ] **Make launch Progress Chance a main visible stat.** Replace the current `Progress Increase` presentation with `Progress Chance - each 5 days`. Show the authoritative total launch-progress success chance after applying the 15% base chance, the applicable +15% starting launch modifier, and any +3% facility upgrade modifiers. The UI should display the calculated total rather than reconstructing it independently.
- [ ] **Prioritize live rival activity before historical progression.** Order each rival card as Programme Status, Current Launch Programme + Science Expedition, Space Centre Construction, Space Centre Facilities, Tech Tree/Research, then detailed Funding.
- [ ] **Show one rival card cleanly and reuse the layout for additional rivals.** The card should be readable as a self-contained programme dashboard so the same component can be repeated for however many rivals are configured.
- [ ] **Show facility capabilities after every facility level.** Do not display only `Level 1/2/3`; immediately state the capability the current level provides, such as Kerbal limit, satellite limit, launch/research chance modifiers, tech-cost ceiling, base funding, or destination access.
- [ ] **Show ongoing construction with a clear ETA.** Display facility, source/target level, elapsed construction days, remaining days/ETA, and paid cost. Do not include explanatory text stating that the old level remains active while construction is underway.
- [ ] **Show researched techs clearly and keep the long tree collapsible.** The compact Tech Tree view should emphasize researched nodes and their experiment unlocks, with a `Show Full Tech Tree` control for the complete progression.

#### Rival Agencies UI text example

```text
RIVAL AGENCIES                                      Next Funding: Year 2, Day 120

══════════════════════════════ KERBAL DYNAMICS ═══════════════════════════════════════

PROGRAMME STATUS

Funds:               186,500              Stored Science:          72
Kerbals on Mission:        2 / 8           Satellites:               4 / 8
Total Next Payout:    42,000


┌─ CURRENT LAUNCH PROGRAMME ─────────────────────┐
│ Next Mission:       Mun Probe Orbit            │
│ Mission Progress:   60%                        │
│ Progress Chance - each 5 days: 36%             │
│ Progress Cost:      25,000 Funds               │
│ Estimated Launch:   80 days                    │
└────────────────────────────────────────────────┘

┌─ SCIENCE EXPEDITION ───────────────────────────┐
│ Status:              IN PROGRESS               │
│ Experiment:          Temperature Scan          │
│ Target:              Kerbin - Shores           │
│ Situation:           Landed                    │
│ Expedition Progress: 45%                       │
│ Science Available:   2.4 Science               │
│ Kerbals Assigned:    1                         │
└────────────────────────────────────────────────┘


┌─ SPACE CENTRE CONSTRUCTION ────────────────────────────────────────────────────────┐
│ Research & Development                                                            │
│ Level 1  ───────────────────────────────►  Level 2                                │
│                                                                                    │
│ Progress:       Day 74 / 180                                                       │
│ ETA:            106 days                                                          │
│ Cost:           100,000 Funds - PAID                                              │
└────────────────────────────────────────────────────────────────────────────────────┘


SPACE CENTRE FACILITIES

Administration Building     LEVEL 2
  └─ Base funding: 20,000 Funds per funding period

Astronaut Complex           LEVEL 2
  └─ Maximum rival Kerbals: 8

Mission Control             LEVEL 2
  └─ Maximum satellites: 8

Research & Development      LEVEL 1       [UPGRADING → LEVEL 2]
  └─ May research tech nodes costing up to 90 Science

Vehicle Assembly Building   LEVEL 2
  └─ Starting Launch Chance +15%
     Launch Progress Chance +3%

Launch Pad                  LEVEL 2
  └─ Starting Launch Chance +15%
     Launch Progress Chance +3%

Spaceplane Hangar           LEVEL 1
  └─ Starting Research Chance +15%

Runway                      LEVEL 1
  └─ Starting Research Chance +15%

Tracking Station            LEVEL 2
  └─ Missions permitted around Kerbin, Mun and Minmus


TECH TREE

Stored Science: 72
Next Research Project: Advanced Rocketry
Science Cost: 45 [SPENT]
Research Time: 90 days
Status: IN PROGRESS
Completion: Next eligible funding date
ETA: 38 days

RESEARCHED

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


FUNDING & EXISTING PROGRAMME INFORMATION

Base Income                                      20,000

Completed Objective Funding
  Probe Orbit                                     8,000
  Crewed Kerbin Orbit                             6,000

Satellite Network Funding
  Kerbin Satellites: 4 / 8                        8,000

                                                   ──────
Total Next Payout                                 42,000
```
