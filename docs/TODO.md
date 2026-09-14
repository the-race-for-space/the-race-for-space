# Development To-Do

This file contains only work that is still active after the Content v0.6 rival-programme implementation.

The full working TODO used while version 0.6 was designed and implemented is preserved unchanged in [`CONTENT_V0_6_TODO_ARCHIVE.md`](CONTENT_V0_6_TODO_ARCHIVE.md). That archive is the historical record of the completed v0.6 tasks, design decisions, implementation plan, and balance notes. Do not edit the archive to reflect later implementation changes; update this active TODO instead.

## Content v0.6 acceptance

- [ ] Complete the live KSP 1.12.x acceptance pass in [`CONTENT_V0_6_TESTING.md`](CONTENT_V0_6_TESTING.md).
  - The KSP-independent logic and controller/integration suites passed during Task 16.
  - The remaining work is the real KSP boundary: build/deploy, stock Science interaction, save/load and timewarp, notifications, and UI presentation.
  - Record the tested commit and any failures/fixes in the acceptance checklist before treating v0.6 as fully accepted.

## Base and station progression

The rival Science/programme work is implemented. The remaining major Content v0.6 content tranche is the surface/orbital base progression discussed during planning.

### Locked base funding model

The base programme uses two separate kinds of funding:

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
- **Desert Base III OR Polar Base III** unlocks the Kerbin orbital-base programme.
- Mun and Minmus base tiers follow the same separation: their Base I-III progression contracts are one-off, while maintaining qualifying established bases provides separate static/recurring funding based on current qualifying Kerbal presence.
- Each Mun/Minmus base programme should have its own associated static funding contract rather than turning the Base I-II-III progression tiers themselves into recurring contracts. The exact split between orbital and ground static programmes is to be finalized during the Mun/Minmus balance pass.

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
- [ ] Make **Desert Base III OR Polar Base III** unlock the Kerbin orbital-base programme.

### Kerbin orbital base

- [ ] Define and balance **Kerbin Orbital Base I-III** as one-off progression contracts.
- [ ] Define crew, duration, orbit, base/station capability, persistence, loss, and resumption rules.
- [ ] Decide whether the Kerbin orbital-base programme also receives its own separate static/recurring crew-based funding contract; the currently locked static-funding decision explicitly covers Desert, Polar, Mun and Minmus programmes.
- [ ] Implement the Kerbin orbital-base progression.
- [ ] Define the point at which Kerbin orbital-base progress unlocks Mun and Minmus base programmes.

### Mun and Minmus bases

- [ ] Define and balance **Mun Orbital Base I-III** as one-off progression contracts.
- [ ] Define and balance **Mun Ground Base I-III** as one-off progression contracts.
- [ ] Define and balance **Minmus Orbital Base I-III** as one-off progression contracts.
- [ ] Define and balance **Minmus Ground Base I-III** as one-off progression contracts.
- [ ] Define the separate Mun/Minmus static base-funding contracts that accompany those one-off progression lines.
  - Static funding is based on the current number of Kerbals in qualifying established bases for the relevant programme.
  - Player qualifying crew counts use the existing 20-second broad vessel check rather than a new polling loop.
  - Finalize whether orbital and ground programmes each have their own static contract or whether any body-level funding is deliberately combined.
- [ ] Define eligibility, payout-per-Kerbal values, any crew caps, maintenance/loss behaviour, and resumption rules for Mun/Minmus static funding.
- [ ] Implement the agreed Mun/Minmus one-off base progression and separate static base funding.

### Base/station balance pass before implementation

Before coding the base/station tranche, lock down:

- required Kerbal counts for every one-off Base I-II-III tier;
- target durations for each one-off tier;
- exact base/station hardware or capability requirements so an ordinary parked capsule does not accidentally qualify unless that is deliberately intended;
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
- the exact Mun/Minmus split between orbital static funding, ground static funding, or any deliberately combined body-level static funding.

### Performance requirement for base tracking

Before implementation, perform the required `AGENTS.md` cost review for the new base checks.

- Reuse the existing 20-second broad loaded/unloaded vessel capture where practical instead of creating another vessel scan.
- Capture only the project-owned fields needed to identify a qualifying base, its location/orbit, stable identity, and Kerbal count.
- Avoid rescanning every part of every vessel multiple times for Desert, Polar, Mun, and Minmus funding separately; derive all relevant base programme observations from one broad snapshot pass where possible.
- Verify behaviour and cost with multiple loaded/unloaded bases and high timewarp/save-load transitions before live acceptance.

Keep this section as design work until those rules are approved. Structural, save-format, or configuration changes needed by the base/station implementation still require the normal approval process in `AGENTS.md`.
