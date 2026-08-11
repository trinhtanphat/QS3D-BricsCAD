# Work claim — bulk-edit object target null validation

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-bulk-edit-null-target`
- Registered: `2026-08-11T23:28:00+07:00`
- Completed: `2026-08-11T23:32:00+07:00`
- Baseline main SHA: `4c4eb7f7d1fd2041ef51bd9bcb7197289adb7fa0`
- Reservation commit: `1c1b2e358ebaa337d8e89b85b86eb8580e9898e7`
- Priority: P1 — complete the existing fail-closed low-level bulk-target contract for object-based callers.

## Defect fixed

`BulkEditService.OwnedDistinct(...)` already validated corrupt project entries, blank target ids and foreign object identities, but silently skipped a caller-supplied `null` target. A requested object batch such as `[ownedElement, null]` could therefore be accepted as a smaller batch and mutate the valid element instead of rejecting the incomplete request.

The object target boundary now rejects that null entry before the caller can reach the mutation executor. This aligns the object-based overloads with the existing fail-closed id-based behavior without changing normal same-object deduplication or ownership semantics.

## Published commits

- `0fa9b09c0815e58a63d0102c9e5cf0ead2a0184e` — `fix(core): reject null bulk edit object targets`.
- `6fd7061df7304458d57924ff607202067a0026a8` — `test(core): guard null bulk edit object targets`; extends the existing auto-registered bulk smoke and proves SetProperty/Multiply reject `[owned, null]` without changing the property, dirty flags or `ChangeVersion`.
- `9a02aedfce071796c474b670f08364eda7837997` — `test(core): guard null bulk edit target rejection`; adds an auto-discovered static gate that requires the fail-closed guard, rejects the legacy silent skip and pins both object overloads to `OwnedDistinct(...)`.

## Preserved contract

- Existing same-object/case-insensitive deduplication, exact project-instance ownership, canonical property-key handling and semantic rollback behavior are unchanged.
- `SetProperty(...)` and `MultiplyNumericProperty(...)` object overloads inherit the guard through `OwnedDistinct(...)`.
- Id-based bulk-edit behavior and Family assignment policy are unchanged.
- No Workspace/selection UI, ProjectElement, persistence or native DWG behavior was modified.

## Validation notes

The source and regression diffs were inspected after publication. One preflight-file create attempt received HTTP 409 because `main` advanced concurrently; current `main` was re-fetched and the create was retried without force-push or overwriting concurrent work. The source/static gate is committed but was not executed in a full repository checkout in this connector-only lane. No GitHub Actions were dispatched, and no Core executable/BricsCAD V25 runtime PASS is claimed.

## Completion condition

Satisfied for the source/static contract: object-based bulk edits fail closed on null-containing target sets and focused regression coverage is on `main`. Executable/native qualification remains a separate environment gate.
