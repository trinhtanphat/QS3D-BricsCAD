# Work claim — Core Bulk property-map persistability preflight

- Status: `ACTIVE`
- Agent: `gpt56sol-bulk-property-map-preflight-20260814-0853`
- Owner: OpenAI ChatGPT
- Registered: `2026-08-14T08:53:00+07:00`
- Baseline main SHA: `3aed2b5af29c33accb0e3df637e2f22e28c4e731`
- Priority: Core bulk-edit / project-element persistability integrity.
- Task key: `CORE-BULK-PROPERTY-MAP-PREFLIGHT`

## Confirmed defect

`BulkEditService.SetProperty(...)` and `MultiplyNumericProperty(...)` validate the requested editable key and correctly route committed writes through `ProjectElement.SetProperty(...)`, but they do not validate the rest of each pending target's existing `Properties` map. QSDB project validation rejects blank or leading/trailing-whitespace element property keys on save. Therefore a legacy/directly-mutated element that contains an unrelated malformed key can receive a real canonical bulk property mutation and successfully return from the semantic operation while remaining non-persistable.

This is distinct from the completed bulk-key canonicalization/freshness lanes: those validate the requested key and mutation dirtiness, not unrelated pre-existing map keys. It is also distinct from the completed Family-member preflight lane, which only covers Family mutation paths.

## Reserved scope

- `src/QS3D.Core/Services/BulkEditService.cs`
- one focused self-registering Core smoke source for generic bulk property-map preflight
- this claim file

## Intended change

For only targets that would actually mutate, preflight the complete element property-key map before entering `ProjectSemanticMutationExecutor.Execute()`. Reject blank, padded and canonical-colliding keys using the same key canonicality enforced by QSDB persistence. Preserve true no-op behavior, editable-key policy, target enumeration/ownership freshness, numeric parsing/non-finite/underflow/overflow behavior, `ProjectElement.SetProperty(...)` freshness semantics, changed-element reporting and Family assignment behavior.

## Regression intent

Cover atomic rejection for both string and numeric bulk mutations when any pending target has a malformed property key, plus malformed-target true no-op behavior and canonical happy paths. No executable/build/native PASS will be claimed unless such validation is actually available and run.

## Excluded scope

No Family assignment behavior, persistence schema/format changes, UI/BricsCAD adapters, MAP/IFC/Rebar/Cost/Measurement/release work, or unrelated agent-owned capability.