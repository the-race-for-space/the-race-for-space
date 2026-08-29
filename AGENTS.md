# The Race for Space - Coding Guidelines

These rules apply to all code changes in this repository, including changes produced by AI coding assistants.

The goal is to keep the prototype easy to understand, easy to review, and easy to extend without unnecessary restructuring.

## 1. Read Before Coding

Before changing code:

1. Read this file.
2. Read `docs/STRUCTURE.md` and identify which existing module owns the change.
3. Inspect the relevant existing classes and functions before adding anything new.
4. Prefer extending the current implementation over introducing a new architecture.
5. Keep the requested change as small and focused as practical.

Do not start by redesigning the surrounding system simply because another design might also work.

## 2. Preserve the Existing Structure

The current project structure is intentional.

When implementing or changing behaviour:

- Use the existing modules, classes, and functions wherever they are suitable.
- Prefer amending an existing function over creating several new helper functions for small pieces of logic.
- Do not split a working function merely to make the code appear more abstract or modular.
- Do not introduce new layers, managers, services, factories, interfaces, base classes, or utility classes unless there is a demonstrated need.
- Do not move files between modules without approval.
- Do not rename public classes, methods, save-data fields, configuration keys, or established concepts without a clear reason and approval.

### Structural-change gate

If a requested change genuinely requires any of the following, stop and ask for approval before implementing it:

- a new source module or major folder;
- a significant new class hierarchy;
- a replacement architecture or design pattern;
- moving responsibilities between existing modules;
- substantially changing an existing public API;
- replacing an existing subsystem rather than extending it;
- a save-data or configuration format change that could affect compatibility.

When asking, explain briefly:

1. why the current structure cannot cleanly support the change;
2. what structural change is proposed;
3. what existing code would be affected.

Small implementation details that fit naturally inside the current design do not require approval.

## 3. Functions and Methods

Avoid unnecessary function proliferation.

- Reuse and extend existing functions when their responsibility already matches the required behaviour.
- Create a new function only when it represents a clear reusable responsibility or significantly improves readability.
- Avoid chains of tiny one- or two-line wrapper methods that add no meaningful abstraction.
- Avoid duplicate methods that perform nearly the same operation with slightly different names.
- Keep methods focused, but do not split them mechanically based on line count alone.
- Prefer straightforward control flow over clever abstractions.
- Make side effects clear, especially when modifying race state, funding, vessel tracking, or persistence data.

A longer but clear method is preferable to many fragmented methods that make the execution path difficult to follow.

## 4. Naming

Use clear, descriptive, and consistent names.

### C# conventions

- Classes, structs, enums, properties, methods, and public members: `PascalCase`.
- Local variables and parameters: `camelCase`.
- Private fields: `_camelCase`.
- Constants: use the established project convention consistently; prefer descriptive names over abbreviations.

### Naming rules

- Names should describe the purpose of the value, not just its type.
- Avoid vague names such as `data`, `info`, `thing`, `temp`, `obj`, `val`, or `manager` when a more specific name is available.
- Avoid unexplained abbreviations.
- Use terminology consistently with KSP and the design documents.
- A concept should have one preferred name throughout the project. For example, do not alternate between `competitor`, `rival`, and `opponent` for the same domain object unless they intentionally mean different things.

Prefer:

```csharp
int crewCount;
string celestialBodyName;
bool isLanded;
```

Instead of:

```csharp
int c;
string body;
bool flag;
```

## 5. Comments and Documentation

Code should be well commented, but comments must add useful information.

### Required comments

Add comments where they explain:

- why a piece of logic exists;
- KSP API behaviour that is not obvious;
- handling differences between loaded and unloaded vessels;
- assumptions or limitations in prototype logic;
- non-obvious milestone, competition, scoring, or funding rules;
- performance-sensitive decisions;
- save/load compatibility behaviour;
- workarounds for KSP or Unity behaviour.

Use XML documentation comments for public classes and public methods where their purpose or contract is not immediately obvious.

### Avoid comments that merely restate the code

Avoid:

```csharp
// Increment count
count++;
```

Prefer:

```csharp
// Count each vessel once even when multiple qualifying parts are present.
vesselCount++;
```

Comments should explain intent and reasoning rather than translate C# into English.

## 6. Keep Changes Focused

Each change should solve the requested problem without unrelated cleanup.

- Do not refactor unrelated code while implementing a feature or bug fix.
- Do not rename unrelated variables or files.
- Do not reformat entire files for a small change.
- Avoid changing working behaviour unless the task requires it.
- Prefer the smallest change that clearly and safely satisfies the requirement.

If unrelated technical debt is discovered, note it separately rather than silently expanding the scope.

## 7. Avoid Duplication, but Do Not Over-Abstract

Repeated logic should be reviewed before being copied.

- Reuse existing code when it already expresses the required behaviour.
- Consolidate meaningful duplication when doing so simplifies maintenance.
- Do not create an abstraction merely because two short pieces of code look similar.
- Wait until a reusable concept is clear before introducing a generic helper or framework.

For this prototype, understandable concrete code is preferred over premature abstraction.

## 8. KSP Integration Boundaries

Keep KSP-specific API access inside `KspIntegration/` wherever practical.

- KSP `Vessel`, `ProtoVessel`, game-event, scene, and Unity interactions should not spread unnecessarily through domain logic.
- Convert KSP state into project-owned data before milestone, competition, scoring, or funding logic consumes it where practical.
- Core gameplay rules should remain testable without needing a live KSP scene whenever possible.
- Do not assume a vessel is loaded. Prototype logic must explicitly consider unloaded/persistent vessels where relevant.

If breaking this boundary appears necessary, treat it as a structural concern and ask before proceeding.

## 9. Nulls, Invalid State, and Defensive Checks

KSP and Unity objects may disappear or become invalid as scenes and vessels change.

- Validate external/KSP data before using it.
- Handle missing vessels, bodies, crew, configuration values, and persistent data deliberately.
- Do not hide failures with broad empty `catch` blocks.
- Avoid silently substituting misleading default values.
- When skipping invalid data, log enough context to diagnose why it was skipped when appropriate.

## 10. Error Handling and Logging

Logging should help diagnose the prototype without flooding the KSP log.

- Use a consistent mod prefix in log messages.
- Log important lifecycle events, failures, and prototype diagnostics.
- Avoid logging every frame or every repeated calculation under normal operation.
- Debug-only verbose output should be easy to disable later.
- Exceptions should include useful context when they are caught and logged.

Do not use exceptions as normal control flow.

## 11. Performance

KSP can contain many vessels and persistent objects, so repeated scanning must be deliberate.

- Avoid expensive work every `Update()` frame unless genuinely required.
- Prefer event-driven updates or controlled refresh intervals where appropriate.
- Avoid repeatedly allocating collections or strings inside frequent loops when the same work can be reused safely.
- Do not optimise speculative bottlenecks at the expense of readability.
- If a performance optimisation makes code less obvious, comment why it is needed.

Correctness and clarity come first for the prototype; optimise measured or obviously frequent work.

## 12. Constants and Configuration

Avoid unexplained magic values.

- Use named constants for fixed values that have domain meaning.
- Use configuration for values expected to be tuned by gameplay design.
- Keep configuration keys stable once they are used in released save/config data.
- Include units in names when ambiguity is possible, for example `refreshIntervalSeconds`.

## 13. Persistence and Save Compatibility

Save data requires additional care.

- Keep persistence models simple and explicit.
- Do not rename or remove persisted fields casually.
- New fields should have safe behaviour when loading older saves.
- Persistence code should not contain gameplay calculations that belong in another module.
- Any potentially breaking save-format change requires approval before implementation.

## 14. Tests and Verification

Where logic can be separated from KSP, add or update tests for meaningful behaviour.

Priority areas include:

- vessel classification;
- body-presence aggregation;
- milestone evaluation;
- first-to-achieve competition rules;
- comparative coverage calculations;
- scoring;
- persistence transformations that can be tested independently.

Do not create trivial tests solely to increase test count.

For code that depends directly on KSP APIs and cannot reasonably be unit tested, provide a clear manual verification path or useful debug logging.

## 15. Dependencies

Keep dependencies minimal.

- Prefer the .NET/KSP/Unity functionality already available to the mod.
- Do not add a third-party library for a small task that can be implemented clearly with existing dependencies.
- Any new external runtime dependency must be discussed before it is added.

## 16. Readability Rules

Prefer code that can be understood quickly by someone returning to the project months later.

- Use braces consistently, including around simple conditionals where that improves safety and readability.
- Avoid deeply nested conditions when a straightforward early return makes the intent clearer.
- Keep related logic together.
- Keep formatting consistent with surrounding code.
- Avoid clever one-liners when a few clear lines express the behaviour better.
- Avoid boolean parameters whose meaning is unclear at the call site when a clearer existing representation is available.

## 17. AI Change Checklist

Before submitting any code change, the AI assistant must check:

- [ ] I read the relevant existing code before changing it.
- [ ] I used the existing project/module structure.
- [ ] I reused or amended existing functions where appropriate.
- [ ] Any new function has a clear reason to exist.
- [ ] I did not introduce unnecessary abstractions or classes.
- [ ] Variable, method, and class names are descriptive and consistent.
- [ ] Non-obvious logic and KSP-specific behaviour are appropriately commented.
- [ ] Comments explain intent rather than repeat the code.
- [ ] The change is limited to the requested scope.
- [ ] I considered loaded and unloaded vessel behaviour where relevant.
- [ ] I considered null/invalid KSP objects and save compatibility where relevant.
- [ ] I avoided unnecessary per-frame work or repeated expensive vessel scans.
- [ ] I reused existing constants/configuration patterns instead of adding magic values.
- [ ] I added or updated meaningful tests where practical.
- [ ] I did not add a dependency without approval.
- [ ] I did not make a structural change without explicit approval.

If the final item cannot be checked because a structural change is required, stop and ask before coding that change.

## 18. Guiding Principle

**Prefer clear, boring, maintainable code over clever code.**

The prototype should evolve by extending known working behaviour in small steps. Architecture should change only when the existing structure demonstrably prevents a clean implementation, and such changes must be discussed before they are made.
