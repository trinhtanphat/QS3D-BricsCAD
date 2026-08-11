# Work claim — bulk-edit object target null validation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bulk-edit-null-target`
- Registered: `2026-08-11T23:28:00+07:00`
- Baseline main SHA: `4c4eb7f7d1fd2041ef51bd9bcb7197289adb7fa0`
- Priority: P1 — complete the existing fail-closed low-level bulk-target contract for object-based callers.

## Confirmed defect

`BulkEditService.OwnedDistinct(...)` validates corrupt project entries, blank target ids and foreign object identities, but currently executes `if (element == null) continue;` for the caller-supplied object collection. A requested batch such as `[ownedElement, null]` is therefore accepted as a smaller batch and mutates the valid element instead of rejecting the incomplete target set.

The id-based overload already rejects blank/missing/duplicate targets atomically (`a053c4ac2e2d1ce958799b691fddecaabf14e17a`). Silently dropping a null object target leaves the parallel object-based API weaker and can report success after not applying the requested operation to every supplied target.

## Reserved scope

- `src/QS3D.Core/Services/BulkEditService.cs`
- `tests/QS3D.Core.SmokeTests/BulkEditCanonicalizationSmoke.cs`
- `scripts/preflight-bulk-edit-null-target.py` (new)
- this claim file for close-out

## Intended contract

- Caller-supplied object target collections reject a null entry before any semantic mutation.
- Existing same-object/case-insensitive deduplication, exact project-instance ownership, canonical property-key handling and semantic rollback behavior remain unchanged.
- `SetProperty(...)` and `MultiplyNumericProperty(...)` object overloads inherit the guard through `OwnedDistinct(...)`.
- Id-based bulk-edit behavior remains unchanged.

## Excluded scope

- No Workspace/selection UI changes, no Family assignment policy change, no ProjectElement/persistence/native DWG changes.
- No GitHub Actions dispatch and no BricsCAD V25/runtime PASS claim.

## Validation plan

Extend the already auto-registered bulk-edit Core smoke with an owned target plus a null object entry and assert exception, unchanged property/dirty state and unchanged `ChangeVersion`. Add a focused auto-discovered static gate requiring null rejection in `OwnedDistinct(...)` and forbidding the legacy silent skip. Re-fetch `main` and both existing blobs before writes, preserve concurrent winners and never force-push.

## Completion condition

Object-based bulk edits fail closed on incomplete null-containing target sets, focused regression source is on current `main`, this claim is closed with exact SHAs, and executable/native qualification is reported truthfully.
