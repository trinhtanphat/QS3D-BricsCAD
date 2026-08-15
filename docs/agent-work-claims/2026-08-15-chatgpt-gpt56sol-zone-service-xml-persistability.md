# Work claim — ProjectZoneService XML persistability

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-zone-service-xml-20260815`
- Registered: `2026-08-15T08:53:13+07:00`
- Baseline main SHA: `0542131348e6393a4d28d6e0945ec60c2ee3bff6`
- Issue: `#1469`
- Branch: `agent/chatgpt-gpt56sol/zone-service-xml-persistability-20260815`
- Priority: Core P1 persistence/failure-atomicity

## Confirmed defect

`ProjectZoneService.Required(...)` validates required/length/control-character semantics but not XML character representability. `Create(...)` can therefore admit service input that canonical QSDB cannot represent. `Update(...)` is more dangerous: after service validation and reference resolution it calls `project.Touch()` before assigning `zone.Name`; when the canonical Zone boundary rejects XML-invalid text, the service can leave project revision/timestamp advanced even though the Zone name mutation failed.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectZoneService.cs`: XML-safe `Required(...)` validation before any mutation.
- `tests/QS3D.Core.SmokeTests/ProjectZoneServiceXmlPersistabilitySmoke.cs`.
- `tests/QS3D.Core.SmokeTests/ProjectZoneServiceXmlPersistabilityRegistration.cs`.
- This claim file for handoff/closeout.

## Coordination / exclusions

- #1442 / PR #1446 owns the public `ZoneDefinition` boundary and has already been merged by the coordinator into `integration/20260815-merge-all`; this lane does not touch `ProjectState.cs`.
- No Floor/Family service, assignment semantics, serializer/schema, adapter/native, workflow/release or product documentation changes.
- No direct write/merge to `main`; stop at branch + PR unless separately authorized.

## Acceptance

- XML-invalid Zone id/name fail in service Create/Update before project/Zone mutation.
- Failed Update preserves Zone name, ChangeVersion and UpdatedUtc exactly.
- Valid supplementary Unicode survives service Create/Update and QSDB SaveNew/Load.
- Reconcile moving main and report validation without inventing managed/native PASS.
