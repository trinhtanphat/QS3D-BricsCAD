# Work claim — Audit command error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-audit-command-error-redaction-20260812-1102`
- Registered: `2026-08-12T11:02:00+07:00`
- Baseline main SHA: `ad62c1648569c5ae792378bdaefc7325b3778f8e`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/AuditCommands.cs` catches `System.Exception ex` at the `QS3DAUDIT` command boundary and reflects `ex.Message` into both `Editor.WriteMessage(...)` and `PaletteCoordinator.SetStatus(...)`. Runtime exception messages may contain filesystem/provider/environment details and should not be exposed in user-visible diagnostics.

## Reserved scope

- Redact raw exception-message reflection from the `QS3DAUDIT` top-level catch.
- Preserve command registration, read-only project lookup, no-project side-effect-free behavior, modeless `AuditLogWindow` lifecycle, success status, and protected failure reporting to both Editor and Palette.
- Add one focused static regression preflight.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/AuditCommands.cs`
- `scripts/preflight-audit-command-error-redaction.py`
- this claim file

## Excluded scope

- No audit event semantics, persistence, window layout/content, project mutation, Actions dispatch, release publication, force push, build PASS, or BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Replace the raw exception detail with one stable generic Audit failure status while preserving protected Editor/Palette sinks.
- Add a focused Python source preflight that rejects `ex.Message` and pins command registration, read-only lookup, modeless lifecycle, success status, and both failure sinks.
- Re-fetch source/preflight from current `main`, verify ancestry/readback, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects exception messages from `QS3DAUDIT`, existing read-only/modeless behavior remains source-pinned, focused regression source exists, and this claim is `COMPLETED` with exact integration evidence.