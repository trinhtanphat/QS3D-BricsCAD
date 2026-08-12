# Work claim — Revision snapshot required element sections

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-12T11:12:00+07:00`
- Baseline main SHA: `2b3e9b9d612f486fa85130ef90f67e824472658d`
- Priority: continue-all remote-safe persisted-state integrity

## Reserved scope

Make each revision `<element>` require the four canonical collection containers emitted by the serializer: `<properties>`, `<quantities>`, `<sourceHandles>`, and `<dependencies>`. The loader currently treats any missing container as an empty collection and later serialization restores it, silently normalizing malformed current-schema input.

## Expected surfaces

- `src/QS3D.Core/Revisions/RevisionSnapshotXmlSchemaValidator.cs`
- `scripts/preflight-revision-snapshot-schema.py`
- this claim file

## Excluded scope

- No revision comparison/report semantics changes.
- No QSDB project-store schema changes.
- No BricsCAD runtime/UI changes.
- No GitHub Actions dispatch.

## Validation planned

- Require exactly one of all four canonical collection containers per revision element.
- Extend focused preflight to reject missing or duplicate element containers while retaining existing namespace/content guards.
