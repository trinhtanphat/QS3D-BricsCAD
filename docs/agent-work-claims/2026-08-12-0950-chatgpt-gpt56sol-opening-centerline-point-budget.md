# Work claim — Opening centerline point budget

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260812-opening-centerline-point-budget`
- Registered: `2026-08-12T09:50:00+07:00`
- Baseline main SHA: `2b2b1479afbd61abed1fd43b0dfc3125a3b73c41`
- Priority: bounded caller-controlled geometry input during owner-requested continue-all audit
- Task Key: `CORE-OPENING-CENTERLINE-POINT-BUDGET`

## Confirmed defect

`PolylineOpeningCutPlanner.Plan(...)` accepts an arbitrary caller-controlled `IReadOnlyList<Point2>` and immediately allocates a `double[input.Centerline.Count - 1]` before scanning every point/segment. `CurvedOpeningFootprintPlanner.Plan(...)` likewise accepts an unbounded centerline, materializes segment/projection collections, and then passes the sliced path into footprint topology work. Neither opening boundary caps the source point count.

The adjacent `CurtainPathFramePlanner` already treats the same centerline/path input class as resource-bounded with `MaxPathPoints = 8192` and rejects larger input before path materialization. The opening planners currently lack that established guard and can therefore perform caller-amplified allocation/CPU work before semantic geometry validation completes.

## Reserved scope

- `src/QS3D.Core/Geometry/PolylineOpeningCutPlanner.cs`
- `src/QS3D.Core/Geometry/CurvedOpeningFootprintPlanner.cs`
- one focused Core smoke file for opening centerline point-budget rejection
- this claim file for close-out

## Contract

- use the established Core path point budget of 8192 for both opening centerline planners;
- reject `Centerline.Count > 8192` before allocating/materializing per-segment structures or indexing the caller list;
- preserve the existing minimum-two-points, finite coordinate, geometry, offset, ambiguity, corner/junction, overflow and footprint semantics for supported inputs;
- do not broaden into `WallFootprintEngine`, Curtain path planning, structural wall opening host canonicality, CAD/native execution or UI.

## Validation plan

Add focused ModuleInitializer smoke coverage with an oversized `IReadOnlyList<Point2>` whose indexer throws, proving both public opening planners reject the oversized count before reading any point. Also keep one small canonical valid path case to pin supported behavior.

No GitHub Actions/build/release dispatch and no BricsCAD V25/V26 runtime PASS claim from this remote lane.
