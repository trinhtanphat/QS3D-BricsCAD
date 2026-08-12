# Work claim — Start Center command diagnostic fail-soft

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-start-center-command-diagnostic-fail-soft`
- Registered: `2026-08-12T08:36:00+07:00`
- Baseline main SHA: `d205d5151158d7617c6fde6014292914414a1889`
- Priority: P1 modeless command exception containment found during owner-requested `continue all` audit.

## Confirmed defect

`StartCenterCommands.ShowStartCenter()` correctly rolls back a newly-created Start Center when `Application.ShowModelessWindow(...)` or another show-path operation fails, but its outer `catch` then writes `QS3DSTART error` directly through the active document editor with no second exception boundary. During document shutdown/switching or other host lifecycle transitions, that optional diagnostic can itself throw and escape the command, replacing the original contained failure with a secondary host/editor exception.

`OnDocumentActivated(...)` already applies the intended contract: refresh failures are contained and the best-effort editor diagnostic is wrapped in its own `try/catch`. The command failure path should be equally fail-soft.

## Reserved scope

- `src/QS3D.BricsCAD.V25/StartCenterCommands.cs`
- `scripts/preflight-start-center.py`
- this claim file for close-out

## Plan

1. Re-fetch moving `main`, this claim, current Start Center source and preflight before source writes.
2. Keep failed-open ownership rollback first and unchanged.
3. Wrap only the optional `QS3DSTART error` editor diagnostic in a nested `try/catch (System.Exception)` so diagnostic failure cannot escape the command.
4. Preserve existing error text, modeless ownership, active-DWG refresh, close/reopen semantics, launcher/Recent/Favorites/Ribbon behavior and project read-only semantics.
5. Extend `scripts/preflight-start-center.py` to require command-diagnostic exception containment and to keep rollback-before-diagnostic ordering.
6. Read back current `main` source/gate after writes; do not dispatch GitHub Actions and do not claim licensed BricsCAD V25 runtime PASS remotely.
7. Close this claim only after source and regression commits are visible on current `main`.

## Excluded

- No BLT code/assets.
- No Start Center XAML/catalog/state-store/Ribbon redesign.
- No Core/project mutation changes.
- No installer/signing/release changes.
- No GitHub Actions dispatch.
- Native BricsCAD V25 lifecycle/fault-injection evidence remains `LOCAL_ONLY` under the existing local validation queue.
