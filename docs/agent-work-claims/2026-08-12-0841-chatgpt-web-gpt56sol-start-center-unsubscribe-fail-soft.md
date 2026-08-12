# Work claim — Start Center unsubscribe fail-soft

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-start-center-unsubscribe-fail-soft`
- Registered: `2026-08-12T08:41:00+07:00`
- Baseline main SHA: `4d1a7b53d90db490c70fd02c1ab11c8ca8fc47b9`
- Priority: P1 residual modeless lifecycle cleanup integrity found during owner-requested `continue all` audit.

## Confirmed defect

`ReleaseStartCenterWindow()` detaches the WPF `Closed` handler, calls `UnsubscribeFromDocumentActivation()`, and only then clears `_window`. `UnsubscribeFromDocumentActivation()` currently performs the BricsCAD host event remove without an exception boundary. If the host rejects/throws while removing `DocumentActivated` during document/application teardown, the exception escapes cleanup and `_window = null` is skipped. The same cleanup helper is used from the failed-show catch, so an unsubscribe failure can also bypass the command-level fail-soft path.

The safe remote contract is to keep a failed unsubscribe marked as still subscribed (avoiding duplicate subscriptions), contain the host exception, and allow exact-owner singleton release to finish. A later release can retry the unsubscribe while the subscription flag remains true.

## Reserved scope

- `src/QS3D.BricsCAD.V25/StartCenterCommands.cs`
- `scripts/preflight-start-center.py`
- this claim file for close-out

## Plan

1. Re-fetch moving `main`, current source/gate and this claim before writes.
2. Preserve the idempotent `_documentActivatedSubscribed` guard and exact-owner `ReleaseStartCenterWindow` contract.
3. Wrap only the host `DocumentActivated -= OnDocumentActivated` call in `try/catch (System.Exception)`; set `_documentActivatedSubscribed = false` only after a successful remove.
4. On remove failure, swallow the optional Start Center lifecycle exception and leave the flag true so the code does not create a duplicate handler; continue singleton release.
5. Extend the canonical Start Center preflight to pin unsubscribe exception containment, success-only flag clear, and `_window = null` after unsubscribe attempt.
6. Read back current `main`; no GitHub Actions, adapter runtime, or licensed BricsCAD V25 PASS claimed remotely.
7. Close the claim only after source/regression commits remain visible on current `main`.

## Excluded

- No XAML/catalog/state-store/Ribbon redesign.
- No Core/project mutation changes.
- No generic `DocumentBoundWindowLifetime` changes.
- No installer/signing/release changes.
- Native BricsCAD V25 event-remove fault injection remains `LOCAL_ONLY`.
