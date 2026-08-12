# Work claim — Grid ARC membership large-finite roundoff

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-grid-arc-membership-roundoff-20260812-1201`
- Registered: `2026-08-12T12:01:00+07:00`
- Completed: `2026-08-12T12:04:00+07:00`
- Baseline main SHA: `0fa97a760133654d8bd7ab8d11a83f07d4d3c3c6`
- Claim SHA: `380ca03ec04c08d01cdac11e56bb83e145ce8368`
- Product SHA: `2d2248cc3cc00edd4a9bb117b689ee5410c28ab9`
- Regression SHA: `4ae556b9e88cff5670b2ce8ea5ec666ba3be793e`
- Priority: P1 — mathematically valid large-finite ARC intersections must survive unavoidable floating-point radial roundoff.

## Confirmed defect

After ARC/ARC arithmetic was normalized, `GridIntersectionPlanner.IsOnArc(...)` still rejected a point whenever `Abs(computedRadius - arc.Radius) > tolerance` using only the user/world absolute tolerance. At very large finite scales, a correctly constructed intersection can have ordinary IEEE-754 relative roundoff that is many orders of magnitude larger than an absolute `1e-8` while still being within a fraction of one ulp of the circle radius.

Concrete full-circle counterexample: `r1=1e200`, `r2=5e199`, center distance `7.5e199` at angle `0.1` rad. The normalized ARC/ARC solver constructs two finite support-circle intersections, while scale-safe radius recomputation differs by about `1.7e184` from `1e200` (roughly `1.7e-16` relative). The old absolute-only radial check rejected both and returned zero intersections.

## Reserved scope

- `src/QS3D.Core/Geometry/GridIntersectionPlanner.cs` — `IsOnArc(...)` radial membership tolerance only, plus one private numeric-precision constant.
- `tests/QS3D.Core.SmokeTests/GridIntersectionPlannerSmoke.cs` — focused large-finite radial-roundoff regression only.
- this claim file.

No smoke registration edit was required.

## Implemented contract

- Configured absolute tolerance remains the minimum radial tolerance.
- A relative floor of `3.5527136788005009e-15` (16 × binary64 machine epsilon) times the larger of computed/declared radius is used only when it exceeds the configured absolute tolerance.
- At ordinary Grid scales the existing absolute tolerance therefore remains unchanged; at extreme finite scales the check admits only the small representational error expected from several binary64 operations.
- Sweep-angle filtering, intersection construction, ordering, deduplication, public API and all other geometry methods remain unchanged.

## Regression

`LargeFiniteArcArcAllowsRadialRoundoff()` covers the concrete `1e200` / `5e199` circles at center distance `7.5e199` and direction `0.1` rad. It requires two deterministic finite intersections around `(8.222969996097536e199, 5.690585597570753e199)` and `(9.189602896267915e199, -3.943500806251261e199)`.

## Coordination

The ARC/ARC large-finite arithmetic lane was completed first. Exact commit search for `IsOnArc` surfaced no concurrent owner before this claim. Concurrent selection/regeneration work observed during implementation owns unrelated source paths.

## Validation

- Product diff readback confirms exactly one private precision constant plus the reserved radial membership tolerance changed.
- Regression diff readback confirms only the focused smoke registration call within the already-registered `GridIntersectionPlannerSmoke.Run()` and its test method were added.
- Independent numeric reproduction measured the prior radial discrepancy at about `1.7e184`, or roughly `1.7e-16` relative, comfortably below the 16-epsilon floor and far above the absolute `1e-8` that caused the false rejection.
- No GitHub Actions were dispatched and no BricsCAD/Windows runtime/build PASS is claimed.
