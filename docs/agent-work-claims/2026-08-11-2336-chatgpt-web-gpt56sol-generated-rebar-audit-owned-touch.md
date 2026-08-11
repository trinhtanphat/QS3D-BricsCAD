# Work claim — Generated rebar audit-owned Touch

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:36:00+07:00`
- Baseline main SHA: `09e3749a856b8d246f46f42e121289df5f3ecb8f`
- Priority: evidence-driven remote-safe native lifecycle correctness

## Reason

Eight generated-rebar builders share the same redundant revision pattern. Their successful per-element `CommitSemanticUpdate(...)` paths already record a dedicated mutation through `AuditTrail.ForProject(project).Record(...)`, and `AuditTrail.Record(...)` owns `ProjectState.Touch()`. After those audited updates, each batch currently performs an additional `if (pending.Count > 0) project.Touch();` before committing the CAD transaction. A successful N-element batch therefore advances `ChangeVersion` N+1 times instead of exactly once per audited semantic mutation, creating avoidable freshness churn for reviewed plans/UI/state consumers.

## Reserved scope

Remove only the redundant batch-level explicit Touch from these existing native generated-rebar mutation paths while preserving per-element audit records and all geometry/ownership/transaction behavior:

- `BeamRebarSolidBuilder`
- `BeamStirrupSolidBuilder`
- `ColumnRebarSolidBuilder`
- `ColumnTieSolidBuilder`
- `ShapeRebarSolidBuilder`
- `SlabMeshSolidBuilder`
- `FoundationMeshSolidBuilder`
- `StructuralWallMeshSolidBuilder`

Add one auto-discovered static preflight that guards the shared audit-owned revision invariant across all eight builders.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/BeamStirrupSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/ColumnRebarSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/ColumnTieSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/ShapeRebarSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/SlabMeshSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs`
- `src/QS3D.BricsCAD.V25/Cad/StructuralWallMeshSolidBuilder.cs`
- `scripts/preflight-generated-rebar-audit-owned-touch.py`
- this claim file

## Excluded scope

- No changes to rebar planners, spacing/count/cover math, BBS calculations, shape paths, fabrication standards, Solid3d placement/boolean geometry, generated ownership/XData, source selection, UI, or commands.
- No changes to `AuditTrail` semantics.
- No overlap with the active Core `RebarShapePath` point-aliasing claim; that lane remains Core-only and explicitly excludes CAD Solid3d generation.
- No GitHub Actions dispatch or release workflow.
- No claim of licensed BricsCAD V25 runtime qualification or structural/code compliance.

## Validation plan

- Re-fetch current `main` and all eight exact target blob SHAs before implementation; never force-push.
- For each builder, preserve `CommitSemanticUpdate(...)` and its dedicated `AuditTrail.ForProject(project).Record(...)` action while removing only the explicit batch-level `project.Touch()`.
- Preserve `ProjectStateSnapshot` rollback, document lock/native transaction, generated-owner validation/erase, metadata updates, and transaction commit ordering.
- Add a static preflight that enumerates exactly these eight builders, requires the expected audit action and audited semantic-update loop before CAD commit, and rejects any explicit `project.Touch()` in the `BuildSelected(...)` lifecycle.
- Record source/static verification only; exact native behavior remains LOCAL_ONLY.

## Coordination

Recent active work includes Core RebarShapePath aliasing and unrelated quantity/table/docs lanes. No current claim or recent commit was found reserving generated-rebar audit-owned revision semantics for these eight native builders.

## Completion condition

All eight generated-rebar batch builders advance project revision only through their existing per-element audit records, retain their native/rollback contracts, include a shared static regression gate, are merged to `main`, and this claim is marked `COMPLETED`.