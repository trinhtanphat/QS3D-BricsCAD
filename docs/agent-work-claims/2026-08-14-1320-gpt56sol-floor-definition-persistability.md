# Work claim — FloorDefinition text persistability

- Status: `ACTIVE`
- Agent: `gpt56sol-floor-definition-persistability-20260814-1320`
- Registered: `2026-08-14T13:20:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `9d7c0c71b1b64587c6b10ef33c4b35d722094064`
- Claim commit: `ff13c47c2c3ea09d913f4af61470a592408f7808`
- Pre-write source blob: `ba8b381b9df7656b4db97029f26a958049cf8c52`

## Confirmed defect

`FloorDefinition` persists immutable `Id` and mutable `Name` as QSDB XML attributes. Both construction and rename use one private `Require` helper which rejects blank text and trims surrounding whitespace but accepts embedded control characters. Supported construction or rename can therefore create floor state which fails later during QSDB XML-character preflight.

This claim is text-only and distinct from the completed FloorDefinition elevation signed-zero lane; elevation semantics are explicitly excluded.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectState.cs` — only `FloorDefinition.Require` behavior.
- new `tests/QS3D.Core.SmokeTests/FloorDefinitionPersistabilitySmoke.cs`.
- this claim file.

## Intended change

Preserve blank rejection and surrounding-whitespace normalization for floor ids/names. Reject control characters in normalized text before immutable id assignment or name mutation. Preserve elevation finite/signed-zero behavior and all active-floor services/canonicalization/audit semantics.

## Regression plan

Focused self-registering smoke will prove canonical/padded id+name behavior, constructor rejection for control-character id/name, failed mutable name update preserves prior value, and a valid elevation remains unchanged.

## Explicit non-scope

- no FloorDefinition elevation changes;
- no Zone/Project/Family/Element/rule identity changes;
- no active-floor service/canonicalization changes;
- no QSDB loader/schema/migration changes;
- no UI/native changes;
- no GitHub Actions or licensed BricsCAD qualification.

## Validation boundary

GitHub connector read/write is available, but there is no local checkout/.NET/native runner. Executable PASS will not be claimed without independent evidence; completion requires remote diff/readback and ancestry verification.
