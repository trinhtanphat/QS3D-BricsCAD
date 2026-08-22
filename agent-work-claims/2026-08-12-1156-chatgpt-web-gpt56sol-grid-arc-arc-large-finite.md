# Work claim — Grid ARC/ARC large finite intersection arithmetic

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-grid-arc-arc-large-finite-20260812-1156`
- Registered: `2026-08-12T11:56:00+07:00`
- Completed: `2026-08-12T11:59:00+07:00`
- Baseline main SHA: `1bfff2b62f61a6dc9bf66db7d133f1c62b1e73d2`
- Claim SHA: `af4d93e3777bae0b5e6957b82eb9cc6d9b72b8c5`
- Product SHA: `f313bd87569c079f93d5aa1b37dd87fd16e80941`
- Regression SHA: `7b4a379da15c8c0bed60536bc0ccca7334eb4712`
- Priority: P1 — finite ARC/ARC geometry must not overflow in squared circle arithmetic.

## Confirmed defect

`GridIntersectionPlanner.IntersectArcs(...)` computed `first.Radius * first.Radius`, `second.Radius * second.Radius`, `distance * distance`, and the derived height directly in world units. Two finite full circles with radii `1e200` and centers separated by `1e200` have two finite intersections, but those squared terms overflow to infinity and the old `a`/`h2` calculation became non-finite. The planner therefore threw instead of returning the representable intersections near `(5e199, ±8.660254e199)`.

The same method also formed `radius1 + radius2 + tolerance` before its separation check, so very large but finite radii could overflow that guard even when normalized separation was well-defined.

## Reserved scope

- `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs` — `IntersectArcs(...)` large-finite arithmetic only.
- `tests/QS3D.Core.SmokeTests/GridIntersectionPlannerSmoke.cs` — focused ARC/ARC large-finite regression only.
- this claim file.

`GridIntersectionPlannerSmoke.Run()` was already registered; no registration file edit was required.

## Implemented contract

- Coincident-support rejection and near-coincident center behavior remain unchanged.
- ARC radii, center distance and tolerance are normalized by one finite common circle scale before separation checks or squared circle arithmetic.
- The circle intersection basis (`a`, `h²`, `h`) is solved in normalized units; the prior `h²` tolerance is algebraically transformed by the same scale.
- World-space points are reconstructed from finite normalized offsets and then pass through the existing finite, sweep and radial membership checks.
- Tangent-vs-two-point branching now compares normalized `h` with normalized tolerance, preserving the world-space threshold without scaling overflow.
- Deterministic point sorting, deduplication and ordinary small-circle behavior are preserved.

## Regression

`LargeFiniteArcArcProducesFinitePoints()` uses two full circles with radius `1e200` and center separation `1e200`. The normalized geometry is `r1=r2=d=1`, giving `a=0.5`, `h²=0.75`, and finite deterministic points `(5e199, ±8.660254037844386e199)`.

## Coordination

Recent Grid commit search before reservation showed only the completed LINE/ARC lanes from this agent and older unrelated radial-grid work. No current commit/claim surfaced for `GridIntersectionPlannerSmoke` or ARC/ARC large-finite arithmetic. Concurrent work observed during implementation remained on unrelated regeneration/documentation/dependency paths.

## Validation

- Product commit readback confirms the only source diff is inside `IntersectArcs(...)`.
- Later `main` source readback confirms the normalized ARC/ARC implementation remains present.
- Later `main` test readback confirms the focused large-finite regression remains present in the already-registered smoke.
- Numeric rereview confirms the regression's normalized solution is finite and yields the expected two points; the existing overflow-fail-closed LINE regression remains untouched.
- No GitHub Actions were dispatched and no BricsCAD/Windows runtime/build PASS is claimed.
