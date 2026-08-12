# Work claim — interchange semantic-reference value canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-interchange-semantic-reference-value-canonicality-20260812-0812`
- Registered: `2026-08-12T08:12:00+07:00`
- Baseline main SHA: `a9faf00389de8e4d5140005ae2f25bb59aeeffac`
- Priority: evidence-driven remote-safe semantic interchange integrity hardening during owner-requested `continue all`

## Reserved scope

Make known semantic property-reference values fail closed on leading/trailing whitespace instead of silently trimming structural identity tokens before lookup.

## Reserved files

- `src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs`
- `src/QS3D.Core/Export/ProjectInterchangeSemanticReferenceValidator.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeSemanticReferenceValidationSmoke.cs`

## Evidence

- `ProjectInterchangeJsonValidator.ValidateSemanticPropertyReferences()` currently applies `Trim()` to known property-reference values before identity lookup, so a padded identity such as `" E-WALL "` validates if `E-WALL` exists.
- `ProjectInterchangeSemanticReferenceValidator.Validate()` has the same behavior: it trims the raw known-reference value and treats the normalized identity as authoritative.
- `ProjectInterchangeValidatedSnapshotReader` preserves property values literally after mandatory validation, so accepted padded structural identities remain non-canonical in the typed snapshot.
- The same interchange boundary already rejects padding for primary/reference IDs, dependencies, source handles, categories, source scope, timestamps, property keys, quantity keys, and unit tokens.

## Excluded scope

- Arbitrary/free-text property values; only properties registered by `ProjectInterchangeSemanticReferencePolicy.KnownPropertyReferences` are structural identity references.
- Physical opening codec/cut logic and its recently completed `HostWallId` canonicality lane.
- Import/remap mutation policy, exporter collection limits, GitHub Actions, release, local build qualification, and BricsCAD V25/native runtime qualification.

## Validation plan

- Canonical known property reference to an existing target remains valid.
- Padded known property-reference value is rejected by the JSON validator with a dedicated canonicality error before typed parse.
- Direct semantic-reference validation reports the padded reference as invalid instead of resolving its trimmed target.
- Typed validated snapshot reader rejects the same padded snapshot.
- Unrelated padded free-text property remains accepted/preserved.
- Preserve existing case-insensitive identity target matching.

## Completion condition

Two validator fixes + focused regression are merged to current `main`, source/test are re-read from `main`, and this claim is marked `COMPLETED` with exact integration SHA and validation boundaries.
