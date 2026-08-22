# Agent work claim — Release #34 Quantity stale-selection clear

- Status: `COMPLETED`
- Owner: `chatgpt-web-gpt56sol`
- Started: `2026-08-12 13:44 Asia/Ho_Chi_Minh`
- Completed: `2026-08-12 13:48 Asia/Ho_Chi_Minh`

## Scope

Fix stale PICKFIRST retention in Quantity Summary and Quantity Insight locate flows after `CadHandleService.Select` was correctly changed to preserve implied selection on zero resolved handles. Zero-candidate and non-resolving candidate branches explicitly clear selection through `CadHandleService.ClearSelection`, while successful locate continues through normal `Select`/zoom/reveal behavior.

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

## Implementation

- claim: `051eeb1a80dc56ee50aa93817eb7de6220f47471`
- Quantity Insight production fix: `9d3c2fb29ea2e3af76f56e8250537aaf33b78897`
- Quantity Summary production fix: `0135ce65a82809de8451cd528ca0ca7962aa17f4`
- regression/preflight contract: `dce1ce7f034f6b2d2ab0abe77c9964db3fbb0fbb`

## Evidence & limitations

Readback confirms both locate surfaces call `ClearSelection` before returning from zero-candidate/zero-resolved branches, while successful selection still uses `Select` and zoom only after a positive resolved count. The gate now pins normal preserve-on-empty selection plus explicit clear semantics. No GitHub Actions or licensed BricsCAD runtime was executed.
