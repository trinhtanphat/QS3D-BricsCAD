# Work claim — Grid spatial ordering line-direction scale invariance

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:32:00+07:00`
- Baseline main SHA: `3990285a4fa98b5d1521f0c52eb5feaa43fe933e`
- Priority: evidence-driven remote-safe Core numeric robustness

## Reason

`GridSpatialOrderingPlanner.OrderParallelLines` uses each Grid LINE displacement to derive direction and to reject only lines degenerate within `coordinateTolerance`. The current `Hypot(dx, dy)` intentionally returns `Infinity` when a finite diagonal displacement has a mathematical norm above `double.MaxValue`, and the planner then rejects that line as "degenerate". A finite line from `(0,0)` to large equal finite coordinates can therefore fail solely because its magnitude overflows the intermediate norm, even though its direction is well-defined and far above the degeneracy tolerance.

## Reserved scope

Preserve the existing finite `Hypot`/degeneracy behavior where the line norm is representable, while normalizing the direction by component scaling only when that norm overflows. Keep invalid coordinate deltas fail-closed, and preserve all axis normalization, alignment, projection, ordering, tie, bounds, identity, and descending semantics.

## Expected surfaces

- `src/QS3D.Core/Geometry/GridSpatialOrderingPlanner.cs`
- `tests/QS3D.Core.SmokeTests/GridSpatialOrderingLineDirectionScaleSmoke.cs`
- `tests/QS3D.Core.SmokeTests/GridSpatialOrderingLineDirectionScaleRegistration.cs`
- this claim file

## Excluded scope

- No Grid intersection, naming, ARC/radial policy, V25 command, or ordering-axis changes.
- No change to the finite representable line-length threshold semantics.
- No GitHub Actions dispatch.

## Validation plan

- Use a single finite Grid LINE from `(0,0)` to `(0.9 * double.MaxValue, 0.9 * double.MaxValue)` with ordering axis `(1,-1)`; its mathematical norm exceeds `double.MaxValue` but direction/projection are well-defined.
- Assert it orders successfully with a finite coordinate instead of being misclassified as degenerate.
- Assert a truly degenerate short line within `coordinateTolerance` remains rejected.
- Re-fetch current `main` and target blob before writes; never force-push.
- Record static/exact-diff/ancestry verification only; do not claim an executed repository `dotnet` or BricsCAD V25 runtime PASS in this hosted session.

## Coordination

This is a distinct follow-up to the completed ordering-axis scale-invariance claim, which explicitly excluded line-direction normalization. Recent claim/commit search found no active reservation for this line-direction defect.

## Completion condition

Current `main` accepts finite non-degenerate Grid LINE directions whose intermediate norm alone overflows, focused smoke coverage locks the behavior, and this claim is `COMPLETED`.
