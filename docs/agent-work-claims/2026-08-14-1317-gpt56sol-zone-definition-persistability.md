# Work claim — ZoneDefinition text persistability

- Status: `COMPLETED`
- Agent: `gpt56sol-zone-definition-persistability-20260814-1317`
- Registered: `2026-08-14T13:17:00+07:00`
- Completed: `2026-08-14T13:19:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `1aeb1c3d1d7f487d8eccb2c970cc67dedb6070ab`
- Claim commit: `ec0ed999566b2a64ace7de213d8eddffaa1db555`
- Pre-write source blob: `c50d6e6e684b3f96bd0e73eccb9a47733f67907f`
- Source: `719e74e8c205151df52c42a88782ba91b97f5262`
- Regression: `30afa77de4cf2db06af41e2685a637f4323fe350`

## Confirmed defect

`ZoneDefinition` persists both immutable `Id` and mutable `Name` as QSDB XML attributes. Construction and rename shared one private `Require` helper which rejected blank text and trimmed surrounding whitespace but accepted embedded control characters. Supported construction or rename could therefore create zone state that QSDB later rejected during XML-character preflight.

The shared helper was the actual defect boundary for both persisted fields, so this lane covered only `ZoneDefinition.Id` and `ZoneDefinition.Name` together rather than splitting one source hunk into artificial claims.

## Completed change

- Preserved blank rejection and surrounding-whitespace normalization in `ZoneDefinition.Require`.
- Added control-character rejection after normalization and before id assignment/name mutation.
- Left `FloorDefinition`, active-zone services/canonicalization/audit, ProjectState revision behavior, QSDB loader/schema/migrations and all other identity models unchanged.

## Regression coverage

Added self-registering `ZoneDefinitionPersistabilitySmoke` which pins:

1. padded id/name constructor input still normalizes to canonical text;
2. valid padded rename still normalizes;
3. constructor rejects a `U+0001` zone id;
4. constructor rejects a `U+0001` zone name;
5. setter rejects a `U+0001` rename and preserves the prior name.

## Validation

Remote GitHub source diff for `719e74e8c205151df52c42a88782ba91b97f5262` confirms exactly one `ZoneDefinition.Require` hunk and shows `FloorDefinition` untouched. Remote regression diff for `30afa77de4cf2db06af41e2685a637f4323fe350` confirms the focused cases with C# `\u0001` literals. GitHub compare reports the regression SHA is ahead of the source SHA with `719e74e8c205151df52c42a88782ba91b97f5262` as merge base.

Executable .NET/native validation was **not run** in this environment because there is no local checkout/.NET/native runner. No GitHub Actions were dispatched and no BricsCAD/native/runtime PASS is claimed.

## Completion condition

Satisfied: claim-first reservation, isolated shared ZoneDefinition writer fix, focused regression source, remote diff/ancestry verification and explicit validation limitations are present on `main`.
