# V25 ProjectFile UI error redaction

## Scope

C03 / issue #6158 covers user-visible error presentation from `ProjectFileUiService.ShowError` for mouse-first project create/open/save/save-as workflows.

## Root cause

The helper previously passed `Exception.Message` directly to `System.Windows.MessageBox.Show`. File, XML, COM/native and persistence exceptions can include absolute paths or implementation details, so user-visible UI was coupled to raw exception content. The presenter itself also had no containment if WPF presentation failed while already handling another exception.

## Invariants

- Preserve silent handling for `OperationCanceledException`.
- Present a stable operation-scoped message for all other failures.
- Do not surface `Exception.Message`, `InnerException`, stack traces, native text or filesystem paths from the caught exception.
- Keep `ShowError` presentation-only: no project creation/bootstrap, document recapture, CAD lock/transaction, selection mutation, command dispatch or deferred dispatcher work.
- Contain `MessageBox.Show` in best-effort `try/catch` so the error presenter cannot mask the original operation result.
- Do not alter Save/SaveAs native-commit truth or sidecar rollback ownership from #6117.

## REMOTE_SAFE verification

Run `python scripts/preflight-v25-project-file-ui-error-redaction.py`, aggregate feature guards, deterministic smoke and admitted-reference BricsCAD V25 compile on the exact candidate SHA. Required protected PR checks must be fresh and GREEN before merge.

## LOCAL_ONLY qualification

Licensed BricsCAD V25 UI interaction remains separate. A representative native check may inject/create a file/native/project error containing sensitive path/native detail and verify the dialog only presents the stable operation-scoped message while cancellation stays silent. Record `LOCAL_PASS` only from an actual licensed V25 run bound to the tested SHA; otherwise classify `LOCAL_ONLY / NO_RESULT`.
