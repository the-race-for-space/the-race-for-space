# Version 0.3 Scope

Version 0.3 expands the current prototype by introducing campaign progression, competitive achievement contracts, and a more useful information-focused Space Race view.

The goal is to prove these systems with a narrow implementation before adding more celestial bodies, more rival behaviours, or a large number of new target types.

## 0.3 Goal List

1. **Funding Target Unlocking**
   - Funding targets can be locked or available.
   - Both Kerbin orbit achievement contracts are active at the beginning of the campaign:
     - Probe Orbit;
     - Crewed Orbit.
   - The Kerbin satellite-network contract begins locked.
   - The Kerbin satellite-network contract unlocks once at least one agency has achieved the Probe Orbit objective.
   - The Mun and Minmus satellite-network contracts begin locked.
   - The Mun and Minmus satellite-network contracts unlock once the Kerbin satellite-network target reaches 60% of its 10-satellite requirement.
   - For the 0.3 design, 60% completion means 6 qualifying Kerbin satellites combined across the player and rival agencies.
   - Once a satellite-network contract unlocks it remains unlocked permanently for that save.
   - Satellite-network contracts do not use declining interest and do not expire.
   - Locked targets do not contribute funding or projected payouts.
   - Locked targets are not valid rival objectives until they become available.
   - Locked targets are hidden from the Funding Targets interface because that view should show only contracts currently in play.
   - Locked and unlocked objectives remain visible in the Space Race information view so the player can understand future progression and unlock requirements.

2. **Competitive Achievement Contracts**
   - Introduce a second funding-target style based on achieving a specific objective rather than maintaining a number of satellites.
   - Version 0.3 contains two Kerbin orbit achievement objectives:
     - **Probe Orbit** — achieve orbit around Kerbin with a qualifying uncrewed probe.
     - **Crewed Orbit** — achieve orbit around Kerbin with a vessel carrying at least one live Kerbal.
   - Both contracts are active from the beginning of the campaign.
   - Probe Orbit has a base payout value of **100,000 funds** at 100% interest.
   - Crewed Orbit has a base payout value of **200,000 funds** at 100% interest.
   - Probe Orbit is the fixed starting objective for both rival agencies even though Crewed Orbit is also available.
   - Each achievement is recorded separately for the player, Aster, and Cobalt.
   - An agency only needs to achieve the objective once to qualify for that contract's future payouts.
   - The first agency gains an advantage only when another agency fails to qualify before the next scheduled funding date.
   - Later agencies can still achieve the objective and join the remaining payouts while the contract is still live.
   - An agency does not receive retroactive shares of payouts that occurred before it achieved the objective.
   - When a competitive achievement contract reaches the end of its interest period it becomes expired and no longer produces funding.
   - Expired achievement contracts are removed from the Funding Targets view but remain described in the Space Race information view.
   - Declining interest applies only to Probe Orbit and Crewed Orbit in version 0.3. Satellite-network contracts use their existing permanent recurring funding model once unlocked.

3. **Shared Funding Dates, Declining Interest, and Competitive Payouts**
   - All contracts use one global funding calendar.
   - Funding is paid every **90 Kerbin days** on the same funding date shown on the Overview tab.
   - Each rival agency also receives a guaranteed **10,000 funds base income** on every global 90-day funding date, independent of contract performance.
   - Probe Orbit and Crewed Orbit do not create independent payout dates.
   - Achieving an orbit objective does **not** pay immediately.
   - The first payout for an orbit contract occurs on the next global funding date after at least one agency has achieved that objective.
   - That first scheduled payout is paid at 100% sponsor/public interest.
   - After each paid global funding date, that orbit contract's interest falls by 10 percentage points for its next payment.
   - The 0.3 payout sequence for each orbit contract is:
     - first eligible global funding date: 100%;
     - second eligible global funding date: 90%;
     - third eligible global funding date: 80%;
     - fourth eligible global funding date: 70%;
     - fifth eligible global funding date: 60%;
     - sixth eligible global funding date: 50%;
     - seventh eligible global funding date: 40%;
     - eighth eligible global funding date: 30%;
     - ninth eligible global funding date: 20%;
     - tenth and final eligible global funding date: 10%.
   - After the final 10% payment the contract is no longer live.
   - Probe Orbit therefore has total available payout amounts of 100,000 at 100% interest, then 90,000, 80,000, and so on down to 10,000 for the final payment.
   - Crewed Orbit therefore has total available payout amounts of 200,000 at 100% interest, then 180,000, 160,000, and so on down to 20,000 for the final payment.
   - Each payment's total available amount is split equally between all agencies that have achieved that objective by that global funding date.
   - Example: the player achieves Probe Orbit on Day 50 and Aster achieves Probe Orbit on Day 80. Both have qualified by the Day 90 funding date, so they split the 100% Probe Orbit payout and receive 50,000 each. The next Probe Orbit payment is then 90% on Day 180.
   - If Cobalt does not achieve Probe Orbit until after Day 90, it receives no share of the Day 90 payment but may join later payments once qualified.
   - Probe Orbit and Crewed Orbit keep independent achievement states and interest stages, but they always pay on the same global funding dates as every other live contract.
   - Satellite-network contracts are not affected by declining interest. Once unlocked, their funding remains live permanently and continues to use the existing recurring satellite-share calculation on the same global 90-day funding dates.

4. **Rival Achievement Missions**
   - Rival agencies satisfy competitive achievement contracts through the existing simulated launch-development system rather than through real KSP vessels.
   - Both rivals begin a new campaign targeting Probe Orbit rather than a satellite-network contract.
   - A rival achievement mission uses the same general development model already used for satellite launches:
     - a planned target;
     - development progress;
     - periodic progress checks;
     - development spending;
     - completion at 100% progress.
   - Probe Orbit uses the same rival development cost as the current Kerbin satellite mission:
     - 20,000 funds for each successful 10% development step;
     - 200,000 funds for a complete 0-100% development cycle.
   - Crewed Orbit costs double the Probe Orbit/Kerbin satellite development amount:
     - 40,000 funds for each successful 10% step;
     - 400,000 funds for a complete 0-100% development cycle.
   - The existing five-Kerbin-day progress-check cadence, 50% success chance, and 10-percentage-point successful progress increment remain the prototype defaults unless separately changed later.
   - Completing an achievement mission marks that agency as having achieved the objective instead of adding a satellite count.
   - After completing Probe Orbit, the rival may select another currently available objective using the existing lightweight target-selection approach.
   - Because Crewed Orbit is active from the start, it is a valid later rival target once the fixed opening Probe Orbit mission has been completed.
   - The Kerbin satellite-network contract becomes a valid rival target once Probe Orbit has been achieved by any agency and the contract unlocks.
   - Mun and Minmus satellite missions become valid rival targets only after the combined Kerbin satellite count reaches 6.
   - Locked objectives cannot be selected.
   - Probe Orbit remains a fixed starting rival objective so the opening race is consistent rather than random.

5. **Space Race Information Centre**
   - Change the Space Race tab from its current milestone-progress role into the main information and guidance view for the mod.
   - The top of the view contains a **Help / Player Guide** section.
   - Version 0.3 may use placeholder player-facing help text while the gameplay systems are being implemented and tested.
   - The final guide wording will be written after the mechanics and interface behaviour are proven.
   - The eventual guide should explain, in concise in-game language:
     - the overall Race for Space gameplay loop;
     - what counts as a qualifying satellite;
     - how the shared 90-day funding date works;
     - how satellite-network funding is shared;
     - how objectives unlock;
     - how the Probe Orbit and Crewed Orbit competitive contracts work;
     - how declining interest and shared payouts work;
     - that declining interest applies only to the two orbit contracts;
     - that satellite-network contracts remain permanent once unlocked;
     - how rival agencies compete for the same objectives;
     - what the main Command Center tabs are used for.
   - Under the help section, display the complete objective list in progression order.
   - Objectives are grouped in this order:
     1. **Orbit Contracts**
        - Probe Orbit;
        - Crewed Orbit.
     2. **Satellite Contracts**
        - Kerbin Orbital Network;
        - Mun Survey Network;
        - Minmus Relay Initiative.
   - The Space Race view shows objectives whether they are locked, active, achieved, or expired.
   - Each objective entry should explain:
     - objective name;
     - objective description;
     - current state;
     - unlock requirement where applicable;
     - basic funding rule;
     - any special competitive or declining-interest rule.
   - The purpose of this view is explanation and progression visibility rather than duplicating all live payout details from the Funding Targets view.
   - Where practical, displayed descriptions and requirements should come from the same target definitions used by gameplay logic so the UI and rules do not drift apart.

6. **Funding Targets Interface Rules**
   - The Funding Targets tab shows only contracts that are currently in play.
   - At the beginning of a new campaign this means Probe Orbit and Crewed Orbit are visible, while all three satellite contracts are hidden.
   - Locked contracts are not shown on this tab.
   - The Kerbin satellite-network contract appears after any agency achieves Probe Orbit.
   - The Mun and Minmus satellite-network contracts appear after the combined qualifying Kerbin satellite count reaches 6.
   - Satellite-network contracts remain visible permanently after unlocking because they continue to participate in the recurring funding system.
   - Probe Orbit and Crewed Orbit remain visible while their declining-interest payment stages are live.
   - All visible contracts use the same next funding date shown on the Overview tab.
   - Each orbit achievement contract is removed from this tab after its final 10% payment because it is no longer an active funding opportunity.
   - The Space Race information view remains the place where the player can see the complete progression, including objectives that are locked or no longer active.

7. **Persistence and Save Compatibility**
   - Save new unlock and achievement state where it cannot be reconstructed safely.
   - Persist each agency's completion of Probe Orbit and Crewed Orbit, including achievement timing where needed to determine eligibility at a funding boundary crossed during time-warp.
   - Persist each orbit contract's started state and current payout stage so declining interest resumes correctly after save/load.
   - Do not maintain independent per-contract payout schedules; the controller's single global 90-day funding date owns all payout timing.
   - Legacy per-contract payout timestamp fields from the initial 0.3 implementation pass may remain readable for save compatibility but are not used by gameplay.
   - Persist satellite-contract unlock state where needed so unlocked contracts remain available permanently even if later game state would no longer independently demonstrate the original unlock moment.
   - Persist rival planned mission and development progress when rivals are targeting achievement contracts, using the existing rival save approach where practical.
   - Keep older saves valid by supplying safe defaults for new 0.3 state.
   - Existing player satellite counts continue to come from real KSP vessel tracking rather than being duplicated in Race for Space persistence.

8. **Keep Version 0.3 Narrow**
   - Do not introduce a major rival-AI redesign in this version.
   - Do not expand the prototype to a large number of additional celestial bodies in this version.
   - Version 0.3 proves only the two opening orbit achievements plus the existing Kerbin, Mun, and Minmus satellite contracts.
   - Use the existing project/module structure wherever practical.
   - Extend the current funding, competition, simulation, persistence, tracking, and UI code rather than replacing them with a general-purpose framework unless the current structure demonstrably cannot support the requirements cleanly.
   - Use 0.3 to test whether unlocking progression, achievement races, declining-interest funding, and the redesigned information view are understandable and enjoyable before expanding them further.

## Version 0.3 Progression Summary

The intended opening progression is:

1. Probe Orbit and Crewed Orbit are both active competitive contracts from the beginning of the campaign.
2. The player, Aster, and Cobalt can qualify for either orbit contract, but Aster and Cobalt always begin by developing Probe Orbit first.
3. When the first agency achieves Probe Orbit, Probe Orbit becomes eligible for its first 100% payment on the next global 90-day funding date.
4. Probe Orbit achievement also permanently unlocks the Kerbin Orbital Network satellite contract.
5. When the first agency achieves Crewed Orbit, Crewed Orbit similarly becomes eligible for its first 100% payment on the next global funding date.
6. Any other agency that achieves an orbit objective before that same funding date joins the split of that payment.
7. Agencies achieving the objective after a funding date only join later payments; earlier payments are never recalculated.
8. After each orbit-contract payment, that contract's interest falls by 10 percentage points for the next global funding date.
9. The Kerbin satellite contract progresses using qualifying satellites from all agencies and does not decline or expire.
10. At 6 combined qualifying Kerbin satellites, representing 60% of the 10-satellite target, the Mun Survey Network and Minmus Relay Initiative permanently unlock.
11. Probe Orbit and Crewed Orbit eventually expire after their final 10% payments.
12. Unlocked satellite-network contracts remain part of the recurring funding competition permanently.

## Version 0.3 Fixed Tuning Values

The implementation should use the following prototype values unless a later design change explicitly replaces them:

- Probe Orbit base 100% payout: **100,000 funds**.
- Crewed Orbit base 100% payout: **200,000 funds**.
- Probe Orbit availability: **active from campaign start**.
- Crewed Orbit availability: **active from campaign start**.
- Probe Orbit rival development cost: **20,000 funds per successful 10% step / 200,000 total**.
- Crewed Orbit rival development cost: **40,000 funds per successful 10% step / 400,000 total**.
- Shared funding interval for all contracts: **90 Kerbin days**.
- Rival base income: **10,000 funds per rival agency on every global 90-day funding date, independent of contract payouts**.
- First orbit-contract payout: **100% on the first global funding date after at least one agency achieves the objective**.
- Orbit-contract interest reduction: **10 percentage points after each paid funding date**.
- Orbit-contract final payment: **10%**, after which that contract expires.
- Kerbin satellite unlock: **any agency achieves Probe Orbit**.
- Mun and Minmus satellite unlock: **6 combined qualifying Kerbin satellites**.
- Satellite contracts: **permanent once unlocked; no declining interest or expiry**.
- Space Race Help / Player Guide wording: **placeholder text for the initial 0.3 implementation**.

With these values defined, there are no remaining gameplay tuning decisions required before continuing the 0.3 implementation pass. Additional balancing can follow prototype testing.
