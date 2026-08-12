# Work claim — Curtain Frame Health command error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-curtain-frame-health-command-error-redaction-20260812-1035`
- Registered: `2026-08-12T10:35:00+07:00`
- Baseline main SHA: `1659644d824add4776759a92d89ab7489053e638`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/CurtainWallFrameHealthCommands.cs` catches `System.Exception ex` at the `QS3DCURTAINFRAMEHEALTH` command boundary and constructs `"QS3DCURTAINFRAMEHEALTH lỗi: " + ex.Message`, then writes it to both Palette status and Editor output. Raw exception details can expose filesystem/provider/environment information.

## Reserved scope

- Redact raw exception-message reflection from the `QS3DCURTAINFRAMEHEALTH` top-level catch.
- Preserve command registration, read-only project access, frame/panel live-handle health aggregation, runtime health aggregation, health summary, modeless window, locate/select/zoom behavior, and both output sinks.
- Add one focused static regression preflight for this command boundary.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/CurtainWallFrameHealthCommands.cs`
- `scripts/preflight-curtain-frame-health-command-error-redaction.py`
- this claim file

## Excluded scope

- No changes to curtain frame/panel health service semantics, handle parsing, generation, project persistence, Actions dispatch, release publication, force push, or BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Replace raw exception-message composition with a stable generic command failure message while preserving all existing frame/panel health and locate behavior.
- Add a focused Python source preflight that rejects `ex.Message` and pins registration/read-only/health aggregation/modeless/select/zoom/output contracts.
- Re-fetch source/preflight from current `main`, verify ancestry/readback, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects `ex.Message` from `QS3DCURTAINFRAMEHEALTH`, focused regression source pins the existing command flow, and this claim is `COMPLETED` with exact integration evidence.