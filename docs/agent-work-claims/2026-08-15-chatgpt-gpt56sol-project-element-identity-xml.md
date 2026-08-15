# Work claim — ProjectElement persisted identity XML representability

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-element-identity-xml-20260815`
- Registered: `2026-08-15T08:39:58+07:00`
- Baseline main SHA: `e9faeedbf251e5a012168cbb2c964d9f74812fa3`
- Issue: `#1454`
- Branch: `agent/chatgpt-gpt56sol/project-element-identity-xml-20260815`
- Priority: Core P1 persistence integrity

## Confirmed defect

`ProjectElement` persists `Id`, optional relation IDs (`FamilyId`, `FloorId`, `ZoneId`) and `DrawingFingerprint` into QSDB XML, but the public validators currently enforce only blank/control-character rules and trimming. XML-illegal UTF-16 such as an unpaired surrogate can therefore be accepted in canonical in-memory state and rejected only later by `QsdbProjectStore.Save*`.

The earlier `ProjectElement.Id` persistability fix `4414b52fcdccfd98f69f643f4fda781187e23ca1` added control-character rejection but did not preflight XML representability; this claim is the narrow follow-up for that remaining gap plus the adjacent persisted relation/fingerprint fields in the same class.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectElement.cs`: XML-safe validation only for `Id`, optional relation IDs and `DrawingFingerprint`.
- `tests/QS3D.Core.SmokeTests/ProjectElementIdentityXmlPersistabilitySmoke.cs`.
- `tests/QS3D.Core.SmokeTests/ProjectElementIdentityXmlPersistabilityRegistration.cs`.
- This claim file for handoff/closeout only.

## Excluded scope

- No `ProjectState.cs` changes.
- No property, quantity, `SourceHandles`, `DependsOn`, generated-output or dirty-flag behavior changes.
- No RevisionService, adapter/native, workflow/release or product documentation changes.
- No overlap with #1411 or broad #1443.
- No direct write or merge to `main`; normal-agent stop point is branch + PR unless separately authorized.

## Acceptance

- XML-invalid element id/relation ids/fingerprint fail at the public boundary.
- Failed relation/fingerprint setters preserve the prior value exactly.
- Valid supplementary Unicode remains accepted and survives QSDB `SaveNew` → `Load` exactly for the covered fields.
- Reconcile against moving `main`, inspect final diff/readback, and report validation without inventing managed/native PASS.
