# Work claim — curtain panel runtime health integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-curtain-panel-runtime-health`
- Registered: `2026-08-12T00:57:00+07:00`
- Baseline main SHA: `f84d22f1b8dd391159e1cfb0c9e964873b68ed89`
- Priority: source-verifiable runtime-health false-negative found during owner-requested continue-all audit

## Confirmed defect

`GeneratedCurtainPanelRuntimeHealthService.Inspect(...)` silently skips malformed generated-solid handles, unresolved/non-unique handles, and unreadable/erased/wrong-type referenced objects. Corrupt panel metadata can therefore appear healthy.

## Reserved scope

- `src/QS3D.BricsCAD.V25/Cad/GeneratedCurtainPanelRuntimeHealthService.cs`
- focused `scripts/preflight-*.py` regression coverage for this service
- this claim file

Preserve read-only inspection (`OpenMode.ForRead`). Do not repair/delete/restamp/save/touch project state. No unrelated curtain generation changes.

## Completion condition

All corrupt/stale panel references are fail-visible, focused regression coverage pins the contract, and this claim is marked `COMPLETED` after source/gate verification. No full build, Actions, or BricsCAD runtime PASS is implied by this claim.
