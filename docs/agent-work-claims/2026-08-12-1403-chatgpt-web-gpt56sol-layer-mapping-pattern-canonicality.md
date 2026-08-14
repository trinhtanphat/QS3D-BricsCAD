# Work claim — Layer mapping pattern canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-layer-mapping-pattern-canonicality-20260812-1403`
- Registered: `2026-08-12T14:03:00+07:00`
- Baseline main SHA: `ab21c0422ebd32319318f35c2d026f111526cdf1`
- Priority: owner-requested continue-all Core integrity

## Confirmed defect

QS3D wrote project layer-mapping keys from trimmed template patterns, but `ProjectRecognitionService.ValidateLayerMappings(...)` trimmed incoming project/profile patterns before validating them. `TemplateProfileStore.Load(...)` likewise read a persisted `pattern` through a trimming `Required(...)` helper. As a result, whitespace-padded layer patterns could be silently normalized during recognition/export/load instead of failing closed, and a programmatic profile could be serialized with a padded pattern even though Apply wrote the trimmed project identity.

## Owned scope

- `src/QS3D.Core/Recognition/ProjectRecognitionService.cs`
- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- `tests/QS3D.Core.SmokeTests/TemplateLayerMappingPatternCanonicalitySmoke.cs`

## Implemented contract

Every nonblank layer-mapping pattern must already equal its trimmed representation before normalized-key ambiguity checks. Persisted template XML now rejects padded `pattern` attributes before generic required-attribute parsing can trim them. Canonical patterns and existing normalized-duplicate detection remain unchanged.

## Explicit exclusions

Layer-mapping category canonicality, mapping prefix casing, template collection order, recognition confidence/rules, unrelated template fields, UI/CAD behavior, and persistence formats outside template layer mappings remain out of scope.

## Completion

- Implementation commit: `a2850033a65fe7fe18c6681a596e33b3331dcc05` (`fix(templates): require canonical layer mapping patterns`).
- Remote lineage verification: the implementation remained an ancestor of current `main` through repeated collision checks during close-out; concurrent commits touched other claim lanes rather than this source/test scope.
- Remote source verification: fetched the pushed `ProjectRecognitionService.cs`, `TemplateProfileStore.cs`, and `TemplateLayerMappingPatternCanonicalitySmoke.cs` from a descendant of the implementation and confirmed the intended blobs/content are present.
- Focused regression added: padded persisted patterns and padded programmatic profile keys are expected to fail closed; canonical pattern save/load remains accepted; invalid programmatic keys are checked before template directory creation.

## Validation actually executed

- Connector-backed commit/ancestry comparison and exact remote file-content inspection on the pushed `main` lineage.
- Static review of the focused regression and changed source contract.
- GitHub Actions were not dispatched.
- A local/full managed build or smoke executable was not run in this connector-only environment, so no managed runtime PASS is claimed.
- No licensed BricsCAD/native runtime scenario was executed for this Core-only change, so no native PASS is claimed.

## Remaining gates

The added deterministic Core smoke should run in the repository's normal managed smoke/build qualification environment. No LOCAL_ONLY or native product behavior is newly claimed by this change.
