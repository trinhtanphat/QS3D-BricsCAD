# Work claim — interchange unit token canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-interchange-unit-token-canonicality-20260812-0747`
- Registered: `2026-08-12T07:47:00+07:00`
- Completed: `2026-08-12T07:54:00+07:00`
- Baseline main SHA: `0696f3cbcf602e140c3cad23282160641f2e659d`
- Integration SHA: `5d803ac9a835352f0d57c4f028e7294d132241b5`
- PR: `#630`
- Priority: evidence-driven remote-safe interchange integrity hardening during owner-requested `continue all`

## Reserved scope

Make canonical semantic snapshot unit tokens fail closed on leading/trailing whitespace instead of accepting padded structural tokens through validator/typed-reader trimming.

## Reserved files

- `src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs`
- `src/QS3D.Core/Export/ProjectInterchangeValidatedSnapshotReader.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeValidatorCanonicalSmoke.cs`

## Confirmed defect

- `ProjectInterchangeJsonValidator.RequireUnit()` compared `(actual ?? string.Empty).Trim()` with the required canonical tokens `m`, `m2`, `m3`, and `kg`, so padded unit tokens validated successfully.
- `ProjectInterchangeValidatedSnapshotReader` validates the snapshot before typed parse, so tightening the validator prevents its later trimming path from silently normalizing padded unit metadata.
- The same canonical interchange boundary already rejects padding for IDs, source handles, dependencies, categories, source scope, fingerprints, timestamps, property keys, and quantity keys.

## Completed contract

- Canonical unit tokens now require exact ordinal matches for `m`, `m2`, `m3`, and `kg`; leading/trailing whitespace is rejected.
- Focused smoke coverage pins all four unit-specific validator error codes.
- The typed validated-snapshot reader is covered to ensure padded units fail at the mandatory validation boundary instead of being normalized.
- Canonical exported snapshot compatibility and existing unit values are preserved.

## Excluded scope

- User-facing project/zone/floor/family names, which canonical domain constructors intentionally trim.
- Import/remap/provenance mutation policy.
- Exporter collection/identity limits already completed by other lanes.
- GitHub Actions, release, local .NET build qualification, and BricsCAD V25/native runtime qualification.

## Verification

PR `#630` was reviewed as a narrow two-file feature diff and squash-merged to `main` at `5d803ac9a835352f0d57c4f028e7294d132241b5`. No GitHub Actions/release was dispatched, and no local .NET smoke/build PASS or BricsCAD V25/native runtime PASS is claimed from this remote session.
