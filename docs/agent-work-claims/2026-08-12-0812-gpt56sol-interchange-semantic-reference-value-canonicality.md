# Work claim — interchange semantic-reference value canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-interchange-semantic-reference-value-canonicality-20260812-0812`
- Registered: `2026-08-12T08:12:00+07:00`
- Baseline main SHA: `a9faf00389de8e4d5140005ae2f25bb59aeeffac`
- Priority: evidence-driven remote-safe semantic interchange integrity hardening during owner-requested `continue all`
- Integration commit: `529b885f1dd2562dea56640aa1fa0e5067e0b427`

## Reserved scope

Make known semantic property-reference values fail closed on leading/trailing whitespace instead of silently trimming structural identity tokens before lookup.

## Reserved files

- `src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs`
- `src/QS3D.Core/Export/ProjectInterchangeSemanticReferenceValidator.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeSemanticReferenceValidationSmoke.cs`

## Evidence

- `ProjectInterchangeJsonValidator.ValidateSemanticPropertyReferences()` previously applied `Trim()` to known property-reference values before identity lookup, so a padded identity such as `" E-WALL "` validated if `E-WALL` existed.
- `ProjectInterchangeSemanticReferenceValidator.Validate()` had the same behavior: it trimmed the raw known-reference value and treated the normalized identity as authoritative.
- `ProjectInterchangeValidatedSnapshotReader` preserves property values literally after mandatory validation, so accepted padded structural identities would remain non-canonical in the typed snapshot.
- The same interchange boundary already rejects padding for primary/reference IDs, dependencies, source handles, categories, source scope, timestamps, property keys, quantity keys, and unit tokens.

## Excluded scope

- Arbitrary/free-text property values; only properties registered by `ProjectInterchangeSemanticReferencePolicy.KnownPropertyReferences` are structural identity references.
- Physical opening codec/cut logic and its recently completed `HostWallId` canonicality lane.
- Import/remap mutation policy, exporter collection limits, GitHub Actions, release, local build qualification, and BricsCAD V25/native runtime qualification.

## Validation evidence

- PR #644 was reviewed against current `main`; live compare showed only the two reserved validators and focused smoke test differed from `main`.
- PR #644 was squash-merged with expected head `3a55dedb4114e86ab38547fbe85bdc72fdf39092`.
- Main integration commit: `529b885f1dd2562dea56640aa1fa0e5067e0b427`.
- Canonical existing-target references and case-insensitive lookup remain supported; padded structural references fail closed; unrelated padded free text remains preserved by the focused regression source.
- No GitHub Actions/build/release were dispatched and no local .NET or BricsCAD V25/V26 runtime PASS is claimed.

## Completion condition

`COMPLETED`: two validator fixes + focused regression are merged to current `main` at `529b885f1dd2562dea56640aa1fa0e5067e0b427`, with the validation boundary above recorded truthfully.
