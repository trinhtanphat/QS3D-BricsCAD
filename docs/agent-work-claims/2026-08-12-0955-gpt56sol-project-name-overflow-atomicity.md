# Work claim — Project name overflow atomicity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-project-name-overflow-atomicity-20260812-0955`
- Registered: `2026-08-12T09:55:00+07:00`
- Completed: `2026-08-12T09:57:00+07:00`
- Baseline main SHA: `f490a146cfc0e1889da993edc03f0cd1acd18d19`
- Claim commit: `1f515d373b5211c9d65ef94ca0cbb8e6c892fa4c`
- Source fix commit: `0255a53315e1b624fc88b6b6a0f48082c51bfc81`
- Regression commit: `46105e82c54012468625a1cf5155e14cdc758678`

## Confirmed defect

PR #722 / merge `503edb6e2bdee487c5d45f3849fa4e5ad5582f6f` made public `ProjectState.Name` changes participate in persistence freshness, but the setter assigned `_name = next` before calling `Touch()`. When `ChangeVersion == long.MaxValue`, `Touch()` threw `OverflowException` after `_name` had already changed. The mutation therefore violated all-or-nothing semantics: the visible project name changed while `ChangeVersion` and `UpdatedUtc` remained at the pre-call persistence state.

## Completed scope

`ProjectState.Name` now computes the checked next change version before mutating any project field. Real renames then commit the canonical name, UTC timestamp and new version without another failure point. Canonical-equivalent assignments still return before revision preflight, and blank input still fails before mutation.

## Regression coverage

`ProjectNameFreshnessSmoke` now loads a canonical QSDB fixture with `changeVersion=long.MaxValue`, verifies canonical-equivalent assignment remains a no-op, then verifies a real rename throws `OverflowException` while preserving Name, ChangeVersion and UpdatedUtc exactly. Existing successful rename, persistence-stamp, invalid-input and snapshot rollback coverage remains intact.

## Validation actually performed

- Re-read integrated `ProjectState.Name` from current `main` and confirmed `checked(ChangeVersion + 1L)` occurs before `_name`, `UpdatedUtc` or `ChangeVersion` mutation.
- Re-read the focused regression and confirmed the `long.MaxValue` QSDB fixture plus no-mutation assertions.
- Verified regression commit `46105e82c54012468625a1cf5155e14cdc758678` is an ancestor of main snapshot `57c21d477cd8e5b47b30a95cfbc07566a9b2ce9c` with `behind_by: 0`; intervening commits touched unrelated claim/test surfaces.
- No GitHub Actions were dispatched. No local .NET build/smoke execution or BricsCAD V25/V26 runtime PASS is claimed.

## Excluded scope honored

No other ProjectState setters, persistence policy, snapshot implementation, CAD adapter, or runtime behavior was redesigned.

## Completion

Completed. Project name freshness now preserves all-or-nothing semantics when revision increment overflows.