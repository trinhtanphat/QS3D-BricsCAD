# Work claim — Sidecar revision path semantics

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-sidecar-revision-path-semantics`
- Registered: `2026-08-12T08:36:00+07:00`
- Baseline main SHA: `c559b1f7b843d618622db2caf86c1417bd0ebc7a`
- Priority: P1 — persistence authority identity must follow the platform path contract.

## Confirmed defect

`ProjectSidecarRevisionStamp` uses `StringComparison.OrdinalIgnoreCase` and `StringComparer.OrdinalIgnoreCase` unconditionally for primary-path identity in `IsForPath(...)`, `Equals(...)` and `GetHashCode()`. The same persistence layer already defines path distinctness in `AtomicFileCommit` using platform-aware comparison (`OrdinalIgnoreCase` on Windows, `Ordinal` on non-Windows). On a case-sensitive platform, two distinct QSDB paths that differ only by casing can therefore be treated as the same sidecar authority/stamp identity.

This is an identity bug in the Core persistence boundary, not a request to broaden supported host platforms. The fix aligns revision-stamp path identity with the path comparison policy already present in the same namespace.

## Reserved scope

- `src/QS3D.Core/Persistence/ProjectSidecarRevisionStamp.cs`
- `tests/QS3D.Core.SmokeTests/ProjectSidecarRevisionPathSemanticsSmoke.cs`
- this claim file

## Intended contract

- Windows path identity remains case-insensitive.
- Non-Windows path identity is case-sensitive, matching `AtomicFileCommit`'s current policy.
- `IsForPath`, stamp equality and hash-code path contribution use the same comparison rule.
- File digest/presence capture, size bounds and `MatchesCurrent()` behavior remain unchanged.

## Excluded scope

- No changes to `AtomicFileCommit`, `ProjectFileLock`, QSDB schema/store, BricsCAD adapters, save/reload workflow or local sidecar runtime probes.
- No attempt to infer filesystem-specific case sensitivity beyond the repository's existing Windows/non-Windows path policy.
- No GitHub Actions dispatch and no V25/V26 runtime qualification claim.

## Validation plan

- Re-fetch the claimed source after claim publication and write against the exact blob.
- Add a focused auto-registered Core smoke using missing sidecar paths (no private fixture required) to verify same-path identity plus case-only path behavior conditionally by platform.
- Verify equality/hash consistency for equivalent paths and distinctness where the repository policy is case-sensitive.
- Review exact pushed diff, read back current `main`, close claim with exact SHA, and ancestry-check without force-push.
- No compile/test-runtime PASS will be claimed unless actually executed.

## Completion condition

Revision-stamp path identity uses the repository's platform path comparison consistently across lookup/equality/hash behavior, focused regression source is on `main`, and this claim is marked `COMPLETED` with truthful validation evidence.
