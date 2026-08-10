# QS3D Semantic Snapshot — immutable validated reader

Updated: 2026-08-10 (UTC+7)

`ProjectInterchangeValidatedSnapshotReader` converts a valid `QS3D.SemanticSnapshot` v1 JSON document into a typed, immutable, CAD-independent snapshot model. It is a source-safe bridge between strict validation and any future import/compare/coordination workflow.

## Validation-first rule

`Read(json)` always calls `ProjectInterchangeJsonValidator.Validate(json)` first. Any validation Error throws before a typed snapshot is returned. The reader does not weaken or duplicate the validator's format/version/SI-unit/reference/dependency/generated-ownership rules.

Validator warnings remain visible through `ProjectInterchangeValidatedSnapshot.Validation`. For example, a missing optional provenance timestamp is readable as `UpdatedUtc = null` while the original raw timestamp string and validation warning remain available.

## Typed portable data

The reader exposes typed snapshots for:

- format/version and SI units;
- project identity, schema version, drawing fingerprint and timestamp provenance;
- Zone IDs/names;
- Floor IDs/names/elevations;
- Family IDs/names/typed `ElementCategory` plus portable properties;
- element IDs/category/catalog references/drawing fingerprint/timestamp;
- `sourceRefScope`, source Handle provenance, dependency IDs, portable properties and quantities.

Every public collection is a defensive read-only copy. Property/quantity maps use case-insensitive immutable boundaries and fail closed if externally-authored JSON would collapse multiple raw keys into the same normalized semantic key.

## Important non-authority boundary

Typed reading is **not import permission**. The reader:

- does not construct or replace `ProjectState`;
- does not write `.qsdb`;
- does not touch DWG entities;
- does not rebind drawing-local source Handles;
- does not create generated/native ownership;
- does not resolve target collision policy;
- does not upgrade/migrate schemas beyond the validator's exact accepted version.

`sourceRefScope = drawing-local` remains provenance only. A Handle in the typed snapshot must never be treated as ownership of the same numeric Handle in another DWG.

## Relationship to import preview

`ProjectInterchangeImportPreview` answers a narrower target-oriented question: which source IDs are new/colliding/incompatible against one existing target project?

`ProjectInterchangeValidatedSnapshotReader` answers a source-oriented question: what portable typed data is present in this already-valid snapshot?

A future importer may consume both, but neither is a mutating importer.

## Source checks

```text
python scripts/preflight-interchange-validated-reader.py
```

`ProjectInterchangeValidatedSnapshotReaderSmoke` covers full portable-field reading, immutable collection boundaries, invalid-snapshot rejection and validation-warning preservation.

## Still open before round-trip/import

A mutating importer still requires explicit policy for import mode, target ID collisions, project/drawing identity, property/quantity/catalog precedence, dependency ordering, source provenance rebinding/discard, generated ownership clearing/rebuild, rollback/audit/confirmation UX and exact V25 adapter qualification.

Current status: **REMOTE_DONE for validation-first immutable typed reading only**. JSON import/round-trip remains intentionally incomplete.
