# Work claim — ProjectElement relation-id persistability

- Status: `COMPLETED`
- Agent: `gpt56sol-project-element-relation-persistability-20260814-1522`
- Registered: `2026-08-14T15:22:00+07:00`
- Completed: `2026-08-14T15:26:00+07:00`
- Workstream: `CORE / persistence-integrity`
- Priority: `P1`
- Baseline main SHA: `9c9b6921f5fce053ad9152d8c307784a2d3329fe`
- Claim commit: `8bdab10a9bff3afc55bea8ef499f6f40e17c7516`
- Pre-write source blob: `f172a9036785ad4084bd785dc3e5372c5043161d`
- Source: `1ea5bc9a0700dc5376cc5ed20097784fff5e4802`
- Regression: `edfc1bfb29b919ce61a2b5461da7c746ce9544df`

## Confirmed defect

`ProjectElement.FamilyId`, `FloorId`, and `ZoneId` are persisted optional relation identities. The completed null-scalar lane intentionally canonicalized only runtime null to `string.Empty` and explicitly preserved every non-null setter value exactly. Post-construction setters therefore accepted padded or control-character relation tokens unchanged.

QSDB save validation and the current XML schema require these optional relation identities to already be canonical. Supported domain mutation could thus create an element state that later failed persistence even though constructor inputs were trimmed.

`DrawingFingerprint` is not an identity token and intentionally retains its exact non-null text contract.

## Completed change

- Routed only `FamilyId`, `FloorId`, and `ZoneId` setter assignments through `NormalizeOptionalRelationId`.
- Runtime null remains `string.Empty`.
- Non-null relation values are trimmed before assignment.
- Embedded control characters are rejected before field assignment.
- Constructor relation normalization remains compatible because constructor assignments still pass through the same public setters after their existing trim step.
- `DrawingFingerprint` retains exact non-null text and null-to-empty behavior.
- No referential-existence, dirty/stale, timestamp, generated-output, SourceHandles/DependsOn, property, quantity, loader or schema behavior changed.

## Regression coverage

Added self-registering `ProjectElementRelationPersistabilitySmoke` which pins:

1. constructor padded FamilyId/FloorId/ZoneId values normalize as before;
2. post-construction padded relation assignments normalize to canonical tokens;
3. embedded `U+0001` is rejected atomically for FamilyId, FloorId and ZoneId;
4. null still clears each optional relation to `string.Empty`;
5. DrawingFingerprint preserves exact padded text.

## Validation

Remote source diff for `1ea5bc9a0700dc5376cc5ed20097784fff5e4802` confirms exactly 13 changed lines: three setter substitutions and one seven-line helper. The whole-file connector write did not modify any unrelated ProjectElement logic.

Remote regression diff/readback for `edfc1bfb29b919ce61a2b5461da7c746ce9544df` confirms the focused self-registering smoke with real `ElementCategory.ArchitecturalWall` and C# `\u0001` literals.

GitHub compare reports claim commit `8bdab10a9bff3afc55bea8ef499f6f40e17c7516` is the merge-base ancestor of source `1ea5bc9a0700dc5376cc5ed20097784fff5e4802`; source is likewise the merge-base ancestor of regression `edfc1bfb29b919ce61a2b5461da7c746ce9544df`. Concurrent commits between these SHAs touched unrelated persistence/mapping surfaces and did not overwrite this lane. Live source/test readback after the regression still contained the intended relation helper and smoke.

This lane did not dispatch or rerun GitHub Actions. Executable .NET smoke/build and licensed BricsCAD/native validation were **not run by this lane** in the connector-only environment, so no fresh managed/native PASS is claimed.

## Explicit non-scope

- no DrawingFingerprint normalization beyond existing null-to-empty behavior;
- no relation referential-existence enforcement;
- no dirty/stale/timestamp policy changes;
- no SourceHandles/DependsOn/property/quantity changes;
- no QSDB loader/schema/migration changes;
- no UI/native changes.

## Completion condition

Satisfied: claim-first reservation, isolated relation-token writer fix, focused regression source, remote diff/readback/ancestry verification, explicit validation limits, and completed claim metadata are present on `main`.
