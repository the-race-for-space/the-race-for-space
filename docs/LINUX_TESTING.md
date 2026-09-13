# Linux / Steam Deck Testing

This guide covers the normal Linux / Steam Deck development cycle for **The Race for Space Content v0.6 alpha**.

Current branch:

```text
Alpha/Content-v0.6
```

Use [`CONTENT_V0_6_TESTING.md`](CONTENT_V0_6_TESTING.md) for the full v0.6 live-KSP acceptance checklist. Use [`KERBAL_CONTRACTS_V0_5_TESTING.md`](KERBAL_CONTRACTS_V0_5_TESTING.md) when you need the detailed retained Pre-Orbit vessel-tracking scenarios.

## Daily test cycle

This block assumes Konsole opens in the home folder:

```bash
cd /home/deck/Projects/the-race-for-space/
export KSP_ROOT="$HOME/.local/share/Steam/steamapps/common/Kerbal Space Program"

git fetch origin
git switch Alpha/Content-v0.6
git pull --ff-only

bash tools/run-logic-tests.sh
bash tools/test-prototype.sh Alpha/Content-v0.6
```

Do not deploy if the automated logic/controller tests fail.

## 1. Confirm the repository and branch

```bash
cd /home/deck/Projects/the-race-for-space/
git status --short
git branch --show-current
```

Expected branch:

```text
Alpha/Content-v0.6
```

If needed:

```bash
git fetch origin
git switch Alpha/Content-v0.6
git pull --ff-only
```

Do not create a replacement branch as part of testing.

## 2. Set `KSP_ROOT`

Common Steam Deck location:

```bash
export KSP_ROOT="$HOME/.local/share/Steam/steamapps/common/Kerbal Space Program"
```

Alternative:

```bash
export KSP_ROOT="$HOME/.steam/steam/steamapps/common/Kerbal Space Program"
```

Confirm KSP references:

```bash
test -f "$KSP_ROOT/KSP_Data/Managed/Assembly-CSharp.dll" && echo "KSP references found"
```

## 3. Run automated tests

```bash
bash tools/run-logic-tests.sh
```

A successful run ends with both suites passing.

Current KSP-independent coverage includes:

- funding contracts and unlock rules;
- the four opening Pre-Orbit offers and sponsor-review progression;
- Directed Power, Mass, Control, and Biome evaluation;
- Flight Attempt switching, staging/splits, docking/undocking, lifecycle pruning, vessel-ID reuse, and persistence;
- orbital objective/satellite tracking rules;
- rival programme state and persistence;
- rival tech catalogue and experiment unlocks;
- Science candidate filtering, destination/facility gates, daily preparation, ETA, and depleted-target replacement;
- live Contract/Science mission snapshots and Difficulty-based success;
- deterministic outcomes, casualties, pending insurance, and satellite reservations;
- crew availability/hiring/contention and exact ready-time Contract priority;
- shared Science exact-time rival ordering;
- facility capabilities, research, construction, and signed rival funding;
- large time jumps / chronological event processing;
- controller funding-boundary ordering.

The standalone suites cannot prove direct KSP/Unity behaviour such as:

- stock `ScienceSubject` lookup/consumption;
- real KSP body/biome/experiment availability;
- Career Funds integration;
- loaded/unloaded vessel capture in a real save;
- vessel-destruction callbacks;
- actual IMGUI layout and launcher lifecycle;
- stock `MessageSystem` notifications;
- save/load through a real KSP ScenarioModule lifecycle.

Those require the live checks in `CONTENT_V0_6_TESTING.md`.

## 4. Build and deploy

```bash
bash tools/test-prototype.sh Alpha/Content-v0.6
```

The helper builds against your local KSP 1.12.x assemblies and deploys:

```text
$KSP_ROOT/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll
$KSP_ROOT/GameData/TheRaceForSpace/Config/CampaignSettings.cfg
```

Confirm if needed:

```bash
ls -l "$KSP_ROOT/GameData/TheRaceForSpace/Plugins/TheRaceForSpace.dll"
ls -l "$KSP_ROOT/GameData/TheRaceForSpace/Config/CampaignSettings.cfg"
```

A real KSP build is especially important after changes under `KspIntegration/` or `UI/`, because those types are not compiled against the live KSP scene lifecycle by the standalone test harness.

## 5. Quick v0.6 smoke test

Use a disposable Career save created with the current build.

### Command Center

1. Start KSP 1.12.x and load/create a Career save.
2. Open the Command Center with F8.
3. Confirm the stock launcher button opens the same window.
4. Confirm **Overview**, **Funding Targets**, **Rival Agencies**, and **Contract Catalogue** are available.
5. Open `?` Help and confirm the Pre-Orbit explanation and worked funding-sharing example render correctly.
6. Hide/reopen the window and confirm campaign/rival progression is not tied to UI visibility.

### Opening campaign state

In **Contract Catalogue**, confirm:

- Directed Power I is Offered;
- Mass I is Offered;
- Control I is Offered;
- Biome I is Offered;
- Levels II-V begin Locked;
- newly unlocked targets wait for sponsor review before becoming Offered.

### Funding Targets and notifications

Confirm:

- Offered one-off objectives not yet completed by the player appear before player-completed one-off objectives;
- satellite funding remains visible between those groups as the recurring programme section;
- completing an eligible player objective can produce the stock funding-target completion message;
- a rival objective completion can produce a rival message;
- a sponsor review with new offers can produce a sponsor-review message;
- a positive campaign payout to the player can produce a payout message;
- loading a save does not replay historical objective/sponsor notifications.

### Flight-only Offered Contracts

Enter Flight and open the separate Flight launcher window.

Confirm:

1. the window is titled **Offered Contracts**;
2. unfinished Offered objectives appear before completed Offered objectives;
3. each row includes the configured base funding reward;
4. completed rows sit at the bottom marked `Complete`;
5. unfinished rows have independent `+` / `-` controls;
6. expanded Pre-Orbit rows wait for fresh vessel telemetry immediately after entering/reloading Flight rather than presenting stale restored data as current;
7. expanded Directed Power/Mass/Control/Biome rows show their relevant live telemetry once capture succeeds;
8. there is only one Flight launcher button;
9. leaving Flight removes the Flight-only launcher.

The current v0.6 Flight window does not require a separate Flight Attempt identity header. Flight Attempt identity remains internal tracking state used to keep histories correct across craft/topology changes.

### Rival Agencies dashboard

For each rival, confirm the sections appear in this order:

1. Programme Status
2. Live Mission Progress
3. Current Launch Programme / Launch Science Expedition
4. Construction
5. Facilities
6. Tech Tree / Research
7. Funding

On a fresh save, confirm one employed Kerbal, zero Stored Science, Level 1 facilities, and `Start` as the initial rival tech state.

Default Level 1 launch capability should show:

- normal Contract Launch Progress Chance: **30%** (VAB + Launch Pad);
- Science Progress Chance: **40% daily** (SPH + Runway).

### Rival live mission smoke test

Timewarp until a rival preparation reaches 100%.

Confirm:

- 100% Launch Progress creates a **Live Mission** instead of instantly completing the target;
- the row shows Contract/Science type, duration, progress, ETA, and success chance;
- the objective/Science result is applied only when the stored completion time is reached;
- large timewarp does not skip intermediate funding boundaries or stored mission events.

### Shared Science smoke test

Observe at least one rival Science Expedition if practical.

Confirm:

- the target corresponds to a real stock Science subject;
- launching the expedition does not immediately consume the player's stock subject;
- successful completion receives only the Science still remaining at completion;
- the stock subject is then depleted by the rival award;
- no repeated Science exceptions appear in `KSP.log`.

### Development/funding smoke test

Across one or more funding boundaries, confirm:

- Administration contributes rival base income;
- payroll/insurance are deductions;
- rival Funds may be negative;
- due research/construction completes before the boundary funding calculation;
- new research/construction starts only after the boundary payout;
- sponsor review happens after development/funding for that boundary.

### Save/load smoke test

Save while at least one rival has non-default v0.6 state, such as a preparation, live mission, research, construction, or Stored Science.

Reload and confirm the same values return without duplicating missions, payouts, Science awards, or notifications.

## 6. Read the log

After the smoke test:

```bash
grep -n "TheRaceForSpace\|The Race for Space\|Exception" "$KSP_ROOT/KSP.log" | tail -n 200
```

Investigate repeated mod exceptions before treating the live pass as successful.

## 7. Complete the acceptance record

Use [`CONTENT_V0_6_TESTING.md`](CONTENT_V0_6_TESTING.md) for the full pass and record:

- tested commit SHA;
- KSP version;
- platform;
- pass/fail notes for the live sections;
- any fixes made during acceptance.

Task 16 is not complete merely because `run-logic-tests.sh` is green; the KSP boundary checks must also be performed.
