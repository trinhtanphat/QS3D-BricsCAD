# Work claim — Sidecar whitespace path identity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-sidecar-whitespace-path-identity`
- Registered: `2026-08-12T10:39:00+07:00`
- Completed: `2026-08-12T10:41:00+07:00`
- Baseline main SHA: `bb50e290d890ec2f5b147f24445ca59d3b4baba4`
- Claim commit: `bed0220286add128d56cb2a582577805f2786820`
- Source commit: `9c808f9f1831134cfadd6698affb1a6198da7b02`
- Regression commit: `a23be9d8eee901ca0b3e63383d9a8488a8f49f20`
- Priority: P1 — persistence freshness authority must observe the exact path used by lock/store.

## Confirmed defect

`ProjectSidecarRevisionStamp.Capture(...)` and `IsForPath(...)` called `Path.GetFullPath(primaryPath.Trim())`, while `ProjectFileLock.Acquire(...)` and `QsdbProjectStore` resolve the supplied path with `Path.GetFullPath(path)` without trimming. `ProjectContextCoordinator` passes the same `GetProjectPath(document)` value into the lock, store and revision-stamp paths.

A valid path containing leading whitespace in the file name could therefore be locked/saved as one QSDB path while the revision stamp captured or compared a different trimmed path. Freshness/path-transition checks could then reason about the wrong primary/backup pair.

The earlier completed sidecar path-semantics lane (`a12f5ed...` → `9668b21...`/`98bf9af...` → `48fef7f...`) addressed platform case comparison only and did not resolve whitespace path identity.

## Completed scope

- `src/QS3D.Core/Persistence/ProjectSidecarRevisionStamp.cs`
- `tests/QS3D.Core.SmokeTests/ProjectSidecarRevisionPathSemanticsSmoke.cs`
- this claim file

## Resulting contract

- Null/empty/whitespace-only primary paths remain rejected.
- Every nonblank path character is preserved when resolving the full path; valid file-name whitespace is not silently trimmed.
- `Capture(...)` and `IsForPath(...)` now use the same exact-path normalization as the lock/store boundary.
- Existing Windows/non-Windows case comparer semantics remain unchanged.
- Digest, pair-stability, size-bound and `MatchesCurrent()` semantics remain unchanged.

## Implementation

Removed `.Trim()` from both `Path.GetFullPath(...)` calls in `ProjectSidecarRevisionStamp` while retaining the existing `string.IsNullOrWhiteSpace(...)` guards.

The existing sidecar path smoke now uses a GUID-based leading-whitespace filename plus its trimmed neighbor. It verifies that the exact whitespace path is recognized, the trimmed neighbor remains a distinct stamp/path identity, and the previous platform case comparison/equality/hash contract remains intact.

## Validation actually performed

- Re-fetched source and smoke after publishing the claim; their pre-fix blobs were `93a6dc37165ec4524b85e23c8b8dfb81055e8e77` and `ec14252735f836ce62b83412e73a157ac195e688`.
- Source update committed as `9c808f9f1831134cfadd6698affb1a6198da7b02`; regression update committed as `a23be9d8eee901ca0b3e63383d9a8488a8f49f20`.
- Refreshed `main` after concurrent writes; HEAD had advanced to `f3f87a7f9b49254c1a94095a3ca0cb97db7d1187` with the regression commit as an ancestor.
- Read back current `main`: source blob `197502ae9929637f2db422d84f298b28dbf151cd` contains exact `Path.GetFullPath(primaryPath)` calls, and smoke blob `b37d8405a85a3ce1a76c24a4cd38c5a2f7e5c7c2` contains the leading-whitespace/trimmed-neighbor regression while preserving case semantics.
- GitHub Actions were not dispatched.
- No executable local .NET/Core smoke run and no licensed BricsCAD V25/V26 runtime PASS are claimed from this remote session.

## Completion

Revision-stamp authority now preserves exact valid path identity consistently with the QSDB store/lock boundary, focused regression source is on `main`, and this claim is released as completed.
