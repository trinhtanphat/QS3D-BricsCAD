# Work claim — Rebar Mode Health command error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-rebar-mode-health-command-error-redaction-20260812-1053`
- Registered: `2026-08-12T10:53:00+07:00`
- Baseline main SHA: `6f2a28aa822a39e8597066744fd9c23632955e0c`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/RebarModeHealthCommands.cs` catches `System.Exception ex` at the `QS3DREBARMODEHEALTH` command boundary and constructs `"QS3DREBARMODEHEALTH lỗi: " + ex.Message`, then writes it to Palette status and Editor output. Raw exception details can expose filesystem/provider/environment information.

## Reserved scope

- Redact raw exception-message reflection from the `QS3DREBARMODEHEALTH` top-level catch.
- Preserve command registration, read-only project access, `GeneratedRebarModeHealthService`, health summary, modeless review, GeneratedRebarHandles-to-SourceHandles fallback, locate/select/zoom behavior, and both output sinks.
- Add one focused static regression preflight for this command boundary.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/RebarModeHealthCommands.cs`
- `scripts/preflight-rebar-mode-health-command-error-redaction.py`
- this claim file

## Excluded scope

- No changes to rebar-mode health semantics, generated/source handle fallback semantics, generation/persistence, Actions dispatch, release publication, force push, or BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Replace raw exception-message composition with a stable generic command failure message while preserving existing health/locate behavior.
- Add a focused Python source preflight that rejects `ex.Message` and pins registration/read-only/service/modeless/fallback/select/zoom/output contracts.
- Re-fetch source/preflight from current `main`, verify ancestry/readback, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects `ex.Message` from `QS3DREBARMODEHEALTH`, focused regression source pins the existing command flow, and this claim is `COMPLETED` with exact integration evidence.