# Work claim — Grid spatial ordering axis scale invariance

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:27:00+07:00`
- Baseline main SHA: `ed2448e545ffaf43422afe57bf02ba007cc2da64`
- Priority: evidence-driven remote-safe Core numeric robustness

## Reason

`GridSpatialOrderingPlanner.OrderParallelLines` uses the caller's ordering axis only as a direction, so multiplying a valid non-zero axis by a positive scalar must not change ordering. The current normalization computes `Hypot(axis.X, axis.Y)` as `scale * sqrt(...)`; for large but finite diagonal components such as `(double.MaxValue, double.MaxValue)`, that representable input overflows the intermediate norm to `Infinity` and is rejected even though it has exactly the same direction as `(1, 1)` and can be normalized safely by scaling first.

## Reserved scope

Make ordering-axis normalization overflow-safe and scale-invariant for finite non-zero `Point2` inputs. Preserve alignment tolerance, coordinate tolerance, curve enumeration bounds, line validation, projection math, ordering/tie semantics, descending behavior, and all BricsCAD command behavior.

## Expected surfaces

- `src/QS3D.Core/Geometry/GridSpatialOrderingPlanner.cs`
- `tests/QS3D.Core.SmokeTests/GridSpatialOrderingAxisScaleSmoke.cs`
- `tests/QS3D.Core.SmokeTests/GridSpatialOrderingAxisScaleRegistration.cs`
- this claim file

## Excluded scope

- No Grid naming, intersection, identity, radial/ARC policy, or V25 command changes.
- No change to line-direction normalization in this lane.
- No GitHub Actions dispatch.

## Validation plan

- Use two modest parallel Grid LINEs perpendicular to the `(1, 1)` ordering direction.
- Assert `(1, 1)` and `(double.MaxValue, double.MaxValue)` produce the same ordered element IDs and finite coordinates within tight tolerance.
- Assert zero and non-finite ordering axes remain rejected.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record static/exact-diff/ancestry verification only; do not claim an executed repository `dotnet` or BricsCAD V25 runtime PASS in this hosted session.

## Coordination

Recent claim/commit search found no active reservation for `GridSpatialOrderingPlanner` or ordering-axis overflow/scale invariance. Existing Grid claims/features cover other surfaces and policies.

## Completion condition

Current `main` normalizes every finite non-zero ordering axis without magnitude-only overflow, focused smoke coverage locks scale invariance, and this claim is `COMPLETED`.
