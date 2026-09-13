# The Race for Space - Coding Guidelines

These rules apply to all code changes in this repository, including changes made by AI assistants.

The goal is simple: keep the prototype easy to understand, easy to review, and easy to extend.

## 1. Read before changing code

Before editing production code:

1. Read this file.
2. Read [`docs/STRUCTURE.md`](docs/STRUCTURE.md).
3. Inspect the existing class or method that owns the behaviour.
4. Prefer extending the current design over creating a new architecture.
5. Keep the change focused on the requested problem.

Do not redesign nearby systems simply because another design is possible.

## 2. Branch rule

Do **not** create a new Git branch without explicit user permission.

Work on the existing requested branch unless the user approves a different branch first.

## 3. Use the current project vocabulary

Use one preferred name for each concept:

- **Campaign** — overall progression for one KSP save.
- **Agency** — player or rival organisation.
- **Objective** — a gameplay goal that can be completed.
- **Objective Funding Contract** — one-off funding tied to an objective.
- **Satellite Network Funding Contract** — recurring funding based on satellite presence.
- **Pre-Orbit Contract** — the current Directed Power, Mass, Control, and Biome Kerbin family.
- **Flight Contract** — generic active-vessel contract infrastructure.
- **Orbital Vessel Tracking** — slower loaded/unloaded vessel scanning.

Do not reintroduce retired terminology for these concepts.

`PreOrbit` should describe the current Kerbin contract family. Generic infrastructure that could support future Mun, Minmus, or other active-vessel contracts should use `FlightContract` terminology instead.

## 4. Keep module ownership clear

The current source structure is intentional:

| Module | Owns |
| --- | --- |
| `Core/` | Runtime scheduling and campaign settings. |
| `Campaign/` | Campaign coordination, offers, sponsor reviews, funding boundaries, and progression. |
| `Agencies/` | Player and rival agency state. |
| `Objectives/` | Objective definitions and unlock rules. |
| `Funding/` | Objective and satellite-network funding contracts. |
| `Rivals/` | Rival mission selection, spending, and progress. |
| `Tracking/` | KSP-independent flight-contract and orbital-vessel evaluation. |
| `Persistence/` | Project-owned save-state models. |
| `KspIntegration/` | Direct KSP/Unity API access, events, config loading, and ScenarioModule hooks. |
| `UI/` | Presentation only. |

Important boundaries:

- `KspIntegration` should own raw KSP objects and API calls where practical.
- `Tracking` should consume project-owned snapshots rather than raw `Vessel` or `ProtoVessel` objects.
- `CampaignController` coordinates campaign state but should not query KSP vessels directly.
- UI code displays state and must not advance campaign progression.

## 5. Ask before structural changes

Stop and ask for approval before making a structural change such as:

- creating a new source module or major folder;
- introducing a significant class hierarchy or framework;
- moving responsibilities between modules;
- replacing an existing subsystem;
- substantially changing a public API;
- changing save-data or configuration formats in a compatibility-affecting way.

When asking, explain briefly:

1. why the current structure cannot cleanly support the change;
2. what structural change is proposed;
3. which existing files or systems would be affected.

Small implementation details that fit the current design do not need separate approval.

## 6. Prefer straightforward methods

- Reuse or extend an existing method when it already owns the behaviour.
- Add a new method only when it represents a clear responsibility or meaningfully improves readability.
- Avoid chains of tiny wrapper methods.
- Avoid duplicate methods that do nearly the same thing.
- Prefer simple control flow over clever abstractions.
- Make side effects clear, especially for campaign state, funding, tracking, and persistence.

A longer clear method is better than many tiny methods that make the execution path harder to follow.

## 7. Naming rules

Use normal C# conventions:

- classes, structs, enums, properties, and methods: `PascalCase`;
- locals and parameters: `camelCase`;
- private fields: `_camelCase`;
- constants: follow the existing project style and use descriptive names.

Prefer names that explain purpose rather than type.

Good:

```csharp
int crewCount;
string celestialBodyName;
bool isLanded;
```

Avoid vague names such as `data`, `info`, `thing`, `temp`, `obj`, or unexplained abbreviations.

Include units where useful, for example `refreshIntervalSeconds` or `maximumAltitudeMeters`.

## 8. Comments and documentation

Comments should explain **why**, not restate the code.

Comment non-obvious behaviour such as:

- KSP API limitations or scene behaviour;
- loaded versus unloaded vessel handling;
- performance-sensitive refresh decisions;
- unusual objective or funding rules;
- save/load assumptions;
- workarounds for KSP or Unity behaviour.

Use XML documentation comments for public APIs when the contract is not obvious from the name.

Keep Markdown documentation simple, current, and consistent with the terminology in this file.

## 9. KSP and Unity safety

KSP objects can disappear or become invalid as scenes and vessels change.

- Validate external/KSP objects before use.
- Handle missing vessels, bodies, crew, config values, and save data deliberately.
- Do not use broad empty `catch` blocks.
- Do not silently substitute misleading defaults.
- Consider both loaded and unloaded vessels when the feature requires them.

## 10. Performance

Consider the performance cost of an algorithm **before implementing it**, especially when it can run from a recurring refresh, per-vessel scan, per-rival event, daily progress check, or timewarp catch-up path.

For any non-trivial loop or repeated algorithm, make a lightweight cost review first:

- identify how often the code can run in real time and game time;
- estimate the worst-case work as frequency × entities × elapsed/caught-up events × candidates/items;
- account for high timewarp, save/load catch-up, and large universal-time jumps that can make many stored events execute during one refresh;
- identify expensive inner-loop operations such as KSP/Unity API calls, catalogue scans, vessel scans, sorting, dictionary/list rebuilding, string work, persistence capture, and repeated allocations;
- prune impossible candidates before expensive resolution work;
- resolve or cache stable metadata outside inner loops when the lifetime and ownership are clear;
- if only one persisted/current item must be checked, do not rebuild, de-duplicate, or sort an entire candidate collection to find it;
- preserve chronological/gameplay correctness first; prefer making each repeated event cheap before batching events unless batching is proven behaviourally equivalent.

A small amount of work can become a severe hitch when multiplied by many rivals, vessels, candidates, or elapsed Kerbin days. Code that is cheap at normal speed must also be considered under the largest realistic campaign state and maximum timewarp/catch-up conditions.

General rules:

- Do not scan all vessels every frame.
- Use the existing controlled refresh cadences.
- Avoid repeated allocations in hot loops when a simple reusable buffer already fits the design.
- Do not optimise speculative bottlenecks at the cost of clarity, but do not defer obvious multiplicative costs until after implementation.
- Comment optimisations that make the code less obvious, including what repeated work they are intentionally avoiding.
- For performance-sensitive KSP-boundary changes, include a practical worst-case verification path where automated tests cannot measure the real API cost.

Correctness and clarity come first, but expected runtime cost is part of correctness for code that executes on the main KSP simulation thread.

## 11. Persistence and configuration

Save data and config require extra care.

- Keep persistence models explicit and simple.
- Store mutable project-owned state by stable IDs.
- Keep gameplay calculations out of persistence code.
- Treat save/config format changes as structural changes when compatibility may be affected.
- Use named constants for fixed domain values.
- Use configuration for values expected to be tuned by design.

## 12. Tests and verification

For KSP-independent logic, update or add meaningful automated tests.

Important areas include:

- objective evaluation;
- unlock rules;
- flight-contract tracking;
- orbital vessel classification and counts;
- funding calculations;
- rival mission selection and progress;
- persistence transformations;
- controller ordering and sponsor-review behaviour.

Run:

```bash
bash tools/run-logic-tests.sh
```

For code that depends directly on KSP APIs, provide a clear manual test path. Use [`docs/KERBAL_CONTRACTS_V0_5_TESTING.md`](docs/KERBAL_CONTRACTS_V0_5_TESTING.md) for the current in-game acceptance checks.

Do not add trivial tests only to increase test count.

## 13. Dependencies

Keep runtime dependencies minimal.

Prefer .NET, KSP, and Unity functionality already available to the mod. Discuss any new external runtime dependency before adding it.

## 14. Keep changes focused

A change should solve the requested problem without unrelated cleanup.

- Do not rename unrelated files or variables.
- Do not reformat whole files for a small change.
- Do not change working behaviour unless the task requires it.
- Record unrelated technical debt separately instead of silently expanding scope.

## 15. Final checklist

Before submitting a code change, confirm:

- [ ] I read `AGENTS.md` and `docs/STRUCTURE.md`.
- [ ] I stayed on the approved branch.
- [ ] I used the existing module that owns the behaviour.
- [ ] I did not add unnecessary classes, layers, or helpers.
- [ ] Names use the current project vocabulary.
- [ ] KSP-specific access remains inside `KspIntegration/` where practical.
- [ ] UI code remains presentation-only.
- [ ] I considered invalid KSP objects and loaded/unloaded vessel behaviour where relevant.
- [ ] I considered save/config compatibility where relevant.
- [ ] I estimated the worst-case performance load of new or changed algorithms, including timewarp/catch-up multiplication where relevant.
- [ ] I avoided unnecessary frequent work and expensive repeated inner-loop operations.
- [ ] I updated meaningful tests where practical.
- [ ] I ran `tools/run-logic-tests.sh` when the change affects testable logic.
- [ ] I did not make an unapproved structural change.

## Guiding principle

**Prefer clear, boring, maintainable code over clever code.**

The prototype should grow by extending known working behaviour in small, understandable steps.
