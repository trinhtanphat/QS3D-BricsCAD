# Work claim — Grid spatial ordering coordinate-delta overflow

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:38:00+07:00`
- Baseline main SHA: `882143b40e3469b88cb50550d3cf1771bc2a7dde`
- Priority: evidence-driven remote-safe Core numeric robustness

## Reason

`GridSpatialOrderingPlanner.OrderParallelLines` validates every projected ordering coordinate as finite, sorts them, then subtracts adjacent coordinates only to detect whether they lie within `coordinateTolerance`. For two valid finite coordinates near opposite ends of the double range, that subtraction can overflow to infinity. The current code treats the overflow as an error, even though an infinite-magnitude difference here proves the coordinates are far apart and therefore cannot be ambiguous within any finite positive tolerance.

## Reserved scope

Allow overflow of the adjacent-coordinate difference to mean "not ambiguous" while preserving the existing finite-delta tolerance test. Keep all coordinate projection finite checks, ordering, tie behavior, curve validation, bounds, axis/line normalization, identities, and descending semantics unchanged.

## Expected surfaces

- `src/QS3D.Core/Geometry/GridSpatialOrderingPlanner.cs`
- `tests/QS3D.Core.SmokeTests/GridSpatialOrderingCoordinateDeltaOverflowSmoke.cs`
- `tests/QS3D.Core.SmokeTests/GridSpatialOrderingCoordinateDeltaOverflowRegistration.cs`
- this claim file

## Excluded scope

- No Grid intersection, naming, ARC/radial policy, V25 command, projection, axis-normalization, or line-direction changes.
- No change to ambiguity behavior for representable finite coordinate differences.
- No GitHub Actions dispatch.

## Validation plan

- Use two vertical finite Grid LINEs at `x = -0.9 * double.MaxValue` and `x = +0.9 * double.MaxValue` with ordering axis `(1,0)`; each coordinate is finite but their subtraction overflows.
- Assert ordering succeeds and returns the negative-coordinate Grid before the positive-coordinate Grid.
- Assert two nearby coordinates still trigger the existing ambiguity failure within `coordinateTolerance`.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record static/exact-diff/ancestry verification only; do not claim an executed repository `dotnet` or BricsCAD V25 runtime PASS in this hosted session.

## Coordination

This is distinct from the completed ordering-axis and line-direction scale-invariance claims. Recent claim/commit search found no active reservation for adjacent ordering-coordinate delta overflow.

## Completion condition

Current `main` no longer rejects valid far-apart finite Grid coordinates solely because their difference overflows, focused smoke coverage locks both the overflow and nearby-ambiguity cases, and this claim is `COMPLETED`.
