# Linux / Steam Deck Testing

This guide is for the normal Linux or Steam Deck development cycle for **The Race for Space 0.5**.

Current branch:

```text
Alpha/KerbalContracts-v0.5
```

For the full gameplay acceptance checklist, use [`KERBAL_CONTRACTS_V0_5_TESTING.md`](KERBAL_CONTRACTS_V0_5_TESTING.md).

## Daily test cycle

From the repository root:

```bash
export KSP_ROOT="$HOME/.local/share/Steam/steamapps/common/Kerbal Space Program"

git fetch origin
git switch Alpha/KerbalContracts-v0.5
git pull --ff-only

bash tools/run-logic-tests.sh
bash tools/test-prototype.sh Alpha/KerbalContracts-v0.5
```

Do not deploy if the automated logic tests fail.

## 1. Confirm the branch

```bash
git branch --show-current
```

Expected:

```text
Alpha/KerbalContracts-v0.5
```

Check the working tree:

```bash
git status --short
```

No output means the tree is clean.

## 2. Set `KSP_ROOT`

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

## 3. Run the automated tests

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
- rival mission progress;
- funding calculations;
- persistence transformations;
- CampaignController ordering.

They cannot prove direct KSP API behaviour such as:

- vessel-destruction callbacks;
- stock biome reporting;
- loaded/unloaded vessel discovery inside a real KSP save;
- Career-funds integration;
- actual Command Center layout.

Those require the in-game checks below.

## 4. Build and deploy

Recommended helper:

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

A successful real KSP build is important after changes in `KspIntegration/`, because the standalone tests do not compile against the actual KSP API.

## 5. Quick in-game smoke test

Use a disposable Career save.

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

### Live Flight Contract telemetry

Launch a vessel and open **Funding Targets**.

The four opening contracts should show the live values they need:

- **Directed Power** — current speed, maximum speed, maximum altitude, and impact readiness/status.
- **Mass** — current mass, distance from launch, and landed state.
- **Control** — current altitude, hold progress, crew count, and safe-landing readiness.
- **Biome** — current biome, target biome, match state, and landed state.

The values should update about once per second rather than every frame.

If the UI shows:

```text
Waiting for active vessel telemetry...
```

while a normal vessel is actively being flown, treat that as a runtime/tracking problem rather than a UI-layout problem.

### One progression check

Complete one Level I Pre-Orbit objective.

Confirm:

1. the completed contract no longer behaves as an active unfinished Flight Contract;
2. the next level in that line becomes `Unlocked`;
3. it does not become `Offered` until the next sponsor review;
4. after the review, all currently unlocked Pre-Orbit contracts are offered, even if there are more than two.

## 6. Quick contract checks

### Directed Power

- Reach the required speed below 70 km.
- Confirm normal landing/recovery does not complete it.
- Impact Kerbin and confirm completion.
- Exceed 70 km first and confirm the attempt remains invalid.
- Enter orbit and confirm the attempt remains invalid.

### Mass

- Travel beyond the required distance.
- Keep enough final mass.
- Confirm completion only after `LANDED` on Kerbin.
- Confirm `SPLASHED` does not count.

### Control

- Use a crewed vessel.
- Hold continuously inside the required altitude band.
- Leave the band early and confirm the timer resets.
- After qualification, land safely on Kerbin with crew.

### Biome

- Fly over the target biome and confirm no completion.
- Land in the target biome and confirm completion.
- Confirm splashdown does not count.

## 7. Save/reload smoke test

Save during an active Flight Contract attempt, reload, and confirm:

- campaign offers and objective completions remain correct;
- rival state remains correct;
- Directed Power maximum history and orbit invalidation survive when relevant;
- Control hold/qualification state survives when relevant;
- live values such as current altitude, mass, biome, and crew are refreshed from the vessel after load rather than copied from stale saved telemetry.

## 8. Orbital vessel check

Put a qualifying Probe or Relay into Kerbin orbit.

Confirm:

1. it is recognised while loaded;
2. it remains counted after returning to Space Center or Tracking Station;
3. the same vessel is not double-counted;
4. a crewed Probe can still count toward a satellite network while not satisfying an uncrewed Probe Orbit requirement by itself.

This verifies the boundary between `KspVesselMonitor` and `OrbitalVesselTracker`.

## 9. Check the KSP log

After testing:

```bash
grep -i "Race for Space\|TheRaceForSpace\|Exception" "$KSP_ROOT/KSP.log" | tail -n 100
```

Look for:

- repeated exceptions;
- Directed Power destruction-callback errors;
- excessive repeated output;
- save/load errors.

Normal gameplay should not produce per-frame Flight Contract log spam.

## One-time Linux setup

If this machine has not been prepared before:

```bash
mkdir -p "$HOME/Projects"
cd "$HOME/Projects"
git clone https://github.com/the-race-for-space/the-race-for-space.git
cd the-race-for-space

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
- Level V -> Probe Orbit convergence;
- funding and rival behaviour;
- save format and scene changes;
- loaded/unloaded orbital vessel tracking;
- Command Center presentation.
