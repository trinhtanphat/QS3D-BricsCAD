# Work claim — semantic tag runtime health integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-semantic-tag-runtime-health`
- Registered: `2026-08-12T00:57:00+07:00`
- Baseline main SHA: `f84d22f1b8dd391159e1cfb0c9e964873b68ed89`
- Priority: source-verifiable runtime-health false-negative found during owner-requested continue-all audit

## Confirmed defect

`GeneratedSemanticTagRuntimeHealthService.Inspect(...)` silently skips a selected persisted handle when it is not valid hexadecimal text. Corrupt semantic-tag metadata can therefore appear healthy instead of surfacing a diagnostic.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Cad/GeneratedSemanticTagRuntimeHealthService.cs`
- focused `scripts/preflight-*.py` regression coverage for this service
- this claim file

Preserve existing handle-selection semantics and read-only inspection (`OpenMode.ForRead`). Do not repair/delete/restamp/save/touch project state. No unrelated tag generation changes.

## Completion condition

Malformed selected handles are fail-visible, focused regression coverage pins the contract, and this claim is marked `COMPLETED` after source/gate verification. No full build, Actions, or BricsCAD runtime PASS is implied by this claim.
