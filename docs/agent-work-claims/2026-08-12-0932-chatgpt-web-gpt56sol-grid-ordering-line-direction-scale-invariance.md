# Work claim — Grid spatial ordering line-direction scale invariance

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T09:32:00+07:00`
- Completed: `2026-08-12T09:37:00+07:00`
- Baseline main SHA: `3990285a4fa98b5d1521f0c52eb5feaa43fe933e`
- Claim commit: `56dedc21982e7abd37c9a0fbd3ae4d43baa02ba9`
- Fix commit: `5232ac739d0f2bb6c0a2d84accdec4c89df735cb`
- Smoke commit: `17a81fcd62acd5d8d0a56a4118b0e0a721060250`
- Registration commit: `6605fb491e8899708bdd93190fb299b1af5f0f49`
- Priority: evidence-driven remote-safe Core numeric robustness

## Reason

`GridSpatialOrderingPlanner.OrderParallelLines` uses each Grid LINE displacement to derive direction and to reject only lines degenerate within `coordinateTolerance`. The previous `Hypot(dx, dy)` returned `Infinity` when a finite diagonal displacement had a mathematical norm above `double.MaxValue`, and the planner then rejected that line as "degenerate". A finite line from `(0,0)` to large equal finite coordinates could therefore fail solely because its magnitude overflowed the intermediate norm, even though its direction was well-defined and far above the degeneracy tolerance.

## Implemented

The existing finite `Hypot` result still controls the degeneracy threshold whenever representable. When finite `dx`/`dy` produce an overflowed `Hypot`, the planner now derives only the unit direction by scaling the displacement components by their maximum absolute magnitude and normalizing in the safe scaled range. Invalid/non-finite coordinate deltas remain rejected before this branch. Axis normalization, alignment, projection, ordering, tie, bounds, identity, and descending semantics are unchanged.

Focused CAD-independent smoke coverage compares a normal `(1,1)` Grid LINE direction with a finite `(0.9 * double.MaxValue, 0.9 * double.MaxValue)` direction under the same perpendicular ordering axis, asserting the same identity and finite/equal ordering coordinate. It also verifies a truly short line within `coordinateTolerance` remains rejected. A dedicated module-initializer registration invokes the smoke without modifying shared registration surfaces.

## Reserved scope

- `src/QS3D.Core/Geometry/GridSpatialOrderingPlanner.cs`
- `tests/QS3D.Core.SmokeTests/GridSpatialOrderingLineDirectionScaleSmoke.cs`
- `tests/QS3D.Core.SmokeTests/GridSpatialOrderingLineDirectionScaleRegistration.cs`
- this claim file

## Excluded scope

- No Grid intersection, naming, ARC/radial policy, V25 command, or ordering-axis changes.
- No change to the finite representable line-length threshold semantics.
- No GitHub Actions dispatch.

## Validation

- Exact product diff: 17 additions / 3 deletions in `GridSpatialOrderingPlanner.cs`, confined to overflowing line-direction normalization.
- Exact smoke diff: one focused 53-line smoke source.
- Exact registration diff: one dedicated 13-line module-initializer source.
- `6605fb491e8899708bdd93190fb299b1af5f0f49` was verified as an ancestor of observed current `main` `8144ac7e23930351a12d116ec4f878dd639487ce` with `behind_by: 0`; the three intervening commits touched disjoint wall/rule/persistence surfaces.
- Static/exact-diff/ancestry verification only. No repository `dotnet` or licensed BricsCAD V25 runtime PASS is claimed from this hosted session.

## Coordination

This is a distinct follow-up to the completed ordering-axis scale-invariance claim, which explicitly excluded line-direction normalization. Recent claim/commit search found no active reservation for this line-direction defect.

## Completion condition

Satisfied: current `main` accepts finite non-degenerate Grid LINE directions whose intermediate norm alone overflows, focused smoke coverage locks the behavior, and this claim is `COMPLETED`.
