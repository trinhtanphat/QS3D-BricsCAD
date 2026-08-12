# Work claim — Room Finish Health command error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-room-finish-health-command-error-redaction-20260812-1042`
- Registered: `2026-08-12T10:42:00+07:00`
- Baseline main SHA: `0fb7520332811ce2380a4de2205dda11800f92cc`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/RoomFinishHealthCommands.cs` catches `System.Exception ex` at the `QS3DROOMFINISHHEALTH` command boundary and constructs `"QS3DROOMFINISHHEALTH lỗi: " + ex.Message`, then writes it to both Palette status and Editor output. Raw exception details can expose filesystem/provider/environment information.

## Reserved scope

- Redact raw exception-message reflection from the `QS3DROOMFINISHHEALTH` top-level catch.
- Preserve command registration, read-only project access, `RoomFinishHealthService` inspection, zero-issue fast return, modeless review, source-handle resolution, locate/select/zoom behavior, and both output sinks.
- Add one focused static regression preflight for this command boundary.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/RoomFinishHealthCommands.cs`
- `scripts/preflight-room-finish-health-command-error-redaction.py`
- this claim file

## Excluded scope

- No changes to room-finish health semantics, source-handle resolver behavior, room finish generation/persistence, Actions dispatch, release publication, force push, or BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Replace raw exception-message composition with a stable generic command failure message while preserving existing health/locate behavior.
- Add a focused Python source preflight that rejects `ex.Message` and pins registration/read-only/service/zero-issue/modeless/resolve/select/zoom/output contracts.
- Re-fetch source/preflight from current `main`, verify ancestry/readback, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects `ex.Message` from `QS3DROOMFINISHHEALTH`, focused regression source pins the existing command flow, and this claim is `COMPLETED` with exact integration evidence.