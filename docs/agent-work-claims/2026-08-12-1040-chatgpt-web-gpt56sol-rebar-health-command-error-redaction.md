# Work claim — Rebar Health command error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rebar-health-command-error-redaction-20260812-1040`
- Registered: `2026-08-12T10:40:00+07:00`
- Baseline main SHA: `cefe3a8c834d62dcc8dfaf3a77bfc33d5e285ab7`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/RebarHealthCommands.cs` catches `System.Exception ex` at the `QS3DREBARHEALTH` command boundary and constructs `"QS3DREBARHEALTH lỗi: " + ex.Message`, then writes it to both Palette status and Editor output. Raw exception details can expose filesystem/provider/environment information.

## Reserved scope

- Redact raw exception-message reflection from the `QS3DREBARHEALTH` top-level catch.
- Preserve command registration, read-only project access, generated rebar handle collection, live-solid inspection, health summary, modeless review, locate/select/zoom behavior, and both output sinks.
- Add one focused static regression preflight for this command boundary.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/RebarHealthCommands.cs`
- `scripts/preflight-rebar-health-command-error-redaction.py`
- this claim file

## Excluded scope

- No changes to `GeneratedRebarHealthService`, rebar generation, handle parsing semantics, project persistence, Actions dispatch, release publication, force push, or BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Replace raw exception-message composition with a stable generic command failure message while preserving existing health/locate behavior.
- Add a focused Python source preflight that rejects `ex.Message` and pins registration/read-only/live-handle/service/modeless/select/zoom/output contracts.
- Re-fetch source/preflight from current `main`, verify ancestry/readback, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects `ex.Message` from `QS3DREBARHEALTH`, focused regression source pins the existing command flow, and this claim is `COMPLETED` with exact integration evidence.