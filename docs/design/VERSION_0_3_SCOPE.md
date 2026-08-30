# Version 0.3 Scope

Version 0.3 expands the current prototype by introducing campaign progression, a second style of competitive funding target, and a more useful information-focused Space Race view.

The goal is to prove these systems with a narrow implementation before adding more celestial bodies, more rival behaviours, or a large number of new target types.

## 0.3 Goal List

1. **Funding Target Unlocking**
   - Funding targets can be locked or available.
   - Only the Kerbin satellite funding target is available at the beginning of the prototype campaign.
   - Each locked target has a clear unlock condition.
   - The interface shows the player what is required to unlock each locked target.
   - Unlock conditions are evaluated as the game progresses.
   - Once unlocked, a target remains available unless a later design explicitly introduces another lifecycle rule.
   - Locked targets do not contribute funding or projected payouts.
   - Locked targets are not valid rival launch objectives until they become available.
   - Persist unlock state when it cannot be reconstructed safely from the current game state.

2. **Competitive Achievement Funding Targets**
   - Introduce a second funding-target style based on achieving a specific objective rather than maintaining a number of satellites.
   - Prototype this system with one example objective, such as being the first agency to achieve orbit around Kerbin.
   - All agencies may eventually achieve the objective.
   - The first agency to achieve it receives the strongest financial benefit.
   - Later agencies can still receive funding, but at a reduced value as sponsor/public interest declines.
   - The target has an interest or reward value that can decrease over time and/or as agencies complete the objective.
   - Track which agencies have achieved the objective and the order in which they achieved it where required by the funding calculation.
   - Display the objective, current reward or interest, achievement state, and relevant first-achievement information to the player.
   - Persist achievement state where it cannot be reconstructed safely.

3. **Space Race Information Centre**
   - Change the Space Race tab from its current milestone-progress role into the main information and guidance view for the mod.
   - Describe each funding target and its objective.
   - Show whether each target is locked or available.
   - Show the unlock requirement for locked targets.
   - Explain how each target's funding works, including any special competitive-achievement rules.
   - Include a concise player guide covering the important Race for Space systems.
   - Explain what counts as a qualifying satellite.
   - Explain scheduled funding and projected payouts.
   - Explain funding sharing for satellite-network targets.
   - Explain target unlocking.
   - Explain competitive achievement targets and declining interest.
   - Explain the purpose of the main Command Center tabs.
   - Where practical, derive displayed target information from the same target definitions used by gameplay logic so the UI and rules do not drift apart.

4. **Persistence and UI Support**
   - Save new unlock and achievement state where required.
   - Keep older saves valid by supplying safe defaults for new 0.3 state.
   - Clearly distinguish locked, available, achieved, and other required target states in the interface.
   - Ensure projected payouts include only funding opportunities that are currently valid.
   - Ensure rival behaviour respects target availability.

5. **Keep Version 0.3 Narrow**
   - Do not introduce a major rival-AI redesign in this version.
   - Do not expand the prototype to a large number of additional celestial bodies in this version.
   - Introduce only one prototype competitive-achievement target initially.
   - Use the existing project/module structure wherever practical.
   - Avoid a general-purpose target framework unless the existing structure demonstrably cannot support these requirements cleanly.
   - Use 0.3 to test whether unlocking progression, declining-interest objectives, and the redesigned information view are understandable and enjoyable before expanding them further.

## Design Decisions Still To Define

The following details are intentionally left open for further discussion before implementation:

- the exact unlock conditions for Mun and Minmus funding targets;
- the exact rules for the first competitive achievement target;
- how quickly competitive-target interest declines;
- how payouts are calculated for first and later achievers;
- how rival agencies satisfy achievement-style objectives;
- the exact layout and wording of the redesigned Space Race information view.
