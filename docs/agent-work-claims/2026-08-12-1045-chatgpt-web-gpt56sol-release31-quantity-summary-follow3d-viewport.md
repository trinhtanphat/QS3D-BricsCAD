# Work claim — release #31 BQ detail/viewport Follow3D preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release31-quantity-summary-follow3d-viewport`
- Registered: `2026-08-12T10:45:00+07:00`
- Baseline main SHA: `84df2060da5d1eb4b5cd7e4c180146cd3937cc8b`
- Priority: release #31 reports `scripts/preflight-quantity-summary-detail-viewport.py` failing because the gate still limits click-to-reveal to Detail mode after Follow3D parity was extended to Summary and Detail.

## Reserved scope

Reconcile only `scripts/preflight-quantity-summary-detail-viewport.py`. Preserve Quantity Summary XAML/code-behind and Commands production behavior unchanged.

## Canonical evidence

- `OnQuantityGridSelectionChanged` now invokes `LocateCurrent()` whenever the window is initialized, Follow3D is enabled, a row exists and the selection event adds an item; it no longer requires `_detailMode`.
- Detail row calculation still uses a detached current-project snapshot and `ProjectQuantityReportBuilder.Detail(previewProject)`.
- Locate still routes through `ResolveCurrentRow(row)` before `_locate(currentRow)`, with current Summary/Detail rows revalidated and native CAD selection/zoom owned by the command callback.
- `_detailMode` remains valid for report mode/recalculation; only click-to-reveal is mode-parity.

## Excluded scope

No production UI/command edits, no removal of detached reporting/current-row validation, no mutation binding, no project bootstrap, and no unrelated #31 work.

## Completion condition

The gate recognizes Follow3D click parity while preserving detached-detail and current-row locate safety, is pushed to `main`, and this claim is closed with exact evidence.