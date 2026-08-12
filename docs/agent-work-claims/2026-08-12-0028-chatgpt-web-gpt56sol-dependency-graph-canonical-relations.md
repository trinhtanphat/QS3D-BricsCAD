# Agent work claim — DependencyGraph canonical relation enforcement

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12T00:28:00+07:00
- Status: `DONE`
- Baseline main SHA: `665390681226ec48c2e71b267ee580bb51d16287`
- Scope: make `DependencyGraph` fail closed on malformed in-memory `ProjectElement.DependsOn` entries instead of silently trimming/skipping/deduplicating them.
- Evidence: `Rebuild(...)` previously used `DependsOn.Where(...).Trim()` and a `HashSet`, while `TopologicalDirtyOrder(...)` also trimmed each dependency. Revision-capture hardening already rejects blank, padded and case-insensitive duplicate dependencies, so graph-based mutation/regeneration paths now use the same canonical relation contract.
- Files reserved during work:
  - `src/QS3D.Core/Services/DependencyGraph.cs`
  - `tests/QS3D.Core.SmokeTests/DependencyGraphCanonicalRelationSmoke.cs`
  - this claim file for close-out
- Implemented:
  1. Added shared dependency validation requiring every entry to be nonblank and already trim-canonical.
  2. Case-insensitive duplicate dependency IDs on one semantic element are now rejected instead of collapsed.
  3. `Rebuild(...)` uses validated canonical dependency text directly; `TopologicalDirtyOrder(...)` validates before ordering and no longer silently normalizes malformed entries.
  4. Existing deterministic dependent ordering and cycle detection remain intact for canonical input.
  5. Added CAD-independent smoke coverage for canonical ordering, blank/padded/duplicate rejection in both graph paths, read-only behavior, and preservation of the previous graph when a later rebuild fails.
- Implementation commit: `6f5a820010a3aa25b4bafee3fe9ef87d6a166d7a` (`fix(dependency): reject malformed relation entries`).
- Regression commit: `ca3aef380b3a841d5867b6528f8799b31b4b5d68` (`test(dependency): guard canonical relation entries`).
- Integration verification: at verification time `main` pointed at regression commit `ca3aef3...`; the implementation and regression are sequential on current main and the reserved source was not overwritten between them.
- Non-overlap: no revision-capture implementation, HostLink/Room mutation canonicalization, DependencyImpactPlanner or adapter/native CAD code was modified.
- Validation boundary: source/smoke contract reviewed remotely. GitHub Actions were not dispatched; the smoke executable was not claimed as run in this session; no licensed BricsCAD V25/V26 runtime PASS is claimed.
- Reservation released: this claim is complete and no longer reserves the listed files.
