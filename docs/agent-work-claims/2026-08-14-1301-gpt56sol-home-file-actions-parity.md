# Work claim — BLT reference Home file actions parity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-home-file-actions`
- Registered: `2026-08-14T13:01:00+07:00`
- Baseline main SHA: `babe450f826d74922239dd15c44306d9a0af6067`
- Owner request: continue all, then review the whole project/session and complete remaining non-overlapping remote-safe gaps.

## Concrete gap

The BLT reference screenshot exposes top-level Home actions for Open, Save, Save As and Settings. QS3D already has equivalent behaviors in Start Center (`OPEN`, `QSAVE`, `SAVEAS`) and project configuration in `QS3DPROJECTTOOLS`, but the canonical Home Ribbon does not surface the complete file/settings cluster. The existing `Lưu` button is `QS3DSAVE` semantic-project persistence, so it must not be silently repurposed as native DWG save.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Ribbon/QuickWorkflowRibbonAugmenter.cs`
- `scripts/preflight-blt-reference-ui-parity.py`
- `docs/BLT-REFERENCE-UI-PARITY-PLAN-2026-08-14.md`
- this claim file

## Implementation boundary

- Add one idempotent Home Ribbon file/settings panel, not a duplicate top-level tab.
- Map `Mở…` to native `_.OPEN`, `Lưu bản vẽ` to native `_.QSAVE`, `Lưu thành…` to native `_.SAVEAS`, and `Cài đặt` to existing `QS3DPROJECTTOOLS`.
- Keep current `QS3DSAVE` semantic persistence untouched and separately visible.
- Reuse the existing clean-room reflection/Ribbon command handler; no proprietary BLT assets/code.
- No startup/lifecycle, RightPanel, Source Reconcile, Curtain, Level/rebar, or LOCAL_ONLY runtime surface changes.

## Validation

- Extend the focused BLT-reference UI parity preflight to require the Home mapping and to ensure `QS3DSAVE` remains present separately.
- Read back current merged source and plan.
- BricsCAD V25 button visibility/clickability, dark theme and DPI remain local/native acceptance; no remote `LOCAL_PASS` claim.
