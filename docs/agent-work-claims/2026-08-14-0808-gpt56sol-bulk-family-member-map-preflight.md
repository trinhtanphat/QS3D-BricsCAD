# Work claim — Core Bulk Family member property-map preflight

- Status: `ACTIVE`
- Agent: `gpt56sol-bulk-family-member-map-20260814-0808`
- Registered: `2026-08-14T08:08:00+07:00`
- Baseline main SHA: `60806a43aa04408b17e1793e1c6eddd06bc268d6`
- Priority: Core bulk-edit / Family assignment integrity.

## Confirmed defect

`BulkEditService.AssignFamily()` validates target and previous Family default maps plus ownership/category/freshness, but actual pending elements' own `Properties` maps are not canonicality-preflighted before inherited-default planning and mutation. A legacy/directly-mutated pending element with a padded/blank property key can retain malformed state while receiving canonical target defaults. `ProjectSemanticMutationExecutor` provides rollback on thrown failures but this path supplies no persistability pre-commit validation, so the malformed mixed state can commit successfully.

## Reserved scope

- `src/QS3D.Core/Services/BulkEditService.cs`
- `src/QS3D.Core/Domain/ProjectFamilyService.cs` only to expose the existing member-map validator internally for canonical reuse
- `tests/QS3D.Core.SmokeTests/BulkFamilyMemberPropertyMapPreflightSmoke.cs`
- this claim file only

## Intended change

Reuse the existing Family-member property-key canonicality validator for actual pending bulk Family assignments after the existing no-op exit and before `ProjectSemanticMutationExecutor.Execute()`. Preserve already-assigned no-op behavior, all-or-nothing category/ownership checks, inherited-default replacement, explicit overrides and dirty semantics. Add focused atomicity regression coverage.

## Excluded scope

No generic bulk SetProperty/MultiplyNumericProperty behavior, Family Manager/UI, persistence schema, Cost/Measurement, MAP/IFC, Rebar, V25 release/native/source-handle surfaces, or other agent-owned capability.
