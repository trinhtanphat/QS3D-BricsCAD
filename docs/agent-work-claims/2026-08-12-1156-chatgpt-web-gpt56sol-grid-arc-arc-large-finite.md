# Work claim — Grid ARC/ARC large finite intersection arithmetic

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-grid-arc-arc-large-finite-20260812-1156`
- Registered: `2026-08-12T11:56:00+07:00`
- Baseline main SHA: `1bfff2b62f61a6dc9bf66db7d133f1c62b1e73d2`
- Priority: P1 — finite ARC/ARC geometry must not overflow in squared circle arithmetic.

## Confirmed defect

`GridIntersectionPlanner.IntersectArcs(...)` computes `first.Radius * first.Radius`, `second.Radius * second.Radius`, `distance * distance`, and the derived height directly in world units. Two finite full circles with radii `1e200` and centers separated by `1e200` have two finite intersections, but those squared terms overflow to infinity and the current `a`/`h2` calculation becomes non-finite. The planner therefore throws instead of returning the representable intersections near `(5e199, ±8.660254e199)`.

The same method also forms `radius1 + radius2 + tolerance` before its separation check, so very large but finite radii can overflow that guard even when normalized separation is well-defined.

## Reserved scope

- `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs` — `IntersectArcs(...)` large-finite arithmetic only.
- `tests/QS3D.Core.SmokeTests/GridIntersectionPlannerSmoke.cs` — focused ARC/ARC large-finite regression only.
- this claim file.

`GridIntersectionPlannerSmoke.Run()` is already registered; no registration file edit is required.

## Intended contract

- Preserve coincident-support rejection, center-distance validation, sweep filtering, point ordering, deduplication, and normal finite outputs.
- Normalize circle radii/center distance by one finite common circle scale before separation and squared geometry arithmetic.
- Compute intersection basis in normalized units, then scale only the finite offsets back into world coordinates.
- Transform the existing `h2` tolerance consistently into normalized units rather than allowing radius-squared overflow.
- Add a regression for two full circles (`r=1e200`, center separation `1e200`) proving two finite deterministic intersections.

## Coordination

Recent Grid commit search shows only the completed LINE/ARC lanes from this agent and older unrelated radial-grid work. No current commit/claim surfaced for `GridIntersectionPlannerSmoke` or ARC/ARC large-finite arithmetic. The concurrent regeneration-profiler lane owns a different service path.

## Validation

Read back exact source/test diffs and close this claim with exact SHAs. Do not dispatch GitHub Actions and do not claim BricsCAD/Windows runtime/build PASS.
