# Work claim — Wall junction adjustment result snapshots

- Status: `COMPLETED`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T07:36:00+07:00`
- Scope narrowed: `2026-08-12T07:38:00+07:00`
- Completed: `2026-08-12T07:41:00+07:00`
- Baseline main SHA: `b53b59879937a1d90a355c8f33fe5efb3bf1b0e8`
- Priority: evidence-driven remote-safe geometry result integrity

## Reason

The public wall-junction adjustment result graph exposed read-only interfaces but retained caller-owned mutable lists. `WallEndpointAdjustment` stored `junctionSegmentIds` directly, and `WallJunctionAdjustmentPlan` stored its junction/adjustment lists directly. Callers could therefore mutate or clear source `List<T>` instances after construction and silently rewrite a supposedly completed adjustment result.

## Changed scope

Materialize owned read-only list snapshots in `WallEndpointAdjustment` and `WallJunctionAdjustmentPlan`. Preserve junction math/classification, endpoint adjustment selection, ordering, identities, public property types, planner limits and native/UI behavior.

## Changed surfaces

- `src/QS3D.Core/Geometry/WallJunctionAdjustmentPlanner.cs`
- `tests/QS3D.Core.SmokeTests/WallJunctionAdjustmentResultSnapshotSmoke.cs`
- this claim file

## Excluded scope

- `WallJunctionPlanner.cs` / direct `WallJunction` constructor aliasing remains explicitly deferred to a separate lane; the larger planner file was not replaced by this focused change.
- No junction tolerance/math, classification, source enumeration, ownership or CAD/native changes.
- No new validation requirements for ids/enums/numeric values beyond removing collection aliasing.
- No GitHub Actions dispatch and no BricsCAD runtime claim.

## Completion

- Initial claim commit: `773b115af16f3dc5023970ed3d21c9652bfb3f9f`.
- Scope-narrowing commit: `872d2beb14eb4b3f6f8c997fe3be68367336d053`.
- Implementation commit: `74f1b87ba527b47b5e41b28e1b6dac60eb95a547` — copy nested junction ids and top-level junction/adjustment result lists into owned read-only lists.
- Regression commit: `b87a449c6b843918ba9587e05dc2f0ea2b39dcb6` — mutate/clear source id/result lists after construction and verify the adjustment graph remains stable.
- Validation actually performed:
  - re-fetched the current constructors and confirmed owned `List<T>.AsReadOnly()` snapshots are present;
  - re-fetched the dedicated smoke and confirmed both nested-id and top-level list alias cases are covered;
  - no repository `dotnet` tests were executed in this hosted session;
  - no GitHub Actions were dispatched or rerun;
  - no BricsCAD runtime PASS is claimed.

## Coordination

Older wall-junction work covered analysis read-only behavior and bounded enumeration. Current native wall/rebar single-bind claims were kept disjoint from these Core constructors.

## Completion condition

Satisfied: current `main` exposes stable wall-junction adjustment result snapshots independent of caller list mutation, focused regression coverage is present, and this claim is released as `COMPLETED`.
