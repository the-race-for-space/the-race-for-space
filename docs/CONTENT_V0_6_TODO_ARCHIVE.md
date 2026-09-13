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
- [ ] **Use rival technology to gate Science experiments only initially.** The rival stock tech tree determines which stock Science experiments are available for Launch Science Expeditions. Do not add a separate rival-tech requirement to Contract launches or destinations in the initial implementation; Contract availability, campaign progression, Tracking Station access, relevant facility limits, Kerbal availability, and Funds remain the Contract-side gates. The tech system may be expanded to gate additional rival capabilities later.
- [ ] **Create Launch Science Expeditions from stock experiments.** Rival science expeditions should represent completing specific stock science experiments in valid situations and locations. Preparing an expedition to 100% launches it; Science is awarded only if the resulting live science mission later succeeds.
- [ ] **Choose expeditions only from unlocked experiments and locations.** A rival may select a Launch Science Expedition only when the experiment has been unlocked by its technology and the body, situation, and biome are inside its achieved expedition access. The selected science subject must also still have Science available and must not already be completed, prepared, or in Live Mission Progress for that same rival. Different rival agencies may race the same still-available subject.
- [ ] **Choose the next valid Science Expedition randomly.** Whenever a rival needs a new Science launch target, build the set of currently valid, unlocked, not-rival-completed Science subjects and select randomly from that set rather than ranking subjects by reward or difficulty.
- [ ] **Keep Science Expeditions free of an additional Funds cost initially.** Preparing or launching a Science Expedition does not charge a separate expedition Funds cost. The expedition still consumes time, Kerbal capacity/payroll, any hiring cost required at launch, and live-mission risk.
- [ ] **Require one Kerbal for current Science Expeditions while keeping crew requirements data-driven.** Every current Launch Science Expedition requires 1 Kerbal. Keep an explicit `RequiredKerbalCount`/`RequiredKerbals` value in mission data so later Contracts or mission types can require more than one Kerbal without redesigning the roster or live-mission model.
- [ ] **Use the Tracking Station as the only facility destination gate.** VAB and Launch Pad levels modify normal Launch Progress Chance but do not restrict destination access. Tracking Station Level 1 permits Pre-Orbit and Kerbin-orbit Contracts; Level 2 is required for Mun and Minmus missions; Level 3 permits missions to other planets and their moons. Mission progression and Science-experiment tech requirements still apply where relevant inside the destinations permitted by the Tracking Station.
- [ ] **Unlock all Kerbin surface biomes for rival Science from campaign start.** Every regular stock Kerbin surface biome and every supported KSC location biome may be selected from the start when the chosen experiment is unlocked and valid for the requested situation. Do not require completion of the Biome Contract line to make Kerbin Science biomes available.
- [ ] **Unlock Kerbin Science situations from the start and Probe Orbit.** Kerbin `Landed`, `Splashed`, `Flying Low`, and `Flying High` situations begin available to valid unlocked experiments across the available Kerbin biomes. Completing the Probe Orbit Contract unlocks both `Low Space` and `High Space` Science around Kerbin.
- [ ] **Unlock non-Kerbin Science situations through orbit and landing progression.** For another supported celestial body, completing that body's Probe Orbit Contract unlocks both `Low Space` and `High Space` Science there. A successful body-specific landing Contract, once landing Contracts are implemented, unlocks `Landed`, `Splashed`, `Flying Low`, and `Flying High` Science situations on that body wherever those stock situations are physically valid. The Sun is not an eligible rival Science destination until corresponding Sun mission/Contract content is explicitly added later.
- [ ] **Gate Surface Sample with R&D Level 2 and surface access.** Surface Sample expeditions require Research and Development Level 2 and the target body's surface Science access to have been unlocked through the applicable starting Kerbin access or successful landing progression. R&D Level 2 alone does not unlock an otherwise inaccessible body's surface.
- [ ] **Gate EVA Report with Astronaut Complex Level 2 plus situation access.** EVA Report does not require a rival tech-tree node, but it requires Astronaut Complex Level 2 and the target body/situation to be otherwise available through the normal expedition-access rules.
- [ ] **Track a clear rival Expedition Range.** Represent the locations currently available to the rival in a player-readable range such as `Kerbin only`, `Kerbin, Mun and Minmus`, or `Planets available`, while still enforcing the more detailed body/situation/biome mission gates internally.
- [ ] **Progress Launch Science Expeditions once per Kerbin day.** Each science-launch preparation receives one progress check every Kerbin day. A successful check adds 10 percentage points to `Launch Progress`, so ten successful checks are required to advance from 0% to 100% and launch the expedition.
- [ ] **Derive science-launch Progress Chance from the SPH and Runway.** The Spaceplane Hangar and Runway each contribute 20% at Level 1, giving a default combined `Progress Chance - daily` of 40%. Each Level 2 facility adds another +3% and each Level 3 facility adds another +3%. Store and expose the authoritative calculated total so the simulation, UI, and ETA use the same value.
- [ ] **Estimate science-expedition launch time from remaining successful checks.** Calculate the average remaining launch-preparation duration as `remaining 10% Launch Progress steps / daily success chance`. At 0% progress and the default 40% daily chance, ten successful checks are required and the expected launch time is 25 Kerbin days. This is an average estimate rather than a guaranteed launch date.
- [ ] **Move a science expedition into Live Mission Progress at 100% Launch Progress.** Reaching 100% should create a launched live science mission with its own mission duration, completion date, success chance, and failure chance. Do not award Science or consume the player's matching science subject at the moment of launch.
- [ ] **Use one shared Science pool across the player and every rival.** Launching an expedition does not reserve a subject across agencies, so different rivals and the player may race the same subject. The first successful completion chronologically receives the Science still available from that stock subject and exhausts the remaining pool. A later successful rival mission for that same subject receives 0 Science but still records that subject as completed for that rival. If completions have the exact same stored completion universal time, resolve the tie in stable agency-ID order so save/reload and collection order cannot change the winner.
- [ ] **Use the remaining shared Science value when a rival succeeds.** At successful live-mission completion, re-check the exact stock Science subject. If the player or another rival has already fully exhausted it, the rival receives 0 Science. If the player has partially depleted it, award the rival only the remaining Science and then exhaust the subject. If the rival succeeds before the player takes any of it, award the available Science and consume the matching player subject.
- [ ] **Select a new valid Launch Science Expedition after the previous expedition launches.** Launch preparation can begin for another valid science subject while the previously launched expedition is still running as a live mission.
- [ ] **Show rival science-launch capability and current target to the player.** The Rival Agencies interface should show the rival's unlocked science experiments, current Expedition Range, current experiment/body/situation/biome target, `Launch Progress`, `Progress Chance - daily`, estimated launch, Science reward, and Kerbals assigned.
- [ ] **Select one rival research project only at funding boundaries.** If the rival has no active research project when a campaign funding boundary is processed, find eligible tech nodes that the rival can afford with its stored Science. Choose from the cheapest eligible Science-cost tier, selecting randomly when several nodes have the same cost, deduct the chosen node's Science cost immediately, and start that one research project. Science earned between funding boundaries must wait for the next funding event before it can start new research.
- [ ] **Allow only one active rival research project at a time.** Do not start another tech project until the current research project has completed and a later funding boundary selects the next project.
- [ ] **Use a fixed 90-day rival research period.** A selected tech project takes 90 campaign days to research and becomes unlocked at the first campaign funding boundary on or after the 90-day research period has elapsed. Persist the project, Science cost, start date, and eligible completion funding date through save/load.
- [ ] **Treat Start as already researched.** Every new rival begins with the stock `Start` node already researched, giving access to its initial experiments. Older saves that predate rival tech state should receive the same `Start` researched compatibility default rather than waiting 90 days to research a zero-cost root node.
- [ ] **Show the next rival research project under Stored Science.** In the Rival Agencies Tech Tree section, show the selected tech, its Science cost, research status, and funding-date completion/ETA directly beneath the rival's Stored Science value. Use `Paid` to show that the Science cost has already been deducted.

#### Live rival mission simulation

- [ ] **Add a Live Mission Progress phase after launch.** Normal rival missions and Launch Science Expeditions should no longer complete their gameplay result immediately when launch preparation reaches 100%. At launch, create a persistent live-mission record and move it into the Rival Agencies `Live Mission Progress` section.
- [ ] **Use only Contract or Science as the live Mission Type.** In the player-facing live mission list, a normal funding/objective mission is `Contract` and a science expedition is `Science`. Keep the values short and do not expose internal mission-class names in this column.
- [ ] **Use a fixed live-mission duration lookup by target location.** Mission Type should not independently change duration. A Contract and a Science mission targeting the same location should use the same configured live duration. Each supported Kerbin biome, KSC/local Pre-Orbit target, and celestial body has one manually assigned travel time in the tables/rules below.
- [ ] **Use explicit Pre-Orbit live durations.** Directed Power, Mass, and Control Contract live missions use a fixed 5-day local/KSC live duration. Biome I-V Contract live missions use the configured duration of their exact target biome: Grasslands 10 days, Highlands 10 days, Mountains 20 days, Deserts 30 days, and Ice Caps 90 days. Contract difficulty still comes from the Contract difficulty table rather than the Science-difficulty value for the location.
- [ ] **Treat the duration tables as authoritative balance data.** Do not calculate travel time from physical distance, orbital distance, launch windows, phase angles, live planet positions, or vessel state. The simulation should look up the configured value for the target location and use it directly.
- [ ] **Keep location durations configurable and stable.** Use stable project-owned location IDs so these manual travel times can be balanced later without changing mission logic or save interpretation. Unknown locations should fail safely rather than borrowing an unrelated travel time.
- [ ] **Give every launched mission persistent timing and difficulty data.** Store the launch date, configured base duration, expected completion date, mission type, target, assigned Kerbals, mission difficulty from 1 to 10, deterministic outcome seed, and any science subject/reward data required by science expeditions. Derive elapsed time, progress, and ETA from the stored dates rather than persisting changing presentation values.
- [ ] **Do not launch the same one-off target twice for one rival.** While a rival already has a one-off Contract in Live Mission Progress, exclude that objective from the rival's next Contract-target selection. Likewise, do not prepare or launch the same Science subject twice within one rival while that subject is already in its Science preparation or live-mission state. Satellite-network Contracts are repeatable and are exempt from the one-off duplicate-target rule because multiple satellite launches are part of their purpose.
- [ ] **Do not complete a rival mission at launch.** Reaching 100% `Launch Progress` only starts the live mission. A Contract remains incomplete, and a Science expedition grants no Science, until the configured live duration has finished and the mission passes its final success check.
- [ ] **Let launched Contract missions finish even if sponsor funding expires.** Once a Contract has moved into Live Mission Progress, later expiry of its Objective Funding Contract does not cancel the live mission. Resolve it normally at its stored completion time; a successful result still records the objective and may unlock later progression, but an already-expired sponsor Contract provides no funding. If the funding Contract expires while the rival is still preparing it and the mission has not launched, abandon that preparation and select another valid target.
- [ ] **Resolve live missions on the normal five-second simulation refresh.** Contract and Science live missions should be checked during the normal repeating rival/campaign refresh and resolve on the first refresh at or after their stored completion universal time. They must not wait for a campaign funding boundary. If time warp or a reload skips past the completion time, resolve them on the next refresh using their stored deterministic state.
- [ ] **Use mission difficulty to determine final Success Chance.** Every live Contract and Science mission uses a difficulty rating from 1 to 10. Contract missions use the contract's difficulty. Science missions derive difficulty from the science target using the lookup tables below. Difficulty 1 has a 90% Success Chance, and each additional difficulty level reduces Success Chance by 5 percentage points, down to 45% at difficulty 10. Use `Success Chance = 95% - (Difficulty × 5%)` for valid difficulty values 1–10.
- [ ] **Use biome-specific science difficulty only on Kerbin.** Kerbin surface Science missions use the target Kerbin biome to choose their 1–10 difficulty. Maintain one compact Kerbin biome difficulty table for Shores, Water, Grasslands, Highlands, Mountains, Deserts, Badlands, Tundra, Ice Caps, Northern Ice Shelf, and Southern Ice Shelf. Do not create equivalent per-biome difficulty tables for other celestial bodies.
- [ ] **Use fixed KSC location-biome science settings.** Science missions targeting KSC location biomes use a fixed 5-day live duration and Difficulty 1. Keep the KSC locations listed explicitly in the table below so these special local science targets do not inherit the wider Shores duration.
- [ ] **Use one science difficulty per supported non-Kerbin body.** Mun, Minmus, the planets, and their moons with implemented mission content each use one body-wide 1–10 difficulty regardless of the target biome. The biome may still be tracked as part of the exact science subject and shown in the UI, but it does not modify Success Chance outside Kerbin. Do not include the Sun as a selectable rival Science body until Sun mission/Contract content exists.
- [ ] **Use the Kerbin Orbit science setting for orbital science around Kerbin.** Kerbin Orbit uses a 10-day live duration and Difficulty 3 for science missions that use the Kerbin-orbit target rather than a surface biome or KSC location biome.
- [ ] **Keep science difficulty configuration compact.** The science difficulty balance data should consist of regular Kerbin biome entries, the fixed KSC location-biome rule, Kerbin Orbit, and one entry for each supported non-Kerbin celestial body. Do not maintain a database containing every biome on every moon and planet.
- [ ] **Resolve the mission with one deterministic outcome roll after its duration ends.** When the expected completion date is reached, generate the mission's Success Chance roll from its persisted outcome seed. A successful check completes the result; otherwise the mission fails. The result must not be pre-rolled or exposed before the live duration has elapsed, and save/reload must not change it.
- [ ] **Apply rewards only after a successful live mission.** On a successful Contract mission, mark the objective complete and apply any related satellite/infrastructure result. On a successful Science mission, apply the shared-subject rule above, record the subject as completed for the rival, and award only the Science still available from that subject at completion. No successful gameplay result is granted before this point.
- [ ] **Lose the spacecraft when a live mission fails.** A failed Contract or Science mission grants no objective completion, satellite/infrastructure result, or Science reward, and its simulated spacecraft is lost.
- [ ] **Apply a 25% Kerbal-loss chance independently to every assigned Kerbal on a failed mission.** For each Kerbal assigned to a failed crewed mission, make an independent 25% loss check. Remove every Kerbal whose check fails from the rival's employed roster; all surviving assigned Kerbals return to availability.
- [ ] **Charge 50,000 Funds insurance for each Kerbal lost.** Every Kerbal lost on a failed live mission adds 50,000 Funds to the rival's pending insurance deduction. Multiple losses stack; for example, two Kerbals lost creates a 100,000-Funds deduction at the next campaign funding boundary.
- [ ] **Start every rival with one employed Kerbal.** New rival programmes begin with one living employed Kerbal. Older saves that predate the rival roster fields should also default each rival to one employed Kerbal when the new state is introduced.
- [ ] **Track each rival's employed Kerbal roster.** Persist the number of living Kerbals employed by each rival. Derive the number currently assigned to live missions and therefore the number available for new launches from active live-mission assignments. Kerbals assigned to a live mission remain unavailable until that mission resolves.
- [ ] **Use data-driven Contract crew requirements.** Every current crewed Contract requires exactly 1 Kerbal, while every current uncrewed/probe Contract and every satellite-network launch requires 0 Kerbals. Keep `RequiredKerbalCount` explicit on Contract definitions because future Contracts may require more than one Kerbal; do not reduce crew requirements back to a simple crewed/uncrewed boolean in rival simulation.
- [ ] **Use the Astronaut Complex as the roster cap.** The rival may never employ more living Kerbals than its current Astronaut Complex level permits: Level 1 = 3 Kerbals, Level 2 = 8 Kerbals, Level 3 = no limit.
- [ ] **Hire missing Kerbals only when a mission is ready to launch.** When Launch Progress reaches 100%, first check whether enough Kerbals are available. If not, and the rival is below its Astronaut Complex roster cap and can pay the cost, hire the missing Kerbal or Kerbals for 100,000 Funds each, add them to the employed roster, and assign them immediately to the launch.
- [ ] **Wait at 100% Launch Progress when crew cannot be provided.** If a crew-required mission is ready but the rival is already at its Astronaut Complex roster cap, does not have enough available Kerbals, or cannot afford a required 100,000-Funds hire, keep the mission ready at 100% Launch Progress and do not launch it. Launch as soon as enough existing Kerbals return or an eligible hire can be made.
- [ ] **Do not impose a simultaneous limit on uncrewed live missions.** Uncrewed Contract missions do not consume Kerbal roster capacity and do not have a separate Mission Control or global simultaneous-live-mission cap. Their normal launch preparation, target eligibility, satellite capacity, and Funds requirements remain the relevant limits.
- [ ] **Reserve Mission Control satellite capacity for launched satellite-producing missions.** Mission Control's Level 1/2/unlimited cap applies to the rival's total satellites across all bodies. Before launching a mission that would create a satellite on success, count existing rival satellites plus active live missions that would create a satellite. A launched Probe Orbit or satellite-network mission therefore reserves one future satellite slot until it resolves; failure releases the reservation and success converts it into the actual satellite count. Launch preparation alone does not reserve a slot, but capacity must be re-checked at 100% before launch. Never delete pre-existing satellites when loading an older save that already exceeds the current facility cap; simply block new satellite-producing launches until capacity permits them.
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

These rules are authoritative for the current Contract catalogue. Every current crewed Contract requires exactly 1 Kerbal; every current uncrewed/probe Contract and satellite-network launch requires 0 Kerbals. `RequiredKerbalCount` remains an explicit Contract field so later Contracts may require more than one Kerbal.

For all four five-stage Pre-Orbit lines, use the same difficulty progression:

| Stage | Difficulty |
| --- | ---: |
| I | 1 |
| II | 1 |
| III | 2 |
| IV | 2 |
| V | 3 |

`Directed Power`, `Mass`, and `Biome` use 0 Kerbals at every stage. `Control` uses 1 Kerbal at every stage.

For orbital Contracts, the Probe difficulty is the base value for that body. The matching Crewed Orbit is always `Probe Difficulty + 1` and currently requires 1 Kerbal. The matching satellite-network launch uses the same difficulty as the Probe Orbit and requires 0 Kerbals.

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
| **Supported non-Kerbin bodies** | One body-wide difficulty per target body with implemented mission content. No per-biome difficulty entries. The Sun is excluded until corresponding content exists. |

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

These are manually assigned campaign travel times from Kerbin and body-wide Science difficulties. The values are authoritative balance data; no stock reference orbit, transfer calculation, or non-Kerbin biome difficulty lookup is required at runtime. The Sun is deliberately excluded until Sun mission/Contract content is added.

| Travel Rank | Target | Base Live Duration | Science Difficulty |
| ---: | --- | ---: | ---: |
| 1 | Kerbin Orbit | 10 days | 3 |
| 2 | Mun | 30 days | 4 |
| 3 | Minmus | 50 days | 4 |
| 4 | Moho | 124 days | 9 |
| 5 | Eve | 171 days | 10 |
| 6 | Gilly | 174 days | 5 |
| 7 | Duna | 303 days | 7 |
| 8 | Ike | 303 days | 5 |
| 9 | Dres | 604 days | 5 |
| 10 | Jool | 1,123 days | 7 |
| 11 | Laythe | 1,124 days | 8 |
| 12 | Vall | 1,124 days | 8 |
| 13 | Tylo | 1,125 days | 8 |
| 14 | Bop | 1,128 days | 8 |
| 15 | Pol | 1,131 days | 8 |
| 16 | Eeloo | 1,587 days | 10 |

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

`Start` is already researched for every rival. Rival tech gates stock Science experiments only in the initial implementation; it does not independently gate Contract launches or destinations. `EVA Report` and `Surface Sample` are not unlocked by a stock tech-tree node. Surface Sample requires R&D Level 2 plus the applicable surface-access progression. EVA Report requires Astronaut Complex Level 2 plus the applicable body/situation access.

### Rival Space Centre progression

- [ ] **Simulate each rival building and upgrading its own Space Centre.** Give every rival persistent facility levels based on the nine separately upgradable stock Career facilities. Rivals should begin with Level 1 facilities and spend their own Funds to improve them through Level 2 and Level 3 rather than receiving all programme capabilities automatically.
- [ ] **Simulate rival facility construction progress.** A facility upgrade should remain under construction for its full build period, with its previous level remaining active until construction completes.
- [ ] **Start facility construction only at funding times.** Rivals should evaluate and begin eligible Space Centre upgrades when a campaign funding boundary is processed rather than starting construction at arbitrary times between funding events.
- [ ] **Charge rival Funds when construction starts.** Upgrading a facility from Level 1 to Level 2 costs 100,000 Funds. Upgrading from Level 2 to Level 3 costs 250,000 Funds. Construction may only begin if the rival can pay the full upgrade cost.
- [ ] **Use fixed rival facility construction times.** Level 1 to Level 2 takes 180 campaign days. Level 2 to Level 3 takes 270 campaign days. Record the construction start date, completion date, source level, and target level so progress survives save/load correctly.
- [ ] **Choose one affordable facility upgrade randomly at a funding boundary.** If a rival has no active construction project when its construction-scheduling step runs at a funding boundary, build the set of eligible facility upgrades whose full cost the rival can currently afford and choose one randomly. If none are affordable, start no construction. Continue to enforce only one active facility construction project at a time.
- [ ] **Use facility levels as capability gates.** Launches, expeditions, technology purchases, crew activity, mission planning, and strategic behaviour should check the relevant simulated rival facilities. Rival tech adds experiment availability to Science Expeditions but does not independently gate Contracts in the initial model.
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
| **Astronaut Complex** | Roster limit 5; no off-Kerbin EVA | Roster limit 12; off-Kerbin EVA and flag planting available | Unlimited roster | Limits the rival's simulated Kerbal roster: Level 1 = 3 Kerbals, Level 2 = 8 Kerbals, Level 3 = no limit. A crew-required mission that reaches 100% Launch Progress may hire missing Kerbals for 100,000 Funds each only if this rival roster cap permits it; otherwise launch waits for a Kerbal to return. EVA Report additionally requires Level 2. |
| **Mission Control** | Maximum 2 active contracts; no flight planning | Maximum 7 active contracts; flight planning available when navigation requirements are met | Unlimited active contracts | Limits the rival's total satellites across all bodies: Level 1 = 3 satellites, Level 2 = 8 satellites, Level 3 = no limit. Existing satellites plus launched satellite-producing live missions count against the cap; it does not impose a general simultaneous-live-mission limit on uncrewed Contracts. |
| **Research and Development** | May unlock tech nodes costing up to 100 Science | May unlock tech nodes costing up to 500 Science; surface sampling/resource transfer capability becomes available with the other requirements met | No tech-node Science-cost limit | Directly gates the rival stock tech tree. Level 1 permits nodes through 90 Science, Level 2 permits nodes through 300 Science, and Level 3 permits the 550- and 1000-Science nodes. Surface Sample additionally requires R&D Level 2 and unlocked surface access for the target body. |
| **Vehicle Assembly Building (VAB)** | 30-part craft limit; no action groups | 255-part craft limit; basic action groups | Unlimited parts; full action groups | Modifies normal rival Launch Progress Chance only: Level 1 = +15%; Level 2 = additional +3%; Level 3 = additional +3%. It does not gate destinations. |
| **Spaceplane Hangar (SPH)** | 30-part craft limit; no action groups | 255-part craft limit; basic action groups | Unlimited parts; full action groups | Modifies Launch Science Expedition preparation: Level 1 = Science Launch Progress Chance +20%; Level 2 = additional +3%; Level 3 = additional +3%. |
| **Launch Pad** | Small launch vehicle size/mass limit; about 18 t maximum mass | Medium launch vehicle limits; about 140 t maximum mass | Unlimited stock size/mass | Modifies normal rival Launch Progress Chance only: Level 1 = +15%; Level 2 = additional +3%; Level 3 = additional +3%. It does not gate destinations. |
| **Runway** | Small aircraft size/mass limit; about 18 t maximum mass | Medium aircraft limits; about 140 t maximum mass | Unlimited stock size/mass | Modifies Launch Science Expedition preparation: Level 1 = Science Launch Progress Chance +20%; Level 2 = additional +3%; Level 3 = additional +3%. |
| **Tracking Station** | Basic orbital tracking | Patched-conic/navigation capability | Adds unowned-object tracking and full stock tracking capability | Sole rival destination facility gate: Level 1 permits Pre-Orbit and Kerbin-orbit Contracts; Level 2 permits Mun and Minmus missions; Level 3 permits other planets and their moons. Mission progression and Science-experiment tech gates still apply where relevant. |

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
- [ ] **Use rival tech only for Science-experiment availability initially.** Do not make Contract launches or destinations depend on rival tech in this implementation. The existing Contract/sponsor progression plus Tracking Station, facilities, roster, and Funds determine Contract eligibility; rival tech determines which Science experiments are selectable and may be expanded later.
- [ ] **Store new rival balance data in the existing `CampaignSettings.cfg`.** Extend the current settings/config loader rather than introducing another balance file. Mission-location durations/difficulties, Kerbal costs, facility values, construction values, research timing, and other tuneable rival rules should use explicit named settings or config nodes.
- [ ] **Use first completion wins for one shared Science pool.** Launching an expedition does not reserve a subject across agencies. The player and all rivals may race the same subject; the first successful completion receives the remaining Science and exhausts it. Later successful rival completions receive 0 Science for the exhausted subject but still record it as completed for that rival. Resolve exact completion-time ties deterministically by stable agency ID.
- [ ] **Start each rival with one employed Kerbal.** The initial rival roster and the compatibility default for older saves missing roster state are both one living employed Kerbal.
- [ ] **Keep crew requirements data-driven.** Current crewed Contracts and current Science Expeditions require 1 Kerbal, and current uncrewed/probe/network launches require 0. Keep explicit required-Kerbal counts because later Contracts may require more than one without changing the roster/live-mission architecture.
- [ ] **Do not impose a simultaneous uncrewed-live-mission limit.** Uncrewed Contracts may coexist without a Mission Control/global live-mission slot system; other normal eligibility, funding, satellite and preparation rules still apply.
- [ ] **Prevent duplicate one-off targets within one rival.** A rival may not prepare/launch the same one-off objective again while that objective is already live, and may not prepare/launch the same Science subject twice within its own programme. Repeatable satellite-network missions remain exempt. Different rivals may still race the same objective or Science subject.
- [ ] **Reserve satellite capacity for launched satellite-producing missions.** Mission Control caps the rival's total satellite population across all bodies. Existing satellites plus active live Probe/network missions that would add satellites on success count against the cap; preparation does not reserve capacity, failure releases a live reservation, and success turns the reservation into an actual satellite. Older saves that already exceed the cap keep their satellites but cannot add more until capacity permits.
- [ ] **Use fixed Pre-Orbit live durations.** Directed Power, Mass, and Control use 5-day live missions. Biome Contracts use their exact target-biome duration from the Kerbin table. Their Contract difficulty still comes from the Contract database rather than the Science-difficulty field.
- [ ] **Let launched missions survive sponsor expiry without funding.** A live Contract continues to its result even if its sponsor Contract expires after launch. Success still records the objective/progression result, but an expired Contract pays no later funding. A Contract that expires before launch invalidates the preparation target and is replaced.
- [ ] **Roll crew losses independently per Kerbal.** Every Kerbal assigned to a failed crewed mission has an independent 25% loss chance. Surviving Kerbals return to availability.
- [ ] **Charge insurance per lost Kerbal.** Every lost Kerbal adds 50,000 Funds to pending insurance; losses stack additively until the next funding boundary applies the deduction.
- [ ] **Allow rival Funds to become negative from funding deductions.** Gross funding minus payroll and the entire pending insurance amount is applied as a signed funding result. Do not floor the payout or Funds at zero and do not carry unpaid insurance forward after it has been applied.
- [ ] **Do not charge a separate Funds cost for Science Expeditions initially.** Science launch preparation has no additional expedition fee beyond normal staffing/hiring/payroll and mission risk.
- [ ] **Choose Science Expedition targets randomly from the valid set.** Do not optimise expedition selection by Science reward, duration or difficulty in the initial model.
- [ ] **Unlock all Kerbin Science biomes from the start.** Regular Kerbin surface biomes and supported KSC location biomes are available immediately when the experiment/situation is valid; the Biome Contract line is not a Science-access gate.
- [ ] **Ignore the Sun as a rival Science destination until content exists.** Do not select or configure active rival Sun expeditions merely because stock KSP has Sun Science subjects. Add Sun Science only alongside explicit Sun mission/Contract progression later.
- [ ] **Gate EVA Report with Astronaut Complex Level 2 plus normal situation access.** EVA Report does not need a tech node; Surface Sample keeps its separate R&D Level 2 plus surface-access rule.
- [ ] **Treat Start as already researched.** New rivals and old-save compatibility defaults begin with the zero-cost Start node researched.
- [ ] **Choose research only at funding boundaries, one project at a time.** If there is no active research project, select from affordable eligible nodes at the funding event. Choose the cheapest Science-cost tier and select randomly among equal-cost nodes; deduct the Science cost when the project is selected. Science gained between funding events waits until the next boundary.
- [ ] **Choose facility construction randomly from affordable eligible upgrades.** At a funding boundary, if no construction project is active, select randomly from upgrades the rival can fully afford. If no upgrade is affordable, do nothing.
- [ ] **Use fixed Contract crew counts and difficulty rules.** Current crewed Contracts require 1 Kerbal; current uncrewed/probe Contracts and satellite-network launches require 0. Pre-Orbit stages I-V use difficulty `1, 1, 2, 2, 3` in every line. Each orbital Crewed mission is one difficulty higher than its matching Probe mission, and each satellite-network launch matches its body's Probe difficulty. Use the authoritative Contract table above for the body-specific Probe values.
- [ ] **Use the Tracking Station as the sole destination facility gate.** Level 1 permits Pre-Orbit and Kerbin-orbit Contracts, Level 2 permits Mun and Minmus missions, and Level 3 permits other planets and moons. VAB and Launch Pad affect Launch Progress Chance only and do not duplicate the Tracking Station destination gate.
- [ ] **Gate Surface Sample with R&D Level 2 plus surface access.** The rival must have R&D Level 2 and must already have the target body's surface Science access before Surface Sample becomes a valid expedition subject.
- [ ] **Use mission progression to unlock Science situations.** Kerbin begins with Landed, Splashed, Flying Low and Flying High available; completing Probe Orbit unlocks Kerbin Low Space and High Space. On other supported bodies, successful Probe Orbit unlocks Low Space and High Space, while a successful landing Contract unlocks Landed, Splashed, Flying Low and Flying High wherever those stock situations are valid.
- [ ] **Resolve live missions independently of funding boundaries.** Live Contract and Science missions are checked on the normal five-second simulation refresh and resolve on the first refresh at or after their completion universal time. Funding dates do not delay live-mission outcomes.
- [ ] **Use the agreed funding-boundary processing sequence.** When a funding boundary is crossed, first advance/catch up all scheduled rival events whose universal time is on or before that boundary, including due normal 5-day Launch Progress checks, due daily Science Launch Progress checks, and due live-mission completions. Then complete due research and facility construction, update funding eligibility, calculate income, apply payroll and insurance and credit the signed payout, choose new research, choose new affordable facility construction, and finally run the sponsor review for the next funding period. Preserve chronological universal-time ordering during time-warp/reload catch-up so only events that actually occurred on or before that boundary can affect it.

### Recommended runtime state ownership

Keep the existing module ownership: `Agencies/` owns mutable agency state, `Rivals/` owns rival simulation behaviour, `Persistence/` owns save-state transforms, `Core/` owns balance settings, `KspIntegration/` owns raw KSP science access/config loading, `Campaign/` coordinates funding boundaries, and `UI/` remains read-only presentation. Do not create a new top-level source module for this design.

Recommended `RivalProgramState` contents:

| State | Recommended representation | Notes |
| --- | --- | --- |
| Stored Science | `double` | Persisted rival Science balance. |
| Science launch preparation | `ScienceLaunchPreparationState` | One current expedition being prepared independently of normal Contract launch preparation. |
| Live missions | `List<RivalLiveMissionState>` | Contains all launched Contract and Science missions still awaiting resolution; no separate uncrewed slot-count state is required. |
| Completed rival Science subjects | `HashSet<ScienceSubjectKey>` | Prevents a rival repeating its own completed subjects. |
| Researched tech | `HashSet<string>` | Stable project-owned tech IDs; `Start` is present by default. |
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

`RequiredKerbals` is 1 for every current Science Expedition but remains explicit for future content. `PlannedScienceReward` is presentation/planning data from the subject at selection/launch; the actual award at successful mission completion is limited to the Science still remaining in the shared stock subject. Do not persist Progress Chance, Estimated Launch, Expedition Range, unlocked-experiment display text, Kerbals Available, a Science Expedition Funds cost, or the `Ready`/`Hire At Launch`/`Waiting For Kerbal` UI state. Derive those values from authoritative facility, tech, mission, roster, Funds, and launch-preparation state.

### Live mission state

Use a simple data class rather than an inheritance hierarchy:

```text
RivalLiveMissionState
  MissionSequence
  MissionType                 // Contract or Science
  ContractId                  // Objective or satellite-network target ID for Contract missions
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

At 100% normal or Science Launch Progress, the simulation should re-check target validity, crew requirements, and any Mission Control satellite capacity required by the launch, then create the live-mission record, reset the relevant preparation state, and begin the next eligible preparation. Objective completion, satellite/infrastructure results, and Science rewards occur only when the live mission later resolves successfully. Uncrewed live missions do not consume a separate mission-slot resource.

Live missions are evaluated during the normal five-second simulation refresh rather than as a funding-boundary action. When a refresh crosses a mission's completion universal time, resolve that mission immediately using its stored deterministic state; time warp and reload catch up on the next refresh. Live one-off Contract targets and Science subjects remain excluded from that rival's duplicate target selection until the live mission resolves. A live Contract is not cancelled merely because its sponsor funding expires after launch.

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

Individual rival Kerbal names/identities are not required for the initial model; roster counts and assigned counts are sufficient unless a later design explicitly needs named rival Kerbals. All crew allocation logic must use the mission's explicit required count rather than assume that every future crewed mission requires one.

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

Store only facility levels. Derive funding, roster caps, satellite caps, destination gates, tech-cost gates, normal Launch Progress Chance, and Science Launch Progress Chance from those levels. Mission Control does not derive or store an uncrewed live-mission count limit. Its satellite capacity is the total of existing rival satellites plus reservations represented by launched live missions that would create a satellite on success. Tracking Station alone supplies the facility-level destination gate; VAB and Launch Pad do not duplicate that responsibility.

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

Use stable tech IDs as gameplay identity and keep researched IDs in a `HashSet<string>`. Copy the approved stock KSP 1.12 prerequisite relationships into this fixed project-owned catalogue rather than reading a modded player tech tree dynamically. `Start` is already researched for every new/default rival. Do not modify the player's stock Research and Development state to represent rival technology, and do not use rival tech nodes as an extra Contract/destination gate in the initial implementation.

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
kerbin:preorbit-local
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

Resolution order for mission locations:

```text
Directed Power / Mass / Control Contract -> kerbin:preorbit-local (5 days; Contract difficulty stays definition-owned)
Biome Contract                           -> exact Kerbin biome entry
KSC location science                     -> exact KSC location entry
Kerbin surface science                   -> Kerbin biome entry
Kerbin orbit                             -> Kerbin Orbit entry
Supported non-Kerbin mission/science     -> body-wide entry; biome does not change difficulty
Sun                                      -> unavailable until explicit Sun content exists
```

The lookup supplies values when a mission launches; the live mission then keeps its snapped values. Target eligibility is separately constrained by Tracking Station level, completed mission progression, and Science-experiment tech where applicable.

### Contract definition additions

Add explicit Contract difficulty and crew requirement data to objective definitions rather than deriving either value from display wording. Recommended fields are:

```text
Difficulty            // 1-10 from the authoritative Contract table/rules above
RequiredKerbalCount    // current values 1 for crewed, 0 for uncrewed; future Contracts may use >1
```

Keep the existing crew-category enum where it remains useful, but use `RequiredKerbalCount` for rival crew reservation. Do not infer a required count from the crew-category enum at runtime; the explicit count is future-proofed for later multi-Kerbal Contracts. The current difficulty values and crew counts are now defined and are no longer an open balance decision.

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
Catch up all rival scheduled events with event time <= funding boundary
  - normal 5-day Launch Progress checks
  - daily Science Launch Progress checks
  - live Contract/Science mission completions
-> Complete due research and facility construction
-> Update funding eligibility
-> Calculate income
-> Apply payroll and insurance; credit the signed payout
-> Choose new research
-> Choose new affordable facility construction
-> Sponsor review for the next funding period
```

Live missions still normally resolve on the five-second refresh. The catch-up step exists for time warp/reload or any refresh that crosses a funding boundary, and must preserve chronological universal-time ordering so no new facility level, funding payout, research result, or sponsor offer can retroactively affect a rival event whose timestamp came earlier.

A live Objective Contract that was launched before its sponsor contract expired still resolves normally. If it succeeds after expiry, record the objective/progression result but do not revive or pay the expired funding contract.

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

Persist gameplay identity and authoritative mutable state, not presentation strings or values that can be safely derived. Older saves missing the new fields should receive safe defaults: zero stored Science, Level 1 facilities, one employed Kerbal, `Start` researched, no live missions, no construction, no active research, no rival-completed Science subjects, and no pending insurance. Rival Funds are not clamped to zero when restoring or applying the new funding rules because negative rival balances are valid state. If an older save already contains more simulated rival satellites than its default Level 1 Mission Control cap, retain those satellites and use the cap only to prevent additional satellite-producing launches until capacity permits them.

### KSP science boundary

Add a small science integration component inside the existing `KspIntegration/` module, for example `KspScienceAdapter`, responsible for resolving the approved stock experiments/subjects, reading the relevant stock Science state/reward, and consuming the player's exact matching subject after a rival wins it. Rival simulation and persistence must operate on project-owned `ScienceSubjectKey` values rather than raw KSP `ResearchAndDevelopment` or `ScienceSubject` objects.

Under the shared first-completion-wins rule, the adapter must re-check the subject at live-mission completion. If the subject is fully depleted by the player or another earlier rival success, return zero available Science. If partially depleted, return only the remaining value and then exhaust it after the rival's successful result. This check is authoritative over any planned reward snapshot recorded when the expedition was selected/launched. Cross-rival races are therefore resolved against the same player-visible stock subject pool rather than separate rival-only copies.

The initial adapter should ignore Sun subjects even if stock KSP exposes them, because Sun expedition progression is deferred until explicit mission/Contract content exists.

### Expected project changes

The recommended design should fit within the existing project modules. Expected changes when implementation is approved:

| Path | Expected change |
| --- | --- |
| `Agencies/AgencyState.cs` | Attach/access composed rival-only programme state and permit valid negative rival Funds. |
| `Agencies/RivalProgramState.cs` | New data-only rival state classes/enums. |
| `Rivals/RivalSimulation.cs` | Launch preparation, duplicate-target checks, five-second live mission resolution, per-Kerbal deterministic casualties, roster, satellite-capacity reservations, facilities, construction, destination/situation gates, Science-only tech gating, and funding-boundary research selection. |
| `Rivals/RivalTechCatalogue.cs` | Fixed project-owned stock rival tech definitions and prerequisite graph, with Start researched by default. |
| `Core/CampaignSettings.cs` | New rival balance/location settings, including the Pre-Orbit local duration and supported body lookup. |
| `KspIntegration/CampaignSettingsLoader.cs` | Parse the added settings from existing `CampaignSettings.cfg`. |
| `GameData/TheRaceForSpace/Config/CampaignSettings.cfg` | Store tuneable rival balance and location values. |
| `Persistence/RivalAgenciesSaveState.cs` | Persist the composed rival programme state under existing rival save records, including negative Funds and the new compatibility defaults. |
| `Campaign/CampaignController.cs` | Coordinate normal five-second rival refresh plus chronological scheduled-event catch-up, the agreed funding-boundary sequence, signed payroll/insurance, construction/research completion and selection, and shared funding calculations. |
| `KspIntegration/KspScienceAdapter.cs` | Keep stock KSP Science subject access/depletion at the integration boundary and apply the cross-agency first-completion pool. |
| `Objectives/ObjectiveDefinition.cs` and catalogue | Carry Contract difficulty and explicit required rival crew counts, retaining support for future values above one. |
| `UI/CommandCenterWindow.cs` | Render the approved Rival Agencies dashboard from read-only state, including signed next payout values. |
| `tests/` | Cover Pre-Orbit duration mapping, duplicate one-off target prevention, Mission Control satellite reservations, supported-body/Sun exclusion, all-Kerbin-biome access, EVA and Surface Sample gates, Science-only tech gating, location lookup, Tracking Station destination gates, Science situation progression, success chance, deterministic five-second outcome/casualty catch-up, Contract difficulty/crew mapping, future multi-crew counts, partial/complete player/rival shared-Science races, exact-time deterministic Science ties, one-Kerbal defaults, roster/hiring/payroll, negative funding, unlimited uncrewed live missions, expired-Contract live completion without funding, random affordable construction selection, chronological funding-boundary event catch-up, funding-boundary research selection, live mission resolution, and persistence round-trips. |
| `docs/STRUCTURE.md` / `docs/CODE_OVERVIEW.md` | Document final ownership/flow after implementation. |

Do not introduce new managers, services, factories, interfaces, class hierarchies, external dependencies, or top-level source modules unless a concrete implementation problem proves the current architecture cannot support the required behaviour.

### Additional locked implementation edge rules

- [ ] **Resolve Contract-vs-Science crew contention by ready time.** If both the normal Contract preparation and Science preparation need the same limited Kerbal capacity, launch whichever preparation first reached 100% `Launch Progress`. Persist the ready universal time for each crew-required preparation so save/load and time warp preserve this ordering. If both preparations reach 100% at exactly the same universal time, give the Contract launch priority, then re-evaluate the Science launch with the remaining crew/capacity.
- [ ] **Abandon a depleted Science target before launch, but not after launch.** Re-check the selected stock Science subject while an expedition is still in preparation. If the player or another rival exhausts that subject before this rival launches, cancel that preparation and randomly select another currently valid subject. Once the expedition has launched into Live Mission Progress, do not cancel it because the pool is later exhausted; it still resolves normally and may succeed for 0 Science.
- [ ] **Retire the legacy global rival progress-chance config when the facility model is implemented.** Remove `rivalProgressChancePercent` from active `CampaignSettings.cfg` gameplay and stop reading it in `CampaignSettingsLoader` once normal Launch Progress Chance is derived from VAB + Launch Pad. Do not keep a user-facing setting that silently does nothing; the new facility-based chances become the single source of truth.

The ready-time rule requires one authoritative readiness timestamp for each preparation path. Add a persisted ready universal time to the normal rival Contract preparation state and to `ScienceLaunchPreparationState`; clear it whenever that preparation target resets or launches. This timestamp is gameplay ordering state, not UI-only state.

### Approved implementation architecture for the rival expansion

This file split is the approved maintainability plan for implementing the rival-programme design. It refines the earlier expected-project-changes table without changing the existing top-level module architecture. Keep `Core/`, `Campaign/`, `Agencies/`, `Rivals/`, `Objectives/`, `Funding/`, `Tracking/`, `Persistence/`, `KspIntegration/`, and `UI/` as the project boundaries; do not add a new top-level source module for the rival expansion.

- [ ] **Keep rival programme state together in `Agencies/RivalProgramState.cs`.** Put `RivalProgramState` and the closely related small data-only types/enums there, including Science launch preparation, live mission state, Science subject identity, facility/construction state, research state, and facility/mission enums. Do not create a separate source file for every tiny state type unless one later grows into a substantial independent responsibility.
- [ ] **Keep `Rivals/RivalSimulation.cs` as the single rival chronological coordinator.** Preserve this as the public rival-simulation entry point and conductor. It owns the normal Contract preparation flow, determines the next due rival event in universal-time order, arbitrates Contract-vs-Science launch readiness/crew contention, and delegates substantial specialist behaviour to the files below. It must not become a second campaign controller or hide the funding-boundary sequence from `CampaignController`.
- [ ] **Add `Rivals/RivalScienceSimulation.cs` for Science preparation rules.** Own valid Science subject selection, experiment/tech eligibility, body/situation/biome access, daily Launch Progress checks, depleted-target replacement, Surface Sample/EVA gates, Science launch ETA inputs, and transition readiness into a live Science mission. It works on project-owned Science/state values and does not hold raw KSP `ScienceSubject`/`ResearchAndDevelopment` objects.
- [ ] **Add `Rivals/RivalLiveMissionSimulation.cs` for launched-mission behaviour.** Own creation and final resolution of persistent Contract/Science live missions, duration/difficulty lookup consumption, success chance, deterministic outcome/casualty rolls, Kerbal return/loss, pending insurance, satellite-producing success/failure, shared-Science success handling, and post-launch sponsor-expiry behaviour.
- [ ] **Add `Rivals/RivalDevelopmentSimulation.cs` for funding-boundary programme development.** Own rival research completion/selection and Space Centre construction completion/selection because both are funding-boundary development activities. Keep small facility, roster-cap, payroll, launch-chance, and affordability calculations close to the owning behaviour instead of creating separate manager/service classes for each one.
- [ ] **Keep `Rivals/RivalTechCatalogue.cs` as fixed definition data.** Store the project-owned stock KSP 1.12 rival tech nodes, prerequisite relationships, Science costs, display names, and experiment unlocks here. The catalogue is deterministic definition data; it does not schedule research or query the player's live tech tree.
- [ ] **Add `KspIntegration/KspScienceAdapter.cs` as the only stock-Science boundary.** Resolve stock experiments/subjects, read remaining Science, and consume the player's matching subject here. Convert KSP objects into project-owned Science subject/value data before returning to rival simulation so raw KSP Science objects do not leak into `Rivals/` or persistence.
- [ ] **Split per-rival persistence into `Persistence/RivalProgramSaveState.cs`.** Keep `RivalAgenciesSaveState.cs` responsible for the collection/top-level `RIVAL_AGENCIES` section and stable agency matching, while the new data-only save-state class owns capture/load/apply of one rival's expanded programme state. This is a source-file split inside the existing persistence subsystem, not a new save section or persistence manager.
- [ ] **Split Rival Agencies drawing into `UI/CommandCenterWindow.Rivals.cs`.** Make `CommandCenterWindow` a partial class and move only the Rival Agencies tab drawing/helper methods into the second file. Keep one `CommandCenterWindow` MonoBehaviour, one window lifecycle, and read-only UI behaviour; do not create another UI controller or gameplay owner.

The chronological event flow is more important than the physical file split. Specialist rival files must not create their own independent realtime schedulers or bulk catch-up loops. `RivalSimulation` should advance a rival toward a target universal time by repeatedly finding and processing the earliest due rival event, then re-evaluating the next event until the target time is reached. Events include normal 5-day Contract Launch Progress checks, daily Science Launch Progress checks, live-mission completions, and any launch that becomes possible because a preceding event returned crew/capacity. Exact-time ties must use the already documented deterministic tie rules.

```text
ModRuntime (existing 5-second campaign refresh)
  -> CampaignController.Refresh(...)
      -> RivalSimulation advances rivals chronologically to the requested UT
          -> normal Contract preparation event
          -> RivalScienceSimulation Science-preparation event
          -> RivalLiveMissionSimulation live-mission event
      -> funding boundary when crossed
          -> RivalSimulation catches scheduled rival events up to the boundary UT
          -> RivalDevelopmentSimulation completes due research/construction
          -> funding eligibility and payout sequence remains visible in CampaignController
          -> RivalDevelopmentSimulation selects new research/construction
          -> sponsor review
```

Keep the existing runtime cadence unchanged: approximately 1 second for active player Flight Contract telemetry, 5 seconds for campaign/rival refresh, and 20 seconds for the broad loaded/unloaded player-vessel scan. Daily Science checks, 5-day rival Contract checks, live mission durations, 90-day research, and 180/270-day construction are universal-time scheduled events evaluated through the existing campaign refresh; they do not receive additional Unity polling loops.

Do not split the implementation into narrow classes such as `RivalKerbalManager`, `RivalInsuranceManager`, `RivalLaunchChanceService`, `RivalDestinationGateService`, `RivalSatelliteReservationManager`, or `RivalRandomService`. Keep small calculations with the substantial feature that owns them. `CampaignController` remains the high-level campaign/funding coordinator, `ModRuntime` remains the scheduler, and the exact funding-day sequence should stay readable in `CampaignController` rather than being hidden behind a generic process-everything abstraction.

After implementation, update `docs/STRUCTURE.md` and `docs/CODE_OVERVIEW.md` to document these final file responsibilities and the chronological rival event flow.

### Remaining design decisions

The decisions below are intentionally still open and can be handled in later passes. Items already decided above should not be re-added to this list unless the design changes.

- [ ] **Complete the base/station content balance pass.** Define crew counts, difficulty, qualification criteria, payouts, maintenance/loss rules and exact progression for Desert, Polar, Kerbin orbital, Mun and Minmus infrastructure content.

### Implementation approval gate

The data model above intentionally changes the persisted `RIVAL_AGENCIES` schema and extends the existing campaign configuration format. Those are compatibility-sensitive structural changes under `AGENTS.md`. Before implementation, explicitly confirm the final save/config additions and obtain approval for that implementation step. Documentation-only refinement of this design does not itself require that structural approval.