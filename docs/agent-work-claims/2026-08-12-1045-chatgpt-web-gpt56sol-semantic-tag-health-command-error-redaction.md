# Work claim — Semantic Tag Health command error redaction

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-semantic-tag-health-command-error-redaction-20260812-1045`
- Registered: `2026-08-12T10:45:00+07:00`
- Baseline main SHA: `f62d6c07de99dfcecc6aad98c8ca130f25a95c2e`
- Priority: owner-requested continue-all residual diagnostic privacy hardening

## Confirmed defect

`src/QS3D.BricsCAD.V25/SemanticTagHealthCommands.cs` catches `Exception ex` at the `QS3DTAGHEALTH` command boundary and constructs `"QS3DTAGHEALTH lỗi: " + ex.Message`, then reports it to Palette status and Editor output. Raw exception details can expose filesystem/provider/environment information.

## Reserved scope

- Redact raw exception-message reflection from the `QS3DTAGHEALTH` top-level catch.
- Preserve command registration, read-only project access, persisted/runtime tag health aggregation and de-duplication, PASS path, issue output cap, semantic-tag locate selection, and both error report sinks.
- Add one focused static regression preflight for this command boundary.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/SemanticTagHealthCommands.cs`
- `scripts/preflight-semantic-tag-health-command-error-redaction.py`
- this claim file

## Excluded scope

- No changes to semantic tag generation, health service/runtime semantics, issue de-duplication, handle keys, project persistence, Actions dispatch, release publication, force push, or BricsCAD runtime PASS claim.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Replace raw exception-message composition with a stable generic command failure message while preserving tag health/locate behavior.
- Add a focused Python source preflight that rejects `ex.Message` and pins registration/read-only/health aggregation/PASS/output-cap/locate/error-output contracts.
- Re-fetch source/preflight from current `main`, verify ancestry/readback, then close with exact SHAs.

## Completion condition

Completed only when current `main` no longer reflects `ex.Message` from `QS3DTAGHEALTH`, focused regression source pins the existing command flow, and this claim is `COMPLETED` with exact integration evidence.