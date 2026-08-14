# Work claim — ProjectState id persistability

- Status: `COMPLETED`
- Agent: `gpt56sol-project-id-persistability-20260814-1314`
- Registered: `2026-08-14T13:14:00+07:00`
- Completed: `2026-08-14T13:16:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `9eeed67755880caa9c2918689fbb3675157529b3`
- Claim commit: `7021fdc75b8972042e9ab238d18bc12553a8b461`
- Pre-write source blob: `b22039230526e3465bc5568c3396c118dbb3203e`
- Source: `e749217c94ca73b73e7ef0cc5022e6c0dfb99acf`
- Regression: `15196a6c5590bcdadc0eba1c11f211eb15a67817`

## Confirmed defect

`ProjectState.ProjectId` is immutable persisted project identity. The public constructor rejected blank input and trimmed surrounding whitespace, but accepted embedded control characters. QSDB serializes `ProjectId` directly into the root XML attribute and verifies XML characters before publication, so construction could succeed for state which later could not be saved.

This is distinct from the completed `QSDB ProjectId canonicality` lane from 2026-08-12. That lane hardened malformed serialized input on load (for example padded XML `projectId`) and explicitly scoped itself to `QsdbProjectStore`; this completed lane is constructor-only writer persistability for in-memory project identity.

## Completed change

- Replaced the inline constructor blank/trim expression with `RequireProjectId`.
- Preserved required-value rejection and surrounding-whitespace normalization.
- Rejected control characters in the normalized project id before immutable `ProjectId` assignment.
- Left QSDB serialized-load canonicality, project display name/freshness behavior and all other persisted scalar setters unchanged.

## Regression coverage

Added self-registering `ProjectIdPersistabilitySmoke` which pins:

1. canonical project ids remain accepted;
2. padded constructor ids still normalize to canonical identity;
3. an embedded `U+0001` project id throws `ArgumentException` during construction.

## Validation

Remote GitHub source diff for `e749217c94ca73b73e7ef0cc5022e6c0dfb99acf` confirms only the ProjectState constructor callsite and `RequireProjectId` helper changed. Remote regression diff for `15196a6c5590bcdadc0eba1c11f211eb15a67817` confirms the focused constructor coverage with a C# `\u0001` literal. GitHub compare reports the regression SHA is ahead of the source SHA with `e749217c94ca73b73e7ef0cc5022e6c0dfb99acf` as merge base.

Executable .NET/native validation was **not run** in this environment because there is no local checkout/.NET/native runner. No GitHub Actions were dispatched and no BricsCAD/native/runtime PASS is claimed.

## Explicit non-scope

- no QSDB loader/schema/migration changes;
- no ProjectState.Name changes;
- no DrawingPath/DrawingFingerprint/ActiveZoneId/ActiveFloorId changes;
- no Zone/Floor/Family/Element/rule identity changes;
- no mapping/export/UI/native changes.

## Completion condition

Satisfied: claim-first reservation, isolated ProjectId writer fix, focused regression source, remote diff/ancestry verification and explicit validation limitations are present on `main`.
