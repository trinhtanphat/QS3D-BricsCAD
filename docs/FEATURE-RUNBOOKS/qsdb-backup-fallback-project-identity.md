# QSDB backup fallback project identity

## Boundary

`LoadWithBackupFallback` may return a validated `.bak` only when doing so cannot cross a known primary project identity. This is deterministic managed Core/Persistence behavior; no BricsCAD runtime is involved.

## Failure mode

A primary QSDB can be well-formed enough to expose a canonical `projectId` and still fail later domain validation. Before issue #6633, fallback then accepted any independently valid `<primary>.bak`, so a damaged project A could silently open project B if the backup path contained B.

## Contract

- Parse the primary once and derive recoverable identity evidence from that same admitted `XDocument`; do not re-read the failed primary after failure.
- When a canonical primary `projectId` is available, a successfully loaded backup must have the exact ordinal same `ProjectId` before a recovery result is published.
- A mismatched backup fails closed with a stable identity error.
- If the primary cannot be parsed far enough to expose trustworthy canonical identity, existing validated-backup recovery remains available.
- Same-project fallback, missing-primary fallback, stable public recovery-reason redaction, path-safety checks and dual-invalid diagnostics remain unchanged.

## Automated evidence

- `tests/QS3D.Core.SmokeTests/QsdbBackupFallbackProjectIdentitySmoke.cs` covers cross-project rejection plus same-project and malformed-primary controls.
- `scripts/preflight-qsdb-backup-fallback-project-identity.py` directly guards the same-generation identity capture, exact ordinal comparison, failure-before-publication ordering and smoke auto-registration.

Runtime boundary: `REMOTE_SAFE` managed Core/Persistence. No licensed BricsCAD `LOCAL_PASS` is required or claimed.
