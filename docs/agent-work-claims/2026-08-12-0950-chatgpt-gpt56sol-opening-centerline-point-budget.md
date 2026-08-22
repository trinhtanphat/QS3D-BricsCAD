# Work claim — Opening centerline point budget

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260812-opening-centerline-point-budget`
- Registered: `2026-08-12T09:50:00+07:00`
- Completed: `2026-08-12T09:56:00+07:00`
- Baseline main SHA: `2b2b1479afbd61abed1fd43b0dfc3125a3b73c41`
- Pull Request: `#727`
- Reviewed head: `186d8d30864a2bfcd1cef658cdddb59afe667237`
- Merge SHA: `8dd7b2c5e854c36dda76dbd303726c71af59a913`
- Priority: bounded caller-controlled geometry input during owner-requested continue-all audit
- Task Key: `CORE-OPENING-CENTERLINE-POINT-BUDGET`

## Confirmed defect

`PolylineOpeningCutPlanner.Plan(...)` and `CurvedOpeningFootprintPlanner.Plan(...)` accepted unbounded caller-controlled centerlines before per-segment allocation/materialization and geometry work, unlike the adjacent established 8192-point Core path budget.

## Completed contract

- Both opening planners reject centerlines above 8192 points immediately after the minimum-count check.
- Oversized input is rejected before indexing/enumerating caller point data or allocating/materializing per-segment structures.
- Existing supported-input finite-coordinate, geometry, offset, ambiguity, corner/junction, overflow and footprint semantics remain unchanged.
- Focused ModuleInitializer smoke uses an 8193-point custom `IReadOnlyList<Point2>` whose indexer/enumerator throws, proving rejection happens before reads, and keeps canonical two-point controls.

## Evidence

- PR #727 exact patch reviewed.
- Moving-main comparison showed no overlap with either geometry planner or the smoke before merge.
- Squash merge: `8dd7b2c5e854c36dda76dbd303726c71af59a913`.

## Validation boundary

No GitHub Actions/build/release dispatch occurred. No local/full .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed.
