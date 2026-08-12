# Work claim — interchange unit token canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-interchange-unit-token-canonicality-20260812-0747`
- Registered: `2026-08-12T07:47:00+07:00`
- Baseline main SHA: `0696f3cbcf602e140c3cad23282160641f2e659d`
- Priority: evidence-driven remote-safe interchange integrity hardening during owner-requested `continue all`

## Reserved scope

Make canonical semantic snapshot unit tokens fail closed on leading/trailing whitespace instead of accepting padded structural tokens through validator/typed-reader trimming.

## Reserved files

- `src/QS3D.Core/Export/ProjectInterchangeJsonValidator.cs`
- `src/QS3D.Core/Export/ProjectInterchangeValidatedSnapshotReader.cs`
- `tests/QS3D.Core.SmokeTests/ProjectInterchangeValidatorCanonicalSmoke.cs`

## Evidence

- `ProjectInterchangeJsonValidator.RequireUnit()` currently compares `(actual ?? string.Empty).Trim()` with the required canonical tokens `m`, `m2`, `m3`, and `kg`, so padded unit tokens validate successfully.
- `ProjectInterchangeValidatedSnapshotReader` currently reads units through `Required(...)`, which trims again, silently changing accepted structural metadata.
- The same canonical interchange boundary already rejects padding for IDs, source handles, dependencies, categories, source scope, fingerprints, timestamps, property keys, and quantity keys.

## Excluded scope

- User-facing project/zone/floor/family names, which canonical domain constructors intentionally trim.
- Import/remap/provenance mutation policy.
- Exporter collection/identity limits already completed by other lanes.
- GitHub Actions, release, local .NET build qualification, and BricsCAD V25/native runtime qualification.

## Validation plan

- Reject padded length/area/volume/mass unit tokens with the existing unit-specific validator error codes.
- Ensure the typed reader cannot normalize padded units into canonical values.
- Preserve canonical exported snapshot acceptance and existing unit values.
- Re-read moving `main` and exact PR diff before integration; do not dispatch Actions.

## Completion condition

Validator + typed-reader defense + focused canonical regression are merged to current `main`, remote source is re-read, and this claim is marked `COMPLETED` with exact integration SHA and validation boundaries.
