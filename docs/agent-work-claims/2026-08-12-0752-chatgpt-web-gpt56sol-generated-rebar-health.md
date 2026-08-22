# Work claim — Generated Rebar health integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web/gpt56sol-generated-rebar-health`
- Registered: `2026-08-12T07:52:00+07:00`
- Baseline main SHA: `21b199383876f900e94567c47a5faa5c89a9724e`
- Task Key: `CORE-GENERATED-REBAR-HEALTH-INTEGRITY`

## Confirmed defect

`GeneratedRebarHealthService` silently skips malformed null project elements in `Inspect`, `InspectShape`, `InspectAll`, and `BuildOwnershipIndex`. Direct health checks can therefore hide invalid project state.

## Reserved scope

- `src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs`
- one focused `scripts/preflight-*.py` gate
- this claim file

Preserve all existing valid-project rebar diagnostics and read-only behavior. Do not modify builders, Rebar Mode, notation/fabrication, ownership policy/index, CAD runtime code, or aggregate health.

## Completion condition

All four traversals fail visible on malformed null project elements, regression coverage pins the contract, and this claim is closed after merged-main readback.
