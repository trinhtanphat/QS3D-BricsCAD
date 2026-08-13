# Agent work claim — detailed quantity explainer

- Agent: `chatgpt-web-gpt56sol-quantity-detail-explainer-20260813-2249`
- Started: `2026-08-13T22:49:00+07:00`
- Completed: `2026-08-13T23:18:00+07:00`
- Status: `COMPLETED`
- Baseline main SHA: `484cd6248a167a6ff67a9ebace7e2504b5f8ecf1`
- Task: add BLT3D-style per-component quantity explanation to QS3D and push it to `main`.

## Plan executed

1. Preserve `ProjectQuantityReportBuilder` as the canonical source instead of duplicating quantity formulas in UI.
2. Keep the existing Floor/Family aggregate tree and attach a `CHI TIẾT CẤU KIỆN` drill-down below it.
3. Resolve a selected aggregate row with the existing stale-row guard, create a detached project snapshot, preview-regenerate it, then call `ProjectQuantityReportBuilder.Detail(preview, selectedRow.ElementIds)`.
4. Show each canonical element separately when a grouped row contains multiple elements.
5. Expose gross/deduction/net concrete, formwork, length, outer/inner perimeter, door/side/bottom/top/other areas, density and mass with units.
6. Show provenance: semantic Element IDs, source CAD handles and drawing fingerprint.
7. Add guarded `Định vị cấu kiện`: rebuild current detail, compare identity/value provenance, resolve current source handles, select and run `QS3DZOOMSELECTED` only when still valid.
8. Add a focused static preflight preventing regression to aggregate-only or a write-enabled detail path.

## Implemented surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Registration.cs`
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.UiShell.cs`
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.UiHeader.cs`
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Selector.cs`
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Metrics.cs`
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Provenance.cs`
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Data.cs`
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Render.cs`
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Format.cs`
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Locate.cs`
- `scripts/preflight-quantity-insight-detail.py`

## Verification / commits

- implementation range on main starts at `81ef142c6e262d12d66d03fb862de4ae59deac6d`
- canonical detail generation: `194d4a5c6e011849886517553ff3d5e3d6137220`
- guarded detail locate fix: `d9c369448f24bdc997d1a459d4179071164f8678`
- regression guard: `53ad7d3ffe93adb0fef402d1729505a7d7be182f`
- final source-contract adjustment before close: `5940d3f93c9f244a3bfd721d57c5702ec82b8d70`
- current source was re-read from `main`; GitHub reported no workflow run attached to the final source commit, so no CI/native runtime PASS is fabricated.

## Completion condition

Source implementation and regression contract are on `main`. Remaining acceptance is native-only: build/load the exact final HEAD in licensed BricsCAD V25, open a real QS3D project, select single and grouped quantity rows, inspect all detail metrics/provenance, then verify `Định vị cấu kiện` selects/zooms the correct live CAD objects. No native V25 PASS is claimed remotely.
