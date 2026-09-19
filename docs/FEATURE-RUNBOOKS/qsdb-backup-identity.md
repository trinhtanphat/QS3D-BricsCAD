# QSDB backup identity recovery

Lane: C01 — Core / Domain / Persistence / Project Model  
Issue: #7277  
Runtime boundary: REMOTE_SAFE managed Core/Persistence only.

## Invariant

A backup may not gain project authority merely because the primary document is parseable but its `projectId` is ambiguous. Missing, blank, or padded/non-canonical primary identity is a fail-closed state. A trustworthy canonical primary identity must match the validated backup identity before fallback succeeds.

Primary absence or XML-level unparseability is a distinct recovery state and must not be conflated with a parseable document that explicitly fails identity canonicality.

## Regression

`tests/QS3D.Core.SmokeTests/QsdbBackupIdentitySmoke.cs` constructs project A as primary and project B as backup, corrupts only the primary identity into padded, missing, and blank forms, and requires `LoadWithBackupFallback` to throw `InvalidDataException` rather than return project B.

## Validation

Run the managed Core smoke suite and `scripts/preflight-qsdb-backup-identity.py`. No licensed BricsCAD runtime is required or claimed by this carrier.

## Review checklist

Confirm same-project canonical backup recovery remains unchanged; malformed primary plus malformed backup still preserves aggregate failure evidence; ambiguous parseable primary cannot authorize a cross-project backup; and the implementation does not broaden recoverable exception classes or weaken path-safety checks.
