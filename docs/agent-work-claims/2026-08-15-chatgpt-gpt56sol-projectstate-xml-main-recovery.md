# Work claim — ProjectState persisted-text XML current-main recovery

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-projectstate-xml-main-recovery-20260815`
- Registered: `2026-08-15T10:04+07:00`
- Exact main baseline: `d521a3f95ee0ed80f12335e2f6affa59ce21fa9d`
- Issue: `#1468`
- Superseded integration-v2 PR: `#1508`
- Branch: `agent/chatgpt-gpt56sol/projectstate-xml-main-recovery-20260815`
- Priority: Core P1 persistence integrity

## Confirmed current-main defect

Current `ProjectState.cs` still accepts XML-illegal UTF-16 at persisted Zone, Floor, Family, project identity/name, drawing path/fingerprint and active Floor/Zone boundaries. Lone surrogate input can therefore enter live state that canonical QSDB XML cannot represent; mutable ProjectState fields can advance `ChangeVersion` / `UpdatedUtc` before a later persistence failure.

## Reserved recovery surfaces

- `src/QS3D.Core/Domain/ProjectState.cs`
- `tests/QS3D.Core.SmokeTests/ProjectStatePersistedTextXmlSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file

## Required recovery contract

- one `PersistedTextXml.Verify(...)` helper using `XmlConvert.VerifyXmlChars(...)` and `ArgumentException` mapping;
- preserve existing null/blank/trim/control semantics;
- guard Zone/Floor/Family ids and names, ProjectId/Name, DrawingPath/Fingerprint, ActiveZoneId/ActiveFloorId before accepted mutation;
- reject both lone high and low surrogates at every listed public boundary;
- preserve old value plus project `ChangeVersion` / `UpdatedUtc` after rejected mutable ProjectState writes;
- prove valid supplementary Unicode through exact QSDB SaveNew/Load including active resolved Floor/Zone ids;
- no ProjectElement, service, adapter/native, workflow/release, schema or product-boundary changes.

## Prior reviewed evidence

- v2 source: `9fe8de0bb0397ce9b73e15eecb6401e35deb307f`
- v2 smoke: `43510db8633912422aa086fc7117be1475a35180`
- v2 registration: `8e609d94356b5d2ec642cb01d4ede7eddd91c22c`

Implementation begins only after this claim is published. No direct main merge and no manual GitHub Actions dispatch/rerun.
