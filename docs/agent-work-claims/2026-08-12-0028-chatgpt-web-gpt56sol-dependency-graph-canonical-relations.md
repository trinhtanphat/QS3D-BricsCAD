# Agent work claim — DependencyGraph canonical relation enforcement

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12T00:28:00+07:00
- Status: `ACTIVE`
- Baseline main SHA: `665390681226ec48c2e71b267ee580bb51d16287`
- Scope: make `DependencyGraph` fail closed on malformed in-memory `ProjectElement.DependsOn` entries instead of silently trimming/skipping/deduplicating them.
- Evidence: current `Rebuild(...)` uses `DependsOn.Where(...).Trim()` and a `HashSet`, while `TopologicalDirtyOrder(...)` also trims each dependency. Recent revision-capture hardening now rejects blank, padded and case-insensitive duplicate dependencies, so graph-based mutation/regeneration paths should not accept a weaker relation contract.
- Files reserved:
  - `src/QS3D.Core/Services/DependencyGraph.cs`
  - `tests/QS3D.Core.SmokeTests/DependencyGraphCanonicalRelationSmoke.cs`
  - this claim file for close-out
- Plan:
  1. Validate every dependency entry encountered by graph rebuild/order as nonblank and already trim-canonical.
  2. Reject case-insensitive duplicate dependency IDs on the same semantic element rather than silently collapsing them.
  3. Preserve canonical dependency graph semantics, deterministic dependent ordering and cycle detection.
  4. Add CAD-independent smoke coverage for canonical success plus blank/padded/duplicate rejection in both rebuild and topological ordering, with no project mutation.
  5. Refresh current `main`, verify reachability/current source, then release the reservation.
- Non-overlap: no revision-capture implementation, no HostLink/Room mutation canonicalization, no DependencyImpactPlanner, no adapter/native CAD code and no active runtime/feature claim.
- Validation boundary: source/smoke contract review only; no GitHub Actions dispatch and no licensed BricsCAD V25/V26 runtime PASS.
