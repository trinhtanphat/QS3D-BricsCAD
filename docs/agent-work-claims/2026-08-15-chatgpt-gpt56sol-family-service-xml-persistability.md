# Work claim — ProjectFamilyService XML persistability

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-family-service-xml-20260815`
- Registered: `2026-08-15T09:11:37+07:00`
- Current main SHA: `1eb5a757845ac1e978b3a9dccb33f439f9dfa46f`
- Integration-v2 baseline: `db571da244531213d986617220996740f4c5b878`
- Issue: `#1491`
- Branch: `agent/chatgpt-gpt56sol/family-service-xml-persistability-20260815`
- Intended PR target: `integration/20260815-merge-all-v2`
- Priority: Core P1 persistence/failure-atomicity

## Confirmed defect

On the current integration-v2 candidate, `ProjectFamilyService.Required(...)` validates required/trimmed-length/control-character semantics but not XML character representability. The helper guards Family id/name inputs for Create/Duplicate/Rename/lookup and Family property keys. `Rename(...)` calls `project.Touch()` before assigning `family.Name`, so once the public ProjectFamily boundary is XML-safe under #1468, an XML-invalid service rename can advance project revision/timestamp and then throw. The current candidate can also accept such text into state that canonical QSDB XML cannot represent.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`: XML-safe `Required(...)` only, preserving the existing Family property-value XML validation imported through #1479.
- `tests/QS3D.Core.SmokeTests/ProjectFamilyServiceXmlPersistabilitySmoke.cs`.
- `tests/QS3D.Core.SmokeTests/ProjectFamilyServiceXmlPersistabilityRegistration.cs`.
- This claim file for handoff.

## Coordination / exclusions

- #1468 owns `ProjectState.cs` and the public ProjectFamily id/name contract; no `ProjectState.cs` edits here.
- #1422/#1479 owns Family property-value XML safety; preserve it unchanged.
- #1474/#1483 owns Floor service; #1469/#1470 owns Zone service.
- No Family assignment business-rule changes, element semantics, serializer/schema, adapter/native, workflow/release or unrelated product changes.
- No direct mutation/merge of integration or main refs; task branch + PR only.
- No manual Actions dispatch/rerun; no managed/native PASS inferred.

## Acceptance

- XML-invalid Family ids/names/lookup ids and property keys fail at the service boundary before mutation.
- Failed Rename preserves Family name, ChangeVersion and UpdatedUtc exactly.
- Existing Family property-value validation remains unchanged.
- Valid supplementary Unicode survives Family service Create/Rename and QSDB SaveNew/Load exactly.
- Final diff/readback proves the task is additive to, not a regression of, the current integration-v2 Family property guard.
