# Revision snapshot backup project identity

## Scope

A revision baseline primary and any recovery `.bak` beside it must belong to the same project identity as the revision being published. A valid foreign primary must never be converted into a recovery backup for another project, and a valid foreign validated backup must never be preserved beside a newly published primary for another project.

Replacement admission is fail-closed before primary or backup mutation. Project identity comparison is exact ordinal, including legacy empty identity: only identical identities may share one replacement/recovery lineage.

## Deterministic regression

`RevisionSnapshotBackupProjectIdentitySmoke` proves:

- a foreign primary is rejected before it can become a backup;
- a foreign validated backup beside a corrupt primary is rejected with both files byte-for-byte unchanged;
- same-project recovery publication remains accepted and later fallback returns the same project.

The focused auto-discovered preflight pins project-identity admission before `ShouldPreserveValidatedBackup` and retains the existing revision recovery behavior.

## Runtime classification

Runtime: NOT_APPLICABLE. This is deterministic Core revision persistence/provenance integrity and does not require licensed BricsCAD execution.
