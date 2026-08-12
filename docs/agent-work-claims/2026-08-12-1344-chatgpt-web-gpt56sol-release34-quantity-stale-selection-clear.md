# Agent work claim — Release #34 Quantity stale-selection clear

- Status: `ACTIVE`
- Owner: `chatgpt-web-gpt56sol`
- Started: `2026-08-12 13:44 Asia/Ho_Chi_Minh`

## Scope

Fix stale PICKFIRST retention in Quantity Summary and Quantity Insight locate flows after `CadHandleService.Select` was correctly changed to preserve implied selection on zero resolved handles. Zero-candidate and non-resolving candidate branches must explicitly clear selection through `CadHandleService.ClearSelection`, while successful locate continues through normal `Select`/zoom/reveal behavior.

## Files

- `src/QS3D.BricsCAD.V25/UI/QuantitySummaryWindow.xaml.cs`
- `src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs`
- `scripts/preflight-quantity-locate-stale-selection-clear.py`
- this claim file

## Out of scope

- `CadHandleService` public semantics
- unrelated Quantity calculation/settings behavior
- updater/signing/release behavior
- licensed BricsCAD runtime qualification

## Acceptance checks

- Summary clears PICKFIRST when the current row has zero live candidates or candidates resolve to zero ObjectIds;
- Insight clears PICKFIRST when there are zero live candidates or selected candidates resolve to zero ObjectIds;
- positive selection keeps normal Select + zoom/reveal behavior;
- gate pins `Select` preserve-on-empty semantics and explicit `ClearSelection` replacement semantics;
- no validation-failure branch can leave stale implied selection visible as a false locate result.
