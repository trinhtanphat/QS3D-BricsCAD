# Work claim — Xref scale-state display

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xref-scale-state`
- Registered: `2026-08-11T22:05:00+07:00`
- Baseline main SHA: `a32473279878b1a5096ddd3159567edfd66cd515`
- Priority: P1 screenshot/reference parity

## Goal

Complete the supplied `QUẢN LÝ BẢN VẼ` table with a real `Tỉ lệ` state derived from current-space Xref instances. Preserve the already-functional Xref/layer actions, including the newly completed native lock/unlock lane.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Cad/DrawingCatalogReader.cs`
- `src/QS3D.BricsCAD.V25/UI/ViewModels/RightPanelViewModel.cs`
- `src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/RightPanel.xaml`
- `scripts/preflight-xref-scale-state.py`
- this claim file

## Functional contract

- For every Xref, inspect only live `BlockReference` instances in the current space, using the same scope already used for instance count/lock state.
- Record the first instance's exact X/Y/Z scale, compare later instances with a small deterministic tolerance, and expose `Hỗn hợp` if scales differ across current-space instances.
- For one consistent uniform positive scale, display a human-friendly ratio: `1:1` for unity, `1:N` for scale factors below 1, and `N:1` for factors above 1. Non-uniform X/Y/Z scale displays explicit `X/Y/Z` values rather than pretending it is one ratio.
- Xrefs with zero current-space instances show `—`. The main DWG row shows `1:1`.
- Keep instance count (`SL`) visible and replace the redundant `Loại` display column with `Tỉ lệ`; internal `Kind` state remains available for behavior/tooltips.
- This is read-only catalog state. No Xref transformation, source-file write, semantic/QSDB mutation or command dispatch is introduced.
- Preserve all existing Xref toolbar/context actions and layer-manager behavior.

## Validation plan

- Re-fetch current `main` and all four source files immediately before writes; preserve concurrent winners.
- Add an auto-discovered static preflight covering current-space scale capture, tolerance/mixed-state handling, uniform/non-uniform ratio formatting, VM propagation, `Tỉ lệ` XAML column, preserved `SL` and existing Xref actions.
- Re-fetch final source/ancestry/status. Do not dispatch GitHub Actions.

## Completion condition

The drawing manager reports actual current-space Xref scale state in the screenshot-style `Tỉ lệ` column without mutating CAD or losing any existing drawing/layer actions, and this claim is marked `COMPLETED` with exact SHAs.
