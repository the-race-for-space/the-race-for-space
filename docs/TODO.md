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

### Kerbin proving grounds

- [ ] Define and balance **Desert Base I-III**.
- [ ] Define and balance **Polar Base I-III**.
- [ ] Decide the exact qualification rules for long-duration Kerbin bases, including crew, location tolerance, continuous-duration behaviour, vessel/base identity, loss, and resumption.
- [ ] Implement the agreed Desert and Polar base objectives/contracts.
- [ ] Make **Desert Base III OR Polar Base III** unlock the Kerbin orbital-base programme.

### Kerbin orbital base

- [ ] Define and balance **Kerbin Orbital Base I-III**.
- [ ] Define crew, duration, orbit, persistence, loss, and resumption rules.
- [ ] Implement the Kerbin orbital-base progression.
- [ ] Define the point at which Kerbin orbital-base progress unlocks Mun and Minmus base funding.

### Mun and Minmus bases

- [ ] Define and balance **Mun Orbital Base I-III**.
- [ ] Define and balance **Mun Ground Base I-III**.
- [ ] Define and balance **Minmus Orbital Base I-III**.
- [ ] Define and balance **Minmus Ground Base I-III**.
- [ ] Define the twelve recurring Mun/Minmus base funding contracts, including eligibility, progression, payouts, maintenance/loss behaviour, and resumption rules.
- [ ] Implement the agreed recurring base funding and progression.

### Base/station balance pass before implementation

Before coding the base/station tranche, lock down:

- required Kerbal counts;
- target durations for each level;
- difficulty values for rival live missions where applicable;
- one-off versus recurring funding values;
- whether a base must remain continuously valid between funding boundaries;
- how vessel/base identity survives docking, undocking, crew changes, save/load, and scene changes;
- what counts as losing a base and how funding resumes after recovery;
- how player and rival base progress share the existing campaign unlock/funding systems.

Keep this section as design work until those rules are approved. Structural, save-format, or configuration changes needed by the base/station implementation still require the normal approval process in `AGENTS.md`.
