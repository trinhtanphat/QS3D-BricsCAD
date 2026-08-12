# Work claim — Start Center unsubscribe fail-soft

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-start-center-unsubscribe-fail-soft`
- Registered: `2026-08-12T08:41:00+07:00`
- Baseline main SHA: `4d1a7b53d90db490c70fd02c1ab11c8ca8fc47b9`
- Completed source commit: `b8451539e24c4c8c906cb63f17e7ab41ac563e8d`
- Regression commit: `0970f1cb7779bcd95d2617c80e66dabb341c1b2a`
- Readback main SHA before close-out: `0c7416c7dcaaac41dfb9749296e37eb4670a14aa`
- Priority: P1 residual modeless lifecycle cleanup integrity found during owner-requested `continue all` audit.

## Confirmed defect

`ReleaseStartCenterWindow()` detached the WPF `Closed` handler, called `UnsubscribeFromDocumentActivation()`, and only then cleared `_window`. `UnsubscribeFromDocumentActivation()` performed the BricsCAD host event remove without an exception boundary. If the host rejected/threw while removing `DocumentActivated` during document/application teardown, the exception escaped cleanup and `_window = null` was skipped. The same cleanup helper is used from the failed-show catch, so an unsubscribe failure could also bypass the command-level fail-soft path.

## Implemented contract

1. The idempotent `_documentActivatedSubscribed` guard is preserved.
2. `DocumentActivated -= OnDocumentActivated` now runs inside `try/catch (System.Exception)`.
3. `_documentActivatedSubscribed = false` is executed only after a successful host event remove.
4. If host event removal throws, the exception is contained and the flag remains true, preventing a duplicate subscription and allowing a later cleanup attempt to retry.
5. `ReleaseStartCenterWindow()` therefore continues through `_window = null` after the fail-soft unsubscribe attempt while retaining its exact-owner guard.
6. The canonical Start Center preflight now pins the unsubscribe exception boundary, success-only flag clear and unsubscribe-before-singleton-release ordering.

## Verification

- Current-main source readback confirmed the contained host event remove and exact-owner singleton release.
- Current-main preflight readback confirmed the unsubscribe ordering/state assertions.
- `0970f1cb7779bcd95d2617c80e66dabb341c1b2a...main` compared as `ahead`, with the regression commit as merge base, while concurrent agents touched unrelated Core/claim files.
- No GitHub Actions, adapter compile, full preflight run, or licensed BricsCAD V25 runtime PASS was claimed from this remote connector session.
- Native BricsCAD V25 event-remove fault injection remains `LOCAL_ONLY` under the existing local validation queue.

## Excluded

- No XAML/catalog/state-store/Ribbon redesign.
- No Core/project mutation changes.
- No generic `DocumentBoundWindowLifetime` changes.
- No installer/signing/release changes.
