# Work claim — Grid spatial bounded enumeration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-grid-spatial-bounded-enumeration-20260811-2349`
- Registered: `2026-08-11T23:49:00+07:00`
- Baseline main SHA: `1fc02e8da3befb7fb00c9eae4f22c2f295ae9c8a`
- Priority: evidence-driven Core resource-bound hardening during owner-requested `continue all`

## Reserved scope

Make `GridSpatialOrderingPlanner.OrderParallelLines` enforce its existing `MaxCurves = 2000` contract during `IEnumerable<GridReferenceCurve>` enumeration rather than after unrestricted materialization.

## Expected surfaces

- `src/QS3D.Core/Geometry/GridSpatialOrderingPlanner.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`OrderParallelLines` currently calls `curves.ToList()` before checking `list.Count > MaxCurves`. A huge or non-terminating enumerable can therefore be consumed without limit despite the explicit 2,000-curve safety bound.

## Explicit exclusions

- No Grid axis/alignment/coordinate tolerance, ID, ordering, ambiguity, descending, native V25 numbering, UI, updater/licensing, Actions, release, or LOCAL_PASS semantics changes.

## Validation plan

- Preserve existing spatial ordering contract.
- Add a non-terminating curve enumerable that throws if item 2,002 is requested; verify oversize input is rejected after exactly 2,001 yielded curves.
- Re-fetch current source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

The 2,000-curve safety limit bounds enumeration/allocation as well as accepted cardinality, focused regression is on current `main`, and this claim is marked `COMPLETED` with exact implementation SHA(s) and validation performed.
