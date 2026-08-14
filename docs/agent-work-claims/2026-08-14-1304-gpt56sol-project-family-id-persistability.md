# Work claim — ProjectFamily id persistability

- Status: `COMPLETED`
- Agent: `gpt56sol-project-family-id-persistability-20260814-1304`
- Registered: `2026-08-14T13:04:00+07:00`
- Completed: `2026-08-14T13:07:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `59d4331d75e0ad91d779955c1c314b9d4d416630`
- Claim commit: `b20e4018d0e1428c2ac7aee19813e21950d4158c`
- Claim reconciliation: `b83ac33705ecb37fb6e1c28ce3982a888af2f55e`
- Pre-write source blob: `25e761658c848ea94f589aa3f528d4cdbf041304`
- Source: `8ff27b20604bd8ad29dbaccefab1f0a668095ccb`
- Initial regression source: `bbfd07366756e4ad6ed374037e9d093114c88589`
- Final regression correction: `81c1856cedf915ec7eeb59445caa277de10275d7`

## Confirmed defect

`ProjectFamily` is the persisted Core family model. Its public constructor validated `id` only for blank input and surrounding whitespace, then exposed the normalized id as immutable relational identity. An id containing an embedded XML-invalid control character such as `U+0001` was therefore accepted by the supported domain boundary.

QSDB serializes `ProjectFamily.Id` directly into family XML attributes and runs `XmlConvert.VerifyXmlChars` before publication. The accepted family could consequently fail only at persistence time, after callers had already constructed and potentially referenced it. This was a constructor persistability gap, not direct collection corruption or Family-assignment semantic behavior.

## Completed change

- Added a dedicated `RequireId` boundary for `ProjectFamily`.
- Preserved blank rejection and surrounding-whitespace normalization.
- Reject embedded control characters before immutable `Id` assignment.
- Left family name/category, property maps, assignment/activation/delete/rename services, Zone/Floor and QSDB schema/migration behavior unchanged.

## Regression coverage

Added self-registering `ProjectFamilyIdPersistabilitySmoke` which pins:

1. canonical family ids remain accepted;
2. padded ids still normalize to canonical identity;
3. an embedded `U+0001` id throws `ArgumentException` at construction.

The first regression-source commit used a nonexistent `ElementCategory.Wall` fixture. Static remote enum inspection caught that before claim closure; correction commit `81c1856cedf915ec7eeb59445caa277de10275d7` replaces it with the real `ElementCategory.ArchitecturalWall`. Only this corrected regression source is considered final validation evidence.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectState.cs` — only `ProjectFamily` id validation.
- `tests/QS3D.Core.SmokeTests/ProjectFamilyIdPersistabilitySmoke.cs`.
- this claim file.

## Explicit non-scope

- no `ProjectFamily.Name` changes;
- no Zone/Floor/Project id or name changes;
- no Family property-map changes;
- no Family assign/activate/delete/rename service changes;
- no QSDB schema/migration changes;
- no mapping/export/UI/native changes;
- no GitHub Actions or licensed BricsCAD qualification.

## Validation

Remote GitHub diff for source commit `8ff27b20604bd8ad29dbaccefab1f0a668095ccb` confirms only the ProjectFamily constructor/id helper changed. Remote readback at corrected regression SHA `81c1856cedf915ec7eeb59445caa277de10275d7` confirms the persisted source guard and a valid `ArchitecturalWall` regression fixture with `U+0001` rejection. GitHub compare reports `81c1856cedf915ec7eeb59445caa277de10275d7` is ahead of source commit `8ff27b20604bd8ad29dbaccefab1f0a668095ccb` with that source SHA as merge base.

Executable .NET/native validation was **not run** in this environment because there is no local checkout/.NET/native runner. No GitHub Actions were dispatched and no BricsCAD/native/runtime PASS is claimed.

## Completion condition

Satisfied: claim-first reservation, live baseline reconciliation, isolated constructor-boundary fix, corrected focused regression source, remote readback/ancestry verification and explicit validation limitations are present on `main`.
