# Work claim — ProjectState id persistability

- Status: `ACTIVE`
- Agent: `gpt56sol-project-id-persistability-20260814-1314`
- Registered: `2026-08-14T13:14:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `9eeed67755880caa9c2918689fbb3675157529b3`
- Pre-write source blob: `b22039230526e3465bc5568c3396c118dbb3203e`

## Confirmed defect

`ProjectState.ProjectId` is immutable persisted project identity. The public constructor currently rejects blank input and trims surrounding whitespace, but accepts embedded control characters. QSDB serializes `ProjectId` directly into the root XML attribute and verifies XML characters before publication, so construction can succeed for state which later cannot be saved.

This is distinct from the completed `QSDB ProjectId canonicality` lane from 2026-08-12. That lane hardened malformed serialized input on load (for example padded XML `projectId`) and explicitly scoped itself to `QsdbProjectStore`. This claim is constructor-only writer persistability for in-memory project identity.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectState.cs` — only ProjectState project-id construction/validation.
- new `tests/QS3D.Core.SmokeTests/ProjectIdPersistabilitySmoke.cs`.
- this claim file.

## Intended change

Preserve required-value rejection and surrounding-whitespace normalization for supported constructor input. Reject control characters in the normalized project id before immutable `ProjectId` assignment. Do not alter serialized-load canonicality, project display name, revision/freshness behavior or other project scalar setters.

## Regression plan

Focused self-registering smoke will prove:

1. canonical project ids remain accepted;
2. padded constructor ids still normalize to canonical identity;
3. an embedded `U+0001` project id throws `ArgumentException` at construction.

## Explicit non-scope

- no QSDB loader/schema/migration changes;
- no ProjectState.Name changes;
- no DrawingPath/DrawingFingerprint/ActiveZoneId/ActiveFloorId changes;
- no Zone/Floor/Family/Element/rule identity changes;
- no mapping/export/UI/native changes;
- no GitHub Actions or licensed BricsCAD qualification.

## Validation boundary

GitHub connector read/write is available, but this environment has no local checkout/.NET/native runner. Executable PASS will not be claimed without independent evidence. Completion requires remote source/test diff/readback and ancestry verification on current `main`.
