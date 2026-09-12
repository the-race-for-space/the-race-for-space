# Content v0.6 - Live KSP Acceptance Checklist

This checklist covers Content v0.6 behaviour that the standalone automated tests cannot fully prove without a real Kerbal Space Program 1.12.x installation.

It supplements [`KERBAL_CONTRACTS_V0_5_TESTING.md`](KERBAL_CONTRACTS_V0_5_TESTING.md). Use the older checklist for detailed Pre-Orbit vessel-tracking acceptance and use this checklist for the v0.6 rival programme, Science, development, funding, and UI additions.

Use a disposable Career save where possible.

## Before testing

1. Run the automated logic and controller regressions:

   ```bash
   bash tools/run-logic-tests.sh
   ```

2. Build and deploy the current `Alpha/Content-v0.6` mod against KSP 1.12.x.
3. Deploy both the current DLL and configuration:

   ```text
   GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll
   GameData/TheRaceForSpace/Config/CampaignSettings.cfg
   ```

4. Start KSP and confirm there are no repeated The Race for Space exceptions in `KSP.log`.
5. Create a fresh Career save for fresh-state checks. Keep a second disposable save for timewarp, failure, and save-edit-assisted checks if needed.

The automated suite already covers deterministic rule-level cases such as exact-time Science tie ordering, casualty rolls, satellite reservations, crew contention, research selection, construction completion, negative funding, live-mission outcome signals, genuine research/facility completion signals, and chronological large-time jumps. The live checks below concentrate on KSP boundaries, persistence, presentation, and end-to-end behaviour.

---

# 1. Fresh v0.6 rival programme state

Open **Command Center > Rival Agencies** soon after creating a fresh Career save.

For each rival, confirm:

- the agency has the configured starting Funds;
- **Stored Science** begins at 0;
- **Kerbals Employed** begins at 1;
- every rival facility begins at Level 1;
- the rival tech tree begins with **Start** researched;
- there is no active research project or facility construction before the first eligible funding-boundary selection;
- normal Contract launch preparation and Launch Science Expedition preparation are separate states.

Confirm the rival selector appears below the next-funding date:

- only active rivals have buttons;
- up to four rival slots are supported;
- the selected rival uses white button text and unselected rivals use grey button text;
- selecting another rival changes the detailed card to that agency only and returns the detail scroll to the top.

Confirm the selected rival card is presented in this order:

1. Programme Status
2. Live Mission Progress
3. Current Launch Programme / Launch Science Expedition
4. Funding
5. Construction
6. Facilities
7. Tech Tree / Research

At default Level 1 facilities, confirm the UI reports the same authoritative launch capability used by the simulation:

- normal Contract **Launch Progress Chance**: 30% per configured normal progress check from VAB + Launch Pad;
- Science **Progress Chance - daily**: 40% from SPH + Runway.

No UI action should itself advance rival progress, spend Funds, complete research, or complete construction.

---

# 2. Rival Contract launch preparation and live mission transition

Observe a rival preparing an offered Contract.

Confirm:

- the **Current Launch Programme** names an offered, valid target;
- Launch Progress increases only on the configured progress checks;
- a successful progress step charges the configured Contract progress cost;
- the displayed ETA changes consistently with remaining progress, chance, and affordability;
- reaching 100% Launch Progress does **not** immediately complete the objective;
- reaching 100% creates a **Contract** entry in **Live Mission Progress**;
- the live row shows target, location, duration, progress, ETA, and Success Chance;
- live progress derives from launch/completion dates rather than changing a separately stored percentage;
- the objective is recorded only when the live mission later resolves successfully.

For a Pre-Orbit target, confirm its live duration matches the configured local/biome duration rather than orbital distance or live vessel position.

If an objective funding Contract expires after the rival mission has already launched, confirm the live mission remains and resolves normally. The expired sponsor Contract should no longer provide funding, but successful mission progression should still be recorded.

---

# 3. Launch Science Expedition preparation

Observe a rival with at least one valid Science target.

Confirm the **Launch Science Expedition** section shows:

- **Experiment** as experiment name plus body, for example `Crew Report, Kerbin`;
- **Situation** as situation plus biome where applicable, for example `Landed, Grasslands`;
- Launch Progress;
- Progress Chance - daily;
- estimated launch time;
- Science Reward;
- **Kerbal Assigned** as available versus required crew, with `- none available` when a crewed expedition is at 100% progress and no Kerbal is available.

Confirm the panel does not repeat the separate unlocked-experiment list or Expedition Range because those capabilities are already shown elsewhere in the rival dashboard.

Confirm:

- Science launch preparation can progress at the same time as normal Contract preparation;
- Science preparation receives its progress opportunities once per Kerbin day;
- each successful check adds 10 percentage points;
- default 40% daily chance produces an average 25-day ETA from 0%, not a guaranteed date;
- preparing or launching a Science expedition does not charge a separate expedition Funds fee;
- reaching 100% launches a **Science** live mission rather than immediately awarding Science;
- a new valid Science preparation may begin while the previous Science expedition is still live.

A rival must not prepare or launch the same Science subject twice at the same time.

---

# 4. Tracking Station destination gates

Use save setup, normal long-term progression, or temporary test balance values if needed to reach each Tracking Station level.

Confirm Contract and Science destination access follows only the Tracking Station destination gate:

- Level 1: Pre-Orbit and Kerbin-orbit destinations;
- Level 2: Mun and Minmus become available;
- Level 3: other supported planets and moons become available.

Confirm VAB and Launch Pad levels change normal launch progress chance but do not independently grant destinations.

Confirm rival tech does not add a separate destination requirement to Contract launches. Rival tech gates Science experiments only in the current v0.6 design.

---

# 5. Kerbin Science access and experiment gates

On a fresh campaign, confirm valid unlocked experiments may target regular Kerbin surface biomes and supported KSC location biomes without first completing the Biome Contract line.

Confirm Kerbin Science situations behave as designed:

- Landed, Splashed, Flying Low, and Flying High are available from campaign start when the experiment itself is valid;
- completing Probe Orbit unlocks both Low Space and High Space around Kerbin.

Confirm the special experiment gates:

- **EVA Report** requires Astronaut Complex Level 2 plus otherwise-valid situation access;
- **Surface Sample** requires Research and Development Level 2 plus otherwise-valid surface access;
- facility level alone does not grant access to a body that the rival has not reached through campaign progression.

Confirm the Sun is not selected as a rival Science destination in v0.6.

---

# 6. Shared Science race

This is the most important live KSP boundary check because it crosses between project-owned rival state and stock KSP Science subjects.

Choose one stock Science subject that still has Science remaining. Arrange for the player and/or more than one rival to pursue that same subject if practical.

Confirm:

- launching a rival Science expedition does not reserve or consume the stock subject;
- the stock subject remains available while the rival mission is live;
- the first successful completion receives only the Science actually remaining at that moment;
- that completion consumes the matching remaining stock Science;
- if the player partially depleted the subject first, the rival receives only the remaining value;
- if another successful agency already exhausted it, a later successful rival receives 0 Science;
- a successful rival still records the Science subject as completed for that rival even when 0 Science remains;
- after a rival wins a stock Science subject, the player may still operate the experiment but receives 0 additional Science from that exact experiment/body/situation/biome subject;
- save/reload keeps a rival-consumed stock subject exhausted;
- no repeated exceptions are produced by the stock Science API interaction.

Exact same-UT rival ties are covered automatically and should resolve in stable agency-ID order. Manual reproduction is optional.

---

# 7. Crew capacity, contention, and hiring

Start with a rival that has the default single employed Kerbal.

For a crewed Contract or Science expedition, confirm:

- a launched mission removes its assigned Kerbal from the displayed available count while the mission is live;
- surviving Kerbals become available again when the mission resolves;
- an uncrewed mission does not consume a Kerbal;
- multiple uncrewed live missions are not blocked merely because another uncrewed mission is active.

Create a state where Contract and Science preparations both require the only available Kerbal.

Confirm:

- the preparation that became ready first gets the available crew;
- when their stored ready times are exactly equal, Contract has priority;
- the other preparation remains ready and waits rather than losing its progress;
- when the Kerbal returns, the waiting ready preparation can launch.

For normal Contract preparation, confirm the UI status distinguishes **Ready**, **Hire At Launch**, and **Waiting For Kerbal** correctly. For Science preparation at 100%, confirm a zero-available crew state is shown as `Kerbal Assigned: 0 available / 1 required - none available` for a one-Kerbal expedition.

If the Astronaut Complex roster limit and available Funds permit hiring, confirm missing Kerbals are hired only at the ready-to-launch point and the configured hire cost is charged then.

---

# 8. Mission Control satellite capacity and reservations

At Mission Control Level 1, confirm the rival cannot exceed the configured Level 1 satellite capacity.

A live satellite-producing mission must reserve capacity before it resolves. Confirm:

- existing qualifying rival satellites count against the limit;
- live Probe Orbit or Satellite Network missions that will produce a satellite also count against the limit;
- another satellite-producing mission does not launch when those existing plus reserved slots fill capacity;
- if a reserved live mission fails, its reservation disappears when the mission resolves;
- if it succeeds, the reservation becomes an actual rival satellite without double-counting;
- raising Mission Control level increases the capacity shown and used by the launch gate.

---

# 9. Mission failure, casualties, insurance, and result notification

Use a higher-difficulty crewed rival mission or a controlled test save to obtain a failure.

Confirm:

- mission failure does not record the Contract objective or award Science;
- each assigned Kerbal receives the independent configured casualty roll;
- only lost Kerbals permanently reduce **Kerbals Employed**;
- surviving assigned Kerbals return to availability when the live mission is removed;
- each casualty adds the configured insurance liability to **Pending Insurance**;
- the Live Mission failure notification states whether assigned Kerbal(s) escaped and survived or were lost;
- a casualty notification states the exact **Insurance Penalty to Pay** generated by that mission; at the current default this is 50,000 Funds per lost Kerbal;
- **Pending Insurance** is hidden in the Funding section while the deduction is 0 and appears only while an insurance payment is actually pending;
- the insurance amount is settled in full at the next funding boundary and then clears;
- insurance can contribute to a negative rival Funds balance.

The exact deterministic casualty result and per-mission insurance amount are covered by automated tests; live testing should focus on state/UI/message consistency around the result.

---

# 10. Rival funding and negative Funds

At a funding boundary, compare the Rival Agencies Funding section immediately before and after the boundary.

Confirm the calculation includes:

- Administration base income for the facility's current completed level;
- Objective Funding income;
- Satellite Network income;
- Kerbal Payroll as a deduction;
- Pending Insurance as a deduction only when non-zero;
- signed **Total Next Payout**.

Confirm:

- rival Funds are allowed to become negative when deductions exceed income;
- negative Funds do not get silently clamped to zero;
- affordability gates stop later paid actions when the rival cannot afford them;
- the UI total agrees with the campaign's applied result rather than independently reconstructing another funding rule.

For a default Level 1 rival with one employed Kerbal and no other income or insurance, Administration income and payroll should currently offset one another.

---

# 11. Rival research

Give a rival enough Stored Science to afford at least one eligible technology and cross a funding boundary.

Confirm:

- research starts only at a funding boundary;
- only one research project is active at a time;
- the selected project comes from the cheapest affordable eligible Science-cost tier;
- its Science cost is deducted immediately when the project starts;
- the active research card shows status and eligible completion timing without repeating the already-paid Science cost or research start date;
- the project takes the configured 90 campaign days to become research-ready;
- it unlocks only at the first funding boundary on or after that ready time;
- it does not unlock early merely because universal time passed the 90-day point between funding boundaries;
- completion clears the current project and adds the tech to Researched Tech;
- exactly one stock inbox message is created with a title such as `Aster Research - COMPLETE`;
- the message names the completed technology;
- when that technology unlocks one or more Science experiments, the message includes `New Science experiment unlocked:` or `New Science experiments unlocked:` with the display names;
- technologies with no experiment unlock omit that extra line;
- **Show Locked Techs** displays only unresearched technologies, each on one line with cost, experiment unlocks, and prerequisites;
- a new research project can be selected later according to the post-boundary stored Science state.

Save and reload while research is active and confirm the project, paid cost, start time, ready time, and eligible completion funding date are unchanged even though some of those stored fields are intentionally not repeated in the compact UI. A research project still active at save time should produce exactly one completion notification when it later completes; already-researched historical tech state must not replay a notification on load.

---

# 12. Rival facility construction

Give a rival enough Funds for an upgrade and cross an eligible funding boundary.

Confirm:

- at most one construction project starts initially;
- the upgrade cost is charged when construction starts;
- the source facility level remains active during construction;
- the UI shows source level, target level, paid Funds, remaining time, and expected completion date without a separate elapsed-time row;
- Level 1 to 2 construction uses the configured duration;
- the new facility level activates only at the first funding boundary where construction is due;
- exactly one stock inbox message is created with a title such as `Aster Facility Upgrade - COMPLETE`;
- the message names the completed facility and new level;
- the next line begins with `Bonus:` and describes the capability granted by the finalized new level;
- examples include `Bonus: Satellite capacity increased to 8.`, `Bonus: Destination access expanded to the Mun and Minmus.`, and `Bonus: Normal Launch Progress Chance increased by 3 percentage points.` using the current configured values;
- Astronaut Complex Level 2 also mentions EVA Report access, and Research and Development Level 2 also mentions Surface Sample access;
- the completed level affects subsequent authoritative capability calculations;
- the same completed level is used by funding calculations at that boundary according to the locked controller ordering.

Save and reload during construction and confirm all construction timing/cost state survives. A construction project still active at save time should produce exactly one completion notification when it later completes; an already-completed/restored facility level must not replay a notification on load.

---

# 13. Large timewarp and crossed funding boundaries

Create a state containing as many of these as practical at once:

- a normal Contract preparation with a scheduled next check;
- a Science preparation with a scheduled daily check;
- one or more live missions;
- active research;
- active facility construction;
- an upcoming campaign funding boundary.

Use high timewarp or a save/reload to jump beyond several scheduled events and, separately, beyond more than one funding boundary.

Confirm:

- stored rival events are processed in chronological order rather than at one arbitrary final timestamp;
- live missions resolve at their stored completion time on the first simulation refresh after the jump;
- each valid resolved Live Mission produces one result notification even when timewarp crosses its completion time;
- progress checks crossed by the jump are not lost;
- research and construction complete only at eligible funding boundaries;
- each genuine research/facility completion crossed by the timewarp produces one corresponding notification;
- every crossed funding boundary processes its payout and sponsor review;
- the final next-funding date is the first boundary after current universal time;
- no duplicated payout, launch, mission resolution, result notification, development notification, construction completion, or research completion occurs.

The automated suite covers a deterministic large-time-jump case; this live check validates the KSP timewarp/runtime integration around it.

---

# 14. Save/load persistence during active rival state

Create a save containing several non-default rival states and reload it from the main menu and by quickload where practical.

Confirm preservation of:

- rival Funds, including a negative value;
- Stored Science;
- researched tech IDs;
- active research project and all timing/cost fields;
- current facility levels;
- active construction project and all timing/cost fields;
- Kerbals Employed;
- Pending Insurance;
- normal Contract Launch Progress and next scheduled progress check;
- normal Contract ready universal time at 100%;
- Science preparation subject, reward, progress, next check, crew requirement, and ready universal time;
- every live mission's sequence, type, target/subject, location, launch time, duration, completion time, difficulty, Success Chance, assigned crew, planned Science, and outcome seed;
- completed rival Science subjects.

After reload, confirm historical objective completion data alone does not replay a rival Live Mission notification, historical researched-tech IDs do not replay research notifications, and historical facility levels do not replay construction notifications. A mission/research/construction project that was still active at save time should produce exactly one appropriate notification only when it later resolves/completes.

A mission with a stored deterministic outcome seed should resolve to the same outcome after reload as it would have before reload.

---

# 15. UI polish and notifications

## Funding Targets ordering

Open **Funding Targets** with a mix of offered objectives the player has and has not completed.

Confirm:

- unfinished player objectives are shown first;
- recurring satellite programmes follow current unfinished objective work;
- player-completed one-off objectives are moved to the bottom;
- rival completion alone does not move an objective into the player's completed group.

## Flight reward display

In Flight, open **Offered Contracts**.

Confirm each Contract row includes the Contract name and its configured funding reward in Funds.

## Notifications

Confirm stock KSP messages are produced for these live events:

- a sponsor review that makes one or more new funding targets Offered, naming the newly offered target(s);
- every valid rival Live Mission resolution, using a title such as `Aster Live Mission - SUCCESS` or `Aster Live Mission - FAILED`;
- a successful rival Contract Live Mission names the completed mission/Contract;
- a successful rival Science Live Mission names the experiment, body, situation, and biome where applicable, and reports the Science gained;
- when a successful rival Science mission finds an already-depleted shared subject, the message says **No Science remained to collect**;
- a failed uncrewed Live Mission reports failure without crew text;
- a failed crewed Live Mission with no casualties says the assigned Kerbal(s) escaped and survived;
- a failed crewed Live Mission with casualties reports the number lost/surviving and the exact **Insurance Penalty to Pay**;
- a rival research completion uses `<Rival> Research - COMPLETE`, names the technology, and lists newly unlocked Science experiments where applicable;
- a rival facility completion uses `<Rival> Facility Upgrade - COMPLETE`, names the facility/new level, and includes its `Bonus:` capability line;
- a successful player campaign funding payout shows the received Funds amount.

Also confirm:

- a successful rival Contract mission produces only the Live Mission result notification and does not also produce the retired **Rival Objective Completed** message;
- each genuine research or facility completion produces only one notification even if later funding boundaries are processed;
- loading an existing save does not replay historical sponsor-review offers;
- restoring historical objective, researched-tech, or facility-level state does not create live completion notifications;
- no funding-received message appears when no positive Career funding award was actually applied.

---

# 16. Task 15 Help window wording

Open the **?** Help / Player Guide.

Confirm the existing tutorial content remains present and the Pre-Orbit section now uses the approved heading:

```text
Pre-Orbit Kerbin Contracts
```

Confirm the approved Pre-Orbit paragraph is readable without clipping and that **Funding Sharing Example** appears at the bottom of the Help window with the 100,000 -> 90,000 -> 45,000/45,000 worked example.

Confirm the expanded Help content remains scrollable inside the Command Center window.

---

# 17. General UI and log regression pass

Move between Space Center, Flight, Tracking Station, VAB/SPH, and back where practical.

Confirm:

- Command Center can still be shown/hidden with F8 and the stock launcher button;
- the Flight-only Offered Contracts launcher does not leak into non-Flight scenes;
- Rival Agencies values continue updating after scene changes;
- no duplicate persistent notification/UI addons appear after repeated scene changes;
- no continuous allocations or visible UI refresh loops cause obvious stutter at normal simulation cadence;
- `KSP.log` contains no repeating exceptions from The Race for Space during normal play, timewarp, save/load, mission resolution, Science consumption, funding boundaries, or notifications.

---

# Acceptance record

Record the build/commit tested and mark each live section when completed.

```text
Commit tested:
KSP version: 1.12.x
Save used:

[ ] 1. Fresh v0.6 rival programme state / selector
[ ] 2. Rival Contract preparation / live mission
[ ] 3. Launch Science Expedition preparation
[ ] 4. Tracking Station destination gates
[ ] 5. Kerbin Science access / experiment gates
[ ] 6. Shared Science race
[ ] 7. Crew capacity / contention / hiring
[ ] 8. Mission Control satellite capacity
[ ] 9. Mission failure / casualties / insurance / notification
[ ] 10. Rival funding / negative Funds
[ ] 11. Rival research / completion notification
[ ] 12. Rival facility construction / completion notification
[ ] 13. Large timewarp / crossed funding boundaries
[ ] 14. Save/load persistence
[ ] 15. UI polish / notifications
[ ] 16. Task 15 Help window wording
[ ] 17. General UI / log regression pass

Notes:
```

Task 16 should be considered fully accepted only after the automated suite is green and the applicable live KSP checks above have been completed without unresolved failures.
