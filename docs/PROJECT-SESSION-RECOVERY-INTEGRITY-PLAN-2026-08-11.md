# ProjectSession recovery integrity — detailed implementation plan

**Date:** 2026-08-11  
**Owner lane:** ChatGPT Web / GPT-5.6 Sol  
**Initial baseline:** `09e3749a856b8d246f46f42e121289df5f3ecb8f`  
**Scope class:** CAD-independent Core persistence; remote/source-safe

## 1. Objective

Close the gap between `ProjectSession` and the recovery guarantees already implemented by `QsdbProjectStore`.

The target behavior is deliberately narrow: if the primary `.qsdb` is unreadable but a validated `.bak` can be loaded, the live session may recover from that backup, but the next save must never rotate the unreadable primary over the known-good backup. Recovery provenance must survive failures and disappear only after a successful recovery-safe publication or a later successful primary reload.

This work is production stabilization, not a new persistence format and not a redesign of `.qsdb`.

## 2. Existing source contract to preserve

### `QsdbProjectStore`

Current Core store already separates three publication modes:

- ordinary `Save(...)` -> replace primary and rotate the previous primary to `.bak`;
- `SaveNew(...)` -> publish a new file;
- `SavePreservingValidatedBackup(...)` -> require/validate `.bak`, replace only primary, then revalidate both primary and backup.

It also exposes `LoadWithBackupFallback(...)`, returning a `ProjectLoadResult` with the loaded `ProjectState`, source path, `RecoveredFromBackup`, and a primary-failure diagnostic.

### `ProjectSession`

Current session already enforces:

- an acquired `ProjectFileLock` before `Save()` or `Reload()`;
- `ProjectStateSnapshot` rollback of in-memory state if save fails;
- staging a reloaded project before replacing `Project` and `Audit`;
- project-level `PROJECT_SAVE` / `PROJECT_RELOAD` audit events.

Those invariants remain mandatory.

## 3. Confirmed integration defect

`ProjectSession.Save()` currently invokes plain `_store.Save(Project, Path)` unconditionally. `ProjectSession.Reload()` invokes plain `_store.Load(Path)`.

Therefore the higher-level session abstraction discards the store's recovery result and cannot distinguish a canonical primary load from a backup fallback after primary corruption.

Without that distinction, the next ordinary session save can rotate the failed primary into `.bak` and destroy the validated recovery copy.

## 4. Proposed session state

Add minimal recovery provenance as a boolean and expose a read-only diagnostic property named `RecoveredFromBackup`.

Semantics:

- constructor starts `false`; caller-supplied `ProjectState` is not presumed to be a recovered store result;
- successful primary `Reload()` -> `false`;
- successful backup-fallback `Reload()` -> `true`;
- failed `Reload()` -> unchanged;
- successful recovery-mode `Save()` -> `false` only after store validation succeeds;
- failed recovery-mode `Save()` -> remains `true`;
- ordinary successful `Save()` while false -> remains false.

No raw primary exception message is retained in session state; `ProjectLoadResult` already owns that diagnostic.

## 5. Reload algorithm

1. Require write lock exactly as today.
2. Call `_store.LoadWithBackupFallback(Path)` into a local result.
3. Build `AuditTrail` against `result.Project` locally.
4. Record `PROJECT_RELOAD` on the staged project.
5. Only after all staged operations succeed assign `Project`, `Audit`, and `RecoveredFromBackup`.
6. If load or staged audit creation/recording fails, leave the existing live session and provenance untouched.

No reload step writes either `.qsdb` or `.bak`.

## 6. Save algorithm

1. Require write lock exactly as today.
2. Capture `ProjectStateSnapshot` before `PROJECT_SAVE` audit mutation.
3. Record `PROJECT_SAVE` exactly once.
4. If recovery provenance is false, use ordinary `_store.Save(Project, Path)`.
5. If recovery provenance is true, use `_store.SavePreservingValidatedBackup(Project, Path)`.
6. Clear recovery provenance only after the selected store call returns successfully.
7. On any save failure, restore the in-memory snapshot, do not alter recovery provenance, and preserve current aggregate-error behavior if rollback itself fails.

This keeps retries safe and preserves the known-good backup until a valid primary has been republished.

## 7. Deterministic Core regression matrix

Use temporary filesystem fixtures created by the smoke test and removed in `finally`.

### A. Primary reload

- save a valid project;
- construct a session around a different in-memory project for the same path;
- acquire lock and reload;
- assert expected persisted project identity/content;
- assert `RecoveredFromBackup == false`.

### B. Backup fallback

- create primary/backup pair with a valid backup;
- corrupt only primary bytes with small invalid XML/text;
- reload through `ProjectSession`;
- assert backup project loaded;
- assert recovery provenance true;
- assert primary bytes remain corrupt after reload;
- assert backup remains loadable.

### C. Recovery-safe save

After B:

- mutate recovered project deterministically;
- capture backup bytes before save;
- call session `Save()`;
- assert primary is now loadable with mutation;
- assert backup bytes are unchanged and loadable;
- assert recovery provenance false.

### D. Failed recovery save

Create a deterministic validation failure after fallback, then:

- capture ChangeVersion/audit count/recovery flag;
- call `Save()` and expect failure;
- assert snapshot restored `PROJECT_SAVE` audit/version mutations;
- assert recovery flag still true;
- assert backup remains valid and unchanged.

### E. Later primary reload

- after restoring or republishing a valid primary, call `Reload()`;
- assert primary is used and recovery flag false.

### F. Both copies invalid

- start with an existing live session project;
- corrupt primary and backup;
- call `Reload()` and expect failure;
- assert existing live `Project` and `Audit` remain unchanged;
- assert previous recovery provenance remains unchanged.

## 8. Static/preflight regression

Add `scripts/preflight-project-session-recovery.py` if no equivalent current guard exists. It should check architectural tokens/order rather than duplicate functional smoke assertions:

- `Reload()` contains `LoadWithBackupFallback` and stages result before assignments;
- recovered flag is assigned only after staged reload audit;
- `Save()` selects `SavePreservingValidatedBackup` when recovered;
- recovery flag clear occurs only after successful store save;
- catch path contains snapshot restore and does not clear recovery provenance;
- focused smoke registration exists.

The preflight is auto-discovered by existing `preflight-all.py`; no workflow edit is needed.

## 9. Concurrency / multi-agent safety

Before every write:

- read latest `main`;
- compare target blobs with this lane's last-read SHAs;
- abort/rebase this patch if another agent touched reserved production/test paths;
- never force-update `main`;
- preserve unrelated concurrent commits.

This lane intentionally excludes active work around Bulk Edit, Build3D, native Tables, Quantity Settings/Rules, Grid annotation, Level V25 tests, uninstall/release mechanics, Ribbon/UI, Curtain, rebar and Direct Draw.

## 10. Validation and evidence policy

Remote/source evidence allowed:

- exact source diff review;
- deterministic Core smoke code and module registration;
- source preflight registration/contract;
- Core build/smoke execution only if a real runnable checkout is available.

Not allowed to claim from this lane:

- BricsCAD V25 NETLOAD/DemandLoad;
- native CAD behavior;
- Windows installer/update qualification;
- private/customer DWG qualification;
- `LOCAL_PASS` without local exact-SHA evidence.

No GitHub Actions will be dispatched by this lane because repository policy keeps them manual-only.

## 11. Commit sequence

1. Claim commit, then this plan commit, before production code.
2. Re-fetch/reconcile current `main`.
3. Implementation commit — session recovery integration + smoke + focused preflight.
4. Review latest concurrent `main`; use non-force writes only.
5. Claim closeout commit — mark claim COMPLETED and record implementation SHA/evidence.

If implementation uncovers a separate defect outside reserved scope, register a new non-overlapping claim first or hand it to `docs/LOCAL-AGENT-INBOX.md` when it is host/runtime-only.

## 12. Definition of done

This batch is complete when:

- `ProjectSession` can recover through the store fallback path;
- recovery provenance controls backup-preserving publication;
- good backup data is not replaced by the corrupt primary that triggered fallback;
- save/reload failures preserve session atomicity/provenance;
- focused regression/preflight coverage is committed;
- claim is closed with exact `main` SHA evidence;
- no unrelated active lane is overwritten;
- no unsupported runtime/release claim is made.
