# Version 0.3 Scope

Version 0.3 expands the current prototype by introducing campaign progression, competitive achievement contracts, and a more useful information-focused Space Race view.

The goal is to prove these systems with a narrow implementation before adding more celestial bodies, more rival behaviours, or a large number of new target types.

## 0.3 Goal List

1. **Funding Target Unlocking**
   - Funding targets can be locked or available.
   - The campaign begins with the probe-orbit competitive achievement contract as the first active objective.
   - The Kerbin satellite-network contract begins locked.
   - The Kerbin satellite-network contract unlocks once at least one agency has achieved the probe-orbit objective.
   - The Mun and Minmus satellite-network contracts begin locked.
   - The Mun and Minmus satellite-network contracts unlock once the Kerbin satellite-network target reaches 60% of its 10-satellite requirement.
   - For the 0.3 design, 60% completion means 6 qualifying Kerbin satellites combined across the player and rival agencies.
   - Once a funding target unlocks it remains unlocked unless a later design explicitly introduces another lifecycle rule.
   - Locked targets do not contribute funding or projected payouts.
   - Locked targets are not valid rival objectives until they become available.
   - Locked targets are hidden from the Funding Targets interface because that view should show only contracts currently in play.
   - Locked and unlocked objectives remain visible in the Space Race information view so the player can understand future progression and unlock requirements.

2. **Competitive Achievement Contracts**
   - Introduce a second funding-target style based on achieving a specific objective rather than maintaining a number of satellites.
   - Version 0.3 will contain two Kerbin orbit achievement objectives:
     - **Probe Orbit** — achieve orbit around Kerbin with an uncrewed probe.
     - **Crewed Orbit** — achieve orbit around Kerbin with a vessel carrying at least one live Kerbal.
   - Probe Orbit is the opening competitive objective for the campaign and is the starting objective for both rival agencies.
   - Each achievement is recorded separately for the player, Aster, and Cobalt.
   - An agency only needs to achieve the objective once to qualify for that contract's future payouts.
   - The first agency gains the largest advantage because it can receive declining-interest payouts before later agencies qualify.
   - Later agencies can still achieve the objective and join the remaining payouts while the contract is still live.
   - An agency does not receive retroactive shares of payouts that occurred before it achieved the objective.
   - When a competitive achievement contract reaches the end of its interest period it becomes expired and no longer produces funding.
   - Expired achievement contracts are removed from the Funding Targets view but remain described in the Space Race information view.

3. **Declining Interest and Competitive Payouts**
   - The declining-interest period begins when the first player or rival agency achieves the competitive objective.
   - The target begins at 100% sponsor/public interest.
   - Interest then reduces by 10 percentage points for each following 90-Kerbin-day payout period.
   - To preserve both the intended nine 90-day periods after the first achievement and the stated final 10% payment, the 0.3 design uses the following sequence:
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
   - Each payment has a total contract payout value for that period based on the current interest percentage.
   - The current payment is split equally between all agencies that have achieved the objective by that payout time.
   - Example: if only Aster has achieved the objective for the 100% payment, Aster receives the entire payment. If the player qualifies before the following 90% payment, the player and Aster split that 90% payment equally.
   - This means first place is rewarded without permanently excluding later agencies from the contract.
   - The exact base funds value assigned to Probe Orbit and Crewed Orbit remains a gameplay tuning value to define before final implementation.

4. **Rival Achievement Missions**
   - Rival agencies satisfy competitive achievement contracts through the existing simulated launch-development system rather than through real KSP vessels.
   - Both rivals begin a new campaign targeting Probe Orbit rather than the Kerbin satellite-network contract.
   - A rival achievement mission uses the same general development model already used for satellite launches:
     - a planned target;
     - development progress;
     - periodic progress checks;
     - development spending;
     - completion at 100% progress.
   - Completing an achievement mission marks that agency as having achieved the objective instead of adding a satellite count.
   - After completing a mission, the rival may select another currently available objective using the existing lightweight target-selection approach.
   - Locked objectives cannot be selected.
   - Satellite contracts only become possible rival targets once their unlock conditions have been met.
   - Probe Orbit is a fixed starting rival objective so the opening race is consistent rather than random.
   - Mission-specific development costs can use the existing rival development-cost model initially, with any different Probe Orbit or Crewed Orbit costs treated as tuning values rather than a new simulation architecture.

5. **Space Race Information Centre**
   - Change the Space Race tab from its current milestone-progress role into the main information and guidance view for the mod.
   - The top of the view contains a **Help / Player Guide** section.
   - The guide should explain, in concise in-game language:
     - the overall Race for Space gameplay loop;
     - what counts as a qualifying satellite;
     - how scheduled funding works;
     - how satellite-network funding is shared;
     - how objectives unlock;
     - how competitive achievement contracts work;
     - how declining interest and shared payouts work;
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
   - Locked contracts are not shown on this tab.
   - A contract appears once it becomes available.
   - Satellite-network contracts remain visible after unlocking because they continue to participate in the recurring funding system.
   - Competitive achievement contracts remain visible while their declining-interest payout period is live.
   - A competitive achievement contract is removed from this tab after the final 10% payment because it is no longer an active funding opportunity.
   - The Space Race information view remains the place where the player can see the complete progression, including objectives that are locked or no longer active.

7. **Persistence and Save Compatibility**
   - Save new unlock and achievement state where it cannot be reconstructed safely.
   - Persist each agency's completion of competitive achievement objectives.
   - Persist enough information to resume an active declining-interest contract without resetting its payout schedule after save/load.
   - This includes the contract's first-achievement/start state, current payout stage, next payout timing, and expired state where required.
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

1. Probe Orbit is the opening competitive contract.
2. The player, Aster, and Cobalt race to achieve Kerbin orbit with a probe.
3. When the first agency achieves Probe Orbit, that contract's declining-interest payout sequence begins.
4. Probe Orbit achievement also unlocks the Kerbin Orbital Network satellite contract.
5. Agencies can continue to achieve Probe Orbit later and join its remaining payouts while the contract is still live.
6. The Kerbin satellite contract progresses using qualifying satellites from all agencies.
7. At 6 combined qualifying Kerbin satellites, representing 60% of the 10-satellite target, the Mun Survey Network and Minmus Relay Initiative unlock.
8. Competitive orbit contracts eventually expire after their final 10% payment, while unlocked satellite-network contracts remain part of the recurring funding competition.

## Remaining Tuning Decisions Before Implementation

The main gameplay structure is now defined. The remaining values or small rules to settle during 0.3 planning are:

- the base payout value for Probe Orbit;
- the base payout value for Crewed Orbit;
- whether Crewed Orbit is active from the start or unlocks after Probe Orbit;
- whether Probe Orbit and Crewed Orbit use the same rival development cost as the current Kerbin satellite mission or separate tuning values;
- final player-facing wording for the Space Race Help / Player Guide section.
