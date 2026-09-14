# Development To-Do

This file contains only work that is still active after the Content v0.6 rival-programme implementation.

The full working TODO used while version 0.6 was designed and implemented is preserved unchanged in [`CONTENT_V0_6_TODO_ARCHIVE.md`](CONTENT_V0_6_TODO_ARCHIVE.md). That archive is the historical record of the completed v0.6 tasks, design decisions, implementation plan, and balance notes. Do not edit the archive to reflect later implementation changes; update this active TODO instead.

## Content v0.6 acceptance

- [ ] Complete the live KSP 1.12.x acceptance pass in [`CONTENT_V0_6_TESTING.md`](CONTENT_V0_6_TESTING.md).
  - The KSP-independent logic and controller/integration suites passed during Task 16.
  - The remaining work is the real KSP boundary: build/deploy, stock Science interaction, save/load and timewarp, notifications, and UI presentation.
  - Record the tested commit and any failures/fixes in the acceptance checklist before treating v0.6 as fully accepted.

## Surface base progression

The current base-content tranche is the surface-base progression. Orbital bases are deliberately a separate future contract line and are recorded later in this TODO rather than being part of the Desert/Polar -> Mun/Minmus path.

### Locked surface-base funding model

The surface-base programme uses two separate kinds of funding:

1. **Base I-III progression contracts are one-off Objective Funding Contracts.** Completing a tier advances campaign progression in the same broad way as the existing one-off objective system.
2. **Maintaining an established base provides separate static/recurring base funding.** This ongoing funding is not the Base I-II-III contract payout. It is based on the number of Kerbals currently present in qualifying bases for that funding programme.

For player vessels, qualifying base presence and Kerbal count should be refreshed on the existing **20-second broad vessel check**, alongside the slower loaded/unloaded vessel tracking work. Do not introduce another realtime or per-frame base scanner. The latest qualifying crew count is then available to the normal campaign funding calculation, following the same broad model as satellite-network funding.

Current locked progression/funding decisions:

- **Desert Base I** becomes available after completion of the Kerbin **Deserts** Biome contract.
- **Polar Base I** becomes available after completion of the Kerbin **Ice Caps** Biome contract.
- **Desert Base I, II and III** are one-off progression contracts.
- **Polar Base I, II and III** are one-off progression contracts.
- Completing **Desert Base I** unlocks a separate **Desert Base static funding** programme.
- Completing **Polar Base I** unlocks a separate **Polar Base static funding** programme.
- Desert static funding is based on the number of Kerbals currently present in qualifying Desert base vessels in Kerbin's Deserts biome.
- Polar static funding is based on the number of Kerbals currently present in qualifying Polar base vessels in Kerbin's Ice Caps biome.
- Completing **Desert Base III OR Polar Base III** satisfies the Kerbin proving-ground prerequisite for both the Mun and Minmus surface-base programmes.
- **Mun Base I** must also require the relevant future **Mun crewed-landing funding/objective** before it can unlock.
- **Minmus Base I** must also require the relevant future **Minmus crewed-landing funding/objective** before it can unlock.
- The Mun/Minmus crewed-landing funding contracts have not been designed or implemented yet and must be added before the surface-base unlock path can be completed.
- **Mun Base I, II and III** and **Minmus Base I, II and III** are one-off surface-base progression contracts.
- Completing **Mun Base I** unlocks a separate **Mun Base static funding** programme.
- Completing **Minmus Base I** unlocks a separate **Minmus Base static funding** programme.
- Mun/Minmus static funding follows the same broad principle as Desert/Polar funding: payout is based on current qualifying Kerbal presence rather than making the Base I-II-III progression contracts recurring.

### Kerbin proving grounds

- [ ] Define and balance **Desert Base I-III** as one-off progression contracts.
  - Desert Base I unlock prerequisite: complete the Kerbin **Deserts** Biome contract.
  - Define the Base I -> II -> III progression requirements, rewards, required Kerbals, durations, and rival mission difficulty.
- [ ] Define and balance **Polar Base I-III** as one-off progression contracts.
  - Polar Base I unlock prerequisite: complete the Kerbin **Ice Caps** Biome contract.
  - Define the Base I -> II -> III progression requirements, rewards, required Kerbals, durations, and rival mission difficulty.
- [ ] Define the separate **Desert Base static funding** contract.
  - Unlock after Desert Base I is completed.
  - Qualifying presence is in Kerbin's Deserts biome.
  - Funding scales from the current number of Kerbals in qualifying Desert base vessels.
  - Player qualifying crew count is refreshed on the existing 20-second broad vessel check.
- [ ] Define the separate **Polar Base static funding** contract.
  - Unlock after Polar Base I is completed.
  - Qualifying presence is in Kerbin's Ice Caps biome.
  - Funding scales from the current number of Kerbals in qualifying Polar base vessels.
  - Player qualifying crew count is refreshed on the existing 20-second broad vessel check.
- [ ] Decide the exact qualification rules for long-duration Kerbin bases, including base hardware/capability, crew, continuous-duration behaviour, vessel/base identity, loss, and resumption.
- [ ] Decide the exact static-funding formula, including Funds per Kerbal, any maximum qualifying crew, whether multiple qualifying vessels aggregate, and how funding is shared when multiple agencies qualify.
- [ ] Implement the agreed Desert and Polar one-off base objectives/contracts.
- [ ] Implement the Desert and Polar static base-funding tracking and payout rules.
- [ ] Make **Desert Base III OR Polar Base III** satisfy the Kerbin proving-ground prerequisite for both Mun Base I and Minmus Base I.

### Crewed landing prerequisites for Mun and Minmus

These prerequisite contracts do not exist yet. They are required content for the surface-base progression and should be designed before Mun/Minmus Base I is implemented.

- [ ] Define a **Mun crewed-landing funding/objective contract**.
  - It must represent a successful crewed landing on the Mun.
  - Completion is required in addition to **Desert Base III OR Polar Base III** before Mun Base I can unlock.
  - Define its unlock rule, one-off reward, rival mission profile, and relationship to existing Mun orbital progression.
- [ ] Define a **Minmus crewed-landing funding/objective contract**.
  - It must represent a successful crewed landing on Minmus.
  - Completion is required in addition to **Desert Base III OR Polar Base III** before Minmus Base I can unlock.
  - Define its unlock rule, one-off reward, rival mission profile, and relationship to existing Minmus orbital progression.
- [ ] Implement the agreed Mun and Minmus crewed-landing funding/objective contracts before implementing the dependent Mun/Minmus Base I unlocks.

### Mun and Minmus surface bases

- [ ] Define and balance **Mun Base I-III** as one-off surface-base progression contracts.
  - Mun Base I unlock requires **Desert Base III OR Polar Base III** plus completion of the future **Mun crewed-landing funding/objective**.
  - Mun Base II and III then progress sequentially from Mun Base I.
- [ ] Define and balance **Minmus Base I-III** as one-off surface-base progression contracts.
  - Minmus Base I unlock requires **Desert Base III OR Polar Base III** plus completion of the future **Minmus crewed-landing funding/objective**.
  - Minmus Base II and III then progress sequentially from Minmus Base I.
- [ ] Define the separate **Mun Base static funding** contract.
  - Unlock after Mun Base I is completed.
  - Funding scales from the current number of Kerbals in qualifying Mun surface bases.
  - Player qualifying crew count is refreshed on the existing 20-second broad vessel check.
- [ ] Define the separate **Minmus Base static funding** contract.
  - Unlock after Minmus Base I is completed.
  - Funding scales from the current number of Kerbals in qualifying Minmus surface bases.
  - Player qualifying crew count is refreshed on the existing 20-second broad vessel check.
- [ ] Define eligibility, payout-per-Kerbal values, any crew caps, maintenance/loss behaviour, and resumption rules for Mun/Minmus static funding.
- [ ] Implement the agreed Mun/Minmus one-off surface-base progression and separate static base funding.

### Surface-base balance pass before implementation

Before coding the surface-base tranche, lock down:

- required Kerbal counts for every one-off Base I-II-III tier;
- target durations for each one-off tier;
- exact base hardware or capability requirements so an ordinary parked capsule does not accidentally qualify unless that is deliberately intended;
- difficulty values for rival live missions where applicable;
- one-off Base I-II-III reward values;
- static/recurring Funds-per-Kerbal values and any qualifying-crew caps;
- whether static funding aggregates Kerbals across multiple qualifying bases or only one qualifying base per programme;
- how a fixed recurring funding pool, if used, is shared between agencies based on qualifying Kerbal counts;
- whether a base must remain continuously valid between 20-second observations and funding boundaries;
- how vessel/base identity survives docking, undocking, crew changes, save/load, and scene changes;
- what counts as losing a base and how static funding resumes after recovery;
- how rival agencies represent established bases and current qualifying Kerbal counts without needing physical KSP vessels;
- how player and rival base progress share the existing campaign unlock/funding systems;
- the final balance and progression rules for the new Mun/Minmus crewed-landing prerequisites.

### Performance requirement for surface-base tracking

Before implementation, perform the required `AGENTS.md` cost review for the new base checks.

- Reuse the existing 20-second broad loaded/unloaded vessel capture where practical instead of creating another vessel scan.
- Capture only the project-owned fields needed to identify a qualifying base, its location, stable identity, and Kerbal count.
- Avoid rescanning every part of every vessel multiple times for Desert, Polar, Mun, and Minmus funding separately; derive all relevant base programme observations from one broad snapshot pass where possible.
- Verify behaviour and cost with multiple loaded/unloaded bases and high timewarp/save-load transitions before live acceptance.

Keep this section as design work until those rules are approved. Structural, save-format, or configuration changes needed by the surface-base implementation still require the normal approval process in `AGENTS.md`.

## Future orbital-base contract line

Orbital bases around **Kerbin, Mun, and Minmus** are intentionally a separate future contract line. They are not prerequisites in the current Desert/Polar -> Mun/Minmus surface-base progression and should be designed at a later date.

- [ ] Define the overall orbital-base/station progression separately from the surface-base programme.
- [ ] Define and balance **Kerbin Orbital Base I-III**.
- [ ] Define and balance **Mun Orbital Base I-III**.
- [ ] Define and balance **Minmus Orbital Base I-III**.
- [ ] Decide the unlock path for the orbital-base line without coupling it to the current surface-base implementation unless deliberately approved later.
- [ ] Define crew, duration, orbit, station capability, persistence, loss, and resumption rules.
- [ ] Decide whether orbital bases receive their own separate static/recurring crew-based funding contracts.
- [ ] Define rival orbital-base mission profiles and representation.
- [ ] Perform a separate performance/save-format review before implementing orbital-base tracking.
- [ ] Implement the orbital-base contract line only after its design is separately approved.
