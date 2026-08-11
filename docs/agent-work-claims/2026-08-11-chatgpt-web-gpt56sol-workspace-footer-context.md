# Work claim — Workspace footer active-context parity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-workspace-footer-context`
- Registered: `2026-08-11T22:31:00+07:00`
- Baseline main SHA: `ee620fdc586a48581aaa9613315aa5510bd3845b`
- Priority: P1 screenshot/reference parity

## Goal

Add the screenshot-style live footer context (`Tầng … • Cao độ … | Đã bỏ chọn / N đối tượng đang chọn`) to the existing QS3D Workspace without replacing the completed footer buttons, native viewport aids or compact-shell work.

## Reserved scope

- `src/QS3D.BricsCAD.V25/UI/WorkspacePanel.FooterContext.cs` (new isolated partial)
- `scripts/preflight-workspace-footer-context.py`
- this claim file
- `WorkspacePanel.xaml`, `WorkspacePanel.xaml.cs`, `WorkspaceViewModel.cs`, Core project/domain/persistence and completed viewport-aid partial are audit-only/no-edit surfaces.

## Functional contract

- On Workspace load, find only the existing footer `Border` in Grid row 2 and its `DockPanel`; append one center/fill `TextBlock` after existing docked left/right controls, once.
- Read the active project only through `ProjectContextCoordinator.TryGetReadOnly(...)`; resolve current `ActiveFloorId` through the project's existing Floor collection/find helper and display Floor name plus elevation in meters with three decimals.
- Selection state comes only from the existing `InspectionList.Items.Count`: zero -> `Đã bỏ chọn`, one -> `1 đối tượng đang chọn`, plural -> `N đối tượng đang chọn`.
- Refresh on load, Workspace ViewModel status changes, InspectionList item-collection changes, DataContext replacement and footer pointer-enter so regular QS3D/CAD workflows update the label without polling.
- If no active document/project/floor exists, display a truthful `Chưa có QS3D project` or `Tầng —` state rather than creating fallback state.
- No project creation/mutation, QSDB write, CAD command/system-variable dispatch or timer.
- Existing `Mô hình`, `BQ`, `Kiểm tra`, `LIVE SEMANTIC`, viewport text, `Vuông góc` and `Bắt điểm` controls remain intact.

## Validation plan

- Re-fetch current Workspace XAML and completed viewport-aid partial before source write; preserve concurrent winners.
- Add auto-discovered static preflight for row-2/footer-only injection, idempotence, read-only floor lookup, selection count text, event-driven refresh and preservation of existing footer/view-aid contracts.
- Re-fetch final source/ancestry/status. Do not dispatch GitHub Actions.

## Completion condition

The Workspace footer reports active floor/elevation and selection context like the supplied reference, without changing project/CAD state or existing footer interactions, and this claim is marked `COMPLETED` with exact SHAs.
