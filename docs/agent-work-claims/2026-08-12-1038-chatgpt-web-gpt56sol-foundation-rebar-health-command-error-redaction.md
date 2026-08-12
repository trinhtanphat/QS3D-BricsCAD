# Work claim — Foundation Rebar Health command error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-foundation-rebar-health-command-error-redaction-20260812-1038`
- Registered: `2026-08-12T10:38:00+07:00`
- Baseline main SHA: `4e49bedf178f560b6fa97a3713a28f1cced3cf8c`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/FoundationMeshHealthCommands.cs` catches `System.Exception ex` at the `QS3DFOUNDATIONREBARHEALTH` command boundary and constructs `"QS3DFOUNDATIONREBARHEALTH lỗi: " + ex.Message`, then writes it to both Palette status and Editor output. Raw exception details can expose filesystem/provider/environment information.

## Reserved scope

- Redact raw exception-message reflection from the `QS3DFOUNDATIONREBARHEALTH` top-level catch.
- Preserve command registration, read-only project access, generated foundation handle collection, live-solid inspection, health summary, modeless review, locate/select/zoom behavior, and both output sinks.
- Add one focused static regression preflight for this command boundary.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/FoundationMeshHealthCommands.cs`
- `scripts/preflight-foundation-rebar-health-command-error-redaction.py`
- this claim file

## Excluded scope

- No changes to `GeneratedFoundationMeshHealthService`, foundation mesh generation, handle parsing semantics, project persistence, Actions dispatch, release publication, force push, or BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Replace raw exception-message composition with a stable generic command failure message while preserving existing health/locate behavior.
- Add a focused Python source preflight that rejects `ex.Message` and pins registration/read-only/live-handle/service/modeless/select/zoom/output contracts.
- Re-fetch source/preflight from current `main`, verify ancestry/readback, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects `ex.Message` from `QS3DFOUNDATIONREBARHEALTH`, focused regression source pins the existing command flow, and this claim is `COMPLETED` with exact integration evidence.