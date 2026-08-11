# Work claim — BQ modeless project identity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-bq-modeless-project-identity`
- Registered: `2026-08-12T00:37:17+07:00`
- Last Updated: `2026-08-12T00:37:17+07:00`
- Baseline main SHA: `987f3da5230636e192d7283d71198e3660a23c99`
- Priority: evidence-driven stale modeless mutation gap found during owner-requested continue-all audit
- Task Key: `BQ-MODELESS-PROJECT-IDENTITY`

## Confirmed defect

`QuantitySummaryWindow` is source-DWG-bound but does not retain the semantic `ProjectId` represented by the rows/window. `EnsureCurrentProject(...)` only proves that some project currently exists for the same DWG, while `PersistColumnPreferences()` then binds and mutates that current canonical project.

This preserves the intended same-project reload rebind, but it also allows an already-open BQ window to write its visible-column preference into a different replacement project loaded into the same DWG. That is inconsistent with the modeless exact-project freshness boundary already enforced for other mutating manager windows and with the documented requirement that modeless writes rebind canonical same-ProjectId state or fail closed.

## Reserved scope

Capture the BQ window's reviewed project identity without retaining a stale mutable `ProjectState`. Allow canonical rebind after reload only when the current project still has the same `ProjectId`; fail closed when the project is missing or replaced by another ID. Apply this guard before the preference metadata mutation and other BQ callbacks that claim current-project freshness.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs`
- `scripts/preflight-bq-modeless-project-safety.py`
- this claim file

## Coordination / exclusions

- Preserve same-`ProjectId` reload behavior introduced by the existing BQ reload-safe preference work; do not restore a retained mutable `ProjectState` reference.
- Do not modify BQ native Table placement/refresh, quantity arithmetic/report builders, ED2 implementation, unit-resolution work, or detail viewport-reveal behavior.
- Do not touch any ACTIVE claim scope, including current Floor/Grid/Semantic ownership lanes.
- No GitHub Actions/build/release dispatch and no BricsCAD V25 runtime qualification claim.

## Validation plan

- Constructor/window captures the reviewed existing `ProjectId` read-only.
- Same-DWG same-`ProjectId` reload remains accepted and preference persistence binds canonical current state.
- Same-DWG replacement with a different `ProjectId` is rejected before preference metadata/timestamp mutation.
- Project unload remains fail-closed and non-creating.
- Source-DWG active-document guard, rollback, recalculation, locate, export, and current BQ row-freshness contracts remain intact.
- Re-fetch `main` and re-check claim collision immediately before every source/test write batch; read back resulting source and commits. Do not claim local runtime execution unless actually run.

## Completion condition

An already-open BQ modeless window can rebind after a same-project reload but cannot mutate or operate as current against a different semantic project loaded into the same DWG, with focused static regression evidence committed.