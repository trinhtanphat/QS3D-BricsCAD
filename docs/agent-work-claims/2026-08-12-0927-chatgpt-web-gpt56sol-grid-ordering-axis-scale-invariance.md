# Work claim — Grid spatial ordering axis scale invariance

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:27:00+07:00`
- Completed: `2026-08-12T09:30:00+07:00`
- Baseline main SHA: `ed2448e545ffaf43422afe57bf02ba007cc2da64`
- Claim commit: `8ae8f5f2ca2c64482c3416284636c4d10b0edfe4`
- Fix commit: `72e203fd6a771b043901c764e3b8c025b43113bd`
- Smoke commit: `7c8bf08b49ba611d097cd2aa9ce6f86424a8d347`
- Registration commit: `eb56a4c5add6c2707ef5d3ff2fcfafd0f3515d15`
- Priority: evidence-driven remote-safe Core numeric robustness

## Reason

`GridSpatialOrderingPlanner.OrderParallelLines` uses the caller's ordering axis only as a direction, so multiplying a valid non-zero axis by a positive scalar must not change ordering. The previous normalization computed `Hypot(axis.X, axis.Y)` as `scale * sqrt(...)`; for large but finite diagonal components such as `(double.MaxValue, double.MaxValue)`, that representable input overflowed the intermediate norm to `Infinity` and was rejected even though it has exactly the same direction as `(1, 1)` and can be normalized safely by scaling first.

## Implemented

Ordering-axis normalization now validates finite components, divides both components by their finite maximum absolute magnitude, computes the normalized length only in the safe `[1, sqrt(2)]` range, and then derives the unit vector. Zero and non-finite axes keep the existing `orderingAxis` argument failure. Alignment tolerance, coordinate tolerance, curve enumeration bounds, line validation, projection math, ordering/tie semantics, descending behavior, and BricsCAD command surfaces are unchanged.

Focused CAD-independent smoke coverage compares two parallel Grid LINEs under `(1, 1)` and `(double.MaxValue, double.MaxValue)`, asserting identical element ordering and finite/equal projected coordinates. It also verifies zero, `NaN`, and infinity axes remain rejected. A dedicated module-initializer registration invokes the smoke without modifying shared registration surfaces.

## Reserved scope

- `src/QS3D.Core/Geometry/GridSpatialOrderingPlanner.cs`
- `tests/QS3D.Core.SmokeTests/GridSpatialOrderingAxisScaleSmoke.cs`
- `tests/QS3D.Core.SmokeTests/GridSpatialOrderingAxisScaleRegistration.cs`
- this claim file

## Excluded scope

- No Grid naming, intersection, identity, radial/ARC policy, or V25 command changes.
- No change to line-direction normalization in this lane.
- No GitHub Actions dispatch.

## Validation

- Exact product diff: 9 additions / 4 deletions in `GridSpatialOrderingPlanner.cs`, confined to ordering-axis normalization.
- Exact smoke diff: one focused 70-line smoke source.
- Exact registration diff: one dedicated 13-line module-initializer source.
- A concurrent registration write initially returned HTTP 409; no overwrite/force was attempted. After refreshing, `7c8bf08b49ba611d097cd2aa9ce6f86424a8d347` was verified as an ancestor of current `main`, and the registration was safely retried.
- Observed current `main` `bb786801c32ff64e8e89fec09c32ee1376e8e640` has `eb56a4c5add6c2707ef5d3ff2fcfafd0f3515d15` as its direct parent, confirming the registration/fix/smoke chain remains in ancestry.
- Static/exact-diff/ancestry verification only. No repository `dotnet` or licensed BricsCAD V25 runtime PASS is claimed from this hosted session.

## Coordination

Recent claim/commit search found no active reservation for `GridSpatialOrderingPlanner` or ordering-axis overflow/scale invariance. Existing Grid claims/features cover other surfaces and policies.

## Completion condition

Satisfied: current `main` normalizes every finite non-zero ordering axis without magnitude-only overflow, focused smoke coverage locks scale invariance, and this claim is `COMPLETED`.
