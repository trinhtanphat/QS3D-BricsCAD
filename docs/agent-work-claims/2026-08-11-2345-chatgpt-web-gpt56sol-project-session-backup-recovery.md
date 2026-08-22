# Work claim — ProjectSession backup recovery contract

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-session-backup-recovery-20260811-2345`
- Registered: `2026-08-11T23:45:00+07:00`
- Completed: `2026-08-12T00:29:00+07:00`
- Baseline main SHA: `f96b59c5b5d3cc964e106000940d2604a7660b35`
- Priority: P0 — preserve validated `.qsdb.bak` recovery through the public Core session save/reload lifecycle.

## Confirmed defect

`ProjectSession.Reload()` called `QsdbProjectStore.Load(Path)` directly, so a corrupt primary `.qsdb` could not use the store's existing validated `.bak` fallback. `ProjectSession.Save()` likewise called the normal `Save(...)` path, whose replace-with-backup behavior could promote a corrupt primary into `.bak`; after a backup recovery this bypassed the store's existing `SavePreservingValidatedBackup(...)` publication contract.

## Completed changes

- `0ad2b89d9f9e7ae9665912182fd040409e00ad37` — `ProjectSession.Reload()` now stages `LoadWithBackupFallback(Path)`, swaps `Project`/`Audit` only after the recovered project and reload audit are prepared, and records whether the canonical session state came from backup.
- `0ad2b89d9f9e7ae9665912182fd040409e00ad37` — `ProjectSession.Save()` now routes recovered sessions through `SavePreservingValidatedBackup(...)`; recovery provenance is cleared only after successful publication, so failed save/rollback keeps the recovery-safe mode for the next attempt. Normal primary-backed sessions retain the ordinary `Save(...)` path.
- `58559e27a8cebe23347037a901275f3d2e6cbfe9` — added `ProjectSessionBackupRecoverySmoke` with module-initializer registration. The disposable-file scenario covers write-lock requirements, corrupt-primary backup fallback, failed recovery-safe save with a temporarily missing backup, preservation of the validated backup on the next successful save, return to ordinary save mode after a successful primary reload, and failed dual-corruption reload without swapping the in-memory session project.

## Validation performed

- Re-read the exact current `main` blobs after concurrent agents continued writing: `ProjectSession.cs` is blob `25e7cd80048f2cd517a9f2310177f751d3b28a31`; `ProjectSessionBackupRecoverySmoke.cs` is blob `377b12b7200da4210dd2a4a9a8df00da397a65f0`.
- Inspected the exact implementation diff for `0ad2b89d...` and exact regression diff for `58559e27...`; no unrelated source surface is changed by either commit.
- Re-read `QsdbProjectStore.LoadWithBackupFallback(...)`, `SavePreservingValidatedBackup(...)`, and `AtomicFileCommit` on current `main` to verify the session routing matches the existing validated-backup and replace-primary-only contracts rather than weakening persistence validation.
- Confirmed the smoke project is SDK-style `net8.0` with default compile-item inclusion, and the new regression follows the repository's existing `[ModuleInitializer]` smoke-registration pattern.
- GitHub combined status for the regression commit has no reported CI statuses. No GitHub Actions workflow was dispatched or re-run.
- Exact smoke execution is `NOT_RUN` in this remote session: the available execution container has no `dotnet`, `csc`, Mono/MSBuild, and outbound DNS prevents cloning GitHub for a local build. This close-out therefore claims source/static completion only, not an executed Core smoke PASS, BricsCAD runtime PASS, private-DWG qualification, signing, or release qualification.

## Coordination / exclusions respected

No persistence schema/serialization changes, BricsCAD adapter/project-cache behavior, updater/release work, private DWG/runtime qualification, GitHub Actions dispatch, or force-push was performed. `main` moved repeatedly during implementation; two Git-data fast-forward attempts were rejected by GitHub and abandoned, then the final source write used the current `ProjectSession.cs` blob SHA guard and the regression was created through the Contents API so concurrent commits were preserved.

## Result

The public Core `ProjectSession` lifecycle now consumes the store's existing backup-fallback/recovery-safe publication contract. A backup-recovered session cannot silently fall back to ordinary replace-with-backup publication after a failed save, successful recovery-safe save clears recovery provenance, a later successful primary reload restores normal save behavior, and failed reload still leaves the prior in-memory session state intact. Focused regression coverage is present on `main`; execution remains explicitly unclaimed in this environment.
