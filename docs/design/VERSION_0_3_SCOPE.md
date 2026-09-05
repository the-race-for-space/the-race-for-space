# Version 0.3 Scope - Historical Design Record

> **Historical document.** Version 0.3 introduced many systems that still exist in evolved form. This file uses current terminology where possible, but the values and campaign shape below describe the 0.3 design rather than current 0.5 balance.

## Purpose

Version 0.3 expanded the early satellite prototype into a real campaign progression model.

Its main goals were:

- unlockable objectives;
- one-off objective funding contracts;
- shared funding dates;
- declining funding interest;
- rival objective missions;
- persistent objective/funding state;
- a clearer progression-information view.

The campaign was still limited to Kerbin, Mun, and Minmus.

## Objective progression

Version 0.3 introduced six one-off orbital objectives:

- Probe Orbit — uncrewed Probe/Relay around Kerbin;
- Crewed Orbit — crewed Kerbin orbit;
- Mun Probe Orbit;
- Minmus Probe Orbit;
- Mun Crewed Orbit;
- Minmus Crewed Orbit.

At campaign start:

- Probe Orbit was available;
- Crewed Orbit was available;
- the four Mun/Minmus objectives were locked.

Unlock flow:

```text
Probe Orbit
    +--> Mun Probe Orbit
    +--> Minmus Probe Orbit

Crewed Orbit
    +--> Mun Crewed Orbit
    +--> Minmus Crewed Orbit
```

Any agency could satisfy the prerequisite.

Completing an uncrewed Probe Orbit objective also created one qualifying satellite for that body. Crewed Orbit completion did not create a satellite.

## Objective funding contracts

Version 0.3 introduced one-off funding tied to objective completion.

Historical base payouts were:

| Objective | 100% base payout |
| --- | ---: |
| Probe Orbit | 100,000 |
| Crewed Orbit | 200,000 |
| Mun Probe Orbit | 200,000 |
| Minmus Probe Orbit | 200,000 |
| Mun Crewed Orbit | 300,000 |
| Minmus Crewed Orbit | 300,000 |

An agency only needed to complete an objective once to become eligible for later payments from that objective funding contract.

Later agencies could join future payments but did not receive retroactive shares.

## Shared funding calendar

All funding used one shared **90 Kerbin-day** calendar.

Objective funding did not pay immediately on completion.

The payment sequence was:

```text
100%, 90%, 80%, 70%, 60%, 50%, 40%, 30%, 20%, 10%
```

After the final 10% payment, the one-off objective funding contract expired.

At each funding boundary, the payment was split between agencies that had completed the objective by that exact time.

Satellite-network funding used the same shared funding dates but did not decline or expire.

This shared-calendar idea remains part of the modern campaign.

## Satellite-network unlocks

Historical unlock flow:

- Kerbin network unlocked after any agency completed Probe Orbit.
- Mun network unlocked after any agency completed Mun Probe Orbit.
- Minmus network unlocked after any agency completed Minmus Probe Orbit.

Satellite-network funding was permanent after unlock.

Locked networks did not fund agencies and were not valid rival targets.

## Rival missions

Version 0.3 moved rivals from fixed body schedules to mission development.

Historical rival rules:

- progress check every **5 Kerbin days**;
- **30%** chance of success;
- **10%** mission progress on success;
- progress cost paid only on a successful step;
- mission cannot progress if the rival cannot afford the step.

Historical costs:

| Mission type | Cost per 10% step | Full cost |
| --- | ---: | ---: |
| Probe Orbit / Kerbin satellite | 20,000 | 200,000 |
| Crewed Orbit / Mun / Minmus missions | 40,000 | 400,000 |

Both rivals began by targeting Probe Orbit.

After the first mission, when both categories had valid targets, target selection used:

- 60% satellite mission;
- 40% one-off objective mission.

Inside the chosen category, valid targets were selected with equal probability.

Locked objectives were never valid rival targets.

## Campaign information view

Version 0.3 turned the fourth Command Center view into a campaign guide.

Funding opportunities were grouped by state so the player could understand:

- what was currently available;
- what was still locked;
- what had expired;
- what requirement would unlock the next objective.

This idea later evolved into the current **Contract Catalogue** with `Offered`, `Unlocked`, `Locked`, and `Expired` states.

## Persistence

Version 0.3 expanded save data to include:

- objective completion state and timestamps for each agency;
- one-off objective funding lifecycle;
- satellite-network unlock state;
- rival mission target and progress;
- shared funding timing.

Player satellite counts continued to come from real KSP vessels instead of being duplicated into campaign save state.

That ownership rule remains important today.

## Historical progression summary

The intended 0.3 flow was:

1. Probe Orbit and Crewed Orbit begin available.
2. Rivals begin with Probe Orbit.
3. Probe Orbit unlocks Mun/Minmus Probe Orbit and the Kerbin network.
4. Crewed Orbit unlocks Mun/Minmus Crewed Orbit.
5. Mun Probe Orbit unlocks the Mun network.
6. Minmus Probe Orbit unlocks the Minmus network.
7. One-off objective funding starts after completion and pays on the next shared funding date.
8. Later agencies may join later payments.
9. One-off funding declines to 10% and expires.
10. Satellite-network funding remains active after unlock.

## Historical fixed values

For reference, version 0.3 used:

- shared funding interval: **90 Kerbin days**;
- rival base income: **20,000 funds per funding date**;
- rival progress check: **every 5 Kerbin days**;
- rival success chance: **30%**;
- rival progress step: **10%**;
- objective funding decline: **10 percentage points per payment**;
- final objective payment: **10%**.

These values are historical and should not be assumed to match current 0.5 tuning.

## What version 0.3 established for later versions

Version 0.3 introduced several ideas still visible in the current architecture:

- campaign-wide objectives;
- Any Agency unlock progression;
- one-off objective funding;
- recurring satellite-network funding;
- one shared funding calendar;
- rival missions against the same target set;
- persistent objective/funding/rival state;
- UI presentation of locked and available progression.

The modern implementation now represents these through `ObjectiveDefinition`, `UnlockRuleEvaluator`, `ObjectiveFundingContract`, `SatelliteNetworkFundingContract`, `RivalSimulation`, `CampaignController`, and the Contract Catalogue.
