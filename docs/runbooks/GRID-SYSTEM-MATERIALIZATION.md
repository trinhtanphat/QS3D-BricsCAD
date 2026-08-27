# Grid system native materialization

Status: SOURCE_READY / PENDING_LOCAL runtime qualification  
Lane: #3991 / `issue-3991`

## Purpose

QS3D uses `GridSystemPlanner` as the single Core authority for rectangular/radial Grid geometry. The V25/V26 adapter materializes each planned `GridReferenceCurve` as exactly one native LINE/ARC source and captures that source through the existing Grid semantic ownership path. This lane does not own pair-intersection markers, numbering, dimensions, or a second Grid catalog.

## Reviewed creation routes

`QS3DGRIDSYSTEMRECT` collects a WCS origin, a positive U-axis direction, bounded U/V counts, positive spacings and a semantic-ID prefix. The V axis is the positive 90-degree perpendicular. Inputs are converted from resolved drawing units to meters, planned by `GridSystemPlanner.PlanRectangular`, summarized, and require explicit `Yes` before mutation. Cancel/Enter at the final review is a no-op.

`QS3DGRIDSYSTEMRADIAL` collects a WCS center, bounded ray/ring counts, positive ring spacing and semantic-ID prefix. Rays are evenly distributed over one revolution and rings use positive multiples of the reviewed spacing. The command calls `GridSystemPlanner.PlanRadial`, summarizes the plan, and requires explicit `Yes` before mutation.

Both routes reject unresolved drawing units and reuse canonical planner validation. Generated semantic IDs are deterministic from the reviewed prefix and ordinal (`-U-###`, `-V-###`, `-RAY-###`, `-RING-###`). Existing semantic IDs fail closed instead of silently duplicating a Grid.

## Transaction and rollback boundary

`GridSystemNativeMaterializer` validates the complete plan before mutation and snapshots semantic project state. It creates all native sources in one CAD transaction and commits that batch before canonical semantic capture begins; this avoids invoking capture/regeneration inside an uncommitted outer CAD transaction.

Each planned curve maps 1:1 to one native source and one planner-owned semantic Grid ID. Canonical `SemanticCaptureService.CaptureSnapshot` remains responsible for family/default/metric/regeneration behavior. If any capture fails, cleanup erases every source created by this operation and restores the full semantic snapshot. Cleanup/rollback failures are aggregated and surfaced fail-closed rather than hidden.

## Deterministic source validation

`scripts/preflight-grid-system-materialization.py` guards the 2,000-curve plan bound, case-insensitive semantic-ID uniqueness, LINE/ARC validation, existing-ID rejection, native-batch/capture ordering, native cleanup, semantic rollback, explicit reviewed commands, canonical planner/materializer reuse, and V26 shared-source inclusion. V26 links the V25 adapter source tree; do not fork a V26 materializer.

## LOCAL_ONLY qualification

Hosted CI and source review do not prove native BricsCAD runtime behavior. After this source carrier lands, qualify the exact integrated SHA on licensed V25 and V26 using disposable drawings with resolved units:

1. Run each creation command with final review cancelled and verify no native/semantic mutation.
2. Create one rectangular system and one radial system; verify planned cardinality equals live native LINE/ARC source count and each semantic Grid has exactly one authoritative source Handle.
3. Repeat the same semantic prefix and verify fail-closed duplicate rejection with no additional native/semantic state.
4. Exercise native Undo/Redo around a successful batch and verify semantic/native ownership remains coherent.
5. Save, close, reopen and verify Grid semantic IDs/source Handles remain resolvable and downstream numbering/annotation/intersection planning can consume the sources.
6. Repeat in two open DWGs and verify project/document affinity with no cross-document mutation.
7. Record exact QS3D SHA, BricsCAD host major/build, command results and sanitized counts only. Do not commit proprietary DLLs, private DWGs, raw machine paths, ProjectIds or customer data.

Until those cells are executed locally, runtime status is `PENDING_LOCAL`; never promote hosted CI or managed-reference compilation to `LOCAL_PASS`.
