# BricsCAD V25 Family Manager exact-project assignment affinity

Issue: #6354
Lane: C03 — BricsCAD V25 UI / Workspace / Modeless / Authoring
Runtime class: REMOTE_SAFE for static/deterministic guard and admitted-reference V25 compile; LOCAL_ONLY for licensed BricsCAD modeless replacement timing.

## Defect

`FamilyManagerWindow.OnAssignClick` previews the selected family and semantic selection from a `ProjectState`, then reacquires mutation context before assigning. A project coordinator replacement can produce a different `ProjectState` instance carrying the same `ProjectId` between preview and mutation. ProjectId-only comparison therefore accepts stale modeless intent and can apply it to a replacement project instance.

## Required invariant

After `ExistingProjectMutationContext.Require(...)` returns and before family or semantic-selection re-resolution, assignment must reject unless the mutation project is the exact `_boundProject` instance and the exact `previewProject` instance. ProjectId comparison remains as a semantic defense but is not a substitute for reference identity.

The existing active-document fence, family re-resolution, selection-id equality check, `ExecuteAtomic` project rollback, and post-commit UI-warning behavior remain unchanged.

## Deterministic verification

Run:

```text
python scripts/preflight-v25-family-manager-exact-project-assign-affinity.py
```

The guard must fail on the pre-fix source and pass only when both exact-instance fences occur immediately after mutation-context reacquisition and before family/selection resolution or mutation.

Then run the repository-required source/preflight suite and the admitted-reference BricsCAD V25 compile on the exact PR head. Remote green proves only deterministic/static/compile contracts; it does not prove licensed native timing behavior.

## LOCAL_ONLY verification

With licensed BricsCAD V25, open Family Manager for project instance A, select a family and semantic elements, replace the coordinator state with a distinct project instance B retaining the same ProjectId between preview and mutation reacquisition, then invoke assignment. Expected result: the operation is rejected before family/element resolution and no mutation is committed to B. Re-Refresh/rebind is required before retrying.

Do not record `LOCAL_PASS` unless this scenario is executed in a real licensed BricsCAD V25 runtime.