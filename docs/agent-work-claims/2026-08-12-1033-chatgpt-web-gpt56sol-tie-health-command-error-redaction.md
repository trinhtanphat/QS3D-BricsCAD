# Work claim — Tie Health command error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-tie-health-command-error-redaction-20260812-1033`
- Registered: `2026-08-12T10:33:00+07:00`
- Baseline main SHA: `bf8f723075f46ed5655a3eedbd5d0cfd5dbd29cb`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/ColumnTieHealthCommands.cs` catches `System.Exception ex` at the `QS3DREBARTIEHEALTH` command boundary and constructs `"QS3DREBARTIEHEALTH lỗi: " + ex.Message`, then writes it to both Palette status and Editor output. Raw exception details can expose filesystem/provider/environment information.

## Reserved scope

- Redact raw exception-message reflection from the `QS3DREBARTIEHEALTH` top-level command catch.
- Preserve command registration, read-only project access, live-handle collection, generated tie health inspection, health summary, modeless window, locate/select/zoom behavior, and both output sinks.
- Add one focused static regression preflight for this command boundary.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/ColumnTieHealthCommands.cs`
- `scripts/preflight-tie-health-command-error-redaction.py`
- this claim file

## Excluded scope

- No changes to `GeneratedTieRebarHealthService`, handle parsing semantics, tie generation, project persistence, Actions dispatch, release publication, force push, or BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Replace raw exception-message composition with a stable generic command failure message while preserving both sinks and all existing health/locate behavior.
- Add a focused Python source preflight that rejects `ex.Message` and pins registration/read-only/live-handle/service/modeless/select/zoom/output contracts.
- Re-fetch source/preflight from current `main`, verify ancestry/readback, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects `ex.Message` from `QS3DREBARTIEHEALTH`, focused regression source pins the existing command flow, and this claim is `COMPLETED` with exact integration evidence.