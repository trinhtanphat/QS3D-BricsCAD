# QSDB missing-primary backup generation integrity

Lane-Key: `issue-5253`

## Contract

`QsdbProjectStore.Save` uses `AtomicFileCommit.ReplaceWithBackup` for ordinary replacement publication. A `.bak` file is eligible for `LoadWithBackupFallback` only when it represents the immediately previous primary generation.

If the primary has already disappeared while an older `.bak` survives, a new replacement save must not publish a new primary beside that stale backup. The stale backup is staged away before the new primary is installed. If publication fails, the staged backup is restored best-effort. After successful publication, the stale generation is retired and is not eligible for fallback.

This preserves the existing contracts for:

- first save when neither primary nor backup exists;
- ordinary replacement where the old primary becomes the new backup;
- `SavePreservingValidatedBackup`, whose separate replace-without-backup path intentionally retains an already validated recovery backup;
- `SaveNew`, which refuses any pre-existing primary or backup pair;
- reparse/path-safety checks and fail-closed rollback behavior.

## Deterministic validation

Run:

```text
python scripts/preflight-qsdb-missing-primary-stale-backup.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

`QsdbSaveAtomicitySmoke.MissingPrimaryReplacementRetiresStaleBackup` creates two generations, removes the current primary while retaining the first-generation backup, publishes a third generation, verifies the stale backup is retired, then corrupts the recreated primary and proves fallback cannot resurrect the first generation.

No licensed BricsCAD runtime evidence is required for this Core persistence invariant.
