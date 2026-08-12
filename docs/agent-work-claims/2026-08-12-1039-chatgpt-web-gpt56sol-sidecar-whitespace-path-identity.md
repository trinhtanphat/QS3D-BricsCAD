# Work claim — Sidecar whitespace path identity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-sidecar-whitespace-path-identity`
- Registered: `2026-08-12T10:39:00+07:00`
- Baseline main SHA: `bb50e290d890ec2f5b147f24445ca59d3b4baba4`
- Priority: P1 — persistence freshness authority must observe the exact path used by lock/store.

## Confirmed defect

`ProjectSidecarRevisionStamp.Capture(...)` and `IsForPath(...)` call `Path.GetFullPath(primaryPath.Trim())`, while `ProjectFileLock.Acquire(...)` and `QsdbProjectStore` resolve the supplied path with `Path.GetFullPath(path)` without trimming. `ProjectContextCoordinator` passes the same `GetProjectPath(document)` value into the lock, store and revision-stamp paths.

A valid path containing leading whitespace in the file name can therefore be locked/saved as one QSDB path while the revision stamp captures or compares a different trimmed path. Freshness/path-transition checks can then reason about the wrong primary/backup pair.

The existing sidecar path-semantics lane (`a12f5ed...` → `9668b21...`/`98bf9af...` → `48fef7f...`) addressed platform case comparison only; its completed claim did not reserve or resolve whitespace path identity.

## Reserved scope

- `src/QS3D.Core/Persistence/ProjectSidecarRevisionStamp.cs`
- `tests/QS3D.Core.SmokeTests/ProjectSidecarRevisionPathSemanticsSmoke.cs`
- this claim file

## Intended contract

- Reject null/empty/whitespace-only primary paths as before.
- Preserve every nonblank path character when resolving the full path; do not silently trim a valid file name.
- `Capture(...)` and `IsForPath(...)` use the same exact-path normalization as the lock/store boundary.
- Preserve the existing Windows/non-Windows case comparer semantics from the completed platform-path lane.
- Preserve digest, pair-stability, size-bound and `MatchesCurrent()` semantics.

## Validation plan

- Re-fetch source/smoke after claim publication and write against current blob SHAs.
- Replace the old padded-path smoke assertion with a leading-whitespace filename fixture that remains portable across Windows/POSIX and proves the trimmed neighbor is a distinct identity.
- Keep the existing case-sensitivity/equality/hash checks.
- Read back source/smoke from current `main` after concurrent writes and close this claim with exact commit SHAs.
- No GitHub Actions dispatch and no BricsCAD V25/V26 runtime PASS claim.

## Completion condition

Revision stamps preserve exact valid path identity consistently with the store/lock boundary, focused regression source is present on `main`, and this claim is marked `COMPLETED` with truthful validation evidence.
