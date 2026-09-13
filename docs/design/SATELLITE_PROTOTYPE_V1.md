# Satellite Prototype v1 - Historical Design Record

> **Historical document.** This file describes the first playable vertical slice. It is not the current 0.5 design. Current terminology is used where possible so the document remains easy to compare with the modern codebase.

## Purpose

The first satellite prototype proved that a small competitive campaign loop could work before the project added flexible objectives, funding lifecycles, persistent rivals, or Pre-Orbit contracts.

The prototype deliberately stayed narrow.

## Included systems

- two simulated rival agencies:
  - Aster Aerospace Directorate;
  - Cobalt Orbital Bureau;
- three satellite targets around Kerbin, Mun, and Minmus;
- player satellite counting from KSP vessel data, including unloaded vessels;
- deterministic rival progress for repeatable testing;
- a basic Command Center;
- F8 show/hide control.

## Early satellite funding model

The original prototype used a simple first-to-complete award model.

| Target | Requirement | Award |
| --- | --- | ---: |
| Kerbin Orbital Network | 2 satellites orbiting Kerbin | 25,000 |
| Mun Survey Network | 1 satellite orbiting Mun | 40,000 |
| Minmus Relay Initiative | 1 satellite orbiting Minmus | 50,000 |

The first agency observed meeting the requirement claimed the award.

This was later replaced by the shared funding systems used by newer versions.

## Early rival schedule

Rival progress was deterministic rather than cost/probability driven.

Aster reached:

- Kerbin after 3 Kerbin days;
- Mun after 20 days;
- Minmus after 45 days.

Cobalt reached:

- Kerbin after 5 Kerbin days;
- Mun after 15 days;
- Minmus after 35 days.

## What this prototype proved

The important technical result was that the mod could:

- count qualifying Probe and Relay vessels;
- continue counting unloaded vessels;
- represent abstract rival agencies without physical rival craft;
- update campaign information on a controlled refresh cadence rather than scanning every frame;
- present the results in one Command Center.

## Historical manual test

The original test flow was:

1. Build against KSP 1.12.x.
2. Deploy the DLL to `GameData/TheRaceForSpace/Plugins/`.
3. Open the Command Center and verify F8 hide/show.
4. Put Probe or Relay vessels into orbit around Kerbin, Mun, and Minmus.
5. Leave vessels unloaded and confirm they were still counted.
6. Time-warp through rival thresholds and confirm rival progress updated.

## Limitations at that time

The first prototype did **not** yet have:

- persistent campaign state;
- real Career-funds integration;
- flexible objective funding contracts;
- sponsor reviews;
- the current `CampaignController` / `ObjectiveCatalogue` model;
- the current Pre-Orbit phase.

Satellite qualification was intentionally narrow: only orbiting `Probe` and `Relay` vessel types counted.

## Current equivalent

The original vessel-counting idea now lives in the slower orbital path:

```text
KspVesselMonitor -> OrbitingVesselSnapshot -> OrbitalVesselTracker
```

Current satellite-network funding is represented by `SatelliteNetworkFundingContract`, and current campaign coordination is owned by `CampaignController`.
