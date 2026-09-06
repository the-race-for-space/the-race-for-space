# Linux / Steam Deck Testing

This guide is for the normal Linux or Steam Deck development cycle for **The Race for Space 0.5**.

Current branch:

```text
Alpha/KerbalContracts-v0.5
```

For the full gameplay acceptance checklist, use [`KERBAL_CONTRACTS_V0_5_TESTING.md`](KERBAL_CONTRACTS_V0_5_TESTING.md).

## Daily test cycle - copy and paste this whole block

This block is designed to work when Konsole opens in your home folder (`~`).

```bash
cd /home/deck/Projects/the-race-for-space/
export KSP_ROOT="$HOME/.local/share/Steam/steamapps/common/Kerbal Space Program"

git fetch origin
git switch Alpha/KerbalContracts-v0.5
git pull --ff-only

bash tools/run-logic-tests.sh
bash tools/test-prototype.sh Alpha/KerbalContracts-v0.5
```

The first `cd` is required. The `git` commands and the scripts under `tools/` must be run from inside the repository.

Do not deploy if the automated logic tests fail.

## 1. Open the repository

If your prompt shows something like:

```text
(deck@steamdeck ~)$
```

you are in your home folder, not in the repository.

Run:

```bash
cd /home/deck/Projects/the-race-for-space/
```

Confirm Git can see the repository:

```bash
git status --short
```

If this command works, you are in the correct folder. No output means the working tree is clean.

## 2. Confirm the branch

```bash
git branch --show-current
```

Expected:

```text
Alpha/KerbalContracts-v0.5
```

If needed, update the branch:

```bash
git fetch origin
git switch Alpha/KerbalContracts-v0.5
git pull --ff-only
```

## 3. Set `KSP_ROOT`

Common Steam Deck / Linux location:

```bash
export KSP_ROOT="$HOME/.local/share/Steam/steamapps/common/Kerbal Space Program"
```

Alternative common location:

```bash
export KSP_ROOT="$HOME/.steam/steam/steamapps/common/Kerbal Space Program"
```

Confirm the main KSP assembly exists:

```bash
test -f "$KSP_ROOT/KSP_Data/Managed/Assembly-CSharp.dll" && echo "KSP references found"
```

`KSP_ROOT` must point to the KSP installation folder, not directly to `GameData`.

## 4. Run the automated tests

From the repository folder:

```bash
bash tools/run-logic-tests.sh
```

A successful run ends with both suites passing.

The automated tests cover KSP-independent behaviour such as:

- objective and unlock-rule evaluation;
- the four opening Pre-Orbit offers;
- later Pre-Orbit unlock and sponsor-review behaviour;
- independent evaluation of multiple offered Flight Contracts;
- Directed Power, Mass, Control, and Biome rules;
- Flight Attempt switching, staging/split lineage, docked reference-lineage reconciliation, and multi-attempt save/load;
- Mass rejection while another lineage is attached and Control reset/preservation across topology changes;
- rival mission progress;
- funding calculations;
- persistence transformations;
- `CampaignController` ordering.

They cannot prove direct KSP API behaviour such as:

- vessel-destruction callbacks;
- active-vessel `Part.persistentId` and reference/control-part capture;
- stock biome reporting;
- loaded/unloaded vessel discovery inside a real KSP save;
- Career-funds integration;
- actual Command Center or FlightActiveUI layout and launcher lifecycle;
- stock `MessageSystem` funding-completion notifications.

Those require the in-game checks below.

## 5. Build and deploy

From the repository folder, use:

```bash
bash tools/test-prototype.sh Alpha/KerbalContracts-v0.5
```

The helper builds the real mod against the local KSP assemblies and deploys:

```text
$KSP_ROOT/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll
$KSP_ROOT/GameData/TheRaceForSpace/Config/CampaignSettings.cfg
```

Confirm the files if needed:

```bash
ls -l "$KSP_ROOT/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll"
ls -l "$KSP_ROOT/GameData/TheRaceForSpace/Config/CampaignSettings.cfg"
```

A successful real KSP build is important after changes in `KspIntegration/` or the KSP-facing UI, because the standalone tests do not compile against the actual running KSP scene lifecycle.

## 6. Quick in-game smoke test

Use a disposable Career save created with the current build. The Step 6 `FLIGHT_CONTRACT_PROGRESS` layout intentionally does not migrate the earlier development single-attempt format.

### Command Center

1. Start KSP 1.12.x.
2. Load or create a Career save.
3. Open the Command Center with F8.
4. Confirm the stock launcher button opens the same window.
5. Hide the window, continue playing, and reopen it. Campaign progress should continue while the UI is hidden.

### Opening campaign state

Open **Contract Catalogue**.

Confirm these four contracts are `Offered`:

- Directed Power I
- Mass I
- Control I
- Biome I

Confirm Levels II-V of all four lines are initially `Locked`.

### FlightActiveUI lifecycle and contract list

Before entering Flight, confirm the separate Flight contract launcher button is not present in Space Center or editor scenes.

Launch a vessel and confirm:

1. a separate Flight-only launcher button appears;
2. it opens a small draggable window titled **Offered Contracts**;
3. the Flight window opens and closes independently of the full Command Center;
4. unfinished Offered objective contracts appear before player-completed Offered objective contracts;
5. completed Offered contracts sit at the bottom marked `Complete` and have no expand/collapse control;
6. unfinished contracts use independent `+` / `-` controls, so several contracts can remain expanded at the same time;
7. the contract list scrolls rather than forcing the compact window to grow indefinitely.

Return to Space Center and confirm the Flight-only launcher disappears. Re-enter Flight and confirm only one FlightActiveUI launcher button is present.

### Live Flight Contract telemetry

In Flight, open **Offered Contracts** and expand the active Pre-Orbit contracts.

The expanded rows should show the live values they need:

- **Directed Power** - current speed, maximum speed, maximum altitude, and impact readiness/status.
- **Mass** - current mass, distance from launch, and landed/splashed state.
- **Control** - current altitude, hold progress, crew count, and safe recovery readiness.
- **Biome** - current biome, target biome, match state, and landed/splashed state.

The values should follow the existing `FlightContractTracker` updates at about the normal once-per-second telemetry cadence. Opening or closing FlightActiveUI must not create another active-vessel sampling loop.

Persistent part-lineage IDs and the current reference/control-part ID are captured while this existing snapshot is built. After flying a normal controllable multi-part craft, the KSP log should contain a deduplicated Flight telemetry line similar to:

```text
[TheRaceForSpace] Flight telemetry: captured active vessel <id> on Kerbin with <n> persistent part IDs and reference part <part-id>.
```

For a normal loaded craft after any topology change has settled, `<n>` and `<part-id>` should both be greater than zero. The tracker uses those IDs to recover remembered Flight Attempts when KSP vessel identity changes or several remembered lineages are docked together.

### Flight Attempt staging lineage

For a focused staging check, use a controllable multi-stage craft while a Directed Power contract is active:

1. reach a noticeable maximum speed before staging;
2. stage so the actively controlled branch keeps only part of the original vessel;
3. confirm the FlightActiveUI maximum speed does not reset when KSP changes the vessel identity;
4. if the detached branch is separately controllable, switch to it and confirm it does **not** inherit the continuing branch's historical maximum;
5. switch back to the continuing branch and confirm its original maximum is still present.

This verifies the staging/split lineage rule.

### Flight Attempt docking and undocking lineage

Directed Power maximum speed is the easiest visible history value to compare:

1. fly Craft A and establish a recognizable maximum speed;
2. switch to unrelated Craft B and establish a different lower maximum speed;
3. return to Craft A and confirm A's original maximum is restored;
4. dock A and B while controlling from a part that belongs to A;
5. confirm the FlightActiveUI still shows A's history rather than merging or replacing it with B's history;
6. use KSP's **Control From Here** on a suitable part belonging to B;
7. after the next telemetry sample, confirm the active history changes to B's remembered values and does not inherit A's earlier maximum;
8. undock the vessels;
9. switch to A and B separately and confirm each branch recovers its own pre-docking history.

### Mass and Control topology rules

Use two independently remembered craft so their part lineages are already known to the tracker.

For **Mass**:

1. make Craft A satisfy the Mass distance requirement and retain enough mass on its own;
2. dock unrelated Craft B to A and keep A's lineage selected with **Control From Here**;
3. land or splash down the combined assembly while it still contains B's parts;
4. confirm the Mass objective does **not** complete while another lineage is attached;
5. undock B so A is again made only from A's remembered lineage;
6. with A still meeting its normal mass, distance, and recovery requirements, confirm the Mass objective can complete.

For **Control**:

1. begin an unqualified Control hold on Craft A and let several seconds accumulate;
2. dock another remembered craft to A while A remains selected and confirm the unfinished hold resets;
3. begin a new hold while the docked topology remains unchanged;
4. undock the other craft and confirm that unfinished hold resets again;
5. complete a fresh uninterrupted hold so Control becomes qualified;
6. dock or undock again and confirm the already-qualified Control state is **not** erased.

Ordinary staging of A's own continuing lineage should not be treated as this external attachment change. These rules reuse the persistent IDs already captured by the normal telemetry sample; they do not add another vessel scan.

### Multiple Flight Attempts across save/reload

Continue with two craft that already have clearly different remembered Directed Power maxima:

1. make sure Craft A and Craft B have both been sampled and each shows its own recognizable maximum;
2. optionally dock them and use **Control From Here** so Craft B's lineage is selected while both histories are present in one KSP vessel;
3. save the game while Craft B is selected;
4. reload that save and confirm B's remembered maximum is restored rather than reset;
5. switch to Craft A, or undock and then switch to A if the save was made while docked;
6. confirm A's older independent maximum is also restored from its saved part lineage;
7. switch back to B and confirm B still has its own separate history;
8. if practical, save/reload once more after the vessels have undocked and confirm both histories continue to resolve correctly.

The important result is that saving while B is selected must no longer discard A. Two docked attempts may have the same last KSP vessel ID in `FLIGHT_CONTRACT_PROGRESS`; their saved `PART_LINEAGE` entries keep the histories distinct.

Open the full **Funding Targets** view while still in Flight and confirm it no longer shows `Live Flight` requirement rows. Funding Targets should remain focused on funding and contract-lifecycle information; `FlightActiveUI` is the dedicated real-time requirement display.

Contract evaluation must continue even when both interfaces are closed. Hiding the UI must never stop `ModRuntime` from maintaining the active Flight Contract tracker.

If an expanded Pre-Orbit contract shows:

```text
Waiting for vessel telemetry...
```

while a normal vessel is actively being flown for more than the initial loading moment, treat that as a runtime/tracking problem rather than a UI-layout problem.

### One progression and notification check

Complete one Level I Pre-Orbit objective.

Confirm:

1. the completed contract no longer behaves as an active unfinished Flight Contract;
2. in FlightActiveUI, an Offered contract completed by the player moves to the bottom and is marked `Complete` with no `+` / `-` control;
3. KSP's stock message system receives one green message for the player completion;
4. the message title uses `Funding Target Completed — <Contract Name>`;
5. the message body uses `<Contract Name> has been achieved. Your agency is now eligible for a share of the remaining contract funding.`;
6. the next level in that line becomes `Unlocked`;
7. it does not become `Offered` until the next sponsor review;
8. after the review, all currently unlocked Pre-Orbit contracts are offered, even if there are more than two.

For example, completing Control II should produce:

```text
Funding Target Completed — Control II
Control II has been achieved. Your agency is now eligible for a share of the remaining contract funding.
```

The notification is for player Objective Funding Contract completions only. Rival completions must not create a player inbox message.

## 7. Quick contract checks

### Directed Power

- Reach the required speed below 70 km.
- Confirm normal landing/recovery does not complete it.
- Impact Kerbin and confirm completion.
- Exceed 70 km first and confirm the attempt remains invalid.
- Enter orbit and confirm the attempt remains invalid.

### Mass

- Travel beyond the required distance.
- Keep enough final mass.
- Confirm completion after either `LANDED` or `SPLASHED` on Kerbin when no outside-lineage parts remain attached.
- Confirm insufficient mass or distance still prevents completion in either final situation.
- Confirm an unrelated docked lineage blocks completion even when the combined vessel mass is high enough.

### Control

- Use a crewed vessel.
- Hold continuously inside the required altitude band.
- Leave the band early and confirm the timer resets.
- Dock or undock another lineage before qualification and confirm the timer resets.
- After qualification, confirm a later docking/undocking change does not erase the qualified state.
- Then land or splash down safely on Kerbin with crew and confirm completion.

### Biome

- Fly over the target biome and confirm no completion.
- Finish either `LANDED` or `SPLASHED` while KSP reports the target biome and confirm completion.
- Confirm merely passing through the target biome without finishing there does not count.

## 8. Save/reload smoke test

Save during Flight Contract activity, reload, and confirm:

- campaign offers and objective completions remain correct;
- rival state remains correct;
- all remembered Flight Attempts survive, not just the craft that was selected when saving;
- each attempt's persistent-part lineage still selects the correct historical state after switching, staging, docking, or undocking;
- Directed Power maximum history and orbit invalidation survive when relevant;
- Control hold/qualification state survives per remembered attempt when relevant;
- the attempt marked selected at save time is restored as the selected history until live KSP telemetry resolves the currently controlled lineage;
- live values such as current altitude, mass, biome, crew, reference part, and current external attachment topology are refreshed from the vessel after load rather than copied from stale saved telemetry.

`FLIGHT_CONTRACT_PROGRESS` uses repeated `ATTEMPT` nodes with nested `PART_LINEAGE` and `CONTROL_STATE` entries. The previous development single-attempt root format is intentionally not migrated; use a current-build disposable save when validating the current persistence format.

The transient external-attachment topology is rebuilt from the first usable live snapshot after load. That first observation establishes the baseline and should not by itself reset a restored partial Control hold; later docking/undocking changes should reset it normally.

FlightActiveUI expansion state and visibility are temporary UI state and do not need to survive a scene/save reload.

After completing an Objective Funding Contract and receiving its stock funding-completion notification, save and reload. Confirm the historical completion is restored **without** generating the same notification again. Notification history itself is not persisted; saved objective completion state is restored through the silent path instead.

## 9. Orbital vessel check

Put a qualifying Probe or Relay into Kerbin orbit.

Confirm:

1. it is recognised while loaded;
2. it remains counted after returning to Space Center or Tracking Station;
3. the same vessel is not double-counted;
4. a crewed Probe can still count toward a satellite network while not satisfying an uncrewed Probe Orbit requirement by itself.

If Probe Orbit is currently Offered and this action newly completes it for the player, the same stock funding-completion notification should appear for Probe Orbit.

This verifies the boundary between `KspVesselMonitor` and `OrbitalVesselTracker`.

## 10. Check the KSP log

After testing:

```bash
grep -i "Race for Space\|TheRaceForSpace\|Exception" "$KSP_ROOT/KSP.log" | tail -n 100
```

Look for:

- repeated exceptions;
- `FlightActiveUI` exceptions or duplicate launcher behaviour;
- `FundingNotificationUI` or `MessageSystem` errors;
- Directed Power destruction-callback errors;
- active Flight telemetry reporting zero persistent part IDs or reference part `0` for a normal settled controllable craft;
- Flight Contract save/load errors or histories unexpectedly resetting after reload;
- excessive repeated output.

A successful completion notification should produce one diagnostic line for that objective ID, not repeated per-frame output. Normal gameplay should not produce per-frame Flight Contract log spam.

## Troubleshooting

### `fatal: not a git repository`

This means the terminal is not inside the cloned repository.

Run:

```bash
cd /home/deck/Projects/the-race-for-space/
```

Then retry:

```bash
git status --short
```

Do not run `git fetch`, `git switch`, `git pull`, or `bash tools/...` from `~`.

### `tools/run-logic-tests.sh: No such file or directory`

This normally has the same cause: the terminal is outside the repository.

Run:

```bash
cd /home/deck/Projects/the-race-for-space/
ls tools
```

You should see the repository helper scripts.

## One-time Linux setup

If this machine has not been prepared before:

```bash
mkdir -p "$HOME/Projects"
cd "$HOME/Projects"
git clone https://github.com/the-race-for-space/the-race-for-space.git
cd /home/deck/Projects/the-race-for-space/

git fetch origin
git switch --track origin/Alpha/KerbalContracts-v0.5
```

Check tools:

```bash
git --version
dotnet --version
```

The standalone logic tests use .NET 8. The KSP mod itself targets .NET Framework 4.7.2 and may require compatible Mono 4.7.2 reference assemblies on Linux.

## Full release-candidate pass

The quick checks above are for day-to-day development.

Before treating a build as a 0.5 release candidate, complete [`KERBAL_CONTRACTS_V0_5_TESTING.md`](KERBAL_CONTRACTS_V0_5_TESTING.md), including:

- all four Pre-Orbit lines;
- multiple simultaneously offered levels;
- Flight Attempt switching, staging, docking/undocking, topology rules, and multi-attempt persistence;
- Level V -> Probe Orbit convergence;
- funding and rival behaviour;
- loaded/unloaded orbital vessel tracking;
- Command Center presentation;
- FlightActiveUI launcher lifecycle, ordering, expansion, and live requirement presentation;
- player funding-completion inbox notifications and no replay after save/reload.
