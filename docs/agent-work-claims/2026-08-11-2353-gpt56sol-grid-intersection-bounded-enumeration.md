# Work claim — Grid intersection bounded enumeration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-grid-intersection-bounded-enumeration-20260811-2353`
- Registered: `2026-08-11T23:53:00+07:00`
- Baseline main SHA: `8fd300f26707ab1a08c838e099764f662f37ee5d`
- Priority: evidence-driven Core resource-bound hardening during owner-requested `continue all`

## Reserved scope

Make `GridIntersectionPlanner.FindIntersections` enforce its existing `MaxCurves = 2000` contract while enumerating `IEnumerable<GridReferenceCurve>`, rather than after unrestricted materialization.

## Expected surfaces

- `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`FindIntersections` currently calls `curves.ToList()` and checks `list.Count > MaxCurves` only afterward. A huge or non-terminating enumerable can therefore be consumed without limit before the planner reaches its declared 2,000-curve safety bound; the downstream pairwise planning is additionally quadratic.

## Explicit exclusions

- No LINE/ARC validation, intersection mathematics, tolerance, duplicate-ID, intersection-count, ownership/identity, native V25 inspection, UI, Actions, release, or LOCAL_PASS semantics changes.

## Validation plan

- Preserve all existing Grid intersection behavior.
- Add a non-terminating valid-curve enumerable that throws if item 2,002 is requested; verify oversize input is rejected after exactly 2,001 yielded curves, before duplicate-ID validation or pairwise planning.
- Re-fetch current source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

The 2,000-curve safety limit bounds enumeration/allocation as well as accepted cardinality, focused regression is integrated on current `main`, and this claim is marked `COMPLETED` with exact implementation SHA(s) and validation performed.
