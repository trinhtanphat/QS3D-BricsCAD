# Work claim — generated solid null element health

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-generated-solid-null-element-health-20260812-0845`
- Registered: `2026-08-12T08:45:00+07:00`
- Baseline main SHA: `d8f69c5ab440e65e5943dda46ca83dc440d03e41`
- Priority: owner-requested continue-all residual runtime-health false-clean hardening

## Confirmed defect

`GeneratedSolidRuntimeHealthService.InspectGeneratedSolidOwnership(...)` currently executes `if (element == null) continue;`. A malformed `ProjectState` containing a null semantic element can therefore be partially inspected while the generated-solid ownership provider silently omits the malformed entry. This conflicts with the fail-visible null-entry contract now used by sibling Core health services. The outer `AddProviderSafely(...)` boundary already converts recoverable provider exceptions into `RUNTIME_HEALTH_PROVIDER_FAILED`, so the provider can reject a null element without aborting aggregate runtime health.

## Reserved scope

- Make generated-solid ownership inspection reject null `project.Elements` entries instead of silently skipping them.
- Preserve provider isolation: the outer runtime-health aggregator must continue translating recoverable provider failures into diagnostics.
- Preserve all existing generated-solid empty-handle, malformed/unresolved/unreadable/erased/type/ownership diagnostics and strict read-only behavior.
- Add one focused static regression preflight for the null-element fail-visible contract.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs`
- `scripts/preflight-generated-solid-null-element-health.py`
- this claim file

## Excluded scope

- No changes to project persistence or null-entry repair.
- No sibling health/provider changes.
- No generated geometry mutation path changes.
- No GitHub Actions, release publication, force push, or licensed BricsCAD V25/V26 runtime PASS claim.

## Validation plan

- Re-fetch current source after claim registration before editing.
- Replace the null-element silent skip inside generated-solid ownership inspection with a deterministic exception; retain `AddProviderSafely` recoverable isolation.
- Add a source preflight that rejects the old null skip, requires the fail-visible null guard, and confirms `RUNTIME_HEALTH_PROVIDER_FAILED` remains in the outer service.
- Re-fetch source/preflight from current `main`, verify ancestry, and close with exact SHAs.

## Completion condition

Completed only when current `main` no longer silently skips null semantic elements during generated-solid ownership health, provider isolation remains intact, focused regression source pins the contract, and this claim is `COMPLETED` with exact integration evidence.
