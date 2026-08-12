# Work claim — Active Family whitespace repair

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-active-family-whitespace-repair`
- Registered: `2026-08-11T22:58:00+07:00`
- Completed: `2026-08-11T23:01:00+07:00`
- Baseline main SHA observed: `227c7259b35879ab76c3b09f4129209560156d4f`
- Priority: P1 — `ProjectFamilyActivationService.ClearIfMissing()` previously returned early for whitespace-only `ActiveFamilyId`. `GetActive()` treated that value as no active Family, but the repair path left stale metadata in place.

## Implemented

- `528cbdbfb03ee25c4d17929be0f9e2fa0daa03a5` — registered this lane before implementation.
- `ab3117366977c0de07a8d6464a31609f6d3f492e` — `ClearIfMissing()` now distinguishes an absent key from a present whitespace/missing identity: absent remains no-op; valid existing Family remains no-op; whitespace-only or missing nonblank identities are removed after one `project.Touch()`.
- `22d4cad70b27e98ff305d0256972d581c57349c9` — added deterministic smoke coverage for missing-key no-op, whitespace cleanup, valid padded/case-varied identity preservation, and missing nonblank identity cleanup.
- `1824f8f9dfab906a7e018b58cffc4856b14d2fc8` — module-registers the focused smoke.
- `797f86f3dc734b3d9be059c514cbae1fbb30a286` — added `scripts/preflight-project-family-activation-whitespace-repair.py`, requiring the explicit absent-key/valid-existing split and rejecting the previous `missing-or-whitespace => return` condition.

## Preserved contracts

- No Qsdb schema/loader validation, Family UI/Workspace, active-Family deletion/reference logic, CAD/Ribbon/updater/release behavior changed.
- Valid padded/case-varied identity is deliberately preserved by `ClearIfMissing()` when it still resolves; explicit `SetActive()` remains the canonicalization mutation path.
- Missing-key behavior remains observational and does not Touch the project.

## Validation

- Re-fetched current source before implementation and confirmed the exact prior early-return defect.
- Compared `797f86f3dc734b3d9be059c514cbae1fbb30a286` to current `main`; it is an ancestor and later concurrent commits did not touch this lane's source/test/preflight files.
- No GitHub Actions workflow was dispatched and no BricsCAD V25 runtime PASS is claimed; this is deterministic Core metadata repair behavior.

## LOCAL_ONLY disposition

- None added.

## Completion evidence

`GetActive()` and `ClearIfMissing()` now agree that whitespace-only `ActiveFamilyId` means no active Family, and the repair path actually removes the stale metadata while preserving valid existing identity. Final implementation/preflight tip for this lane: `797f86f3dc734b3d9be059c514cbae1fbb30a286`.
