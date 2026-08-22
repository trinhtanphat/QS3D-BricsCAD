# Work claim — Xref scale-state display

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-xref-scale-state`
- Registered: `2026-08-11T22:05:00+07:00`
- Completed: `2026-08-11T22:15:00+07:00`
- Baseline main SHA: `a32473279878b1a5096ddd3159567edfd66cd515`
- Rebased audit SHA: `34b871b659c4e7ee87a5d0bc9076367d4ac1b6af`
- Priority: P1 screenshot/reference parity

## Implemented

- `b46e1abce077eb20e393a487e9cfba48980747df` — `DrawingCatalogReader.ReadReferences(...)` now captures the first live current-space Xref instance's native `ScaleFactors`, compares later current-space instances with a deterministic relative tolerance, exposes `Hỗn hợp` when scale states differ, and formats one consistent scale as `1:1`, `1:N`, `N:1`, or explicit `X/Y/Z` values for non-uniform/non-ratio-safe scale.
- Xrefs with no current-space instances remain `—`; existing instance count and layer-lock state are calculated in the same read-only transaction as before.
- `858bc0bdea32880a32a65293b62efd92926186de` — `DrawingItemViewModel` now implements `INotifyPropertyChanged` for a dedicated `ScaleText` property while preserving existing row properties including internal `Kind` and `InstanceText`.
- `50eb5d22b9d341f52fe9e13b939484a9628020a3` — added isolated `RightPanel.XrefScale.cs`. A class-level Loaded hook subscribes once to the existing drawing collection, coalesces collection rebuilds through the WPF Dispatcher, re-reads current catalog scale state, maps by Xref name, sets the main DWG row to `1:1`, and updates only `ScaleText`. It does not call `RefreshDrawingsOnly()` and therefore does not introduce a collection refresh loop.
- `2d9bb01774f84344296945ee69b9b9b2df81d1be` — drawing table now shows screenshot-style `Tên / Khóa / SL / Tỉ lệ`; the redundant displayed `Loại` column was removed while internal `Kind` remains available. All existing Add/Reload/Move/Lock/Unlock/Zoom/Detach actions and the layer manager remain intact.
- `078225db5fa524dde20fad82d8e16d6ad35cc60b` plus correction `0b332aaaaacbc33ef20c3fcdab7d8fbf01fc6a92` — added/fixed `scripts/preflight-xref-scale-state.py`, covering current-space scale capture, tolerance/mixed-state handling, ratio/non-uniform formatting, notifying VM propagation, idempotent collection/Dispatcher enrichment, `Tỉ lệ` XAML wiring, preservation of `SL`, native Xref lock controls and all pre-existing Xref operations.

## Source validation

- Re-fetched the current `DrawingCatalogReader.cs`, `RightPanelViewModel.cs`, `RightPanel.XrefScale.cs` and the relevant `RightPanel.xaml` section from `main`; all scale-state contracts remain present after concurrent commits.
- Re-fetched the focused preflight and corrected two string-match tokens so the static contract reflects the actual C#/XAML source rather than escaped Python literals.
- `compare_commits` from `b46e1abce077eb20e393a487e9cfba48980747df` to current `main` reports `behind_by: 0` with that implementation as merge base; 53 concurrent commits were preserved and no force push/reset was used.
- GitHub exposes no combined status checks for `0b332aaaaacbc33ef20c3fcdab7d8fbf01fc6a92`; no GitHub Actions were dispatched.
- The remote connector environment did not execute the BricsCAD adapter build or licensed runtime. Validation for this lane is source/preflight inspection only; no native runtime PASS is claimed.

## LOCAL_ONLY disposition

- Physical BricsCAD V25 verification of mirrored/non-uniform Xref display, table rendering and current-space scale changes remains part of the existing RightPanel/palette runtime qualification boundary. No duplicate local inbox item was added.

## Completion evidence

The screenshot-inspired drawing manager now reports actual native current-space Xref scale state in a `Tỉ lệ` column, including mixed and non-uniform cases, without transforming CAD objects or touching QS3D semantic/QSDB state.
