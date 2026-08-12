# Work claim — Start Center command diagnostic fail-soft

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-start-center-command-diagnostic-fail-soft`
- Registered: `2026-08-12T08:36:00+07:00`
- Baseline main SHA: `d205d5151158d7617c6fde6014292914414a1889`
- Completed source commit: `fb9d011d2e251585c61b932c0e14248c25e00110`
- Regression commit: `509e908a700af54dece643e69cdb0df75293f3fd`
- Readback main SHA before close-out: `6ca1966f67c55594f779620704ecf59badca7220`
- Priority: P1 modeless command exception containment found during owner-requested `continue all` audit.

## Confirmed defect

`StartCenterCommands.ShowStartCenter()` correctly rolled back a newly-created Start Center when `Application.ShowModelessWindow(...)` or another show-path operation failed, but its outer `catch` then wrote `QS3DSTART error` directly through the active document editor with no second exception boundary. During document shutdown/switching or other host lifecycle transitions, that optional diagnostic could itself throw and escape the command, replacing the original contained failure with a secondary host/editor exception.

`OnDocumentActivated(...)` already applied the intended contract: refresh failures were contained and the best-effort editor diagnostic was wrapped in its own `try/catch`. The command failure path now follows the same fail-soft rule.

## Reserved scope

- `src/QS3D.BricsCAD.V25/StartCenterCommands.cs`
- `scripts/preflight-start-center.py`
- this claim file for close-out

## Implemented contract

1. Failed-open ownership rollback remains first and unchanged.
2. Active-document resolution plus `Editor.WriteMessage("\nQS3DSTART error: ...")` now run inside a nested `try/catch (System.Exception)`.
3. A secondary document/editor diagnostic failure cannot escape the command failure boundary.
4. Existing error text, singleton ownership, activation subscription, close/reopen behavior, launcher/Recent/Favorites/Ribbon behavior and project read-only semantics remain unchanged.
5. The canonical `scripts/preflight-start-center.py` now pins both rollback-before-diagnostic ordering and the nested command-diagnostic exception boundary, while retaining the existing Start Center source contract checks.

## Verification

- Current-main source readback confirmed the nested diagnostic boundary and preserved rollback ordering.
- Current-main preflight readback confirmed `diagnostic_try_pos` / `diagnostic_catch_pos` containment assertions and the `command-diagnostic-fail-soft` contract marker.
- `509e908a700af54dece643e69cdb0df75293f3fd...main` compared as `ahead` with the regression commit as the merge base, confirming the source/regression batch remained in current `main` while concurrent agents advanced it.
- Full repo preflight, adapter compile and GitHub Actions were not run from this remote connector session; no PASS is fabricated.
- Licensed BricsCAD V25 lifecycle/fault-injection verification remains `LOCAL_ONLY` under the existing local validation queue.

## Excluded

- No BLT code/assets.
- No Start Center XAML/catalog/state-store/Ribbon redesign.
- No Core/project mutation changes.
- No installer/signing/release changes.
- No GitHub Actions dispatch.
