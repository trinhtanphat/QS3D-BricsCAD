# Work claim — Xref scale-state display

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xref-scale-state`
- Registered: `2026-08-11T22:05:00+07:00`
- Baseline main SHA: `a32473279878b1a5096ddd3159567edfd66cd515`
- Rebased audit SHA: `34b871b659c4e7ee87a5d0bc9076367d4ac1b6af`
- Priority: P1 screenshot/reference parity

## Goal

Complete the supplied `QUẢN LÝ BẢN VẼ` table with a real `Tỉ lệ` state derived from current-space Xref instances. Preserve the already-functional Xref/layer actions, including the newly completed native lock/unlock lane.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Cad/DrawingCatalogReader.cs`
- `src/QS3D.BricsCAD.V25/UI/ViewModels/RightPanelViewModel.cs`
- `src/QS3D.BricsCAD.V25/UI/RightPanel.xaml`
- `src/QS3D.BricsCAD.V25/UI/RightPanel.XrefScale.cs` (new isolated partial)
- `scripts/preflight-xref-scale-state.py`
- this claim file
- `src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs` is **audit-only / no edit planned**. The isolated partial subscribes to the existing drawing collection and enriches scale state after the core refresh path, avoiding replacement of the large concurrent interaction surface.

## Implementation shape

- `DrawingCatalogReader.ReadReferences(...)` remains the source of truth for current-space Xref state and additionally records first-instance X/Y/Z scale, deterministic same-scale comparison across later current-space instances, mixed-state detection, and a display string.
- `DrawingItemViewModel` gains a notifying `ScaleText` property only; existing immutable drawing row properties remain unchanged.
- `RightPanel.XrefScale.cs` registers a class-level Loaded hook through a static field initializer, subscribes once to `_viewModel.Drawings.CollectionChanged`, coalesces refreshes through the WPF Dispatcher, re-reads current `DrawingCatalogReader.ReadReferences(...)`, maps by Xref name, sets the model row to `1:1`, and updates Xref `ScaleText` without changing collection membership.
- The partial is read-only and intentionally does not call `RefreshDrawingsOnly()` itself, preventing refresh loops while allowing every existing core drawing rebuild to be enriched automatically.

## Functional contract

- For every Xref, inspect only live `BlockReference` instances in the current space, using the same scope already used for instance count/lock state.
- Record the first instance's exact X/Y/Z scale, compare later instances with a small deterministic relative tolerance, and expose `Hỗn hợp` if scales differ across current-space instances.
- For one consistent uniform positive scale, display a human-friendly ratio: `1:1` for unity, `1:N` for scale factors below 1, and `N:1` for factors above 1. Non-uniform, mirrored/non-positive, or otherwise non-ratio-safe X/Y/Z scale displays explicit `X/Y/Z` values rather than pretending it is one ratio.
- Xrefs with zero current-space instances show `—`. The main DWG row shows `1:1`.
- Keep instance count (`SL`) visible and replace the redundant displayed `Loại` column with `Tỉ lệ`; internal `Kind` state remains available for behavior/tooltips.
- This is read-only catalog state. No Xref transformation, source-file write, semantic/QSDB mutation or command dispatch is introduced.
- Preserve all existing Xref toolbar/context actions and layer-manager behavior.

## Validation plan

- Re-fetch current `main` and all reserved source files immediately before writes; preserve concurrent winners.
- Add an auto-discovered static preflight covering current-space scale capture, tolerance/mixed-state handling, uniform/non-uniform ratio formatting, notifying VM propagation, collection-change/Dispatcher enrichment, `Tỉ lệ` XAML column, preserved `SL`, native lock controls and all existing Xref actions.
- Re-fetch final source/ancestry/status. Do not dispatch GitHub Actions.

## Completion condition

The drawing manager reports actual current-space Xref scale state in the screenshot-style `Tỉ lệ` column without mutating CAD or losing any existing drawing/layer actions, and this claim is marked `COMPLETED` with exact SHAs.
