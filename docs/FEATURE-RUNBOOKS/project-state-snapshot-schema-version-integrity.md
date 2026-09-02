# ProjectState snapshot schema-version integrity

`ProjectStateSnapshot` is an in-memory persistence snapshot boundary and must not retain schema states that canonical QSDB migration would reject.

## Contract

Before any snapshot copy/retention, `ProjectStateSnapshot` rejects:

- schema versions `<= 0`;
- schema versions greater than `ProjectState.CurrentSchemaVersion`.

The current schema version remains accepted. Rejection is observational only: the source project's `SchemaVersion`, `ChangeVersion`, and `UpdatedUtc` are not mutated.

The guard applies to both `ProjectStateSnapshot.Capture` and `ProjectStateSnapshot.CreateDetachedCopy` through the shared validation path. It intentionally does not constrain the public `ProjectState.SchemaVersion` setter because persistence/migration code must be able to materialize historical schema versions before canonical migration.

## Deterministic validation

- registered smoke: `ProjectStateSnapshotSchemaVersionIntegritySmoke`;
- source guard: `scripts/preflight-project-state-snapshot-schema-version-integrity.py`;
- shared Core smoke/build through normal CI.
