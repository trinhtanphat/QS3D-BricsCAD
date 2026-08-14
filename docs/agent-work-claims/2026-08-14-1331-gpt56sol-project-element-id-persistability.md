# Work claim — ProjectElement id persistability

- Status: `COMPLETED`
- Agent: `gpt56sol-project-element-id-persistability-20260814-1331`
- Registered: `2026-08-14T13:31:00+07:00`
- Completed: `2026-08-14T13:35:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `a12a51880a3b6c2764ae6891d6f1556673d73162`
- Claim commit: `b881bcf031d9d61383b82fd2d240372da014f1d8`
- Claim reconciliation: `f99a8a22b0f56c592dd7ebf47b0350d10d4d6f4f`
- Pre-write source blob: `849917da4953047414568678c7ed87d254ecb8e3`
- Source: `4414b52fcdccfd98f69f643f4fda781187e23ca1`
- Regression: `49af187faf5a383ed9cdc6af78e8859d77babd6c`

## Confirmed defect

`ProjectElement.Id` is immutable persisted element identity. The public constructor rejected blank input and trimmed surrounding whitespace, but accepted embedded control characters. QSDB serializes `element.Id` directly into the element XML `id` attribute and validates XML characters before publication, so supported construction could succeed for state that later could not be persisted.

This lane remained constructor-only. It did not change the separate QSDB serialized-load canonicality behavior, Browser/Revision element-id surfaces, relation ids or lookup semantics.

## Completed change

- Replaced the inline constructor blank/trim expression with `RequireId`.
- Preserved required-value rejection and surrounding-whitespace normalization.
- Rejected control characters in the normalized element id before immutable `Id` assignment.
- Preserved category validation, family/floor/zone relation normalization, dirty state, generated-output behavior and the independently added SetProperty/SetQuantity persistability guards.

## Regression coverage

Added self-registering `ProjectElementIdPersistabilitySmoke` which pins:

1. canonical element ids remain accepted;
2. padded element ids normalize to canonical identity;
3. valid constructor category remains unchanged;
4. valid padded FamilyId/FloorId/ZoneId inputs still normalize exactly as before;
5. embedded `U+0001` element id throws `ArgumentException` at construction.

## Validation

Remote GitHub source diff for `4414b52fcdccfd98f69f643f4fda781187e23ca1` confirms exactly two scoped hunks: the constructor callsite and the new `RequireId` helper. The whole-file write preserved the existing property-name and quantity-name control-character guards. Remote regression diff for `49af187faf5a383ed9cdc6af78e8859d77babd6c` confirms the focused constructor coverage with the real `ElementCategory.ArchitecturalWall` enum and C# `\u0001` literal. GitHub compare reports the regression SHA is ahead of source SHA `4414b52fcdccfd98f69f643f4fda781187e23ca1` with that source SHA as merge base. At the close gate, remote `main` was exactly `49af187faf5a383ed9cdc6af78e8859d77babd6c`.

Executable .NET/native validation was **not run** in this environment because there is no local checkout/.NET/native runner. No GitHub Actions were dispatched by this lane and no BricsCAD/native/runtime PASS is claimed.

## Explicit non-scope

- no QSDB loader/schema/migration changes;
- no Browser/Revision element-id canonicality changes;
- no FamilyId/FloorId/ZoneId policy changes;
- no SourceHandles/DependsOn/property/quantity changes;
- no UI/native changes.

## Completion condition

Satisfied: claim-first reservation, corrected live baseline metadata, isolated constructor-boundary fix, focused regression source, remote diff/ancestry verification and explicit validation limitations are present on `main`.
