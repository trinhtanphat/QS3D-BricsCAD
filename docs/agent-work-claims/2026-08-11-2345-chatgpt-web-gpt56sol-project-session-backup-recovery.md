# Work claim — ProjectSession backup recovery contract

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-project-session-backup-recovery-20260811-2345`
- Registered: `2026-08-11T23:45:00+07:00`
- Baseline main SHA: `f96b59c5b5d3cc964e106000940d2604a7660b35`
- Priority: P0 — preserve validated `.qsdb.bak` recovery through the public Core session save/reload lifecycle.

## Confirmed defect

`ProjectSession.Reload()` currently calls `QsdbProjectStore.Load(Path)` directly, so a corrupt primary `.qsdb` cannot use the store's existing validated `.bak` fallback. `ProjectSession.Save()` likewise calls the normal `Save(...)` path, whose replace-with-backup behavior can promote a corrupt primary into `.bak`; after a backup recovery this bypasses the store's existing `SavePreservingValidatedBackup(...)` publication contract.

## Reserved scope

- `src/QS3D.Core/Services/ProjectSession.cs`
- focused Core smoke coverage for the session recovery/save lifecycle
- this claim file for close-out

## Intended contract

- Session reload uses the store's existing validated primary→backup fallback without weakening corruption checks.
- A session that was recovered from backup preserves that validated backup while publishing its repaired primary.
- Failed reload/save remains atomic for the in-memory session state and existing save-audit rollback behavior.
- A successful recovery-safe save returns the session to a normal primary-backed state; a later primary reload must not be reported as backup recovery.
- Normal sessions keep the existing ordinary replace-with-backup behavior.

## Explicit exclusions

No persistence schema/serialization changes, no BricsCAD adapter/project-cache behavior, no updater/release work, no private DWG/runtime qualification, and no changes to another agent's active lane.

## Validation plan

Add deterministic Core smoke coverage using disposable files: create valid primary+backup state, corrupt the primary, recover through `ProjectSession.Reload()`, mutate/save through the session, verify primary is valid/current while the previously validated backup remains loadable, then reload the repaired primary. Also retain normal save/reload and lock requirements.

No GitHub Actions will be dispatched; licensed BricsCAD V25 runtime qualification is outside this Core-only lane.

## Completion condition

The session is wired to the existing recovery-safe store APIs with focused regression coverage on current `main`, and this claim is marked `COMPLETED` with exact commit SHA(s) and validation actually performed.
