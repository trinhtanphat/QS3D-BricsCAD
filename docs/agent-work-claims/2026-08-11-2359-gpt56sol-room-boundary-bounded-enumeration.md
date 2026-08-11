# Work claim — Room boundary bounded enumeration

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-boundary-bounded-enumeration-20260811-2359`
- Registered: `2026-08-11T23:59:00+07:00`
- Baseline main SHA: `2976e554568902964e6079c3fb0ed66c3fa5094a`
- Priority: evidence-driven Core resource-bound hardening during owner-requested `continue all`

## Reserved scope

Make `RoomBoundaryEngine.Discover` enforce its existing `MaxInputSegments = 5000` contract while enumerating `IEnumerable<BoundarySegment>`, rather than after unrestricted materialization.

## Expected surfaces

- `src/QS3D.Core/Geometry/RoomBoundaryEngine.cs`
- isolated focused Core smoke regression
- this claim file for close-out

## Concrete defect

`Discover` currently calls `source.ToList()` before checking `segments.Count > MaxInputSegments`. A huge or non-terminating enumerable can therefore be consumed without limit before the declared 5,000-segment guard; broad-phase pair discovery, subdivision and graph processing occur only after that late check.

## Explicit exclusions

- No boundary geometry validation, pair broad-phase, cut/subdivision, graph/bridge/face tracing, boundary-key, minimum-area, Auto Room/native V25, UI, Actions, release, or LOCAL_PASS semantics changes.

## Validation plan

- Preserve all existing Room boundary behavior.
- Add a non-terminating valid-segment enumerable that throws if item 5,002 is requested; verify oversize input is rejected after exactly 5,001 yielded segments, before per-segment validation/broad-phase work.
- Re-fetch current source before implementation and do not overwrite concurrent edits.
- No GitHub Actions will be dispatched and no BricsCAD runtime PASS will be claimed from this web session.

## Completion condition

The 5,000-segment safety limit bounds enumeration/allocation as well as accepted cardinality, focused regression is integrated on current `main`, and this claim is marked `COMPLETED` with exact implementation SHA(s) and validation performed.
