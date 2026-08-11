# Work claim — Wall Quantity Schedule Hub launcher

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-wall-quantity-schedule-hub-launcher`
- Registered: `2026-08-11T21:36:00+07:00`
- Baseline main SHA: `7f63f7939aea0e9b13dbb5de217b61612d84fc3f`
- Priority: P2

## Reserved scope

Expose the already-merged `QS3DWALLQTY` Wall Quantity workspace from the drawing-bound Schedule Hub so the owner-requested wall takeoff workflow is discoverable alongside BQ/ED2/schedule tools.

## Reserved files

- `src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml`
- one new focused source-safe preflight if no existing Schedule Hub launcher guard is present
- this claim file for close-out

## Contract

- add a visible `Khối lượng Tường` launcher under `BẢNG TỔNG HỢP`, tagged `QS3DWALLQTY` and routed through the existing `OnCommandClick` document-affinity path;
- update the Schedule Hub subtitle to include Tường without redesigning unrelated cards;
- do not modify `ScheduleHubWindow.xaml.cs` unless the existing generic command dispatcher proves insufficient;
- do not edit quantity formulas, persistence, `Commands.cs`, Ribbon, Start Center, RightPanel, Wall Quantity implementation, Core or the shared local inbox;
- add a narrow static guard for the launcher and XML validity without duplicating unrelated Schedule Hub policy;
- do not dispatch/re-run GitHub Actions and do not claim licensed BricsCAD V25 runtime PASS remotely.

## Completion condition

The Wall Quantity launcher and focused static guard are merged onto current `main`, the PR touches only the reserved surface, and this claim is marked `COMPLETED` with the exact implementation SHA.
