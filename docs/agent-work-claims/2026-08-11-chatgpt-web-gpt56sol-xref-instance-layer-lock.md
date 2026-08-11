# Work claim — Xref instance-layer lock controls

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-xref-instance-layer-lock`
- Registered: `2026-08-11T21:40:00+07:00`
- Baseline main SHA: `5ed731f70dd1d03948b689dc5a524411ff87ae02`
- Priority: P1 screenshot/reference workflow parity

## Reserved scope

- `src/QS3D.BricsCAD.V25/Cad/XrefService.cs`
- `src/QS3D.BricsCAD.V25/UI/RightPanel.xaml`
- `src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs`
- `scripts/preflight-xref-instance-layer-lock.py`
- this claim file

## Goal

Complete the visible `Khóa` state in the screenshot-inspired `QUẢN LÝ BẢN VẼ` area with real actions instead of read-only status. The current catalog already derives each Xref's live current-space lock state from its instance layers; this lane adds explicit lock/unlock controls using that same native layer model.

## Functional contract

- Add `Khóa Xref` and `Mở khóa` controls to the drawing toolbar and drawing context menu.
- Actions apply only to the currently selected Xref; the main DWG row must remain rejected by the existing `SelectedXref()` boundary.
- Resolve the selected Xref block definition, enumerate only its live references in the current space, deduplicate their layer IDs, and set those native `LayerTableRecord.IsLocked` values inside a document lock + transaction.
- If the Xref has zero current-space instances, return a zero count and do not touch unrelated layers.
- Do not modify Xref source files, block definition contents, semantic project state or QSDB data.
- After a successful operation, refresh both drawing lock-state and layer manager data so the UI reflects the native result immediately.
- Preserve existing attach/reload/move/detach/zoom/select behavior and recent compact-interaction winners.

## Validation plan

- Re-fetch current `main`, Xref service and RightPanel files immediately before each write; preserve concurrent winners.
- Add an auto-discovered static preflight covering Xref-record resolution, current-space reference filtering, deduplicated layer writes, document-lock/transaction ordering, zero-instance isolation, UI handlers/context-menu wiring, post-mutation refresh and no semantic/QSDB mutation.
- Re-fetch final source/ancestry. Do not dispatch GitHub Actions.

## Completion condition

The drawing manager's lock status becomes directly actionable through native current-space Xref instance layers, with immediate UI refresh, additive regression coverage and this claim marked `COMPLETED` with exact SHAs.
