# QSDB backup-fallback recovery reason

## Scope

Issue #5869 / Lane-Key `issue-5869` hardens the public `ProjectLoadResult.PrimaryFailureMessage` emitted by `QsdbProjectStore.LoadWithBackupFallback`.

## Contract

When the primary QSDB fails with a recoverable persistence/data error and the validated `.bak` loads successfully, the result must preserve the backup project, exact backup source path, and `RecoveredFromBackup=true`, while exposing only the stable reason `Primary QSDB was invalid; loaded validated backup.`. Raw `Exception.Message` text from XML parsing, schema validation, formatting, or missing-file diagnostics must not cross the successful recovery result boundary.

If no backup exists, the original recoverable primary failure is still rethrown. If both primary and backup are invalid, the existing stable `InvalidDataException` remains authoritative and retains the primary/backup exceptions as aggregate inner diagnostic evidence.

## Deterministic validation

Run:

```text
python scripts/preflight-qsdb-backup-fallback-recovery-reason.py
dotnet run --project tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj -c Release
```

The smoke covers malformed XML, structurally invalid primary data, missing-backup behavior, valid-primary control behavior, recovered project identity, source-path provenance, recovery flag, and stable public reason text.

Runtime classification: `NOT_APPLICABLE`; this package is Core persistence/public-result behavior and does not require licensed BricsCAD evidence.
