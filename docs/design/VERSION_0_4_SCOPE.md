# Version 0.4 Alpha Scope

Version 0.4 starts from the working Funding Targets 0.3 prototype and treats the late Duna additions on that branch as part of the new baseline.

The first 0.4 branch is **Alpha/Cleanup-0.4**. Its purpose is maintenance and consolidation before larger structural or gameplay expansion.

## 0.4 Baseline

The 0.4 alpha baseline currently contains:

- Probe Orbit and Crewed Orbit achievements around Kerbin.
- Probe Orbit and Crewed Orbit achievements around Mun, Minmus, and Duna.
- Satellite-network funding around Kerbin, Mun, Minmus, and Duna.
- Player plus Aster and Cobalt space-program state.
- Shared 90 Kerbin-day funding dates.
- Declining-interest achievement funding.
- Lightweight rival mission simulation.
- Persistent race, achievement, funding, satellite, and rival mission state.
- The four-view Command Center interface: Overview, Funding Targets, Rival Agencies, and Space Race.

## Alpha Cleanup Goals

The initial 0.4 cleanup pass is intentionally narrow:

1. Align version numbers and documentation with the code that actually exists.
2. Keep rival cost and ETA queries free of hidden mission-state mutation; legacy target migration remains part of simulation refresh/load behaviour.
3. Add small defensive checks where invalid state can otherwise cause avoidable failures.

This pass does **not** change save formats, move module responsibilities, replace the runtime architecture, add rival agencies, or introduce a general configuration/rule framework.

## Compatibility

Version 0.4 continues to read the existing 0.3 persistence format. Compatibility paths for legacy rival mission names and older fixed save fields remain in place during this cleanup pass.

## Next Decisions

After the cleanup baseline is stable, larger 0.4 work can be selected separately. Candidate work includes moving race progression ownership out of the UI lifecycle, improving the KSP tracking boundary, making rival persistence collection-driven, and centralising target definitions. Those changes are intentionally outside this cleanup scope because they cross the structural-change gate in `AGENTS.md`.
