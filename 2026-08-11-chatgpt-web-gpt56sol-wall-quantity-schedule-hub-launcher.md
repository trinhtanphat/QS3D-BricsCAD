# Work claim — Wall Quantity Schedule Hub launcher

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-wall-quantity-schedule-hub-launcher`
- Registered: `2026-08-11T21:36:00+07:00`
- Completed: `2026-08-11T21:41:00+07:00`
- Baseline main SHA: `7f63f7939aea0e9b13dbb5de217b61612d84fc3f`
- Implementation SHA on `main`: `85ec9aec52a22b036a127b4d246ecba848299f0e`
- Integration: PR `#480` squash-merged to `main`
- Priority: P2

## Delivered scope

Exposed the already-merged `QS3DWALLQTY` Wall Quantity workspace from the drawing-bound Schedule Hub so the owner-requested wall takeoff workflow is discoverable alongside BQ/ED2/schedule tools.

## Delivered files

- `src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml`
- `scripts/preflight-wall-quantity-schedule-hub.py`

## Delivered contract

- added visible `Khối lượng Tường` under `BẢNG TỔNG HỢP`, tagged `QS3DWALLQTY` and routed through the existing `OnCommandClick` document-affinity dispatcher;
- updated the Schedule Hub subtitle to include Tường;
- preserved `ScheduleHubWindow.xaml.cs` unchanged because the existing generic dispatcher already validates the active source Document before queueing a command;
- added a narrow static guard for launcher uniqueness, XML validity, generic dispatcher wiring, document affinity and the existing read-only detached Schedule snapshot boundary;
- no quantity formulas, persistence, `Commands.cs`, Ribbon, Start Center, RightPanel, Wall Quantity implementation, Core or shared local inbox were modified;
- PR #480 changed exactly two reserved files and the branch-head lookup exposed no GitHub Actions workflow runs;
- no workflow was dispatched/re-run and no licensed V25 runtime PASS is claimed remotely.

## Completion

The Schedule Hub launcher is merged on `main` at `85ec9aec52a22b036a127b4d246ecba848299f0e`. Licensed BricsCAD V25 click/modeless qualification remains local-only.
