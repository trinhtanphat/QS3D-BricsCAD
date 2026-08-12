# Work claim — Grid ARC membership large-finite roundoff

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-grid-arc-membership-roundoff-20260812-1201`
- Registered: `2026-08-12T12:01:00+07:00`
- Baseline main SHA: `0fa97a760133654d8bd7ab8d11a83f07d4d3c3c6`
- Priority: P1 — mathematically valid large-finite ARC intersections must survive unavoidable floating-point radial roundoff.

## Confirmed defect

After ARC/ARC arithmetic is normalized, `GridIntersectionPlanner.IsOnArc(...)` still rejects a point whenever `Abs(computedRadius - arc.Radius) > tolerance` using only the user/world absolute tolerance. At very large finite scales, a correctly constructed intersection can have ordinary IEEE-754 relative roundoff that is many orders of magnitude larger than an absolute `1e-8` while still being within a fraction of one ulp of the circle radius.

Concrete full-circle counterexample: `r1=1e200`, `r2=5e199`, center distance `7.5e199` at angle `0.1` rad. The normalized ARC/ARC solver constructs two finite support-circle intersections, but scale-safe radius recomputation differs by about `1.7e184` from `1e200` (roughly `1.7e-16` relative). The current absolute-only radial check rejects both and returns zero intersections.

## Reserved scope

- `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs` — `IsOnArc(...)` radial membership tolerance only, plus one private numeric-precision constant if needed.
- `tests/QS3D.Core.SmokeTests/GridIntersectionPlannerSmoke.cs` — focused large-finite radial-roundoff regression only.
- this claim file.

No smoke registration edit is required.

## Intended contract

- Preserve the configured absolute tolerance as the minimum radial tolerance.
- Add only a small IEEE-754 machine-precision floor proportional to the circle radius/computed radius, so normal-scale behavior remains unchanged while unavoidable representational error at huge scale is accepted.
- Do not change sweep-angle filtering, intersection construction, ordering, deduplication, or public API.
- Regression must prove the concrete large-finite full-circle pair returns two finite intersections instead of zero.

## Coordination

The ARC/ARC large-finite arithmetic lane is completed. Exact commit search for `IsOnArc` surfaced no concurrent owner. Current concurrent selection/regeneration work owns unrelated source paths.

## Validation

Read back exact source/test diffs and close with exact SHAs. Do not dispatch GitHub Actions and do not claim BricsCAD/Windows runtime/build PASS.
