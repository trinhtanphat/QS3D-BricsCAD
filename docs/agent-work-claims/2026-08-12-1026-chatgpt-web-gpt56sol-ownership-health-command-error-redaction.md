# Work claim — Ownership Health command error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-ownership-health-command-error-redaction-20260812-1026`
- Registered: `2026-08-12T10:26:00+07:00`
- Baseline main SHA: `8ec6973a0d4af8f870e835c4b4647954db9916d7`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/SafeGeneratedHandleOwnershipHealthCommands.cs` still catches `System.Exception ex` at the `QS3DOWNERSHIPHEALTH` command boundary and reports `"QS3DOWNERSHIPHEALTH lỗi: " + ex.Message` through `Report(...)`, which writes to both Palette status and Editor output. Raw exception messages may therefore expose filesystem/provider/environment details. This is separate from the already-completed Core `SafeGeneratedHandleOwnershipHealthService` invalid-project diagnostic redaction lane.

## Reserved scope

- Redact raw exception-message reflection from the `QS3DOWNERSHIPHEALTH` top-level command catch.
- Preserve command registration, read-only project access, health summary, modeless health window, locate/select/zoom behavior, Palette status sink, and Editor sink.
- Add one focused static regression preflight for this command boundary.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/SafeGeneratedHandleOwnershipHealthCommands.cs`
- `scripts/preflight-ownership-health-command-error-redaction.py`
- this claim file

## Excluded scope

- No changes to `SafeGeneratedHandleOwnershipHealthService` Core diagnostics.
- No generated-handle ownership semantics, selection logic, health issue text, project persistence, or geometry mutation changes.
- No GitHub Actions dispatch, release publication, force push, or licensed BricsCAD V25/V26 runtime PASS claim.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Replace the raw exception-message catch with a stable generic command failure message while preserving `Report(...)` and modeless/selection behavior.
- Add a focused Python source preflight that rejects `ex.Message` and pins command registration plus reporting/selection tokens.
- Re-fetch source/preflight from current `main`, verify ancestry/readback, and close with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects `ex.Message` from `QS3DOWNERSHIPHEALTH`, the existing ownership-health UX flow remains source-pinned, focused regression source exists, and this claim is `COMPLETED` with exact integration evidence.