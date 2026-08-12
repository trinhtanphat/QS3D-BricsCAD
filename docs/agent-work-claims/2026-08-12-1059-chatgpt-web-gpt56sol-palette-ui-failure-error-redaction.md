# Work claim — Palette UI failure error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-palette-ui-failure-error-redaction-20260812-1059`
- Registered: `2026-08-12T10:59:00+07:00`
- Baseline main SHA: `8162f77e16d4aed27281738a972fac9ee023848b`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/PaletteCoordinator.cs` routes failures from `Show()`, `ShowSafeMode()`, and `SetStatus()` into `ReportPaletteFailure(...)`. That reporter calls `DescribeException(error)`, which walks up to eight inner exceptions and includes each raw `Exception.Message` in `Editor.WriteMessage(...)`. Filesystem paths, provider details, environment data, or other sensitive runtime text can therefore be reflected into user-visible Editor diagnostics even though command-level health errors are redacted.

## Reserved scope

- Redact raw exception-message reflection from the central Palette UI failure reporter.
- Preserve `Show`, `ShowSafeMode`, `SetStatus`, palette creation/disposal/layout persistence, and best-effort Editor reporting behavior.
- Keep operation context (`Workspace`, `Safe Mode`, `Status`) while replacing raw exception detail with stable generic UI failure text.
- Remove the now-unused `DescribeException(...)` helper if no longer referenced.
- Add one focused static regression preflight.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/PaletteCoordinator.cs`
- `scripts/preflight-palette-ui-failure-error-redaction.py`
- this claim file

## Excluded scope

- No WPF layout/theme/style changes.
- No PaletteSet sizing/docking/visibility lifecycle changes.
- No WorkspacePanel/RightPanel/QuantityInsightPanel behavior changes.
- No GitHub Actions dispatch, release publication, force push, build PASS, or BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch `PaletteCoordinator.cs` after claim registration before editing.
- Replace `ReportPaletteFailure(...)` raw exception description with stable generic text while retaining operation context and protected Editor sink.
- Ensure `DescribeException(...)` and raw `error.Message` reflection are absent from this reporter.
- Add focused Python source preflight covering the three callers, generic message, protected Editor sink, and absence of exception-message reflection.
- Re-fetch source/preflight from current `main`, verify ancestry/readback, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects exception messages from Palette UI failures, operation context and best-effort reporting remain intact, focused regression source exists, and this claim is `COMPLETED` with exact integration evidence.