# Work claim — FloorDefinition text persistability

- Status: `COMPLETED`
- Agent: `gpt56sol-floor-definition-persistability-20260814-1320`
- Registered: `2026-08-14T13:20:00+07:00`
- Completed: `2026-08-14T13:22:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `9d7c0c71b1b64587c6b10ef33c4b35d722094064`
- Claim commit: `ff13c47c2c3ea09d913f4af61470a592408f7808`
- Claim reconciliation: `c6645b4b8ab49fdd69402c5e15bc8109774e23d1`
- Pre-write source blob: `ba8b381b9df7656b4db97029f26a958049cf8c52`
- Source: `5e8ae1afdc3e433b7e792c692f9805cdc87dd1d9`
- Regression: `cc15eff72f89cd74920238a423c0d0cc39d241bf`

## Confirmed defect

`FloorDefinition` persists immutable `Id` and mutable `Name` as QSDB XML attributes. Both construction and rename used one private `Require` helper which rejected blank text and trimmed surrounding whitespace but accepted embedded control characters. Supported construction or rename could therefore create floor state which failed later during QSDB XML-character preflight.

This lane is text-only and distinct from the completed FloorDefinition elevation signed-zero lane; elevation semantics remained excluded.

## Completed change

- Preserved blank rejection and surrounding-whitespace normalization in `FloorDefinition.Require`.
- Added control-character rejection after normalization and before id assignment/name mutation.
- Preserved finite elevation validation and signed-zero canonicalization unchanged.
- Left Zone/Project/Family/Element/rule identity, active-floor services/canonicalization/audit, QSDB loader/schema/migrations and UI/native code unchanged.

## Regression coverage

Added self-registering `FloorDefinitionPersistabilitySmoke` which pins:

1. padded floor id/name input still normalizes to canonical text;
2. a valid negative elevation remains unchanged through construction and rename;
3. constructor rejects a `U+0001` floor id;
4. constructor rejects a `U+0001` floor name;
5. setter rejects a `U+0001` rename and preserves both prior name and elevation.

## Validation

Remote GitHub source diff for `5e8ae1afdc3e433b7e792c692f9805cdc87dd1d9` confirms exactly one `FloorDefinition.Require` hunk; the elevation setter is unchanged. Remote regression diff for `cc15eff72f89cd74920238a423c0d0cc39d241bf` confirms focused text rejection plus elevation-preservation cases with C# `\u0001` literals. GitHub compare reports the regression SHA is ahead of the source SHA with `5e8ae1afdc3e433b7e792c692f9805cdc87dd1d9` as merge base; the intervening sheet-preflight commit is unrelated to this lane.

Executable .NET/native validation was **not run** in this environment because there is no local checkout/.NET/native runner. No GitHub Actions were dispatched and no BricsCAD/native/runtime PASS is claimed.

## Completion condition

Satisfied: claim-first reservation, corrected live baseline metadata, isolated FloorDefinition shared-helper fix, focused regression source, remote diff/ancestry verification and explicit validation limitations are present on `main`.
