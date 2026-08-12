# Work claim — Wall junction adjustment result snapshots

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:36:00+07:00`
- Scope narrowed: `2026-08-12T07:38:00+07:00`
- Baseline main SHA: `b53b59879937a1d90a355c8f33fe5efb3bf1b0e8`
- Priority: evidence-driven remote-safe geometry result integrity

## Reason

The public wall-junction adjustment result graph exposes read-only interfaces but retains caller-owned mutable lists. `WallEndpointAdjustment` stores `junctionSegmentIds` directly, and `WallJunctionAdjustmentPlan` stores its junction/adjustment lists directly. Callers can therefore mutate or clear source `List<T>` instances after construction and silently rewrite a supposedly completed adjustment result. The contained identifiers are immutable strings and the result objects expose read-only scalar/value properties, so these two constructors can be made stable snapshots without changing planner semantics.

## Reserved scope

Materialize owned read-only list snapshots in `WallEndpointAdjustment` and `WallJunctionAdjustmentPlan`. Preserve junction math/classification, endpoint adjustment selection, ordering, identities, public property types, planner limits and native/UI behavior. Add focused CAD-independent regression coverage.

## Expected surfaces

- `src/QS3D.Core/Geometry/WallJunctionAdjustmentPlanner.cs` (`WallEndpointAdjustment` and `WallJunctionAdjustmentPlan` constructors only)
- `tests/QS3D.Core.SmokeTests/WallJunctionAdjustmentResultSnapshotSmoke.cs`
- this claim file

## Excluded scope

- `WallJunctionPlanner.cs` / direct `WallJunction` constructor aliasing is explicitly deferred to a separate lane because that larger planner file has substantial independent history and should not be replaced as part of this focused adjustment-result change.
- No junction tolerance/math, classification, source enumeration, ownership or CAD/native changes.
- No new validation requirements for ids/enums/numeric values beyond removing collection aliasing.
- No GitHub Actions dispatch and no BricsCAD runtime claim.

## Validation plan

- Construct a `WallEndpointAdjustment` from a mutable junction-id list, mutate/clear the source list, and assert the adjustment retains its original ids/order.
- Construct a `WallJunctionAdjustmentPlan` from mutable junction/adjustment lists, clear those sources, and assert the plan retains the original result objects and the adjustment's nested ids.
- Re-fetch current source blob before write; never force-push.
- Record source/static verification only; do not claim an executed repository `dotnet` run in this hosted session.

## Coordination

Older wall-junction work covered analysis read-only behavior and bounded enumeration. No current/recent claim was found for `WallJunctionAdjustmentPlanner.cs` public result-constructor collection aliasing, and current native wall/rebar single-bind claims are disjoint from these Core constructors.

## Completion condition

Current `main` exposes stable wall-junction adjustment result snapshots independent of caller list mutation, focused regression coverage is present, and this claim is marked `COMPLETED`.
