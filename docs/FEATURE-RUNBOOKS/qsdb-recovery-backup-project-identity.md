# QSDB recovery backup project identity

Lane-Key: `issue-5256`

## Contract

`QsdbProjectStore.SavePreservingValidatedBackup` is used only when a previously validated `.bak` must remain available while a repaired/recovered primary is published. Parseability alone is not sufficient recovery evidence: the backup must belong to the same persisted QS3D project identity as the candidate primary.

Before any call to `SaveCore`, the store loads the backup through the normal strict persistence boundary and requires its canonical `ProjectId` to equal the candidate project's `ProjectId` using ordinal identity semantics. A valid but foreign backup fails closed. Rejection must happen before the candidate's persistence version/timestamp or either primary/backup file can change.

A same-project validated backup remains preserved by the existing replace-primary-only path. This contract does not weaken XML/schema validation, backup fallback validation, atomic publication, reparse/path safety, or the dedicated missing-primary stale-generation policy.

## Deterministic validation

Run:

```text
python scripts/preflight-qsdb-recovery-backup-project-identity.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The regression creates a valid backup from a foreign project, attempts recovery-safe publication of a different project, and requires rejection while preserving the candidate persistence state plus exact primary/backup bytes. Existing same-project recovery coverage remains the positive control.

No licensed BricsCAD runtime evidence is required for this Core persistence invariant.
