# FieldMerge generated-output rebuild

Issue: #3800  
Lane-Key: `issue-3800`  
Status: `SOURCE_READY / PENDING_LOCAL`

## Purpose

A reviewed `QS3DINTERCHANGEFIELDMERGE` import can invalidate generated dependents. The post-import path automatically rebuilds only the supported generated outputs for the exact reviewed affected closure; it never widens into project-wide regeneration.

## Source contract

`InterchangeFieldMergeGeneratedRebuildPlan` is the immutable boundary object for the rebuild phase. The automatic set is intentionally limited to:

- `NativeGeometry` — supported `GeneratedSolidHandle` owners only;
- `Quantity` — dirty-subset semantic/quantity regeneration for the reviewed affected ids.

`Workbook` and `Trace` remain reserved explicit external outputs. Asking the plan to include either one, or any unknown output flag, fails closed before mutation.

Before destructive invalidation, `InterchangeFieldMergeGeneratedRebuildExecutor.Prepare` verifies that every requested element remains inside the reviewed closure and refuses unsupported generated owner slots, unsupported structural categories, ambiguous/missing CAD sources, duplicate source ownership, and Slab states that require specialized slab-opening peer replay. FieldMerge therefore does not silently replace specialized physical-cut, rebar, curtain, grid, or semantic-tag workflows.

The native flow is:

1. rebind the canonical project and reviewed affected ids under the document lock;
2. recheck generated-cleanup coverage and backing-store authority;
3. construct and preflight the bounded `NativeGeometry + Quantity` rebuild manifest while retiring ownership metadata and reviewed source handles still exist;
4. start one outer FieldMerge semantic/native Undo transition and CAD transaction;
5. prepare native invalidation;
6. recheck backing-store authority and execute the exact reviewed Core authorization/apply;
7. clear retiring generated-owner metadata;
8. rebuild supported native solids through the production structural builder and regenerate only the affected semantic/quantity subset;
9. recheck backing-store authority, stage the after-state, then commit the outer CAD transaction and confirm the single Undo transition.

Nested production-builder transactions remain inside the active outer database transaction. A failure before outer CAD commit aborts CAD changes and restores the captured `ProjectState` snapshot, so semantic ownership cannot claim a rebuild that CAD did not retain.

## Hosted validation

The source-ready carrier must pass exact-head Shared CI: repository preflight, Core deterministic smoke, trusted V25 reference validation, and V25 plugin build. Hosted success is source evidence only; it is not licensed BricsCAD runtime evidence and must never be reported as `LOCAL_PASS`.

## Local-agent handoff

After the handoff PR is merged, the local agent should only:

1. `git fetch origin`;
2. `git checkout --detach <exact-merged-main-sha>`;
3. verify `git status --short` is empty;
4. build the normal candidate and prove plugin/Core/PDB identity against that exact SHA;
5. run the licensed V25 FieldMerge matrix against repository-generated/disposable inputs;
6. publish only sanitized runtime evidence.

The licensed matrix must cover supported success, no-op/no-affected-output behavior, stale reviewed authorization refusal, unsupported/specialized owner-slot refusal, rebuild-failure rollback, native Undo/Redo, save/cold-reopen, and document isolation.

The local lane must **not** patch production source. Any reproducible runtime product defect returns to a separate remote/source-fix issue/PR. After that source fix is merged, the local agent pulls the new exact merged SHA and reruns only the affected boundary before resuming the matrix.
