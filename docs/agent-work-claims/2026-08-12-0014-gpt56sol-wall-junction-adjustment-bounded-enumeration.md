# Work claim — Wall junction adjustment bounded enumeration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-wall-junction-adjustment-bounded-enumeration-20260812-0014`
- Registered: `2026-08-12T00:14:00+07:00`
- Baseline main SHA: `fbc4ae81f7867bfc7eaf78adbaa0e918a5ca3715`
- Priority: evidence-driven Core resource-bound hardening during owner-requested `continue all`

## Reserved scope

Make `WallJunctionAdjustmentPlanner.Plan` preserve the same 10,000 wall-segment safety boundary as `WallJunctionPlanner.Plan` before materializing its `IEnumerable<WallAxisSegment>` source.

## Expected surfaces

- `src/QS3D.Core/Geometry/WallJunctionAdjustmentPlanner.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`WallJunctionAdjustmentPlanner.Plan` currently calls `source.ToList()` and only then delegates the materialized list to the bounded `WallJunctionPlanner`. The adapter therefore defeats the planner's 10,000-segment enumeration guard: a huge or non-terminating source can be consumed without limit before junction planning ever receives it.

## Explicit exclusions

- No junction detection, endpoint movement, collapse/ambiguity checks, tolerance, ordering, command/native V25, UI, Actions, release, or LOCAL_PASS semantics changes.

## Validation plan

- Preserve all existing adjustment behavior for bounded inputs.
- Add a non-terminating valid segment enumerable that throws if item 10,002 is requested; verify adjustment planning rejects after exactly 10,001 yields before junction detection/adjustment work.
- Re-fetch current source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

The adjustment adapter can no longer bypass the Wall junction planner's 10,000-segment resource boundary, focused regression is integrated on current `main`, and this claim is marked `COMPLETED` with exact implementation SHA(s) and validation performed.
