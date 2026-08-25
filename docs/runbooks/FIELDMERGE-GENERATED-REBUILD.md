# FieldMerge generated-output rebuild

Issue: #3800  
Lane-Key: `issue-3800`
Status: `SOURCE_READY / PENDING_LOCAL`

## Purpose

A reviewed `QS3DINTERCHANGEFIELDMERGE` import can invalidate generated dependents. The post-import path rebuilds only the supported generated outputs for the exact reviewed affected closure; it never widens into project-wide regeneration.

## Source contract

`InterchangeFieldMergeGeneratedRebuildPlan` is the immutable boundary object for the rebuild phase. The source-ready automatic set is intentionally limited to:

- `NativeGeometry` — supported `GeneratedSolidHandle` owners only;
- `Quantity` — dirty-subset semantic regeneration for the reviewed affected ids.

`Workbook` and `Trace` remain reserved/explicit external outputs. Asking the plan to include either one, or any unknown flag, fails closed before mutation. IDs use the repository's canonical trimmed string identity, dedupe case-insensitively and sort deterministically; an empty id/output set is a no-op.

Before destructive invalidation, `InterchangeFieldMergeGeneratedRebuildExecutor.Prepare` verifies that every requested element remains inside the reviewed closure and refuses unsupported generated owner slots, unsupported structural categories, ambiguous/missing CAD sources, duplicate source ownership, and Slab states that require specialized slab-opening peer replay. FieldMerge therefore does not silently replace specialized physical-cut/rebar/curtain/grid/tag workflows.

The native flow is:

1. rebind the canonical project and affected ids under the document lock;
2. recheck generated-cleanup coverage and backing-store authority;
3. construct and preflight the bounded NativeGeometry + Quantity rebuild manifest while retiring ownership metadata still exists;
4. start one outer FieldMerge semantic/native Undo transition and CAD transaction;
5. prepare native invalidation;
6. recheck backing-store authority and execute the exact reviewed Core authorization/apply;
7. clear retiring generated-owner metadata;
8. rebuild supported native solids through the production structural builder and regenerate only the affected semantic/quantity subset;
9. recheck backing-store authority, stage the after-state, then commit the outer CAD transaction.

Nested production-builder transactions remain inside the active outer database transaction. Under the database transaction model, aborting the outer transaction rolls back changes made by successfully ended nested transactions as well. A failure before outer commit also restores the captured `ProjectState` snapshot, so semantic ownership cannot claim a rebuild that the CAD transaction did not retain.

## Hosted validation

Exact-head Shared CI must pass repository preflight, Core deterministic smoke, trusted V25 reference validation and V25 plugin compile before merge. Static preflight pins the ordering/rollback contract and the bounded rebuild fail-closed rules. Hosted success is source evidence only; it is not licensed BricsCAD runtime evidence and must never be reported as `LOCAL_PASS`.

## Local-agent handoff

After this PR is merged and the exact merged-main SHA is published, the local agent should only:

1. pull/checkout that exact merged-main SHA with a clean worktree;
2. build the normal candidate and prove plugin/Core/PDB identity against that SHA;
3. run the licensed V25 FieldMerge qualification against repository-generated/disposable data;
4. exercise success, no-op/no-affected-output behavior, stale reviewed authorization refusal, unsupported/specialized owner-slot refusal, rebuild-failure rollback, native Undo/Redo, save/cold-reopen and document isolation as applicable;
5. publish only sanitized runtime evidence.

The local lane must not patch production source. Any reproducible runtime product defect becomes a separate remote/source-fix issue/PR, after which the local agent pulls the new exact merged SHA and reruns only the affected boundary before resuming the matrix.
