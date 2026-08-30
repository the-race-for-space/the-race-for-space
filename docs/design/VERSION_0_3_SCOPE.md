# Version 0.3 Scope

Version 0.3 expands the current prototype by introducing campaign progression, competitive achievement contracts, and a more useful information-focused Space Race view.

The goal is to prove these systems with a narrow implementation before adding more celestial bodies, more rival behaviours, or a large number of new target types.

## 0.3 Goal List

1. **Funding Target Unlocking**
   - Funding targets can be locked or available.
   - Two one-off achievement contracts are active at the beginning of the campaign:
     - Probe Orbit;
     - Crewed Orbit.
   - Mun Probe Orbit, Minmus Probe Orbit, Mun Crewed Orbit, and Minmus Crewed Orbit begin locked.
   - Both lunar probe-orbit contracts unlock once at least one agency has achieved Probe Orbit around Kerbin.
   - Both lunar crewed-orbit contracts unlock once at least one agency has achieved Crewed Orbit around Kerbin.
   - The Kerbin satellite-network contract begins locked and unlocks once at least one agency achieves Probe Orbit.
   - The Mun satellite-network contract begins locked and unlocks once at least one agency achieves Mun Probe Orbit.
   - The Minmus satellite-network contract begins locked and unlocks once at least one agency achieves Minmus Probe Orbit.
   - A qualifying uncrewed probe that completes a probe-orbit achievement also counts as one satellite around that same body.
   - Crewed-orbit achievements do not add satellites to a network.
   - Once a satellite-network contract unlocks it remains unlocked permanently for that save.
   - Satellite-network contracts do not use declining interest and do not expire.
   - Locked targets do not contribute funding or projected payouts and are not valid rival objectives.
   - Locked targets are hidden from Funding Targets but remain visible in Space Race so the unlock requirement is clear.

2. **Competitive Achievement Contracts**
   - Achievement contracts are based on completing a specific objective rather than maintaining a number of satellites.
   - Version 0.3 contains six one-off achievement objectives:
     - **Probe Orbit** — orbit Kerbin with a qualifying uncrewed Probe or Relay vessel;
     - **Crewed Orbit** — orbit Kerbin with at least one live Kerbal aboard;
     - **Mun Probe Orbit** — orbit Mun with a qualifying uncrewed Probe or Relay vessel;
     - **Minmus Probe Orbit** — orbit Minmus with a qualifying uncrewed Probe or Relay vessel;
     - **Mun Crewed Orbit** — orbit Mun with at least one live Kerbal aboard;
     - **Minmus Crewed Orbit** — orbit Minmus with at least one live Kerbal aboard.
   - Probe Orbit and Crewed Orbit are available from campaign start.
   - Mun Probe Orbit and Minmus Probe Orbit become available only after any agency has achieved Probe Orbit around Kerbin.
   - Mun Crewed Orbit and Minmus Crewed Orbit become available only after any agency has achieved Crewed Orbit around Kerbin.
   - Probe Orbit has a base payout of **100,000 funds** at 100% interest.
   - Crewed Orbit has a base payout of **200,000 funds** at 100% interest.
   - Mun Probe Orbit has a base payout of **200,000 funds** at 100% interest.
   - Minmus Probe Orbit has a base payout of **200,000 funds** at 100% interest.
   - Mun Crewed Orbit has a base payout of **300,000 funds** at 100% interest.
   - Minmus Crewed Orbit has a base payout of **300,000 funds** at 100% interest.
   - Probe Orbit remains the fixed starting objective for both rival agencies.
   - Each achievement is recorded separately for the player, Aster, and Cobalt.
   - An agency only needs to achieve an objective once to qualify for that contract's future payouts.
   - Later agencies can join remaining payouts while the contract is live, but never receive retroactive shares.
   - Each achievement contract expires after its final 10% payment.
   - Expired achievement contracts are removed from Funding Targets but remain in Space Race.
   - Declining interest applies to all six one-off achievement contracts. Satellite contracts remain permanent once unlocked.

3. **Shared Funding Dates, Declining Interest, and Competitive Payouts**
   - All contracts use one global funding calendar.
   - Funding is paid every **90 Kerbin days** on the same funding date shown throughout the Command Center.
   - Each rival agency also receives a guaranteed **20,000 funds base income** on every global funding date.
   - Achievement contracts do not create independent payout dates and never pay immediately on completion.
   - Locked achievement contracts do not start, project funding, or pay until their unlock requirement has been met.
   - The first payout for an achievement contract occurs on the next global funding date after at least one agency has achieved that objective.
   - The first scheduled payout is 100%, followed by 90%, 80%, 70%, 60%, 50%, 40%, 30%, 20%, and a final 10% payout.
   - After the final 10% payment the achievement contract expires.
   - Each payment is split equally between every agency that had achieved that objective by the exact global funding timestamp.
   - Agencies qualifying after a funding date only join later payments.
   - Satellite-network contracts use the same global funding dates but do not decline or expire.

4. **Rival Achievement and Satellite Missions**
   - Rival agencies satisfy achievement contracts through the existing simulated launch-development system rather than real KSP vessels.
   - Both rivals begin a new campaign targeting Probe Orbit.
   - Rival progress is checked every **5 Kerbin days**.
   - Each eligible progress check has a **25% success chance**.
   - A successful check adds **10 percentage points** of mission progress and deducts the applicable development cost.
   - Funds are deducted only when a successful progress step occurs, and progress cannot succeed if the rival lacks the required funds.
   - Probe Orbit and Kerbin satellite development cost **20,000 funds per successful 10% step / 200,000 total**.
   - Crewed Orbit, Mun Probe Orbit, Minmus Probe Orbit, Mun Crewed Orbit, Minmus Crewed Orbit, and ordinary Mun/Minmus satellite missions cost **40,000 funds per successful 10% step / 400,000 total**.
   - Completing Probe Orbit marks the achievement and adds one Kerbin satellite.
   - Completing Mun Probe Orbit marks the achievement and adds one Mun satellite.
   - Completing Minmus Probe Orbit marks the achievement and adds one Minmus satellite.
   - Completing Crewed Orbit, Mun Crewed Orbit, or Minmus Crewed Orbit marks only the relevant achievement and does not add a satellite.
   - Completing a normal satellite mission adds one satellite to the selected body.
   - After the fixed opening Probe Orbit mission, rivals choose randomly from currently valid live achievement and unlocked satellite objectives.
   - Mun Probe Orbit and Minmus Probe Orbit are not valid rival targets until Probe Orbit around Kerbin has been achieved by any agency.
   - Mun Crewed Orbit and Minmus Crewed Orbit are not valid rival targets until Crewed Orbit around Kerbin has been achieved by any agency.
   - Kerbin satellite missions become valid after Probe Orbit unlocks the Kerbin network.
   - Mun satellite missions become valid after Mun Probe Orbit unlocks the Mun network.
   - Minmus satellite missions become valid after Minmus Probe Orbit unlocks the Minmus network.
   - Locked objectives cannot be selected.

5. **Space Race Information Centre**
   - Space Race is the main information and guidance view for the mod.
   - Its Help / Player Guide explains the overall competition, one-off achievements, satellite contracts, and unlock progression.
   - Funding entries are grouped as **Available Funding**, **Locked Funding**, and **Expired Funding**.
   - Live unlocked achievement contracts and unlocked satellite contracts appear under Available Funding.
   - Mun Probe Orbit and Minmus Probe Orbit appear under Locked Funding until Probe Orbit around Kerbin has been achieved by any agency.
   - Mun Crewed Orbit and Minmus Crewed Orbit appear under Locked Funding until Crewed Orbit around Kerbin has been achieved by any agency.
   - Locked satellite contracts also appear under Locked Funding with their unlock requirement.
   - Achievement contracts move to Expired Funding after their final 10% payment.
   - Each objective entry explains its objective, current state, unlock requirement where applicable, and basic funding rule.
   - Where practical, descriptions and requirements come from the same definitions used by gameplay logic so the UI and rules do not drift apart.

6. **Funding Targets Interface Rules**
   - Funding Targets shows only contracts currently in play.
   - At campaign start Probe Orbit and Crewed Orbit are visible; all four lunar achievement contracts and all three satellite contracts are locked and hidden.
   - Mun Probe Orbit and Minmus Probe Orbit appear after any agency achieves Probe Orbit around Kerbin.
   - Mun Crewed Orbit and Minmus Crewed Orbit appear after any agency achieves Crewed Orbit around Kerbin.
   - The Kerbin satellite contract appears after any agency achieves Probe Orbit.
   - The Mun satellite contract appears after any agency achieves Mun Probe Orbit.
   - The Minmus satellite contract appears after any agency achieves Minmus Probe Orbit.
   - Satellite contracts remain visible permanently after unlocking because they continue participating in recurring funding.
   - Achievement contracts remain visible while unlocked and while their declining-interest payment stages are live.
   - All visible contracts use the same next funding date.
   - Achievement contracts disappear from Funding Targets after their final 10% payment.
   - Space Race remains the complete progression view for locked and expired targets.

7. **Persistence and Save Compatibility**
   - Persist each agency's Probe Orbit, Crewed Orbit, Mun Probe Orbit, Minmus Probe Orbit, Mun Crewed Orbit, and Minmus Crewed Orbit completion state and timestamps.
   - Persist each achievement contract's started state and current payment stage.
   - Lunar probe-contract availability is derived from the persisted Kerbin Probe Orbit achievement state; no separate unlock field is required.
   - Lunar crewed-contract availability is derived from the persisted Kerbin Crewed Orbit achievement state; no separate unlock field is required.
   - Do not maintain independent per-contract payout schedules; the controller's single global 90-day funding date owns payout timing.
   - Persist satellite-contract unlock state so an unlocked contract remains permanently available in that save.
   - Persist rival planned mission and development progress using the existing rival save model.
   - Older saves that lack the new Mun/Minmus probe or crewed achievement fields load them with safe defaults.
   - A rival from an older save that already owns a Mun or Minmus satellite is treated as having demonstrated the corresponding probe-orbit achievement at load time rather than losing existing progress.
   - Already-unlocked satellite contracts in older saves remain unlocked.
   - Existing player satellite counts continue to come from real KSP vessel tracking rather than being duplicated in Race for Space persistence.

8. **Keep Version 0.3 Narrow**
   - Do not introduce a major rival-AI redesign.
   - Do not expand beyond the existing Kerbin, Mun, and Minmus progression in this version.
   - Extend the existing funding, competition, simulation, persistence, tracking, and UI code rather than replacing them with a general-purpose framework.
   - Use 0.3 to test whether unlocking progression, achievement races, declining-interest funding, and the information view are understandable and enjoyable before expanding further.

## Version 0.3 Progression Summary

The intended progression is:

1. Probe Orbit and Crewed Orbit are live competitive one-off contracts from campaign start; all four lunar achievement contracts begin locked.
2. Aster and Cobalt always begin by developing Probe Orbit first.
3. Completing Probe Orbit also creates that agency's first Kerbin satellite, unlocks the Kerbin Orbital Network, and unlocks both Mun Probe Orbit and Minmus Probe Orbit for everyone.
4. Completing Crewed Orbit around Kerbin unlocks both Mun Crewed Orbit and Minmus Crewed Orbit for everyone.
5. Completing Mun Probe Orbit also creates that agency's first Mun satellite and unlocks the Mun Survey Network for everyone.
6. Completing Minmus Probe Orbit also creates that agency's first Minmus satellite and unlocks the Minmus Relay Initiative for everyone.
7. Completing Mun Crewed Orbit or Minmus Crewed Orbit records only that crewed achievement and does not alter satellite-network counts or unlocks.
8. Each unlocked one-off achievement becomes eligible for its first 100% payment on the next shared funding date after at least one agency completes it.
9. Other agencies that complete the same achievement before that funding date join the split of that payment.
10. After each achievement-contract payment, interest falls by 10 percentage points until the final 10% payment expires the contract.
11. Unlocked satellite contracts use qualifying satellites from all agencies and never decline or expire.
12. A rival can only select ordinary satellite missions for a body after that body's satellite contract has unlocked.

## Version 0.3 Fixed Tuning Values

The implementation uses these prototype values unless a later design change explicitly replaces them:

- Probe Orbit base 100% payout: **100,000 funds**.
- Crewed Orbit base 100% payout: **200,000 funds**.
- Mun Probe Orbit base 100% payout: **200,000 funds**.
- Minmus Probe Orbit base 100% payout: **200,000 funds**.
- Mun Crewed Orbit base 100% payout: **300,000 funds**.
- Minmus Crewed Orbit base 100% payout: **300,000 funds**.
- Mun Survey Network total available payout: **100,000 funds**.
- Minmus Relay Initiative total available payout: **100,000 funds**.
- Probe Orbit and Crewed Orbit: **available from campaign start**.
- Mun Probe Orbit and Minmus Probe Orbit unlock: **any agency achieves Probe Orbit around Kerbin**.
- Mun Crewed Orbit and Minmus Crewed Orbit unlock: **any agency achieves Crewed Orbit around Kerbin**.
- Probe Orbit rival development cost: **20,000 funds per successful 10% step / 200,000 total**.
- Crewed Orbit rival development cost: **40,000 funds per successful 10% step / 400,000 total**.
- Mun/Minmus probe, crewed-orbit, and satellite development cost: **40,000 funds per successful 10% step / 400,000 total**.
- Rival mission progress check: **every 5 Kerbin days**.
- Rival progress success chance: **25%**.
- Successful rival progress increment: **10 percentage points**.
- Shared funding interval: **90 Kerbin days**.
- Rival base income: **20,000 funds per rival agency on every shared funding date**.
- Achievement-contract interest reduction: **10 percentage points after each paid funding date**.
- Achievement-contract final payment: **10%**, after which the contract expires.
- Kerbin satellite unlock: **any agency achieves Probe Orbit**.
- Mun satellite unlock: **any agency achieves Mun Probe Orbit**.
- Minmus satellite unlock: **any agency achieves Minmus Probe Orbit**.
- Satellite contracts: **permanent once unlocked; no declining interest or expiry**.
