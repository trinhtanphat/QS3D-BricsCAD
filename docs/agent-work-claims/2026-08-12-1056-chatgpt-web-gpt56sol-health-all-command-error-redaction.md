# Work claim — Health All command error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-health-all-command-error-redaction-20260812-1056`
- Registered: `2026-08-12T10:56:00+07:00`
- Baseline main SHA: `98b6af8b84431b4002dc7a2da415d06ea0cd0a65`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/HealthAllCommands.cs` catches `System.Exception ex` at the `QS3DHEALTHALL` command boundary and constructs `"QS3DHEALTHALL lỗi: " + ex.Message`, then writes it to Palette status and Editor output. Raw exception details can expose filesystem/provider/environment information.

## Reserved scope

- Redact raw exception-message reflection from the `QS3DHEALTHALL` top-level catch.
- Preserve command registration, read-only project access, source/generated live-handle collection, all existing health-service/native-table aggregation, de-duplication/sorting, modeless review, project-artifact/element locate behavior, source-handle fallback, select/zoom behavior, and both output sinks.
- Add one focused static regression preflight for this command boundary.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/HealthAllCommands.cs`
- `scripts/preflight-health-all-command-error-redaction.py`
- this claim file

## Excluded scope

- No health-service semantics, native table inspection, handle routing, project persistence, generated geometry, Actions dispatch, release publication, force push, or BricsCAD runtime changes.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Replace only raw exception-message composition with a stable generic command failure message.
- Add a focused Python source preflight that rejects `ex.Message` and pins representative aggregation plus modeless/artifact/element locate and output contracts.
- Re-fetch source/preflight from current `main`, verify ancestry/readback, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects `ex.Message` from `QS3DHEALTHALL`, focused regression source pins the existing aggregator flow, and this claim is `COMPLETED` with exact integration evidence.