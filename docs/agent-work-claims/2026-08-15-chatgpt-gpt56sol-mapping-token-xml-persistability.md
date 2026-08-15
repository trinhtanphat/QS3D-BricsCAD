# Work claim — Measurement/work-item mapping token XML persistability

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-mapping-token-xml-20260815`
- Registered: `2026-08-15T08:46:51+07:00`
- Baseline main SHA: `96862d6cdfddd8bb2ea4a0055505005859467ea7`
- Issue: `#1460`
- Branch: `agent/chatgpt-gpt56sol/mapping-token-xml-persistability-20260815`
- Priority: Core P1 persistence integrity

## Confirmed defect

`MeasurementWorkItemMappingContract.RequireToken(...)` validates blank/trim/control-character semantics but not XML character representability. Mapping tokens are persisted through reserved project metadata: `MappingId` becomes part of the metadata key and the other three identifiers become fields in the metadata value. The owned metadata write path deliberately bypasses generic public metadata XML validation, so XML-illegal UTF-16 can enter canonical reserved metadata after the project revision is touched and only fail during later QSDB serialization.

## Reserved scope

- `src/QS3D.Core/Mapping/MeasurementWorkItemMapping.cs`: XML-safe mapping token validation only.
- `tests/QS3D.Core.SmokeTests/MeasurementWorkItemMappingTokenXmlPersistabilitySmoke.cs`.
- `tests/QS3D.Core.SmokeTests/MeasurementWorkItemMappingTokenXmlPersistabilityRegistration.cs`.
- This claim file for handoff/closeout only.

## Excluded scope

- No metadata dictionary, mapping codec/collection mutation/version changes.
- No ProjectElement/ProjectState, measurement calculation, adapter/native, workflow/release or product documentation changes.
- No GitHub Actions dispatch/rerun and no licensed runtime claim.
- No direct write/merge to `main`; normal-agent stop point is branch + PR unless separately authorized.

## Acceptance

- XML-invalid mapping id, measurement item id, classification id and work-item id fail at mapping construction before project/metadata mutation.
- Existing blank/trim/control-character rules remain unchanged.
- Valid supplementary Unicode survives mapping add and exact QSDB SaveNew/Load round-trip.
- Reconcile moving `main`, inspect final diff/readback, and report validation truthfully without inventing managed/native PASS.
