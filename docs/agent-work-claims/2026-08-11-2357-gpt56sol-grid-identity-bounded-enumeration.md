# Work claim — Grid intersection identity bounded enumeration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-grid-identity-bounded-enumeration-20260811-2357`
- Registered: `2026-08-11T23:57:00+07:00`
- Baseline main SHA: `f3dc5be32f3bd86d1e8e617c788f50a59af24896`
- Priority: evidence-driven Core resource-bound hardening during owner-requested `continue all`

## Reserved scope

Make `GridIntersectionIdentityPlanner.Assign` enforce its existing `MaxIntersections = 100000` contract while enumerating `IEnumerable<GridIntersection>`, rather than after unrestricted materialization.

## Expected surfaces

- `src/QS3D.Core/Geometry/GridIntersectionIdentityPlanner.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`Assign` currently calls `intersections.ToList()` and only then checks `input.Count > MaxIntersections`. A huge or non-terminating enumerable can therefore be consumed without limit before the declared 100,000-intersection identity bound is reached.

## Explicit exclusions

- No pair-key canonicalization, SHA-256 pair/owner token, occurrence ordering, per-pair cardinality, point tolerance, native marker ownership, UI, Actions, release, or LOCAL_PASS semantics changes.

## Validation plan

- Preserve all existing identity behavior.
- Add a non-terminating intersection enumerable that throws if item 100,002 is requested; verify oversize input is rejected after exactly 100,001 yielded intersections, before pair/group processing.
- Re-fetch current source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

The 100,000-intersection limit bounds enumeration/allocation as well as accepted cardinality, focused regression is integrated on current `main`, and this claim is marked `COMPLETED` with exact implementation SHA(s) and validation performed.
