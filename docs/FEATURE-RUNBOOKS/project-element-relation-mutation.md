# ProjectElement relation mutation lifecycle

Issue: #6087  
Lane-Key: `issue-6087`  
Runtime: `REMOTE_SAFE` deterministic managed Core/Domain/Persistence.

## Problem

`ProjectElement.SourceHandles` and `ProjectElement.DependsOn` are persisted and snapshotted semantic relation state. A raw mutable `List<string>` lets callers change that state without the element relation lifecycle: no `ElementDirtyFlags.Relations`, no `UpdatedUtc` ownership, and no generated-output invalidation boundary.

## Contract

- Preserve the public `IList<string>` API.
- Effective semantic Add/Insert/index replacement/Remove/RemoveAt/Clear routes through the owning `ProjectElement` relation dirty boundary.
- Removing a missing value, clearing an empty list, and assigning the identical value are no-ops.
- Relation values are non-empty, already canonical/unpadded XML-safe text with no control characters.
- Case-insensitive duplicates are rejected before mutation because QSDB persistence already rejects duplicate relation identities.
- Persistence and snapshot hydration reconstruct the backing relation values through explicit internal bypass methods, then restore persisted Dirty/timestamp state; they do not impersonate user edits.
- Relation ordering remains insertion/order preserving.

## Validation

`tests/QS3D.Core.SmokeTests/ProjectElementRelationMutationSmoke.cs` is the deterministic behavior regression. `scripts/preflight-project-element-relation-mutation.py` is the auto-discovered source/fixture guard.

Before merge: reconcile latest protected main without force push, run fresh exact-head protected `preflight` + `core`, resolve review findings, and merge only after both required jobs are terminal SUCCESS.
