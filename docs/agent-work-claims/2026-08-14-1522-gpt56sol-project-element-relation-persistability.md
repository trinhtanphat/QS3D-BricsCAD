# Work claim — ProjectElement relation-id persistability

- Status: `ACTIVE`
- Agent: `gpt56sol-project-element-relation-persistability-20260814-1522`
- Registered: `2026-08-14T15:22:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline observed main SHA: `9c9b6921f5fce053ad9152d8c307784a2d3329fe`
- Pre-write source blob: `f172a9036785ad4084bd785dc3e5372c5043161d`

## Confirmed defect

`ProjectElement.FamilyId`, `FloorId`, and `ZoneId` are persisted optional relation identities. The completed null-scalar lane intentionally canonicalized only runtime null to `string.Empty` and explicitly preserved every non-null setter value exactly. Current post-construction setters therefore accept padded or control-character relation tokens unchanged.

QSDB save validation and the current XML schema require these optional relation identities to already be canonical. Supported domain mutation can thus create an element state that later fails persistence even though constructor inputs are trimmed.

`DrawingFingerprint` is not an identity token and intentionally retains its exact non-null text contract.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectElement.cs` — only `FamilyId`, `FloorId`, and `ZoneId` setter lexical normalization plus a dedicated helper.
- new `tests/QS3D.Core.SmokeTests/ProjectElementRelationPersistabilitySmoke.cs`.
- this claim file.

## Intended change

For the three optional relation setters only: runtime null remains `string.Empty`; non-null values are trimmed; embedded control characters are rejected before assignment. Preserve constructor relation normalization, ProjectElement Id/category behavior, and exact DrawingFingerprint text.

Do not add referential-existence checks because project catalogs may be assembled in stages and QSDB save already owns orphan-reference validation. Do not introduce new dirty/stale/timestamp semantics in this lexical-persistability lane.

## Regression plan

Focused self-registering Core smoke will prove:

1. constructor and post-construction padded FamilyId/FloorId/ZoneId values normalize to canonical tokens;
2. embedded `U+0001` is rejected atomically for each relation setter;
3. null still clears each relation to `string.Empty`;
4. DrawingFingerprint still preserves exact padded text;
5. no relation setter test asserts or changes dirty/generated-output behavior in this lane.

## Explicit non-scope

- no DrawingFingerprint normalization beyond existing null-to-empty behavior;
- no relation referential-existence enforcement;
- no dirty/stale/timestamp policy changes;
- no SourceHandles/DependsOn/property/quantity changes;
- no QSDB loader/schema/migration changes;
- no UI/native changes;
- no GitHub Actions dispatch or licensed BricsCAD qualification.

## Validation boundary

Remote GitHub diff/readback and ancestry verification only unless independent executable evidence becomes available. This lane will not dispatch Actions and will not claim fresh .NET/native PASS without an independently executed runner.
