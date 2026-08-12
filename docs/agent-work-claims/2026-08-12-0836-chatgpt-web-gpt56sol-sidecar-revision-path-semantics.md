# Work claim — Sidecar revision path semantics

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-sidecar-revision-path-semantics`
- Registered: `2026-08-12T08:36:00+07:00`
- Completed: `2026-08-12T08:43:00+07:00`
- Baseline main SHA: `c559b1f7b843d618622db2caf86c1417bd0ebc7a`
- Claim commit: `a12f5ed784ebd61715a94a672a5edcb258df4be1`
- Source commit: `9668b21ba17b3c1a713464ad9656e8969ad7957d`
- Regression commit: `98bf9af4a4eb2a57683bab1fa9f0de48e3e9e1bd`
- Priority: P1 — persistence authority identity must follow the platform path contract.

## Confirmed defect

`ProjectSidecarRevisionStamp` used `StringComparison.OrdinalIgnoreCase` and `StringComparer.OrdinalIgnoreCase` unconditionally for primary-path identity in `IsForPath(...)`, `Equals(...)` and `GetHashCode()`. The same persistence layer already defines path distinctness in `AtomicFileCommit` using platform-aware comparison (`OrdinalIgnoreCase` on Windows, `Ordinal` on non-Windows). On a case-sensitive platform, two distinct QSDB paths that differ only by casing could therefore be treated as the same sidecar authority/stamp identity.

This lane aligned revision-stamp path identity with the existing persistence path policy; it did not broaden product host/platform claims.

## Completed scope

- `src/QS3D.Core/Persistence/ProjectSidecarRevisionStamp.cs`
- `tests/QS3D.Core.SmokeTests/ProjectSidecarRevisionPathSemanticsSmoke.cs`
- this claim file

## Resulting contract

- Windows path identity remains case-insensitive.
- Non-Windows path identity is case-sensitive, matching `AtomicFileCommit`'s current policy.
- `IsForPath`, stamp equality and hash-code path contribution now use one shared platform comparer.
- File digest/presence capture, size bounds and `MatchesCurrent()` behavior remain unchanged.

## Implementation

`ProjectSidecarRevisionStamp` now defines one `PathComparer` using the same Windows/non-Windows distinction already used by `AtomicFileCommit`. `IsForPath(...)`, `Equals(...)` and `GetHashCode()` all consume that comparer so equality and hashing cannot disagree about path casing.

The focused module-initializer smoke uses GUID-based non-existing temporary paths, so no private fixture or sidecar bytes are required. It verifies same-path normalization/equality/hash consistency and checks case-only path identity conditionally against the repository platform rule.

## Validation actually performed

- Re-fetched the claimed source after claim publication; the pre-fix blob remained `cbcfdfbe7d518e59ea92a825e305b3261443a3ef` before the successful source write.
- Reviewed the exact source commit `9668b21ba17b3c1a713464ad9656e8969ad7957d`; its diff only adds the shared comparer and switches the three path-identity uses.
- Read back current `main` source blob `93a6dc37165ec4524b85e23c8b8dfb81055e8e77` and smoke blob `ec14252735f836ce62b83412e73a157ac195e688`.
- Multiple Git database fast-forward attempts were safely rejected because `main` advanced between commit creation and ref update. No force update was used. Intervening comparisons showed no overlap with the reserved source/test paths.
- The final source and smoke were integrated through GitHub Contents API using the exact current source blob SHA, preserving concurrent `main` history.
- GitHub Actions were not dispatched.
- No local .NET/Core smoke execution or licensed BricsCAD V25/V26 runtime PASS is claimed from this remote session.

## Excluded scope honored

- No changes to `AtomicFileCommit`, `ProjectFileLock`, QSDB schema/store, BricsCAD adapters, save/reload workflow or local sidecar runtime probes.
- No filesystem-specific probing beyond the repository's existing Windows/non-Windows path policy.

## Completion

Revision-stamp path authority now follows the repository's platform path identity contract consistently across lookup, equality and hashing. Focused regression source is on `main`, and the claim is released as completed.
