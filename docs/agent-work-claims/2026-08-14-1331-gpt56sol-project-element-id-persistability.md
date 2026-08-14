# Work claim — ProjectElement id persistability

- Status: `ACTIVE`
- Agent: `gpt56sol-project-element-id-persistability-20260814-1331`
- Registered: `2026-08-14T13:31:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `a12a51880a3b6c2764ae6891d6f1556673d73162`
- Claim commit: `b881bcf031d9d61383b82fd2d240372da014f1d8`
- Pre-write source blob: `849917da4953047414568678c7ed87d254ecb8e3`

## Confirmed defect

`ProjectElement.Id` is immutable persisted element identity. The public constructor rejects blank input and trims surrounding whitespace, but accepts embedded control characters. QSDB serializes `element.Id` directly into the element XML `id` attribute and validates XML characters before publication, so supported construction can succeed for state that later cannot be persisted.

This lane is constructor-only. It does not change the separate QSDB serialized-load canonicality behavior, Browser/Revision element-id surfaces, relation ids, or lookup semantics.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectElement.cs` — only ProjectElement id construction/validation.
- new `tests/QS3D.Core.SmokeTests/ProjectElementIdPersistabilitySmoke.cs`.
- this claim file.

## Intended change

Preserve required-value rejection and surrounding-whitespace normalization, but reject control characters in the normalized element id before immutable `Id` assignment. Preserve category validation, family/floor/zone relation normalization, dirty state and generated-output behavior.

## Regression plan

Focused self-registering smoke will prove:

1. canonical element ids remain accepted;
2. padded constructor ids still normalize to canonical identity;
3. embedded `U+0001` id throws `ArgumentException` at construction;
4. category/relation constructor behavior remains unchanged for valid input.

## Explicit non-scope

- no QSDB loader/schema/migration changes;
- no Browser/Revision element-id canonicality changes;
- no FamilyId/FloorId/ZoneId policy changes;
- no SourceHandles/DependsOn/property/quantity changes;
- no UI/native changes;
- no GitHub Actions or licensed BricsCAD qualification.

## Validation boundary

GitHub connector read/write is available, but there is no local checkout/.NET/native runner. Executable PASS will not be claimed without independent evidence; completion requires remote diff/readback and ancestry verification.
