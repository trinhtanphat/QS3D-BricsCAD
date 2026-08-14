# Work claim — ProjectElement null persisted-scalar persistability

- Status: `ACTIVE`
- Agent: `gpt56sol-project-element-null-scalar-persistability-20260814-1334`
- Registered: `2026-08-14T13:34:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline observed main SHA: `f9466f3400e0c85b4702646ecf62b4d11d8f86fe`

## Confirmed defect

`ProjectElement` constructor canonicalizes nullable `familyId`, `floorId`, and `zoneId` to non-null strings, and `DrawingFingerprint` starts as `string.Empty`. After construction, however, all four are public auto-properties. Runtime callers can assign null and leave the semantic element in a representation that `QsdbProjectStore.Serialize(...)` later rewrites to `string.Empty` (`?? string.Empty`), while load also returns non-null strings.

The defect is representation/persistability only. This claim does not add relation validation or dirty/version semantics to these setters.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectElement.cs` — only storage/canonicalization for `FamilyId`, `FloorId`, `ZoneId`, `DrawingFingerprint`.
- new `tests/QS3D.Core.SmokeTests/ProjectElementNullScalarPersistabilitySmoke.cs`.
- this claim file.

## Intended change

Replace the four auto-properties with private non-null backing fields and setters that canonicalize runtime null to `string.Empty`. Preserve every non-null value exactly as the current setters do; constructor continues trimming its three relation inputs before assignment. Do not introduce implicit dirtying, reference lookup, trimming or casing changes in setter paths.

## Regression plan

Self-registering Core smoke will prove immediate null canonicalization for all four fields, exact non-null setter preservation, constructor trim behavior remains unchanged, and QSDB SaveNew -> Load preserves canonical empty values.

## Non-scope

- no relation existence/category validation;
- no dirty/UpdatedUtc mutation from these setters;
- no generated-output invalidation;
- no SourceHandles/DependsOn/property/quantity changes;
- no QSDB schema/migration/UI/native/Actions/runtime changes.

## Validation boundary

Remote source/read-back only. Executable smoke, GitHub Actions and licensed BricsCAD runtime will not be claimed as PASS unless independently executed.
