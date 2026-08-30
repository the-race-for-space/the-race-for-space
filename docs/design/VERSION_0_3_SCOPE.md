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
   - The first agency gains the largest advantage because it can receive declining-interest payouts before later agencies qualify.
   - Later agencies can still achieve the objective and join the remaining payouts while the contract is still live.
   - An agency does not receive retroactive shares of payouts that occurred before it achieved the objective.
   - When a competitive achievement contract reaches the end of its interest period it becomes expired and no longer produces funding.
   - Expired achievement contracts are removed from the Funding Targets view but remain described in the Space Race information view.
   - Declining interest applies only to Probe Orbit and Crewed Orbit in version 0.3. Satellite-network contracts use their existing permanent recurring funding model once unlocked.

3. **Declining Interest and Competitive Payouts**
   - The declining-interest period begins separately for each orbit achievement contract when the first player or rival agency achieves that objective.
   - The target begins at 100% sponsor/public interest.
   - Interest then reduces by 10 percentage points for each following 90-Kerbin-day payout period.
   - The 0.3 payout sequence is:
     - initial achievement payout: 100%;
     - first 90-day payout: 90%;
     - second 90-day payout: 80%;
     - third 90-day payout: 70%;
     - fourth 90-day payout: 60%;
     - fifth 90-day payout: 50%;
     - sixth 90-day payout: 40%;
     - seventh 90-day payout: 30%;
     - eighth 90-day payout: 20%;
     - ninth and final 90-day payout: 10%.
   - After the final 10% payment the contract is no longer live.
   - Probe Orbit therefore has total available payout amounts of 100,000 at 100% interest, then 90,000, 80,000, and so on down to 10,000 for the final payment.
   - Crewed Orbit therefore has total available payout amounts of 200,000 at 100% interest, then 180,000, 160,000, and so on down to 20,000 for the final payment.
   - Each payment's total available amount is split equally between all agencies that have achieved that objective by that payout time.
   - Example: if only Aster has achieved Probe Orbit for the 100% payment, Aster receives 100,000. If the player qualifies before the following 90% payment, the player and Aster split the 90,000 payment equally and receive 45,000 each.
   - This rewards first achievement without permanently excluding later agencies from the contract.
   - Probe Orbit and Crewed Orbit have independent achievement states, interest stages, payout schedules, and expiry times.
   - Satellite-network contracts are not affected by this declining-interest system. Once unlocked, their funding remains live permanently and continues to use the existing recurring satellite-share calculation.

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
     - 40,000 funds for each successful 10% development step;
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
     - how scheduled funding works;
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
   - Probe Orbit and Crewed Orbit remain visible while their own declining-interest payout periods are live.
   - Each orbit achievement contract is removed from this tab after its final 10% payment because it is no longer an active funding opportunity.
   - The Space Race information view remains the place where the player can see the complete progression, including objectives that are locked or no longer active.

7. **Persistence and Save Compatibility**
   - Save new unlock and achievement state where it cannot be reconstructed safely.
   - Persist each agency's completion of Probe Orbit and Crewed Orbit.
   - Persist enough information to resume each active declining-interest contract without resetting its payout schedule after save/load.
   - This includes each orbit contract's first-achievement/start state, current payout stage, next payout timing, and expired state where required.
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
3. When the first agency achieves Probe Orbit, that contract's independent declining-interest payout sequence begins.
4. Probe Orbit achievement also permanently unlocks the Kerbin Orbital Network satellite contract.
5. When the first agency achieves Crewed Orbit, that contract's separate declining-interest payout sequence begins.
6. Agencies can achieve either orbit objective later and join that contract's remaining payouts while it is still live.
7. The Kerbin satellite contract progresses using qualifying satellites from all agencies and does not decline or expire.
8. At 6 combined qualifying Kerbin satellites, representing 60% of the 10-satellite target, the Mun Survey Network and Minmus Relay Initiative permanently unlock.
9. Probe Orbit and Crewed Orbit eventually expire after their own final 10% payments.
10. Unlocked satellite-network contracts remain part of the recurring funding competition permanently.

## Version 0.3 Fixed Tuning Values

The implementation should use the following prototype values unless a later design change explicitly replaces them:

- Probe Orbit base 100% payout: **100,000 funds**.
- Crewed Orbit base 100% payout: **200,000 funds**.
- Probe Orbit availability: **active from campaign start**.
- Crewed Orbit availability: **active from campaign start**.
- Probe Orbit rival development cost: **20,000 funds per successful 10% step / 200,000 total**.
- Crewed Orbit rival development cost: **40,000 funds per successful 10% step / 400,000 total**.
- Orbit-contract payout interval after the initial achievement payment: **90 Kerbin days**.
- Orbit-contract interest reduction: **10 percentage points per payout period**.
- Orbit-contract final payment: **10%**, after which that contract expires.
- Kerbin satellite unlock: **any agency achieves Probe Orbit**.
- Mun and Minmus satellite unlock: **6 combined qualifying Kerbin satellites**.
- Satellite contracts: **permanent once unlocked; no declining interest or expiry**.
- Space Race Help / Player Guide wording: **placeholder text for the initial 0.3 implementation**.

With these values defined, there are no remaining gameplay tuning decisions required before beginning the first 0.3 implementation pass. Additional balancing can follow prototype testing.
