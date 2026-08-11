# Agent work claim — Semantic handle selection input bound

- Agent: ChatGPT Web / GPT-5.6 Sol
- Started: 2026-08-12T00:16:00+07:00
- Status: `ACTIVE`
- Baseline main SHA: `1a46aca8d70db0730c6ae23ae2449212fd6d063f`
- Scope: bound `SemanticHandleOwnershipResolver.Resolve(ProjectState, IEnumerable<string>)` selection input so arbitrary/lazy CAD-handle enumerables cannot enumerate indefinitely or allocate without limit before ownership resolution.
- Evidence: current `Resolve(...)` constructs a `HashSet<string>` directly from `selectedHandles.Where(...).Select(...)` with no enumeration ceiling. This is inconsistent with the 10,000-entry live semantic selection/workspace safety envelope already used by the project.
- Files reserved:
  - `src/QS3D.Core/Services/SemanticHandleOwnershipResolver.cs`
  - `tests/QS3D.Core.SmokeTests/SemanticHandleOwnershipSelectionBoundSmoke.cs`
  - this claim file for close-out
- Plan:
  1. Enforce a 10,000 raw-entry ceiling, with fast-fail for known generic collection counts and max+1 termination for arbitrary/lazy enumerables.
  2. Preserve existing input semantics inside the bound: blank handles ignored, surrounding whitespace trimmed, case-insensitive dedupe.
  3. Fully materialize the bounded selection set before scanning project ownership; failure must be read-only and must not alter `ProjectState.ChangeVersion`.
  4. Add CAD-independent smoke coverage for exact-bound success, known 10,001 failure, bounded lazy enumeration, normalization/dedupe, and no project mutation.
  5. Refresh current `main`, verify implementation/regression reachability/current source, then release this reservation.
- Non-overlap: no adapter/editor/PICKFIRST code, no Source Reconcile command implementation, no generated-handle policy mutation, no native CAD mutation, and no currently ACTIVE beam-stirrup/BOM/interchange/opening-boolean/license/V26/support-bundle lane.
- Validation boundary: source/smoke contract review only; no GitHub Actions dispatch and no licensed BricsCAD V25/V26 runtime PASS.
