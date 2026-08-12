# Work claim — Layer mapping pattern canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-layer-mapping-pattern-canonicality-20260812-1403`
- Registered: `2026-08-12T14:03:00+07:00`
- Baseline main SHA: `ab21c0422ebd32319318f35c2d026f111526cdf1`
- Priority: owner-requested continue-all Core integrity

## Confirmed defect

QS3D writes project layer-mapping keys from trimmed template patterns, but `ProjectRecognitionService.ValidateLayerMappings(...)` trims incoming project/profile patterns before validating them. `TemplateProfileStore.Load(...)` likewise reads a persisted `pattern` through a trimming `Required(...)` helper. As a result, whitespace-padded layer patterns can be silently normalized during recognition/export/load instead of failing closed, and a programmatic profile can be serialized with a padded pattern even though Apply writes the trimmed project identity.

## Owned scope

- `src/QS3D.Core/Recognition/ProjectRecognitionService.cs`
- `src/QS3D.Core/Templates/TemplateProfileStore.cs`
- focused Core smoke coverage for layer-mapping pattern canonicality

## Intended contract

Require every nonblank layer-mapping pattern to already equal its trimmed representation before normalized-key ambiguity checks. Persisted template XML must reject padded `pattern` attributes rather than trimming them. Canonical patterns and existing normalized-duplicate detection remain unchanged.

## Explicit exclusions

Layer-mapping category canonicality, mapping prefix casing, template collection order, recognition confidence/rules, unrelated template fields, UI/CAD behavior, and persistence formats outside template layer mappings are out of scope.

## Completion condition

Narrow source fix and focused regression are integrated on current `main`, same-file concurrent edits are reconciled, and this claim is closed with exact source/regression SHAs and remote validation boundaries. No GitHub Actions/full build/licensed BricsCAD runtime PASS is implied by this claim.
