# Agent work claim — SelectionState input bound

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12 (UTC+7)
- Status: `ACTIVE`
- Scope: bound live semantic selection replacement so arbitrary/lazy input cannot enumerate indefinitely or allocate without limit before `SelectionState` changes.
- Evidence: `SelectionState.Replace(IEnumerable<string>)` currently builds a `HashSet` directly from a LINQ pipeline with no enumeration ceiling. The persisted Project Browser workspace already caps selected semantic element IDs at 10,000, so the live selection state should enforce the same safety envelope instead of accepting an unbounded enumerable.
- Files reserved:
  - `src/QS3D.Core/Services/SelectionState.cs`
  - `tests/QS3D.Core.SmokeTests/SelectionStateInputBoundSmoke.cs`
  - this claim file for close-out
- Plan:
  1. Materialize selection input explicitly with a 10,000 raw-entry ceiling, stopping at max+1 even for lazy enumerable input.
  2. Preserve the established semantics for allowed input: blank IDs ignored, surrounding whitespace trimmed, case-insensitive dedupe, deterministic snapshot ordering and no `Changed` event for canonical-equivalent state.
  3. Build the full candidate set before mutating `_ids`, so oversize input leaves selection/event state unchanged.
  4. Add CAD-independent smoke coverage for the boundary, max+1 failure, bounded lazy enumeration and no partial mutation/event.
  5. Refresh `main`, verify reachability/current source and release the claim.
- Non-overlap: no Project Browser planner/workspace persistence source, no WPF/native selection bridge, no PICKFIRST/editor code and no V25 runtime behavior.
- Validation: source/smoke contract review only; no GitHub Actions dispatch and no licensed BricsCAD V25 runtime PASS.
