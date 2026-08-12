# Work claim — Wall Rebar Health command error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-wall-rebar-health-command-error-redaction-20260812-1050`
- Registered: `2026-08-12T10:50:00+07:00`
- Baseline main SHA: `fb9e2bf36c84c8ffb340a8f94d7118cea3c42fae`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/StructuralWallMeshHealthCommands.cs` catches `System.Exception ex` at the `QS3DWALLREBARHEALTH` command boundary and constructs `"QS3DWALLREBARHEALTH lỗi: " + ex.Message`, then writes it to Palette status and Editor output. Raw exception details can expose filesystem/provider/environment information.

## Reserved scope

- Redact raw exception-message reflection from the `QS3DWALLREBARHEALTH` top-level catch.
- Preserve command registration, read-only project access, generated wall-mesh handle collection, live-solid inspection, `GeneratedWallMeshHealthService` inspection, health summary, capped issue output, and both output sinks.
- Add one focused static regression preflight for this command boundary.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/StructuralWallMeshHealthCommands.cs`
- `scripts/preflight-wall-rebar-health-command-error-redaction.py`
- this claim file

## Excluded scope

- No changes to wall-mesh handle canonicality/service semantics, generation, project persistence, Actions dispatch, release publication, force push, or BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch current source after claim registration before editing and preserve any concurrent wall-mesh canonicality work.
- Replace raw exception-message composition with a stable generic command failure message while preserving existing health reporting behavior.
- Add a focused Python source preflight that rejects `ex.Message` and pins registration/read-only/handle/live-service/output-cap/output-sink contracts.
- Re-fetch source/preflight from current `main`, verify ancestry/readback, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects `ex.Message` from `QS3DWALLREBARHEALTH`, focused regression source pins the existing command flow, and this claim is `COMPLETED` with exact integration evidence.