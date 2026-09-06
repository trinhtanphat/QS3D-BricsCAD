# QSDB backup fallback path-safety boundary

## Scope

C01 deterministic Core/Persistence safety for `QsdbProjectStore.LoadWithBackupFallback` and the path-admission helpers used by QSDB reads.

Runtime classification: `REMOTE_SAFE`. This contract does not require licensed BricsCAD execution.

## Invariant

Backup fallback is data recovery, not a path-trust recovery mechanism.

A malformed or otherwise recoverably invalid primary QSDB may fall back to the validated `.bak` path. A persistence path-safety failure must fail closed and must not be reclassified as recoverable corruption merely because both failures historically used `InvalidDataException`.

`PersistencePathSafetyException` is therefore an `IOException`. Redirect/reparse-point trust violations use that typed exception. Filesystem generation/final-path identity failures already use the same non-recoverable IO family. `QsdbProjectStore.IsRecoverableDataFailure` remains limited to data-format/corruption cases and does not admit `IOException`.

## Required ordering

`LoadWithBackupFallback` performs its initial pathname check before the recovery `try`, then `Load`/`LoadDocument` perform additional path/generation checks while opening and binding the held stream. Any trust failure from those in-try checks must escape the recoverable catch filter.

Ordinary invalid primary data must continue to enter backup fallback. Ordinary invalid backup data must continue to aggregate with the primary failure as `InvalidDataException`.

## Regression authority

Run:

```text
python scripts/preflight-qsdb-backup-fallback-path-safety.py
```

The guard is auto-discovered by `scripts/preflight-all.py` and pins:

- typed `PersistencePathSafetyException : IOException`;
- redirect/reparse rejection sites use the typed exception;
- QSDB fallback still treats ordinary `InvalidDataException` as recoverable;
- fallback must not broaden recovery to `IOException` or the typed path-safety exception;
- primary and backup catches remain filtered through `IsRecoverableDataFailure`.

## Compatibility boundary

The change intentionally preserves successful read/save semantics and normal corruption fallback. It only changes the exception family for persistence path trust rejection from recoverable data-error semantics to non-recoverable IO/path-safety semantics.
