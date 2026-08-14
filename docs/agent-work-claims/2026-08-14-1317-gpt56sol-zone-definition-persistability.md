# Work claim — ZoneDefinition text persistability

- Status: `ACTIVE`
- Agent: `gpt56sol-zone-definition-persistability-20260814-1317`
- Registered: `2026-08-14T13:17:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline: `1aeb1c3d1d7f487d8eccb2c970cc67dedb6070ab`
- Pre-write source blob: `c50d6e6e684b3f96bd0e73eccb9a47733f67907f`

## Confirmed defect

`ZoneDefinition` persists both immutable `Id` and mutable `Name` as QSDB XML attributes. Construction and rename share one private `Require` helper which currently rejects blank text and trims surrounding whitespace but accepts embedded control characters. Therefore supported construction or rename can create zone state that QSDB later rejects during XML-character preflight.

The shared helper is the defect boundary for both persisted fields, so this claim intentionally covers only `ZoneDefinition.Id` and `ZoneDefinition.Name` together rather than splitting one source hunk into artificial competing claims.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectState.cs` — only `ZoneDefinition.Require` behavior.
- new `tests/QS3D.Core.SmokeTests/ZoneDefinitionPersistabilitySmoke.cs`.
- this claim file.

## Intended change

Preserve blank rejection and surrounding-whitespace normalization for zone ids/names. Reject control characters in normalized text before immutable id assignment or name mutation. Preserve all active-zone selection/canonicalization/audit semantics and ProjectState revision behavior.

## Regression plan

Focused self-registering smoke will prove canonical/padded id+name behavior, constructor rejection for control-character id/name, and failed mutable name update preserves prior value.

## Explicit non-scope

- no FloorDefinition changes;
- no Project/Family/Element/rule identity changes;
- no active-zone service/canonicalization changes;
- no QSDB loader/schema/migration changes;
- no UI/native changes;
- no GitHub Actions or licensed BricsCAD qualification.

## Validation boundary

GitHub connector read/write is available, but there is no local checkout/.NET/native runner. Executable PASS will not be claimed without independent evidence; completion requires remote diff/readback and ancestry verification.
