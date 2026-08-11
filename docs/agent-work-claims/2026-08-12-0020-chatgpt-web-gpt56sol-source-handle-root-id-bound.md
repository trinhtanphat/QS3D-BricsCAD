# Agent work claim — SourceHandleResolver root element-ID input bound

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12T00:20:00+07:00
- Status: `ACTIVE`
- Baseline main SHA: `fc8926b7190c163a24b435d366b9376daab15c52`
- Scope: bound root semantic element-ID input to `SourceHandleResolver.Resolve(ProjectState, IEnumerable<string>)` so lazy/foreign enumerables cannot run indefinitely before deterministic dependency traversal begins.
- Evidence: current `Resolve(...)` iterates `elementIds.Where(x => !string.IsNullOrWhiteSpace(x))` with no raw-input ceiling. `SelectionState` and persisted Project Browser selected IDs already use a 10,000-entry envelope, but this public Core locate resolver does not enforce it itself.
- Files reserved:
  - `src/QS3D.Core/Services/SourceHandleResolver.cs`
  - `tests/QS3D.Core.SmokeTests/SourceHandleResolverRootInputBoundSmoke.cs`
  - this claim file for close-out
- Plan:
  1. Materialize/canonicalize root element IDs with a 10,000 raw-entry limit, known-count fast-fail and lazy max+1 termination.
  2. Preserve allowed-input behavior: blanks ignored, surrounding whitespace trimmed, case-insensitive duplicate roots naturally coalesce through the existing visited set.
  3. Preserve dependency traversal order, source/boundary/generated fallback priority and fail-closed project identity checks.
  4. Add CAD-independent smoke coverage for exact-bound success, known and lazy oversize failure, normalization/dedupe, bounded enumeration and no project mutation.
  5. Refresh current `main`, verify reachability/current source and release the claim.
- Non-overlap: no `SemanticHandleOwnershipResolver`, no native `CadHandleService`, no WPF/PICKFIRST/editor code, no generated-handle policy mutation and no concurrent active runtime/feature lane.
- Validation boundary: source/smoke contract review only; no GitHub Actions dispatch and no licensed BricsCAD V25/V26 runtime PASS.
